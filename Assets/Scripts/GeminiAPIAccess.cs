using UnityEngine;

public class GeminiAPIAccess : MonoBehaviour
{
    private static GeminiAPIAccess _instance;

    public static GeminiAPIAccess Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("GeminiAPIAccess");
                _instance = go.AddComponent<GeminiAPIAccess>();
                DontDestroyOnLoad(go);
            }

            return _instance;
        }
    }
    
    [SerializeField] private string geminiAPIKey = "";
    [SerializeField] private string modelVersion = "gemini-2.0-flash";
    
    public string GeminiAPIURL => $"https://generativelanguage.googleapis.com/v1beta/models/{modelVersion}:generateContent?key={geminiAPIKey}";
    public string TextToSpeechURL => $"https://texttospeech.googleapis.com/v1/text:synthesize?key={geminiAPIKey}";
    public string SpeechToTextURL => $"https://speech.googleapis.com/v1/speech:recognize?key={geminiAPIKey}";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}