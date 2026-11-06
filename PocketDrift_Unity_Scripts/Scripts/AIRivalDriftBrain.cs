using UnityEngine;

public class AIRivalDriftBrain : MonoBehaviour
{
    public enum AIPersonality
    {
        PrecisionDrifter,
        WorkbenchBrawler,
        ChaosTrickster
    }

    [Header("Personality & Tuning")]
    public AIPersonality personality = AIPersonality.PrecisionDrifter;

    [Header("References")]
    public ArcadeDriftController carController;
    public TrackWaypointCircuit trackCircuit;

    [Header("AI Driving & Drift Behavior")]
    public float waypointAdvanceDistance = 4.2f;
    public float driftCornerAngleThreshold = 28f;
    public float steerSensitivity = 2.2f;

    [Header("Table Edge Hazard Avoidance")]
    public float edgeLookAheadDistance = 5.5f;
    public LayerMask groundLayer = ~0;

    public int CurrentWaypointIndex { get; private set; }
    public int CompletedLaps { get; private set; }

    private void Awake()
    {
        if (carController == null) carController = GetComponent<ArcadeDriftController>();
        if (trackCircuit == null) trackCircuit = FindFirstObjectByType<TrackWaypointCircuit>();
    }

    private void Update()
    {
        if (carController == null || trackCircuit == null) return;

        Transform targetWP = trackCircuit.GetWaypoint(CurrentWaypointIndex);
        if (targetWP == null) return;

        Vector3 toWaypoint = targetWP.position - transform.position;
        toWaypoint.y = 0f;

        if (toWaypoint.magnitude <= waypointAdvanceDistance)
        {
            CurrentWaypointIndex++;
            if (trackCircuit.waypoints.Count > 0 && CurrentWaypointIndex >= trackCircuit.waypoints.Count)
            {
                CurrentWaypointIndex = 0;
                CompletedLaps++;
            }
            targetWP = trackCircuit.GetWaypoint(CurrentWaypointIndex);
            toWaypoint = targetWP.position - transform.position;
            toWaypoint.y = 0f;
        }

        // Compute normalized steering toward target waypoint
        Vector3 localTarget = transform.InverseTransformPoint(targetWP.position);
        float steer = Mathf.Clamp((localTarget.x / Mathf.Max(1f, localTarget.magnitude)) * steerSensitivity, -1f, 1f);
        float throttle = 1f;

        // Check upcoming corner sharpness to trigger autonomous power-sliding
        float cornerAngle = trackCircuit.GetUpcomingTurnAngle(CurrentWaypointIndex);
        bool wantDrift = cornerAngle >= driftCornerAngleThreshold && Mathf.Abs(steer) > 0.32f && carController.CurrentSpeed > 7f;

        // Release drift once Tier 2 boost is charged and car has cleared the apex
        if (carController.IsDrifting && carController.CurrentBoostTier >= 2 && Mathf.Abs(steer) < 0.25f)
        {
            wantDrift = false;
        }

        // Table Edge Drop-Off Whisker Check
        Vector3 whiskerOrigin = transform.position + transform.forward * edgeLookAheadDistance + Vector3.up * 0.5f;
        if (!Physics.Raycast(whiskerOrigin, Vector3.down, 2.5f, groundLayer))
        {
            // Brake and steer sharply inward away from the workbench edge
            throttle = -0.5f;
            wantDrift = false;
            steer = localTarget.x >= 0f ? 1f : -1f;
        }

        carController.SetInputs(throttle, steer, wantDrift, false);
    }
}
