using UnityEngine;
using System.IO;
using System;
using System.Collections;
using Kyub.EmojiSearch.UI;

[RequireComponent(typeof(AudioSource))]
public class VoiceRecorder : MonoBehaviour
{
    [Header("Components")] [SerializeField]
    private AudioClip startRecordSE;

    [SerializeField] private AudioClip finishRecordSE;
    [SerializeField] private SpeechToText speechToText;
    [SerializeField] private TMP_EmojiTextUGUI statusText;

    private AudioSource audioSource;
    private AudioClip clip;
    private string micName;
    private bool isRecording = false;
    private string lastSavedPath;

    private const int MaxRecordingTime = 60;
    private const int SampleRate = 16000;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        statusText.text = "録音できるよ★";
    }

    public IEnumerator StartAutoStopRecording(float silenceDuration = 3f)
    {
        if (!InitializeMicrophone()) yield break;

        clip = Microphone.Start(micName, true, MaxRecordingTime, SampleRate);
        audioSource.PlayOneShot(startRecordSE);
        isRecording = true;

        Log($"録音開始！最長{MaxRecordingTime}秒録音できるよ！");

        float silentTime = 0f;
        const float checkInterval = 0.1f;
        const float volumeThreshold = 0.01f;

        while (isRecording)
        {
            float volume = GetCurrentMicVolume();
            silentTime = volume < volumeThreshold ? silentTime + checkInterval : 0f;

            if (silentTime >= silenceDuration)
                break;

            yield return new WaitForSeconds(checkInterval);
        }

        FinalizeRecordingAndSave();
    }

    private bool InitializeMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Log("マイクが検出されていません！");
            return false;
        }

        micName = Microphone.devices[0];
        Log($"使用マイク: {micName}");
        return true;
    }

    private float GetCurrentMicVolume()
    {
        if (clip == null) return 0f;

        int position = Microphone.GetPosition(micName) - 256;
        if (position < 0) return 0f;

        float[] samples = new float[256];
        clip.GetData(samples, position);
        float sum = 0f;
        foreach (var s in samples) sum += s * s;

        return Mathf.Sqrt(sum / samples.Length);
    }

    private void FinalizeRecordingAndSave()
    {
        if (!isRecording)
        {
            Log("録音されていません");
            return;
        }

        int recordedSamples = Microphone.GetPosition(micName);
        Microphone.End(micName);
        isRecording = false;

        if (clip == null || recordedSamples <= 0)
        {
            Log("AudioClip が無効のため保存できません");
            return;
        }

        clip = TrimClip(clip, recordedSamples);

        if (clip.length < 1f)
        {
            Log("録音時間が短すぎたため保存しませんでした。");
            return;
        }

        byte[] wav = WavUtility.FromAudioClip(clip, out _, true);

        if (wav.Length < 1000)
        {
            Log("WAVファイルが小さすぎて保存をスキップしました。");
            return;
        }

        lastSavedPath = AudioFileManager.Instance.SaveAudioFile(wav, "wav");
        audioSource.PlayOneShot(finishRecordSE);

        Log($"WAVファイル保存成功: {lastSavedPath}");
    }

    private AudioClip TrimClip(AudioClip source, int samples)
    {
        AudioClip trimmed = AudioClip.Create("Trimmed", samples, source.channels, source.frequency, false);
        float[] data = new float[samples * source.channels];
        source.GetData(data, 0);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    public IEnumerator ConvertLastRecordingToText(Action<string> onTextReady)
    {
        if (string.IsNullOrEmpty(lastSavedPath) || !File.Exists(lastSavedPath))
        {
            Log("録音ファイルが存在しません。STTに送信できません。");
            onTextReady?.Invoke(null);
            yield break;
        }

        Log("音声をテキストに変換中...");
        yield return speechToText.RecognizeSpeech(
            lastSavedPath, result =>
            {
                Log(string.IsNullOrEmpty(result) ? "音声認識に失敗しました（null または空）" : "音声認識成功！");
                onTextReady?.Invoke(result);
                AudioFileManager.Instance.TryDeleteFile(lastSavedPath);
                lastSavedPath = string.Empty;
            });
    }

    private void Log(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}