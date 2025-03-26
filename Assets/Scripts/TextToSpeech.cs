using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using System.IO;
using Kyub.EmojiSearch.UI;

[RequireComponent(typeof(AudioSource))]
public class TextToSpeech : MonoBehaviour
{
    [SerializeField] private TMP_EmojiTextUGUI statusText;
    [SerializeField] private GeminiAvatarController geminiAvatar; // 👄 リップシンク連携用（任意）
    [SerializeField] private string voiceLanguage = "ja-JP";
    [SerializeField] private string voiceType = "ja-JP-Wavenet-A";
    [SerializeField] private float voicePitch = 12.0f;
    [SerializeField] private float speakSpeed = 1.1f;
    private AudioSource audioSource;
    private string currentAudioPath;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        statusText.text = "";
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            LogStatus("テキストが空です。読み上げをスキップします。");
            return;
        }

        StartCoroutine(SendToGoogleTTS(CleanForTTS(text)));
    }

    private IEnumerator SendToGoogleTTS(string text)
    {
        string json = BuildTTSRequestJson(text);
        UnityWebRequest request = CreateTTSRequest(json);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            LogStatus($"TTS リクエスト失敗: {request.error}");
            yield break;
        }

        string base64Audio = ExtractAudioContent(request.downloadHandler.text);
        if (string.IsNullOrEmpty(base64Audio))
        {
            LogStatus("TTS から音声データが返ってきませんでした。");
            yield break;
        }

        byte[] audioBytes = Convert.FromBase64String(base64Audio);
        if (audioBytes.Length < 100)
        {
            LogStatus("音声データが異常に短い、または空です。");
            yield break;
        }

        currentAudioPath = AudioFileManager.Instance.SaveAudioFile(audioBytes, "mp3");
        yield return PlayMp3(currentAudioPath);
    }

    private IEnumerator PlayMp3(string path)
    {
        if (!File.Exists(path))
        {
            LogStatus("MP3ファイルが存在しません: " + path);
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                LogStatus("MP3読み込み失敗: " + www.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            if (clip == null)
            {
                LogStatus("AudioClipがnullです。音声の再生に失敗しました。");
                yield break;
            }

            audioSource.clip = clip;

            // 🎤 リップシンクON
            geminiAvatar.PlayLipSync();

            audioSource.Play();
            yield return new WaitUntil(() => !audioSource.isPlaying);

            // 🎤 リップシンクOFF
            geminiAvatar.StopLipSync();

            AudioFileManager.Instance.TryDeleteFile(path);
            currentAudioPath = null;
        }
    }

    private string BuildTTSRequestJson(string text)
    {
        return $@"
{{
  ""input"": {{ ""text"": ""{EscapeJson(text)}"" }},
  ""voice"": {{
    ""languageCode"": ""{voiceLanguage}"",
    ""name"": ""{voiceType}""
  }},
  ""audioConfig"": {{
    ""audioEncoding"": ""MP3"",
    ""pitch"": {voicePitch},
    ""speakingRate"": {speakSpeed}
  }}
}}";
    }

    private UnityWebRequest CreateTTSRequest(string json)
    {
        var request = new UnityWebRequest(GeminiAPIAccess.Instance.TextToSpeechURL, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    private string ExtractAudioContent(string json)
    {
        const string tag = "\"audioContent\": \"";
        int start = json.IndexOf(tag);
        if (start == -1) return null;
        start += tag.Length;
        int end = json.IndexOf("\"", start);
        return end == -1 ? null : json.Substring(start, end - start);
    }

    private string CleanForTTS(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        var regex = new System.Text.RegularExpressions.Regex(@"[\p{Cs}\p{So}\p{Sk}\p{Sm}]+", System.Text.RegularExpressions.RegexOptions.Compiled);
        string cleaned = regex.Replace(input, "");

        cleaned = cleaned.Replace("♪", "。").Replace("♡", "。").Replace("💖", "。").Replace("!", "。").Replace("！", "。").Replace("?", "。").Replace("？", "。").Replace("ｗ", "。").Replace("w", "。").Replace("…", "。").Replace("*", "").Replace(":", "。").Replace(";", "。");

        cleaned = cleaned.Trim();
        if (!cleaned.EndsWith("。")) cleaned += "。";

        return cleaned;
    }

    private string EscapeJson(string input)
    {
        return input.Replace("\"", "\\\"").Replace("\n", "").Replace("\r", "");
    }

    private void LogStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}