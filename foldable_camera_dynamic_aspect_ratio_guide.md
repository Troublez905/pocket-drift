# Pocket Drift: Overdrive — Foldable Camera & Dynamic Aspect Ratio System

This document provides the complete C# implementation for `FoldableCameraAdapter.cs` and the Unity camera rig setup. It solves one of the biggest challenges in cross-platform isometric racing: **maintaining fair horizontal track visibility while dynamically adapting to the Pixel 10 Pro Fold's unfolded near-square (`~1:1`) display, its folded `20:9` outer screen, and a `16:9` / `21:9` PC monitor.**

---

## 1. How the Camera Architecture Works

1. **Isometric Rig with Velocity + Drift Look-Ahead:**
   * Rather than locking the camera dead-center on the car (which makes it hard to see upcoming workbench hazards at high speeds), the camera smoothly projects a target point **ahead of the car's velocity vector** and **into the direction of an active drift**.
2. **Real-Time Aspect Ratio Detection (Fold/Unfold Event Handling):**
   * When a player unfolds their Pixel 10 Pro Fold mid-race, Android resizes the surface window (`Screen.width / Screen.height` shifts from `~2.22` down to `~1.03`).
   * Standard Unity cameras preserve *vertical* FOV, which severely crops the left and right sides of the track on a square screen!
   * `FoldableCameraAdapter.cs` automatically calculates horizontal frustum preservation—zooming the camera out or adjusting FOV smoothly when switching to the square inner screen so you never lose peripheral sightlines, while giving unfolded players bonus vertical visibility ahead and behind.
3. **Tilt-Shift Macro Depth of Field Hook:**
   * Includes distance scaling output so your URP Post-Processing Volume keeps the toy-scale tilt-shift blur centered cleanly on the car.

---

## 2. Complete C# Script: `FoldableCameraAdapter.cs`

Create a new C# script named `FoldableCameraAdapter.cs` and attach it directly to your **Main Camera** (or your Cinemachine Virtual Camera root):

