using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Kyub.EmojiSearch.UI;

public class GeminiSender : MonoBehaviour
{
    [SerializeField] private TMP_EmojiTextUGUI statusText;

    public IEnumerator SendImageAndText(string base64Image, string userText, Action<string> onResponse, List<ConversationEntry> history = null)
    {
        string json = BuildRequestBody(base64Image, userText, history);
        UnityWebRequest request = CreateGeminiRequest(json);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            LogStatus("Gemini Error: " + request.error);
            onResponse?.Invoke(null);
            yield break;
        }

        string rawJson = request.downloadHandler.text;
        string extracted = ExtractReply(rawJson);

        onResponse?.Invoke(extracted);
        LogStatus("返答するね★");
    }

    private string BuildRequestBody(string base64Image, string userText, List<ConversationEntry> history)
    {
        // 💅 ギャル人格のシステムプロンプト（先頭固定）
        string persona = "あなたは明るく元気なギャルAIです！語尾に「〜じゃん」「〜だし」「〜ね〜」をよく使って、テンション高めに親しみやすく話してください♡";

        var contents = new List<string>();

        // 🧠 ギャル設定を最初に追加
        contents.Add(
            $@"
        {{
          ""role"": ""user"",
          ""parts"": [{{ ""text"": ""{EscapeJson(persona)}"" }}]
        }}");

        // 📝 会話履歴（任意）
        if (history != null)
        {
            foreach (var entry in history)
            {
                contents.Add(
                    $@"
                {{
                  ""role"": ""{entry.role}"",
                  ""parts"": [{{ ""text"": ""{EscapeJson(entry.text)}"" }}]
                }}");
            }
        }

        // 📷 画像＋現在の質問（直近の発話）
        contents.Add(
            $@"
        {{
          ""role"": ""user"",
          ""parts"": [
            {{
              ""inline_data"": {{
                ""mime_type"": ""image/png"",
                ""data"": ""{base64Image}""
              }}
            }},
            {{
              ""text"": ""{EscapeJson(userText)}""
            }}
          ]
        }}");

        return $"{{\n  \"contents\": [\n{string.Join(",\n", contents)}\n  ]\n}}";
    }


    private UnityWebRequest CreateGeminiRequest(string json)
    {
        var request = new UnityWebRequest(GeminiAPIAccess.Instance.GeminiAPIURL, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        return request;
    }

    private string ExtractReply(string json)
    {
        try
        {
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(json);
            return response?.candidates?[0]?.content?.parts?[0]?.text ?? "(テキストなし)";
        }
        catch (Exception ex)
        {
            LogStatus("JSONパース失敗: " + ex.Message);
            return "(パースエラー)";
        }
    }

    private string EscapeJson(string input)
    {
        return input.Replace("\"", "\\\"").Replace("\n", "").Replace("\r", "");
    }

    private void LogStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}

#region Gemini JSON構造体

[Serializable] public class GeminiResponse
{
    public Candidate[] candidates;
}

[Serializable] public class Candidate
{
    public Content content;
}

[Serializable] public class Content
{
    public Part[] parts;
}

[Serializable] public class Part
{
    public string text;
}

#endregion