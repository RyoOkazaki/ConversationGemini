using System;
using System.Collections;
using System.Collections.Generic;
using Kyub.EmojiSearch.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Oculus.Interaction;
using PassthroughCameraSamples;

public class VoiceGeminiController : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private VoiceRecorder recorder;

    [SerializeField] private GeminiSender geminiSender;
    [SerializeField] private TextToSpeech speaker;
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    [SerializeField] private SelectorUnityEventWrapper handPoseEvent;

    [Header("UI")] [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_EmojiTextUGUI conversationText;
    [SerializeField] private float scrollDuration = 0.3f; // スクロールにかける時間（秒）

    private bool isWorking = false;
    private List<ConversationEntry> conversationHistory = new();
    private Coroutine scrollCoroutine;

    private void Start()
    {
        handPoseEvent.WhenSelected.AddListener(OnGestureTriggered);
        InitializeConversationText();
    }

    private void OnDestroy()
    {
        handPoseEvent.WhenSelected.RemoveAllListeners();
    }

    private void OnApplicationQuit()
    {
        conversationHistory.Clear();
    }

    private void OnGestureTriggered()
    {
        if (!isWorking)
        {
            StartCoroutine(ConversationFlow());
        }
    }

    private IEnumerator ConversationFlow()
    {
        isWorking = true;

        yield return StartCoroutine(recorder.StartAutoStopRecording());

        string userInput = null;
        yield return StartCoroutine(ConvertVoiceToText(result => userInput = result));

        if (string.IsNullOrEmpty(userInput))
        {
            speaker.Speak("音声の認識に失敗しちゃった★");
            isWorking = false;
            yield break;
        }

        AddToConversation("user", userInput);
        AppendConversationUI("あなた", userInput);
        string base64Image = CaptureCameraImageAsBase64();
        yield return StartCoroutine(SendToGemini(userInput, base64Image));
        isWorking = false;
    }

    private IEnumerator ConvertVoiceToText(Action<string> onTextReady)
    {
        yield return recorder.ConvertLastRecordingToText(
            result =>
            {
                if (string.IsNullOrEmpty(result))
                {
                    speaker.Speak("レスポンスが空だよ★");
                    isWorking = false;
                    return;
                }

                onTextReady?.Invoke(result);
            });
    }

    private IEnumerator SendToGemini(string question, string base64Image)
    {
        yield return geminiSender.SendImageAndText(
            base64Image,
            question,
            response =>
            {
                AddToConversation("model", response);
                AppendConversationUI("Geminiちゃん", response);
                speaker.Speak(response);
            },
            conversationHistory
        );
    }

    private void AddToConversation(string role, string text)
    {
        conversationHistory.Add(new ConversationEntry(role, text));
    }

    private void AppendConversationUI(string speakerName, string message)
    {
        conversationText.text += $"\n<color=yellow>{speakerName}</color>\n{message}\n";
        StartSmoothScrollToBottom();
    }

    private void StartSmoothScrollToBottom()
    {
        // すでにスクロール中なら止める
        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);

        scrollCoroutine = StartCoroutine(SmoothScrollToBottom());
    }

    private IEnumerator SmoothScrollToBottom()
    {
        yield return null; // レイアウト更新のため1フレーム待つ
        Canvas.ForceUpdateCanvases();

        float startPos = scrollRect.verticalNormalizedPosition;
        float targetPos = 0f; // 一番下
        float time = 0f;

        while (time < scrollDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / scrollDuration);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, targetPos, t);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = 0f;
    }


    private void InitializeConversationText()
    {
        if (conversationText != null)
        {
            conversationText.text = "左手の指先を上に向けて、親指と人差し指をつまむと録音開始できるよ★\n";
        }
    }

    private string CaptureCameraImageAsBase64()
    {
        var webCamTexture = webCamTextureManager.WebCamTexture;
        if (webCamTexture == null)
        {
            speaker.Speak("WebCamTextureがないよ★");
            isWorking = false;
            return null;
        }

        Texture2D texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
        texture.SetPixels(webCamTexture.GetPixels());
        texture.Apply();

        byte[] png = texture.EncodeToPNG();
        Destroy(texture);

        return Convert.ToBase64String(png);
    }
}