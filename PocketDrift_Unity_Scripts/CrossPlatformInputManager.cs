using UnityEngine;

public class CrossPlatformInputManager : MonoBehaviour
{
    [Header("Target Vehicle & Weapon System")]
    public ArcadeDriftController targetCar;
    public WeaponInventoryManager weaponInventory;

    [Header("Razer Wolverine TE / Gamepad Profile")]
    public bool enableGamepadSupport = true;
    public bool isRazerWolverineDetected = false;
    [Range(0.05f, 0.35f)] public float stickDeadzone = 0.12f;
    [Range(1.0f, 2.5f)] public float steeringCurvePower = 1.25f;
    [Tooltip("Hair-trigger mode switch support: reach full throttle with limited trigger travel")]
    [Range(0.1f, 0.5f)] public float hairTriggerThreshold = 0.20f;

    [Header("Controller Button Bindings (Razer Wolverine / XInput)")]
    public KeyCode gamepadDriftBumper = KeyCode.JoystickButton5; // RB (Right Bumper)
    public KeyCode gamepadDriftAlt = KeyCode.JoystickButton0;    // Button A (South)
    public KeyCode gamepadWeaponFire = KeyCode.JoystickButton2;  // Button X (West)
    public KeyCode gamepadWeaponFireAlt = KeyCode.JoystickButton1; // Button B (East)
    public KeyCode gamepadRespawn = KeyCode.JoystickButton6;     // View / Back Button
    public KeyCode gamepadPause = KeyCode.JoystickButton7;       // Menu / Start Button

    [Header("Touch Thumb-Zone Settings (Pixel 10 Pro Fold & Mobile)")]
    public bool enableTouchControlsOnMobile = true;
    public bool autoAccelerateOnTouch = true;
    [Range(0.05f, 0.4f)] public float touchSteerDeadzone = 0.08f;

    // Runtime UI Touch Override Values
    private float uiTouchSteer;
    private bool uiTouchDrift;
    private bool uiTouchFire;

    private void Awake()
    {
        if (targetCar == null) targetCar = GetComponent<ArcadeDriftController>();
        if (weaponInventory == null) weaponInventory = GetComponent<WeaponInventoryManager>();
        CheckConnectedGamepad();
    }

