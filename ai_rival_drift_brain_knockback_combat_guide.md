# Pocket Drift: Overdrive — AI Rival Drift Brain & Knockback Combat System

To make **Pocket Drift: Overdrive** feel chaotic, fair, and exciting, the AI rivals **do not cheat with fake rail-movement**. Instead, `AIRivalDriftBrain.cs` feeds simulated `throttle`, `steer`, and `holdDrift` inputs directly into the exact same `ArcadeDriftController.cs` script that the player uses!

---

## 1. How the AI Rival Architecture Works

1. **Corner Curvature Look-Ahead (Autonomous Drifting):**
   * The AI looks at its current target waypoint *and* the next waypoint ahead of it. If the angle between them is sharp (e.g., $> 28^\circ$), the AI automatically holds the Drift button early, counter-steers through the apex, and releases the Drift button on corner exit to unleash a Tier 1/2/3 Drift Boost.
2. **Whisker Raycast Hazard & Table-Edge Avoidance:**
   * Three forward-angled raycasts ("whiskers") check for giant workbench props (wrenches, soda cans) and drop-offs at the edge of the table so the AI steers away from ring-outs while battling side-by-side.
3. **Aggressive Ramming & Weapon AI:**
   * If a rival car enters the AI's forward cone or side-by-side proximity, the AI biases its steering to bump them toward the nearest table edge or fires its equipped weapon.

---

## 2. Complete C# Script: `TrackWaypointCircuit.cs`

Create this lightweight helper script first and attach it to an empty GameObject named `TrackWaypoints_Container`. Any child transforms you place inside it automatically become the racing line and draw a clean visual loop in the Unity Scene view:

```csharp
using System.Collections.Generic;
using UnityEngine;

public class TrackWaypointCircuit : MonoBehaviour
{
    [Tooltip("Radius within which a car counts as having reached a waypoint.")]
    public float waypointReachRadius = 5.5f;
    public Color gizmoLineColor = Color.cyan;

    [HideInInspector]
    public List<Transform> waypoints = new List<Transform>();

    private void Awake()
    {
        RefreshWaypoints();
    }

    public void RefreshWaypoints()
    {
        waypoints.Clear();
        foreach (Transform child in transform)
        {
            if (child != transform)
            {
                waypoints.Add(child);
            }
        }
    }

    public Transform GetWaypoint(int index)
    {
        if (waypoints.Count == 0) RefreshWaypoints();
        if (waypoints.Count == 0) return transform;
        return waypoints[(index % waypoints.Count + waypoints.Count) % waypoints.Count];
    }

    private void OnDrawGizmos()
    {
        RefreshWaypoints();
        if (waypoints.Count < 2) return;

        Gizmos.color = gizmoLineColor;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform current = waypoints[i];
            Transform next = waypoints[(i + 1) % waypoints.Count];
            if (current != null && next != null)
            {
                Gizmos.DrawWireSphere(current.position, 0.75f);
                Gizmos.DrawLine(current.position, next.position);
            }
        }
    }
}
```

---

## 3. Complete C# Script: `AIRivalDriftBrain.cs`

Attach this script to any AI Car's `VisualCarRoot` (right alongside `ArcadeDriftController.cs`—just omit `CrossPlatformInputManager.cs` on AI cars):

