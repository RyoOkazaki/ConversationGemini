using UnityEngine;

public class GeminiAvatarController : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private AudioSource audioSource;

    [SerializeField] private SkinnedMeshRenderer faceRenderer;
    [SerializeField] private Animator animator;

    [Header("Lip Sync Settings")] [SerializeField]
    private string lipBlendShapeName = "A"; // "あ" の口

    [SerializeField] private float sensitivity = 100f;
    [SerializeField] private float smoothing = 0.5f;

    [Header("Animation Settings")] [SerializeField]
    private int speakAnimationCount = 8;

    [SerializeField] private int idleAnimationIndex = 1;
    [SerializeField] private string animParamName = "animBaseInt";

    private int blendShapeIndex;
    private float currentWeight = 0f;
    private bool isLipSyncActive = false;

    private void Start()
    {
        if (faceRenderer == null || faceRenderer.sharedMesh == null)
        {
            Debug.LogError("FaceRenderer or Mesh is missing.");
            enabled = false;
            return;
        }

        blendShapeIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(lipBlendShapeName);
        if (blendShapeIndex < 0)
        {
            Debug.LogError($"BlendShape '{lipBlendShapeName}' not found.");
            enabled = false;
            return;
        }

        SetIdleAnimation();
    }

    public void PlayLipSync()
    {
        isLipSyncActive = true;
        PlayRandomSpeakingAnimation();
    }

    public void StopLipSync()
    {
        isLipSyncActive = false;
        faceRenderer.SetBlendShapeWeight(blendShapeIndex, 0f);
        SetIdleAnimation();
    }

    private void Update()
    {
        if (isLipSyncActive)
        {
            UpdateLipSync();
        }
    }

    private void UpdateLipSync()
    {
        float[] samples = new float[256];
        audioSource.GetOutputData(samples, 0);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }

        float volume = sum / samples.Length;
        float targetWeight = Mathf.Clamp01(volume * sensitivity) * 100f;

        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * smoothing);
        faceRenderer.SetBlendShapeWeight(blendShapeIndex, currentWeight);
    }

    private void SetIdleAnimation()
    {
        if (animator != null)
        {
            animator.SetInteger(animParamName, idleAnimationIndex);
        }
    }

    private void PlayRandomSpeakingAnimation()
    {
        if (animator != null && speakAnimationCount > 0)
        {
            int randomIndex = Random.Range(2, 2 + speakAnimationCount); // 2〜(2 + N -1)
            animator.SetInteger(animParamName, randomIndex);
        }
    }
}