```csharp
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FoldableCameraAdapter : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("Assign the VisualCarRoot transform here.")]
    public Transform targetCar;
    [Tooltip("Assign the detached Physics Sphere Rigidbody here for velocity look-ahead.")]
    public Rigidbody targetSphereRb;

    [Header("Isometric Rig Positioning")]
    [Tooltip("Pitch angle (X) and Yaw angle (Y) of the isometric view.")]
    public Vector3 isometricEulerAngles = new Vector3(50f, 0f, 0f);
    [Tooltip("Base distance from the car on a standard 16:9 PC monitor.")]
    public float baseCameraDistance = 24f;
    [Tooltip("Extra distance added when the car is boosting at top speed.")]
    public float maxSpeedZoomOut = 5.5f;
    public float followSmoothTime = 0.14f;

    [Header("Corner & Drift Look-Ahead")]
    [Tooltip("How far ahead of the car's velocity the camera looks.")]
    public float velocityLookAheadFactor = 0.28f;
    public float maxLookAheadDistance = 8.5f;
    public float lookAheadSmoothSpeed = 6f;

    [Header("PC to Pixel 10 Pro Fold Aspect Scaling")]
    [Tooltip("Reference aspect ratio (16:9 = 1.777).")]
    public float referenceAspect = 16f / 9f;
    [Tooltip("How strongly the camera zooms out on narrower/square screens (0 = vertical lock, 1 = full horizontal preservation).")]
    [Range(0f, 1f)] public float horizontalPreservationWeight = 0.78f;
    [Tooltip("Base Field of View on a 16:9 screen.")]
    public float baseFieldOfView = 42f;

    [Header("Screen Shake (Combat & Boost Juice)")]
    public float shakeDecay = 5f;

    private Camera cam;
    private Vector3 currentFollowVelocity;
    private Vector3 smoothedLookAheadOffset;
    private float currentZoomDistance;
    private float currentShakeIntensity;
    private float lastAspectRatio;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        currentZoomDistance = baseCameraDistance;
        transform.rotation = Quaternion.Euler(isometricEulerAngles);
        UpdateCameraProjectionForAspect(true);
    }

    private void LateUpdate()
    {
        if (targetCar == null) return;

        // 1. Check if device was folded/unfolded or window resized
        float currentAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        if (Mathf.Abs(currentAspect - lastAspectRatio) > 0.01f)
        {
            UpdateCameraProjectionForAspect(false);
            lastAspectRatio = currentAspect;
        }

        // 2. Calculate Velocity + Drift Look-Ahead Offset
        Vector3 rawVelocity = (targetSphereRb != null) ? targetSphereRb.linearVelocity : Vector3.zero;
        Vector3 planarVelocity = new Vector3(rawVelocity.x, 0f, rawVelocity.z);

        Vector3 desiredLookAhead = Vector3.ClampMagnitude(planarVelocity * velocityLookAheadFactor, maxLookAheadDistance);
        smoothedLookAheadOffset = Vector3.Lerp(smoothedLookAheadOffset, desiredLookAhead, lookAheadSmoothSpeed * Time.deltaTime);

        // 3. Calculate Dynamic Speed Zoom + Aspect Ratio Distance Compensation
        float speedRatio = Mathf.Clamp01(planarVelocity.magnitude / 38f);
        float aspectMultiplier = GetAspectDistanceMultiplier(currentAspect);
        float targetDistance = (baseCameraDistance + (speedRatio * maxSpeedZoomOut)) * aspectMultiplier;
        currentZoomDistance = Mathf.Lerp(currentZoomDistance, targetDistance, 4f * Time.deltaTime);

        // 4. Position the Isometric Camera
        Quaternion isoRotation = Quaternion.Euler(isometricEulerAngles);
        Vector3 focusPoint = targetCar.position + smoothedLookAheadOffset;
        Vector3 desiredCameraPos = focusPoint - (isoRotation * Vector3.forward * currentZoomDistance);

        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, desiredCameraPos, ref currentFollowVelocity, followSmoothTime);

        // 5. Apply Arcade Screen Shake (from EMP blasts, wall bumps, or Tier 3 Boost)
        if (currentShakeIntensity > 0.01f)
        {
            smoothedPos += Random.insideUnitSphere * currentShakeIntensity;
            currentShakeIntensity = Mathf.Lerp(currentShakeIntensity, 0f, shakeDecay * Time.deltaTime);
        }

        transform.position = smoothedPos;
        transform.rotation = isoRotation;
    }

    /// <summary>
    /// Trigger camera shake from weapons, knockback impacts, or drift boost release.
    /// </summary>
    public void TriggerCameraShake(float intensity)
    {
        currentShakeIntensity = Mathf.Max(currentShakeIntensity, intensity);
    }

    private float GetAspectDistanceMultiplier(float currentAspect)
    {
        // When unfolded on Pixel 10 Pro Fold (~1.03 aspect), currentAspect is smaller than referenceAspect (1.777).
        // We pull the camera back slightly so horizontal track width isn't cut off.
        if (currentAspect < referenceAspect)
        {
            float ratioDiff = referenceAspect / Mathf.Max(0.5f, currentAspect);
            return Mathf.Lerp(1f, ratioDiff, horizontalPreservationWeight);
        }
        return 1f;
    }

    private void UpdateCameraProjectionForAspect(bool instant)
    {
        if (cam == null) return;
        float currentAspect = (float)Screen.width / Mathf.Max(1, Screen.height);

        if (cam.orthographic)
        {
            float baseOrthoSize = 11f;
            cam.orthographicSize = baseOrthoSize * GetAspectDistanceMultiplier(currentAspect);
        }
        else
        {
            cam.fieldOfView = baseFieldOfView;
        }
    }
}
```

---

## 3. Tilt-Shift "Toy Scale" Post-Processing Setup (Unity URP)

To make your garage workbench, bowling alley, and rooftop look like hyper-detailed macro environments:

1. In your Unity Hierarchy, right-click and create **Volume $\rightarrow$ Global Volume**.
2. Click **New** next to Profile, then click **Add Override $\rightarrow$ Post-processing $\rightarrow$ Depth Of Field**.
3. Set **Mode** to **Bokeh**:
   * **Focus Distance:** `24` (matches `baseCameraDistance` on `FoldableCameraAdapter`)
   * **Focal Length:** `85mm` to `115mm` (creates the miniature toy lens blur at the top and bottom edges of the screen)
   * **Aperture (f-stop):** `f/2.8`
4. Add **Bloom** (`Intensity: 0.4`, `Threshold: 0.9`) so your Tier 1/2/3 drift sparks, EMP arcs, and neon bowling lanes pop on the Pixel 10 Pro Fold's OLED screen.