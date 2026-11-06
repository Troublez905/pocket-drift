# Pocket Drift: Overdrive — Cross-Platform Input & Foldable Touch UI

This file contains the complete C# script for `CrossPlatformInputManager.cs` and instructions for setting up the **Adaptive Foldable Touch HUD** in Unity. It connects seamlessly to `ArcadeDriftController.cs` and `WeaponInventoryManager.cs`.

---

## 1. Ergonomic Design for PC & Pixel 10 Pro Fold

* **PC Mode (Keyboard / Gamepad):**
  * **Keyboard:** `W/S` or `Up/Down` for Throttle/Reverse, `A/D` or `Left/Right` for Steering, `Spacebar` or `Left Shift` to Hold Drift / Release Boost, `Left Click` or `E` to Fire Weapon.
  * **Gamepad (Xbox / DualSense / Bluetooth Controller on Fold):** `RT/LT` for Throttle/Brake, `Left Stick` to Steer, `RB` or `South Button (A/Cross)` to Drift, `West Button (X/Square)` to Fire Weapon.
  * *Auto-Hiding Touch UI:* Touch controls automatically hide the instant a physical key or gamepad button is pressed, and reappear immediately if the screen is touched.
* **Pixel 10 Pro Fold Touch Mode (Split Thumb-Zone Ergonomics):**
  * On the wide unfolded square screen, reaching the center of the display is uncomfortable.
  * All touch controls are anchored to the **Bottom-Left** and **Bottom-Right** edges with dynamic safe-area padding (`Screen.safeArea`).
  * **Auto-Accelerate Toggle (Optional for Mobile):** Allows mobile players to focus 100% on left-thumb steering and right-thumb drifting/combat, with a dedicated Brake/Reverse zone.

---

## 2. Complete C# Script: `CrossPlatformInputManager.cs`

