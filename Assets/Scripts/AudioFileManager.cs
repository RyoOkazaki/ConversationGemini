using UnityEngine;
using System;
using System.IO;

public class AudioFileManager : MonoBehaviour
{
    private static AudioFileManager _instance;

    public static AudioFileManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AudioFileManager");
                _instance = go.AddComponent<AudioFileManager>();
                DontDestroyOnLoad(go);
            }

            return _instance;
        }
    }

    private string RecordingFolder => Path.Combine(Application.persistentDataPath, "recordings");

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

    /// <summary>
    /// ファイル保存（wav/mp3）してフルパスを返す
    /// </summary>
    public string SaveAudioFile(byte[] data, string extension)
    {
        try
        {
            if (!Directory.Exists(RecordingFolder))
                Directory.CreateDirectory(RecordingFolder);

            string fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
            string fullPath = Path.Combine(RecordingFolder, fileName);
            File.WriteAllBytes(fullPath, data);

            Debug.Log($"ファイル保存成功: {fullPath}");
            return fullPath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ファイル保存失敗: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 単一ファイルを削除
    /// </summary>
    public void TryDeleteFile(string path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Resources.UnloadUnusedAssets();

                File.Delete(path);
                Debug.Log($"ファイル削除成功: {path}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ファイル削除失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// recording フォルダごと削除
    /// </summary>
    private void DeleteRecordingFolder()
    {
        if (!Directory.Exists(RecordingFolder))
        {
            Debug.Log("recording フォルダが存在しません。");
            return;
        }

        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Resources.UnloadUnusedAssets();

            Directory.Delete(RecordingFolder, true);
            Debug.Log("recording フォルダごと削除しました。");
        }
        catch (Exception ex)
        {
            Debug.LogError("recording フォルダ削除失敗: " + ex.Message);
        }
    }

    private void OnApplicationQuit()
    {
        DeleteRecordingFolder();
    }
}