```csharp
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ArcadeDriftController))]
public class AIRivalDriftBrain : MonoBehaviour
{
    public enum AIPersonality
    {
        PrecisionDrifter, // Prioritizes clean racing lines and Tier 3 boosts
        WorkbenchBrawler, // Actively swerves to ram nearby cars toward edges
        ChaosTrickster    // Fires weapons immediately and takes wide drift angles
    }

    [Header("Core References")]
    public TrackWaypointCircuit trackCircuit;
    public AIPersonality personality = AIPersonality.PrecisionDrifter;
    public UnityEvent onAIFireWeapon;

    [Header("Navigation & Drift Intelligence")]
    [Tooltip("Corner angle (in degrees) between current and next waypoint that triggers an AI drift.")]
    public float cornerAngleToStartDrift = 26f;
    [Tooltip("Corner angle below which the AI releases drift to trigger its boost.")]
    public float cornerAngleToReleaseBoost = 11f;
    [Tooltip("Random lateral offset so 4-6 AI cars don't drive in a single-file train.")]
    public float maxRacingLineSpread = 2.8f;

    [Header("Obstacle & Table-Edge Avoidance")]
    public LayerMask obstacleLayer;
    public LayerMask groundLayer = ~0;
    public float whiskerLength = 6.5f;
    public float whiskerSpreadAngle = 28f;
    public float avoidanceSteerWeight = 1.35f;

    [Header("Combat & Rubberbanding")]
    public Transform playerTarget;
    [Tooltip("Subtle speed multiplier adjustments to keep packs tight and exciting.")]
    public bool enableArcadeRubberbanding = true;

    private ArcadeDriftController driftController;
    private int currentWaypointIndex;
    private float lateralLineOffset;
    private float stuckTimer;
    private float reverseRecoveryTimer;
    private float weaponFireCooldown;
    private float baseMaxSpeed;

    private void Awake()
    {
        driftController = GetComponent<ArcadeDriftController>();
        baseMaxSpeed = driftController.maxSpeed;
        lateralLineOffset = Random.Range(-maxRacingLineSpread, maxRacingLineSpread);
        weaponFireCooldown = Random.Range(2f, 5f);
    }

    private void Update()
    {
        if (trackCircuit == null || driftController == null || driftController.sphereRb == null) return;

        // 1. Check if we reached the current waypoint
        Transform targetWp = trackCircuit.GetWaypoint(currentWaypointIndex);
        Transform nextWp = trackCircuit.GetWaypoint(currentWaypointIndex + 1);

        Vector3 carPos = transform.position;
        float distToWaypoint = Vector3.Distance(new Vector3(carPos.x, targetWp.position.y, carPos.z), targetWp.position);

        if (distToWaypoint < trackCircuit.waypointReachRadius)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % trackCircuit.waypoints.Count;
            // Pick a fresh slight offset on straightaways for natural pack movement
            lateralLineOffset = Mathf.Lerp(lateralLineOffset, Random.Range(-maxRacingLineSpread, maxRacingLineSpread), 0.45f);
            targetWp = trackCircuit.GetWaypoint(currentWaypointIndex);
            nextWp = trackCircuit.GetWaypoint(currentWaypointIndex + 1);
        }

        // 2. Check if stuck against a giant wrench / wall
        HandleStuckRecovery();
        if (reverseRecoveryTimer > 0f)
        {
            reverseRecoveryTimer -= Time.deltaTime;
            driftController.SetInputs(-1f, -Mathf.Sign(lateralLineOffset), false);
            return;
        }

        // 3. Calculate Target Point with Racing Line Spread + Brawler Ramming Bias
        Vector3 waypointRight = Vector3.Cross(Vector3.up, (nextWp.position - targetWp.position).normalized);
        Vector3 aimedWorldPos = targetWp.position + (waypointRight * lateralLineOffset);

        if (personality == AIPersonality.WorkbenchBrawler && playerTarget != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distToPlayer < 6.5f)
            {
                // Nudge target point toward the player to trade paint!
                aimedWorldPos = Vector3.Lerp(aimedWorldPos, playerTarget.position, 0.35f);
            }
        }

        // 4. Calculate Steering Input (-1 to +1)
        Vector3 localTarget = transform.InverseTransformPoint(aimedWorldPos);
        float steerAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        float desiredSteer = Mathf.Clamp(steerAngle / 38f, -1f, 1f);

        // 5. Apply Whisker Obstacle & Table-Edge Dropoff Avoidance
        float avoidanceSteer = CalculateWhiskerAvoidance();
        if (Mathf.Abs(avoidanceSteer) > 0.05f)
        {
            desiredSteer = Mathf.Clamp(desiredSteer + avoidanceSteer * avoidanceSteerWeight, -1f, 1f);
        }

        // 6. Decide When to Hold Drift & Release Boost
        Vector3 dirToCurrent = (targetWp.position - transform.position).normalized;
        Vector3 dirToNext = (nextWp.position - targetWp.position).normalized;
        float upcomingCornerAngle = Vector3.Angle(dirToCurrent, dirToNext);

        bool shouldHoldDrift = false;
        if (upcomingCornerAngle > cornerAngleToStartDrift || Mathf.Abs(steerAngle) > cornerAngleToStartDrift)
        {
            shouldHoldDrift = true;
        }
        else if (upcomingCornerAngle < cornerAngleToReleaseBoost && Mathf.Abs(steerAngle) < cornerAngleToReleaseBoost)
        {
            // Release drift on corner exit to fire the accumulated Tier 1/2/3 boost!
            shouldHoldDrift = false;
        }

        // 7. Apply Subtle Rubberbanding & Send Inputs to ArcadeDriftController
        UpdateRubberbanding();
        driftController.SetInputs(1f, desiredSteer, shouldHoldDrift);

        // 8. Autonomous Weapon Usage
        HandleAIWeaponFiring();
    }

    private float CalculateWhiskerAvoidance()
    {
        float steerAdjustment = 0f;
        Vector3 origin = transform.position + Vector3.up * 0.35f;

        Vector3 leftRayDir = Quaternion.Euler(0f, -whiskerSpreadAngle, 0f) * transform.forward;
        Vector3 rightRayDir = Quaternion.Euler(0f, whiskerSpreadAngle, 0f) * transform.forward;

        // Obstacle Raycasts (Giant Props / Barriers)
        if (Physics.Raycast(origin, leftRayDir, whiskerLength, obstacleLayer))
            steerAdjustment += 0.85f; // Steer Right
        if (Physics.Raycast(origin, rightRayDir, whiskerLength, obstacleLayer))
            steerAdjustment -= 0.85f; // Steer Left

        // Table-Edge Dropoff Detection (Cast downward ahead of the car's left & right corners)
        Vector3 leftEdgeProbe = origin + leftRayDir * 4.5f;
        Vector3 rightEdgeProbe = origin + rightRayDir * 4.5f;

        if (!Physics.Raycast(leftEdgeProbe, Vector3.down, 2.5f, groundLayer))
            steerAdjustment += 1.0f; // Abyss on left! Steer hard right!
        if (!Physics.Raycast(rightEdgeProbe, Vector3.down, 2.5f, groundLayer))
            steerAdjustment -= 1.0f; // Abyss on right! Steer hard left!

        return Mathf.Clamp(steerAdjustment, -1f, 1f);
    }

    private void HandleStuckRecovery()
    {
        if (driftController.sphereRb.linearVelocity.magnitude < 1.8f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 1.35f)
            {
                reverseRecoveryTimer = 0.85f;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void UpdateRubberbanding()
    {
        if (!enableArcadeRubberbanding || playerTarget == null) return;

        float signedDistToPlayer = Vector3.Dot(transform.forward, playerTarget.position - transform.position);
        // If the player is far ahead (+signedDist), give AI a 7% top speed bump; if player is far behind, ease off by 6%
        if (signedDistToPlayer > 18f)
            driftController.maxSpeed = baseMaxSpeed * 1.07f;
        else if (signedDistToPlayer < -18f)
            driftController.maxSpeed = baseMaxSpeed * 0.94f;
        else
            driftController.maxSpeed = baseMaxSpeed;
    }

    private void HandleAIWeaponFiring()
    {
        weaponFireCooldown -= Time.deltaTime;
        if (weaponFireCooldown <= 0f)
        {
            onAIFireWeapon?.Invoke();
            weaponFireCooldown = (personality == AIPersonality.ChaosTrickster)
                ? Random.Range(2.5f, 4.5f)
                : Random.Range(4.5f, 8.0f);
        }
    }
}
```

