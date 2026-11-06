using UnityEngine;

public class ArcadeDriftController : MonoBehaviour
{
    [Header("Core References")]
    public Rigidbody sphereRb;
    public Transform visualRoot;
    public Transform frontLeftWheel;
    public Transform frontRightWheel;

    [Header("Movement & Speed Settings")]
    public float forwardAcceleration = 38f;
    public float reverseAcceleration = 20f;
    public float maxSpeed = 24f;
    public float boostMultiplier = 1.45f;
    public float gravityForce = 25f;
    public LayerMask groundLayer = ~0;

    [Header("Steering & Arcade Drift Settings")]
    public float turnStrength = 140f;
    public float driftTurnMultiplier = 1.35f;
    [Range(0f, 1f)] public float normalGrip = 0.92f;
    [Range(0f, 1f)] public float driftGrip = 0.35f;
    public float minSpeedToDrift = 5f;

    [Header("3-Tier Boost Charge System")]
    public float tier1Threshold = 1.0f;
    public float tier2Threshold = 2.2f;
    public float tier3Threshold = 3.8f;
    public float tier1BoostDuration = 0.8f;
    public float tier2BoostDuration = 1.6f;
    public float tier3BoostDuration = 2.8f;

    [Header("Visual Juice & VFX")]
    public float maxChassisRollAngle = 10f;
    public float maxDriftYawOffset = 22f;
    public TrailRenderer[] rearSkidTrails;
    public ParticleSystem boostFlameParticles;

    // Runtime state exposed for Camera, AI, Knockback & HUD
    public bool IsDrifting { get; private set; }
    public bool IsBoosting => boostTimeRemaining > 0f;
    public bool IsGrounded { get; private set; }
    public int CurrentBoostTier { get; private set; }
    public float DriftChargeTimer { get; private set; }
    public float NormalizedBoostCharge => Mathf.Clamp01(DriftChargeTimer / tier3Threshold);
    public float CurrentSpeed => sphereRb != null ? new Vector3(sphereRb.linearVelocity.x, 0f, sphereRb.linearVelocity.z).magnitude : 0f;

    private float throttleInput;
    private float steerInput;
    private bool driftHeld;
    private int driftDirection;
    private float boostTimeRemaining;
    private float externalSpeedMultiplier = 1f;
    private float stunTimer;

    private void Start()
    {
        if (sphereRb != null)
        {
            sphereRb.transform.parent = null;
        }
        SetSkidTrailsEmitting(false);
    }

    public void SetInputs(float throttle, float steer, bool driftButton, bool fireWeapon = false)
    {
        if (stunTimer > 0f)
        {
            throttleInput = 0f;
            steerInput = 0f;
            driftHeld = false;
            return;
        }

        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steerInput = Mathf.Clamp(steer, -1f, 1f);
        driftHeld = driftButton;
    }

    private void Update()
    {
        if (sphereRb == null) return;

        // Sync root position to the decoupled physics sphere
        transform.position = sphereRb.transform.position - new Vector3(0f, 0.35f, 0f);

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
        }

