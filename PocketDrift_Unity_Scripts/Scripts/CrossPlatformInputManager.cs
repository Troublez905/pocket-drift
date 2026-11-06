using UnityEngine;

public class CrossPlatformInputManager : MonoBehaviour
{
    [Header("Target Vehicle")]
    public ArcadeDriftController targetCar;

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
    }

    private void Update()
    {
        if (targetCar == null) return;

        // 1. Read Keyboard / Gamepad Axes
        float kbThrottle = Input.GetAxisRaw("Vertical");
        float kbSteer = Input.GetAxisRaw("Horizontal");
        bool kbDrift = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Jump");
        bool kbFire = Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);

        // 2. Read Touch Screen Split Thumb-Zones if active
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

                // Left Half of Screen = Analog Thumb Steering
                if (normX < 0.5f)
                {
                    float centeredLeftX = (normX - 0.25f) / 0.22f;
                    if (Mathf.Abs(centeredLeftX) > touchSteerDeadzone)
                    {
                        touchSteer = Mathf.Clamp(centeredLeftX, -1f, 1f);
                    }
                }
                // Right Half of Screen = Bottom Right Drift Hold / Top Right Weapon Fire
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

        float finalThrottle = Mathf.Abs(kbThrottle) > 0.01f ? kbThrottle : touchThrottle;
        float finalSteer = Mathf.Abs(kbSteer) > 0.01f ? kbSteer : touchSteer;
        bool finalDrift = kbDrift || touchDrift;
        bool finalFire = kbFire || touchFire;

        targetCar.SetInputs(finalThrottle, finalSteer, finalDrift, finalFire);
        uiTouchFire = false;
    }

    // Public UI Hooks for Canvas EventTriggers
    public void SetUITouchSteer(float value) => uiTouchSteer = Mathf.Clamp(value, -1f, 1f);
    public void SetUITouchDrift(bool held) => uiTouchDrift = held;
    public void TriggerUITouchFire() => uiTouchFire = true;
}