---

## 4. Complete C# Script: `KnockbackCollisionHandler.cs`

Attach this script directly to the **`PhysicsSphere`** GameObject of **both the Player Car and AI Cars**. It detects high-speed car-to-car collisions, rewards the faster/boosting car with an exaggerated arcade knockback impulse to send rivals flying off the workbench, and triggers camera shake + sparks!

```csharp
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class KnockbackCollisionHandler : MonoBehaviour
{
    [Header("Owner Reference")]
    [Tooltip("Assign the sibling VisualCarRoot's ArcadeDriftController here.")]
    public ArcadeDriftController ownerCarController;
    public FoldableCameraAdapter playerCameraShake;

    [Header("Arcade Knockback Tuning")]
    [Tooltip("Base multiplier for car-on-car collision knockback.")]
    public float knockbackForceMultiplier = 1.65f;
    [Tooltip("Minimum impact speed required to trigger heavy knockback & sparks.")]
    public float minImpactVelocity = 7.5f;
    [Tooltip("Slight upward pop on heavy impacts so cars hop over table lip bevels.")]
    public float upwardPopForce = 3.2f;

    [Header("VFX & Audio Hooks")]
    [Tooltip("Particle system spawned or played at the exact contact point.")]
    public ParticleSystem clashSparksParticles;
    public UnityEvent onHeavyImpact;

    private Rigidbody mySphereRb;
    private float impactCooldown;

    private void Awake()
    {
        mySphereRb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (impactCooldown > 0f)
        {
            impactCooldown -= Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (impactCooldown > 0f) return;

        // Check if we collided with another car's PhysicsSphere
        KnockbackCollisionHandler rivalSphere = collision.gameObject.GetComponent<KnockbackCollisionHandler>();
        if (rivalSphere != null && rivalSphere.mySphereRb != null)
        {
            float relativeSpeed = collision.relativeVelocity.magnitude;
            if (relativeSpeed < minImpactVelocity) return;

            impactCooldown = 0.18f;

            // Calculate horizontal push direction away from our sphere toward the rival sphere
            Vector3 pushDir = (rivalSphere.transform.position - transform.position);
            pushDir.y = 0f;
            pushDir.Normalize();

            // Determine who had higher momentum at impact
            float myMomentum = mySphereRb.linearVelocity.magnitude * mySphereRb.mass;
            float rivalMomentum = rivalSphere.mySphereRb.linearVelocity.magnitude * rivalSphere.mySphereRb.mass;

            // The faster/heavier aggressor delivers a massive ring-out impulse to the defender
            if (myMomentum >= rivalMomentum)
            {
                float finalImpulse = relativeSpeed * knockbackForceMultiplier;
                Vector3 impulseVector = (pushDir * finalImpulse) + (Vector3.up * upwardPopForce);
                rivalSphere.mySphereRb.AddForce(impulseVector, ForceMode.VelocityChange);
            }

            // Spawn clash sparks at contact point
            if (clashSparksParticles != null && collision.contactCount > 0)
            {
                clashSparksParticles.transform.position = collision.GetContact(0).point;
                clashSparksParticles.Play();
            }

            // Trigger screen shake if connected to the player's camera
            if (playerCameraShake != null)
            {
                float shakeAmount = Mathf.Clamp(relativeSpeed * 0.025f, 0.15f, 0.65f);
                playerCameraShake.TriggerCameraShake(shakeAmount);
            }

            onHeavyImpact?.Invoke();
        }
    }
}
```