        HandleDriftStateAndBoostCharge();
        UpdateVisualChassisAndWheels();
    }

    private void FixedUpdate()
    {
        if (sphereRb == null) return;

        CheckGroundNormal();
        ApplyDriveAndLateralGripForces();
    }

    private void HandleDriftStateAndBoostCharge()
    {
        if (boostTimeRemaining > 0f)
        {
            boostTimeRemaining -= Time.deltaTime;
            if (boostTimeRemaining <= 0f && boostFlameParticles != null)
            {
                boostFlameParticles.Stop();
            }
        }

        // Start Drift
        if (driftHeld && !IsDrifting && IsGrounded && CurrentSpeed >= minSpeedToDrift && Mathf.Abs(steerInput) > 0.15f)
        {
            IsDrifting = true;
            driftDirection = steerInput > 0f ? 1 : -1;
            DriftChargeTimer = 0f;
            CurrentBoostTier = 0;
            SetSkidTrailsEmitting(true);
        }

        // Sustain Drift & Accumulate Charge
        if (IsDrifting)
        {
            if (!driftHeld || CurrentSpeed < minSpeedToDrift * 0.65f || !IsGrounded)
            {
                ReleaseDriftAndTriggerBoost();
            }
            else
            {
                float steerBonus = Mathf.Lerp(0.75f, 1.35f, Mathf.Abs(steerInput));
                DriftChargeTimer += Time.deltaTime * steerBonus;

                if (DriftChargeTimer >= tier3Threshold) CurrentBoostTier = 3;
                else if (DriftChargeTimer >= tier2Threshold) CurrentBoostTier = 2;
                else if (DriftChargeTimer >= tier1Threshold) CurrentBoostTier = 1;
            }
        }
    }

    private void ReleaseDriftAndTriggerBoost()
    {
        if (CurrentBoostTier == 1) TriggerBoost(tier1BoostDuration);
        else if (CurrentBoostTier == 2) TriggerBoost(tier2BoostDuration);
        else if (CurrentBoostTier == 3) TriggerBoost(tier3BoostDuration);

        IsDrifting = false;
        DriftChargeTimer = 0f;
        CurrentBoostTier = 0;
        SetSkidTrailsEmitting(false);
    }

    public void TriggerBoost(float duration)
    {
        boostTimeRemaining = Mathf.Max(boostTimeRemaining, duration);
        if (sphereRb != null)
        {
            sphereRb.AddForce(transform.forward * (forwardAcceleration * 0.45f), ForceMode.VelocityChange);
        }
        if (boostFlameParticles != null && !boostFlameParticles.isPlaying)
        {
            boostFlameParticles.Play();
        }
    }

    public void ApplyEMPStun(float duration)
    {
        stunTimer = Mathf.Max(stunTimer, duration);
        if (IsDrifting)
        {
            IsDrifting = false;
            DriftChargeTimer = 0f;
            CurrentBoostTier = 0;
            SetSkidTrailsEmitting(false);
        }
    }

    public void SetSurfaceSpeedMultiplier(float multiplier)
    {
        externalSpeedMultiplier = Mathf.Clamp(multiplier, 0.25f, 1.5f);
    }

    private void CheckGroundNormal()
    {
        if (Physics.Raycast(sphereRb.position, Vector3.down, out RaycastHit hit, 0.55f, groundLayer))
        {
            IsGrounded = true;
            Quaternion slopeTarget = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeTarget, Time.fixedDeltaTime * 12f);
        }
        else
        {
            IsGrounded = false;
        }
    }

    private void ApplyDriveAndLateralGripForces()
    {
        if (!IsGrounded)
        {
            sphereRb.AddForce(Vector3.down * gravityForce, ForceMode.Acceleration);
            return;
        }

        // Steering Yaw
        float speedFactor = Mathf.Clamp01(CurrentSpeed / 6f);
        float effectiveSteer = steerInput;
        if (IsDrifting)
        {
            effectiveSteer = Mathf.Clamp(driftDirection * 0.55f + steerInput * 0.75f, -1.35f, 1.35f) * driftTurnMultiplier;
        }

        float reverseSign = Vector3.Dot(sphereRb.linearVelocity, transform.forward) < -0.5f ? -1f : 1f;
        float yawDelta = effectiveSteer * turnStrength * speedFactor * reverseSign * Time.fixedDeltaTime;
        transform.Rotate(0f, yawDelta, 0f, Space.Self);

        // Forward / Reverse Drive Force
        float activeMaxSpeed = maxSpeed * (IsBoosting ? boostMultiplier : 1f) * externalSpeedMultiplier;
        float accel = throttleInput >= 0f ? forwardAcceleration : reverseAcceleration;
        if (IsBoosting) accel *= boostMultiplier;

        if (CurrentSpeed < activeMaxSpeed || throttleInput < 0f)
        {
            sphereRb.AddForce(transform.forward * (throttleInput * accel), ForceMode.Acceleration);
        }

        // Lateral Slip Cancellation (Arcade Drift Grip)
        Vector3 localVel = transform.InverseTransformDirection(sphereRb.linearVelocity);
        float activeGrip = IsDrifting ? driftGrip : normalGrip;
        localVel.x = Mathf.Lerp(localVel.x, 0f, activeGrip * 12f * Time.fixedDeltaTime);
        sphereRb.linearVelocity = transform.TransformDirection(localVel);
    }

    private void UpdateVisualChassisAndWheels()
    {
        if (visualRoot != null)
        {
            float targetYaw = IsDrifting ? (driftDirection * maxDriftYawOffset + steerInput * 8f) : (steerInput * 5f);
            float targetRoll = -steerInput * maxChassisRollAngle * (IsDrifting ? 1.4f : 0.7f);
            Quaternion targetLocalRot = Quaternion.Euler(0f, targetYaw, targetRoll);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetLocalRot, Time.deltaTime * 10f);
        }

        float wheelSteerAngle = steerInput * 32f;
        if (frontLeftWheel != null)
            frontLeftWheel.localRotation = Quaternion.Euler(0f, wheelSteerAngle, 0f);
        if (frontRightWheel != null)
            frontRightWheel.localRotation = Quaternion.Euler(0f, wheelSteerAngle, 0f);
    }

    private void SetSkidTrailsEmitting(bool emit)
    {
        if (rearSkidTrails == null) return;
        foreach (TrailRenderer tr in rearSkidTrails)
        {
            if (tr != null) tr.emitting = emit;
        }
    }

    private void OnDestroy()
    {
        if (sphereRb != null && sphereRb.gameObject != null)
        {
            Destroy(sphereRb.gameObject);
        }
    }
}