    private void Update()
    {
        if (targetCar == null) return;

        // 1. Read Gamepad / Keyboard Inputs
        float rawSteer = 0f;
        float rawThrottle = 0f;
        bool rawDrift = false;
        bool rawFire = false;

        // Periodic gamepad poll
        if (Time.frameCount % 180 == 0)
        {
            CheckConnectedGamepad();
        }

        if (enableGamepadSupport)
        {
            // --- Steering (Left Stick & D-Pad) ---
            float stickX = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(stickX) > stickDeadzone)
            {
                // Apply deadzone subtraction and non-linear response curve for Razer micro-tuning
                float sign = Mathf.Sign(stickX);
                float normalized = (Mathf.Abs(stickX) - stickDeadzone) / (1f - stickDeadzone);
                rawSteer = sign * Mathf.Pow(Mathf.Clamp01(normalized), steeringCurvePower);
            }

            // --- Throttle & Brake (Analog Triggers, Vertical Axis, & Buttons) ---
            float stickY = Input.GetAxisRaw("Vertical");
            
            // Try reading analog trigger axes (safe fallback if custom axis is not registered)
            float rtAxis = 0f;
            float ltAxis = 0f;
            try
            {
                // Axis 10 = Right Trigger (Windows XInput), Axis 9 = Left Trigger
                rtAxis = Mathf.Clamp01(Input.GetAxis("RightTrigger"));
                ltAxis = Mathf.Clamp01(Input.GetAxis("LeftTrigger"));
            }
            catch
            {
                // Custom trigger axes not mapped in InputManager; use standard Vertical axis
            }

            if (rtAxis > hairTriggerThreshold)
            {
                rawThrottle = Mathf.Clamp01((rtAxis - hairTriggerThreshold) / (1f - hairTriggerThreshold));
            }
            else if (ltAxis > hairTriggerThreshold)
            {
                rawThrottle = -Mathf.Clamp01((ltAxis - hairTriggerThreshold) / (1f - hairTriggerThreshold));
            }
            else if (Mathf.Abs(stickY) > stickDeadzone)
            {
                rawThrottle = Mathf.Clamp(stickY, -1f, 1f);
            }

            // Button Gas/Reverse Overrides (e.g. Button A for gas, Button X for reverse or hair-trigger toggle)
            if (Input.GetKey(KeyCode.JoystickButton0) && rawThrottle < 0.1f)
            {
                rawThrottle = 1f; // Button A Gas
            }

            // --- Drift / Boost (RB / LB / Button A / Space) ---
            rawDrift = Input.GetKey(gamepadDriftBumper) || 
                       Input.GetKey(KeyCode.JoystickButton4) || // LB
                       Input.GetKey(gamepadDriftAlt) || 
                       Input.GetKey(KeyCode.Space) || 
                       Input.GetKey(KeyCode.LeftShift);

            // --- Weapon Fire (Button X / Button B / Enter / Left Click) ---
            rawFire = Input.GetKeyDown(gamepadWeaponFire) || 
                      Input.GetKeyDown(gamepadWeaponFireAlt) || 
                      Input.GetKeyDown(KeyCode.Return) || 
                      Input.GetKeyDown(KeyCode.E) || 
                      Input.GetMouseButtonDown(0);

            // --- Quick Respawn / Reset (Back / View button or Key R) ---
            if (Input.GetKeyDown(gamepadRespawn) || Input.GetKeyDown(KeyCode.R))
            {
                TableEdgeFallTrigger fallTrigger = Object.FindAnyObjectByType<TableEdgeFallTrigger>();
                if (fallTrigger != null && targetCar != null)
                {
                    fallTrigger.RespawnCar(targetCar);
                }
            }
        }
        else
        {
            // Standard Keyboard Fallback
            rawThrottle = Input.GetAxisRaw("Vertical");
            rawSteer = Input.GetAxisRaw("Horizontal");
            rawDrift = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift);
            rawFire = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
        }

        // 2. Read Touch Screen Split Thumb-Zones (Foldables & Tablets)
        float touchThrottle = 0f;
        float touchSteer = uiTouchSteer;
        bool touchDrift = uiTouchDrift;
        bool touchFire = uiTouchFire;

        if (enableTouchControlsOnMobile && Input.touchCount > 0)
        {
            if (autoAccelerateOnTouch) touchThrottle = 1f;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                float normX = t.position.x / Mathf.Max(1f, (float)Screen.width);
                float normY = t.position.y / Mathf.Max(1f, (float)Screen.height);

                // Left Half = Analog Thumb Steering
                if (normX < 0.5f)
                {
                    float centeredLeftX = (normX - 0.25f) / 0.22f;
                    if (Mathf.Abs(centeredLeftX) > touchSteerDeadzone)
                    {
                        touchSteer = Mathf.Clamp(centeredLeftX, -1f, 1f);
                    }
                }
                // Right Half = Bottom Right Drift Hold / Top Right Weapon Fire
                else
                {
                    if (normY < 0.55f)
                    {
                        touchDrift = (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled);
                    }
                    else if (t.phase == TouchPhase.Began)
                    {
                        touchFire = true;
                    }
                }
            }
        }

        // Combine inputs
        float finalThrottle = Mathf.Abs(rawThrottle) > 0.01f ? rawThrottle : touchThrottle;
        float finalSteer = Mathf.Abs(rawSteer) > 0.01f ? rawSteer : touchSteer;
        bool finalDrift = rawDrift || touchDrift;
        bool finalFire = rawFire || touchFire;

        targetCar.SetInputs(finalThrottle, finalSteer, finalDrift, finalFire);

        // Fire Weapon via inventory directly
        if (finalFire && weaponInventory != null && weaponInventory.HasWeapon)
        {
            weaponInventory.FireCurrentWeapon();
        }

        uiTouchFire = false;
    }

    private void CheckConnectedGamepad()
    {
        string[] joysticks = Input.GetJoystickNames();
        isRazerWolverineDetected = false;

        if (joysticks != null)
        {
            for (int i = 0; i < joysticks.Length; i++)
            {
                if (!string.IsNullOrEmpty(joysticks[i]))
                {
                    string joy = joysticks[i].ToLower();
                    if (joy.Contains("razer") || joy.Contains("wolverine") || joy.Contains("xbox") || joy.Contains("xinput"))
                    {
                        isRazerWolverineDetected = true;
                        break;
                    }
                }
            }
        }
    }

    // Public UI Hooks for Canvas EventTriggers / On-screen buttons
    public void SetUITouchSteer(float value) => uiTouchSteer = Mathf.Clamp(value, -1f, 1f);
    public void SetUITouchDrift(bool held) => uiTouchDrift = held;
    public void TriggerUITouchFire() => uiTouchFire = true;
}