---

## 5. Quick Setup Steps in Unity (3 Minutes)

1. **Build Your First Racing Line:**
   * Create an empty GameObject named `TrackWaypoints_Container` and attach `TrackWaypointCircuit.cs`.
   * Create 8–12 empty child GameObjects (`WP_01`, `WP_02`, etc.) and place them around your workbench obstacles. You will immediately see a glowing cyan racing loop in the Scene view!
2. **Duplicate Your Player Car to Create 3 AI Rivals:**
   * Duplicate `PlayerCar_Container` three times (`AICar_Scrapper`, `AICar_VoltDozer`, `AICar_NeonViper`).
   * On each AI car's `VisualCarRoot`, **remove** `CrossPlatformInputManager` and **add** `AIRivalDriftBrain.cs`.
   * Drag `TrackWaypoints_Container` into the `Track Circuit` slot on each AI car, and set their `AIPersonality` dropdowns (`PrecisionDrifter`, `WorkbenchBrawler`, `ChaosTrickster`).
3. **Add Knockback Physics:**
   * Select the `PhysicsSphere` inside your Player Car and all 3 AI Cars, and attach `KnockbackCollisionHandler.cs`.
   * Hit **Play**—you now have a full 4-car micro-scale drift battle running on PC and ready to deploy to your Pixel 10 Pro Fold!