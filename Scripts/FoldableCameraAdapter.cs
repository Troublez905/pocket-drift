using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FoldableCameraAdapter : MonoBehaviour
{
    [Header("Target Tracking")]
    public Transform targetCarVisual;
    public ArcadeDriftController targetDriftController;

    [Header("Isometric Framing & Offset")]
    public Vector3 baseCameraOffset = new Vector3(0f, 15f, -12.5f);
    public float followSmoothSpeed = 9f;
    public float lookAheadDistance = 4.2f;
    public float lookAheadSmoothSpeed = 5f;

    [Header("Dynamic Aspect Ratio Adaptation (PC 16:9 vs Pixel Fold 1:1)")]
    [Tooltip("FOV used on standard 16:9 widescreen PC or folded outer phone display")]
    public float widescreenBaseFOV = 44f;
    [Tooltip("Expanded FOV on Pixel 10 Pro Fold unfolded ~1:1 inner display so hairpin corners stay visible")]
    public float squareFoldUnfoldedFOV = 56f;
    public float boostFOVKick = 6.5f;

    private Camera cam;
    private Vector3 currentLookAhead;
    private float lastAspect;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (targetCarVisual == null) return;

        // 1. Compute velocity/drift look-ahead so player sees around tight tabletop hairpins
        Vector3 forwardDir = targetCarVisual.forward;
        float speedRatio = targetDriftController != null
            ? Mathf.Clamp01(targetDriftController.CurrentSpeed / targetDriftController.maxSpeed)
            : 0.5f;

        Vector3 desiredLookAhead = forwardDir * (lookAheadDistance * speedRatio);
        currentLookAhead = Vector3.Lerp(currentLookAhead, desiredLookAhead, Time.deltaTime * lookAheadSmoothSpeed);

        // 2. Smoothly follow target + look-ahead
        Vector3 desiredPos = targetCarVisual.position + currentLookAhead + baseCameraOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followSmoothSpeed);

        Quaternion desiredRot = Quaternion.LookRotation((targetCarVisual.position + currentLookAhead) - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * followSmoothSpeed);

        // 3. Dynamically adapt FOV for Pixel 10 Pro Fold unfolded (~1.0 - 1.25 aspect) vs 16:9 / 20:9
        float currentAspect = (float)Screen.width / Mathf.Max(1f, (float)Screen.height);
        // t = 1 when near-square (1.05), t = 0 when widescreen (1.77+)
        float foldBlend = Mathf.InverseLerp(1.77f, 1.08f, currentAspect);
        float targetFOV = Mathf.Lerp(widescreenBaseFOV, squareFoldUnfoldedFOV, foldBlend);

        if (targetDriftController != null && targetDriftController.IsBoosting)
        {
            targetFOV += boostFOVKick;
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 6f);
        lastAspect = currentAspect;
    }
}
