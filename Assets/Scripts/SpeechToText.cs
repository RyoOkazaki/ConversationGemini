using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;
using System.Text;
using Kyub.EmojiSearch.UI;

public class SpeechToText : MonoBehaviour
{
    [SerializeField] private TMP_EmojiTextUGUI statusText;

    private const string TranscriptTag = "\"transcript\": \"";
    private const int SampleRate = 16000;
    private const string LanguageCode = "ja-JP";

    public IEnumerator RecognizeSpeech(string wavFilePath, Action<string> onResult)
    {
        if (!File.Exists(wavFilePath))
        {
            LogStatus($"STT: WAVファイルが見つかりません: {wavFilePath}");
            onResult?.Invoke(null);
            yield break;
        }

        string json = BuildRequestJson(wavFilePath);
        UnityWebRequest request = CreateSTTRequest(json);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            LogStatus($"STT: リクエスト失敗: {request.error}");
            onResult?.Invoke(null);
            yield break;
        }

        string transcript = ExtractTranscript(request.downloadHandler.text);
        if (string.IsNullOrEmpty(transcript))
        {
            LogStatus("STT: 音声の認識結果が空でした。");
        }

        onResult?.Invoke(transcript);
    }

    private string BuildRequestJson(string wavFilePath)
    {
        byte[] audioBytes = File.ReadAllBytes(wavFilePath);
        string base64Audio = Convert.ToBase64String(audioBytes);

        return $@"
        {{
          ""config"": {{
            ""encoding"": ""LINEAR16"",
            ""sampleRateHertz"": {SampleRate},
            ""languageCode"": ""{LanguageCode}""
          }},
          ""audio"": {{
            ""content"": ""{base64Audio}""
          }}
        }}";
    }

    private UnityWebRequest CreateSTTRequest(string json)
    {
        UnityWebRequest request = new UnityWebRequest(GeminiAPIAccess.Instance.SpeechToTextURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    private string ExtractTranscript(string json)
    {
        int start = json.IndexOf(TranscriptTag);
        if (start == -1) return null;

        start += TranscriptTag.Length;
        int end = json.IndexOf("\"", start);
        if (end == -1) return null;

        return json.Substring(start, end - start);
    }

    private void LogStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}