Create a new C# script named `CrossPlatformInputManager.cs` and attach it to your **PlayerCar_Container** or an **InputManager** GameObject in the scene:

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CrossPlatformInputManager : MonoBehaviour
{
    public enum ActiveInputMode
    {
        KeyboardOrGamepad,
        TouchScreen
    }

    [Header("Target Controllers")]
    public ArcadeDriftController driftController;
    [Tooltip("Invoked when the player presses the Fire Weapon button or key.")]
    public UnityEvent onFireWeaponPressed;

    [Header("Mobile / Foldable Ergonomics")]
    [Tooltip("When enabled in Touch Mode, the car accelerates automatically so thumbs focus on steering & drifting.")]
    public bool autoAccelerateOnTouch = true;
    [Tooltip("Root CanvasGroup for the on-screen touch thumb zones.")]
    public CanvasGroup touchHudCanvasGroup;
    [Tooltip("RectTransform of the Left Thumb Panel (anchored Bottom-Left).")]
    public RectTransform leftThumbZonePanel;
    [Tooltip("RectTransform of the Right Thumb Panel (anchored Bottom-Right).")]
    public RectTransform rightThumbZonePanel;

    [Header("Runtime Telemetry (Read-Only)")]
    public ActiveInputMode currentInputMode = ActiveInputMode.KeyboardOrGamepad;

    // Touch State Flags (Driven by UI PointerDown / PointerUp events or virtual thumbstick)
    private float touchSteerValue;
    private bool touchLeftHeld;
    private bool touchRightHeld;
    private bool touchDriftHeld;
    private bool touchBrakeReverseHeld;
    private float touchThrottleOverride;

    private void Start()
    {
        // Default to Touch Mode if running on Android/iOS, otherwise Keyboard/Gamepad
        if (Application.isMobilePlatform)
        {
            SwitchInputMode(ActiveInputMode.TouchScreen);
        }
        else
        {
            SwitchInputMode(ActiveInputMode.KeyboardOrGamepad);
        }

        AdaptTouchZonesForFoldableAspect();
    }

    private void Update()
    {
        DetectHardwareInputSwitch();

        float finalThrottle = 0f;
        float finalSteer = 0f;
        bool finalDrift = false;

        if (currentInputMode == ActiveInputMode.KeyboardOrGamepad)
        {
            // Read Standard Unity Input Axes (Compatible with Keyboard & Gamepad out of the box)
            finalThrottle = Input.GetAxisRaw("Vertical");
            finalSteer = Input.GetAxisRaw("Horizontal");
            finalDrift = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Jump");

            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0) || Input.GetButtonDown("Fire1"))
            {
                // Ignore mouse clicks if clicking a UI button during testing
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    TriggerWeaponFire();
                }
            }
        }
        else
        {
            // Touch Screen Input Calculation
            if (touchBrakeReverseHeld)
            {
                finalThrottle = -1f;
            }
            else if (autoAccelerateOnTouch)
            {
                finalThrottle = 1f;
            }
            else
            {
                finalThrottle = touchThrottleOverride;
            }

            // Combine split Left/Right buttons or analog virtual slider
            if (touchLeftHeld && !touchRightHeld) finalSteer = -1f;
            else if (touchRightHeld && !touchLeftHeld) finalSteer = 1f;
            else finalSteer = touchSteerValue;

            finalDrift = touchDriftHeld;
        }

        // Send unified inputs to the ArcadeDriftController
        if (driftController != null)
        {
            driftController.SetInputs(finalThrottle, finalSteer, finalDrift);
        }
    }

    private void DetectHardwareInputSwitch()
    {
        // Switch to Touch if any touch is detected on screen
        if (Input.touchCount > 0 && currentInputMode != ActiveInputMode.TouchScreen)
        {
            SwitchInputMode(ActiveInputMode.TouchScreen);
            AdaptTouchZonesForFoldableAspect();
        }
        // Switch to Keyboard/Gamepad if physical keys are pressed
        else if (currentInputMode == ActiveInputMode.TouchScreen)
        {
            if (Input.anyKeyDown && Input.touchCount == 0 && !Input.GetMouseButtonDown(0))
            {
                SwitchInputMode(ActiveInputMode.KeyboardOrGamepad);
            }
        }
    }

    public void SwitchInputMode(ActiveInputMode newMode)
    {
        currentInputMode = newMode;

        if (touchHudCanvasGroup != null)
        {
            bool showTouchUI = (newMode == ActiveInputMode.TouchScreen);
            touchHudCanvasGroup.alpha = showTouchUI ? 1f : 0f;
            touchHudCanvasGroup.interactable = showTouchUI;
            touchHudCanvasGroup.blocksRaycasts = showTouchUI;
        }
    }

    /// <summary>
    /// Repositions thumb zones slightly higher when unfolded on the Pixel 10 Pro Fold (~1:1 aspect)
    /// so the user's hands naturally balance the tablet weight around the center of gravity.
    /// </summary>
    public void AdaptTouchZonesForFoldableAspect()
    {
        if (leftThumbZonePanel == null || rightThumbZonePanel == null) return;

        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        bool isUnfoldedSquareScreen = (aspect < 1.35f);

        // Raise thumb zones slightly on the tall square inner screen for comfortable grip balance
        float verticalOffset = isUnfoldedSquareScreen ? 140f : 45f;

        leftThumbZonePanel.anchoredPosition = new Vector2(40f, verticalOffset);
        rightThumbZonePanel.anchoredPosition = new Vector2(-40f, verticalOffset);
    }

    // =========================================================================
    // UI EVENT HOOKS (Wire these to EventTrigger components on your UI Buttons)
    // =========================================================================

    public void OnTouchSteerLeft(bool pressed) => touchLeftHeld = pressed;
    public void OnTouchSteerRight(bool pressed) => touchRightHeld = pressed;
    public void OnTouchAnalogSteer(float value) => touchSteerValue = Mathf.Clamp(value, -1f, 1f);

    public void OnTouchDriftHold(bool pressed) => touchDriftHeld = pressed;
    public void OnTouchBrakeHold(bool pressed) => touchBrakeReverseHeld = pressed;
    public void OnTouchThrottleHold(bool pressed) => touchThrottleOverride = pressed ? 1f : 0f;

    public void TriggerWeaponFire()
    {
        onFireWeaponPressed?.Invoke();
    }
}
```

---

## 3. How to Wire Up the Foldable Touch Canvas in Unity (5 Minutes)

1. **Create the Canvas:**
   * In Unity, right-click in the Hierarchy $\rightarrow$ **UI $\rightarrow$ Canvas** and rename it `HUD_Canvas`.
   * On the **Canvas Scaler** component:
     * Set **UI Scale Mode** to `Scale With Screen Size`.
     * Set **Reference Resolution** to `1920 x 1080`.
     * Set **Screen Match Mode** to `Match Width Or Height` and set the **Match slider to `0.5`** (critical so UI buttons remain the exact same physical thumb size whether folded or unfolded!).
2. **Create `TouchControls_Root`:**
   * Inside `HUD_Canvas`, create an Empty RectTransform stretched across the screen, add a **Canvas Group** component, and assign it to `touchHudCanvasGroup` on `CrossPlatformInputManager`.
3. **Left Thumb Panel (`LeftThumbZonePanel`):**
   * Anchor to **Bottom-Left**. Place two side-by-side translucent buttons (`SteerLeft_Btn` and `SteerRight_Btn`).
   * Add an **Event Trigger** component to `SteerLeft_Btn`:
     * Add `Pointer Down` $\rightarrow$ call `CrossPlatformInputManager.OnTouchSteerLeft(true)`.
     * Add `Pointer Up` $\rightarrow$ call `CrossPlatformInputManager.OnTouchSteerLeft(false)`.
   * Repeat for `SteerRight_Btn` using `OnTouchSteerRight(true/false)`.
4. **Right Thumb Panel (`RightThumbZonePanel`):**
   * Anchor to **Bottom-Right**.
   * Create a large primary button (`DriftBoost_Btn`):
     * Add `Pointer Down` $\rightarrow$ call `OnTouchDriftHold(true)`.
     * Add `Pointer Up` $\rightarrow$ call `OnTouchDriftHold(false)` (releasing automatically triggers your Tier 1/2/3 Drift Boost!).
   * Above the Drift button, create `FireWeapon_Btn` and wire its `Pointer Down` event to `TriggerWeaponFire()`.
   * Below/left of the Drift button, create a smaller `BrakeReverse_Btn` and wire its `Pointer Down / Up` to `OnTouchBrakeHold(true/false)`.