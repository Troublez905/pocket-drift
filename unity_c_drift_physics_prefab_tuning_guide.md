# Pocket Drift: Overdrive — C# Drift Physics & Prefab Tuning Guide

This file contains the foundational **C# Sphere-Rigidbody Drift Controller** code alongside a step-by-step Unity Editor setup guide so you can drop a car into a scene and start drifting on PC and your Pixel 10 Pro Fold immediately.

---

## 1. Unity Editor Prefab Hierarchy Setup

Do **not** parent the `VisualCarRoot` directly inside the `PhysicsSphere` in the Hierarchy—if you do, the car mesh will tumble head-over-heels as the sphere rolls! Instead, keep them siblings inside an empty container:

```text
▼ PlayerCar_Container (Empty GameObject @ 0,0,0)
   ► PhysicsSphere (SphereCollider + Rigidbody + PhysicsMaterial [Zero Friction])
   ▼ VisualCarRoot (ArcadeDriftController.cs attached here)
      ▼ CarBodyTiltPivot (Handles roll/pitch during drifts & jumps)
         ► ChassisMesh (Your 3D RC Car Body)
         ► Wheel_FL (Turns on Y-axis with steering + spins on X-axis)
         ► Wheel_FR (Turns on Y-axis with steering + spins on X-axis)
         ► Wheel_RL (Spins on X-axis)
         ► Wheel_RR (Spins on X-axis)
         ► RoofWeaponMount (Empty Transform on roof)
         ► RearWeaponMount (Empty Transform on rear bumper)
      ▼ VFX_Container
         ► SkidTrail_RL (TrailRenderer positioned 0.02m above ground)
         ► SkidTrail_RR (TrailRenderer positioned 0.02m above ground)
         ► DriftSparks_Particles (ParticleSystem)
         ► BoostFlame_Particles (ParticleSystem)
```

---

## 2. Core C# Script: `ArcadeDriftController.cs`

Create a new script named `ArcadeDriftController.cs` in Unity and paste the following complete implementation:

```csharp
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class ArcadeDriftController : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The detached Rigidbody sphere that drives physical movement and collisions.")]
    public Rigidbody sphereRb;
    [Tooltip("Child transform that tilts and rolls for visual game juice.")]
    public Transform carBodyTiltPivot;
    public LayerMask groundLayer = ~0;

    [Header("Acceleration & Speed Tuning")]
    public float forwardAccel = 45f;
    public float reverseAccel = 25f;
    public float maxSpeed = 35f;
    public float boostSpeedMultiplier = 1.45f;
    public float groundDrag = 3.5f;
    public float airDrag = 0.4f;
    public float extraGravity = 35f;

    [Header("Steering & Snappy Drift Tuning")]
    public float turnStrength = 140f;
    public float driftTurnMultiplier = 1.35f;
    [Tooltip("Minimum speed required to initiate a drift.")]
    public float minDriftSpeed = 8f;
    [Tooltip("How far the rear of the car kicks out visually during a drift (degrees).")]
    public float maxDriftSlipAngle = 32f;
    [Tooltip("0 = icy slide, 1 = instant grip recovery.")]
    [Range(0.1f, 1f)] public float lateralGripNormal = 0.85f;
    [Range(0.1f, 1f)] public float lateralGripDrifting = 0.35f;

    [Header("Tiered Drift Boost System")]
    public float tier1Threshold = 0.75f; // Yellow Sparks
    public float tier2Threshold = 1.65f; // Orange Flame
    public float tier3Threshold = 2.80f; // Blue Overdrive
    public float boostImpulseForce = 28f;
    public float boostDurationPerTier = 0.65f;

    [Header("Visual Juice & Wheel Transforms")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public float maxWheelTurnAngle = 30f;
    public float bodyRollAngle = 8f;
    public TrailRenderer leftSkidTrail;
    public TrailRenderer rightSkidTrail;

    [Header("Events for VFX / Feel / Audio")]
    public UnityEvent onDriftStart;
    public UnityEvent<int> onDriftTierChanged;
    public UnityEvent<int> onBoostTriggered;
    public UnityEvent onLandingImpact;

    // Runtime State
    private float moveInput;
    private float steerInput;
    private bool driftHeld;

    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool isDrifting;
    private int driftDirection; // -1 = Left, 1 = Right
    private float currentDriftCharge;
    private int currentDriftTier;
    private float boostTimer;
    private float currentVisualSlipAngle;

    private void Start()
    {
        // Detach the physics sphere so it rolls independently in world space
        if (sphereRb != null)
        {
            sphereRb.transform.parent = null;
        }
        SetSkidTrails(false);
    }

    /// <summary>
    /// Call this from your CrossPlatformInputManager every Update frame.
    /// </summary>
    public void SetInputs(float throttle, float steering, bool holdDrift)
    {
        moveInput = Mathf.Clamp(throttle, -1f, 1f);
        steerInput = Mathf.Clamp(steering, -1f, 1f);
        driftHeld = holdDrift;
    }

    private void Update()
    {
        if (sphereRb == null) return;

        // 1. Sync Visual Car Root position to the rolling sphere
        transform.position = sphereRb.transform.position - new Vector3(0f, 0.45f, 0f);

        // 2. Temporary Keyboard Fallback for quick Editor testing
        if (Mathf.Approximately(moveInput, 0f) && Mathf.Approximately(steerInput, 0f))
        {
            moveInput = Input.GetAxisRaw("Vertical");
            steerInput = Input.GetAxisRaw("Horizontal");
            driftHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift);
        }

        HandleDriftStateMachine();
        UpdateVisualsAndWheels();

        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (sphereRb == null) return;

        CheckGroundAlignment();

        if (isGrounded)
        {
            sphereRb.linearDamping = groundDrag;

            // Calculate active speed cap
            float activeMaxSpeed = (boostTimer > 0f) ? maxSpeed * boostSpeedMultiplier : maxSpeed;

            // Apply Forward / Reverse Force along the car's forward vector
            float accelForce = (moveInput > 0f) ? forwardAccel : reverseAccel;
            if (sphereRb.linearVelocity.magnitude < activeMaxSpeed)
            {
                sphereRb.AddForce(transform.forward * (moveInput * accelForce), ForceMode.Acceleration);
            }

            // Apply Steering Rotation
            float speedFactor = Mathf.Clamp01(sphereRb.linearVelocity.magnitude / 6f);
            float effectiveSteer = steerInput;

            if (isDrifting)
            {
                // Lock in an inward drift bias while allowing counter-steer control
                effectiveSteer = (driftDirection * 0.65f) + (steerInput * 0.55f);
                effectiveSteer *= driftTurnMultiplier;
            }

            float turnAmount = effectiveSteer * turnStrength * speedFactor * Mathf.Sign(Vector3.Dot(sphereRb.linearVelocity, transform.forward) + 0.1f);
            transform.Rotate(0f, turnAmount * Time.fixedDeltaTime, 0f, Space.Self);

            // Lateral Velocity Dampening (Simulates arcade tire grip vs. drift slide)
            Vector3 localVel = transform.InverseTransformDirection(sphereRb.linearVelocity);
            float activeGrip = isDrifting ? lateralGripDrifting : lateralGripNormal;
            localVel.x = Mathf.Lerp(localVel.x, 0f, activeGrip * 10f * Time.fixedDeltaTime);
            sphereRb.linearVelocity = transform.TransformDirection(localVel);
        }
        else
        {
            // Airborne Physics
            sphereRb.linearDamping = airDrag;
            sphereRb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    private void HandleDriftStateMachine()
    {
        float currentSpeed = sphereRb.linearVelocity.magnitude;

        // Start Drifting
        if (!isDrifting && isGrounded && driftHeld && currentSpeed > minDriftSpeed && Mathf.Abs(steerInput) > 0.2f)
        {
            isDrifting = true;
            driftDirection = (steerInput > 0f) ? 1 : -1;
            currentDriftCharge = 0f;
            currentDriftTier = 0;
            SetSkidTrails(true);
            onDriftStart?.Invoke();
        }

        // Accumulate Drift Charge
        if (isDrifting)
        {
            if (!driftHeld || currentSpeed < minDriftSpeed * 0.6f || !isGrounded)
            {
                EndDriftAndTriggerBoost();
                return;
            }

            // Charge faster when angling hard into the corner
            float angleMultiplier = 1f + Mathf.Abs(steerInput) * 0.5f;
            currentDriftCharge += Time.deltaTime * angleMultiplier;

            int newTier = 0;
            if (currentDriftCharge >= tier3Threshold) newTier = 3;
            else if (currentDriftCharge >= tier2Threshold) newTier = 2;
            else if (currentDriftCharge >= tier1Threshold) newTier = 1;

            if (newTier != currentDriftTier)
            {
                currentDriftTier = newTier;
                onDriftTierChanged?.Invoke(currentDriftTier);
            }
        }
    }

    private void EndDriftAndTriggerBoost()
    {
        isDrifting = false;
        SetSkidTrails(false);

        if (currentDriftTier > 0)
        {
            boostTimer = currentDriftTier * boostDurationPerTier;
            float impulse = boostImpulseForce * (0.7f + (currentDriftTier * 0.3f));
            sphereRb.AddForce(transform.forward * impulse, ForceMode.VelocityChange);
            onBoostTriggered?.Invoke(currentDriftTier);
        }

        currentDriftCharge = 0f;
        currentDriftTier = 0;
        onDriftTierChanged?.Invoke(0);
    }

    private void CheckGroundAlignment()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(sphereRb.transform.position, Vector3.down, out hit, 1.1f, groundLayer);

        if (isGrounded)
        {
            if (!wasGroundedLastFrame)
            {
                onLandingImpact?.Invoke();
            }

            // Smoothly align the car's pitch and roll to match workbench books/ramps
            Quaternion targetTilt = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetTilt, 14f * Time.fixedDeltaTime);
        }

        wasGroundedLastFrame = isGrounded;
    }

    private void UpdateVisualsAndWheels()
    {
        // Kick out the rear visual slip angle during a drift
        float targetSlipAngle = isDrifting ? (driftDirection * maxDriftSlipAngle + steerInput * 10f) : 0f;
        currentVisualSlipAngle = Mathf.Lerp(currentVisualSlipAngle, targetSlipAngle, 10f * Time.deltaTime);

        if (carBodyTiltPivot != null)
        {
            float targetRoll = -steerInput * bodyRollAngle - (isDrifting ? driftDirection * 5f : 0f);
            Quaternion targetLocalRot = Quaternion.Euler(0f, currentVisualSlipAngle, targetRoll);
            carBodyTiltPivot.localRotation = Quaternion.Slerp(carBodyTiltPivot.localRotation, targetLocalRot, 12f * Time.deltaTime);
        }

        // Turn front wheels visually
        if (frontLeftWheel != null && frontRightWheel != null)
        {
            float wheelAngle = steerInput * maxWheelTurnAngle;
            frontLeftWheel.localRotation = Quaternion.Euler(0f, wheelAngle, 0f);
            frontRightWheel.localRotation = Quaternion.Euler(0f, wheelAngle, 0f);
        }
    }

    private void SetSkidTrails(bool emitting)
    {
        if (leftSkidTrail != null) leftSkidTrail.emitting = emitting;
        if (rightSkidTrail != null) rightSkidTrail.emitting = emitting;
    }
}
```

---

## 3. Designer "Game Feel" Tuning Cheat Sheet

Once you attach `ArcadeDriftController.cs` in Unity, use these presets in the Inspector to dial in different car classes:

| Parameter | "The Scrapper" (Balanced All-Rounder) | "Volt Dozer" (Heavy Rammer) | "Neon Viper" (Twitch Drift Specialist) |
| :--- | :--- | :--- | :--- |
| **`sphereRb.mass`** | `1.5` | `2.4` | `1.1` |
| **`forwardAccel`** | `45` | `38` | `52` |
| **`maxSpeed`** | `35` | `36` | `38` |
| **`turnStrength`** | `140` | `115` | `165` |
| **`lateralGripDrifting`** | `0.35` | `0.45` | `0.25` |
| **`maxDriftSlipAngle`** | `32°` | `24°` | `42°` |
| **`boostImpulseForce`** | `28` | `24` | `34` |