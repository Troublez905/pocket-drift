using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Race Configuration")]
    public int totalLaps = 3;
    public float fallOutOfBoundsY = -4.5f;
    public TrackWaypointCircuit circuit;
    public List<ArcadeDriftController> racers = new List<ArcadeDriftController>();
    public event System.Action<ArcadeDriftController, int> LapCompleted;
    public event System.Action<ArcadeDriftController> RaceFinished;
    private readonly Dictionary<ArcadeDriftController, int> nextCheckpoints = new Dictionary<ArcadeDriftController, int>();
    private readonly Dictionary<ArcadeDriftController, int> completedLaps = new Dictionary<ArcadeDriftController, int>();

    public int GetCompletedLaps(ArcadeDriftController racer) => completedLaps.TryGetValue(racer, out int laps) ? laps : 0;

    private Dictionary<ArcadeDriftController, Vector3> spawnPositions = new Dictionary<ArcadeDriftController, Vector3>();
    private Dictionary<ArcadeDriftController, Quaternion> spawnRotations = new Dictionary<ArcadeDriftController, Quaternion>();

    private void Awake()
    {
        Instance = this;
        if (circuit == null) circuit = FindAnyObjectByType<TrackWaypointCircuit>();
        if (racers.Count == 0)
        {
            racers.AddRange(FindObjectsByType<ArcadeDriftController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        }

        foreach (var r in racers)
        {
            if (r != null)
            {
                spawnPositions[r] = r.transform.position;
                spawnRotations[r] = r.transform.rotation;
                nextCheckpoints[r] = 0;
                completedLaps[r] = 0;
            }
        }
    }

    private void Update()
    {
        foreach (var r in racers)
        {
            if (r == null) continue;
            if (r.transform.position.y < fallOutOfBoundsY)
            {
                RespawnRacerOnTrack(r);
                continue;
            }
            UpdateLapProgress(r);
        }
    }

    private void UpdateLapProgress(ArcadeDriftController racer)
    {
        if (circuit == null || circuit.waypoints.Count < 3 || !nextCheckpoints.ContainsKey(racer)) return;
        if (completedLaps[racer] >= Mathf.Max(1, totalLaps)) return;
        int next = nextCheckpoints[racer];
        Transform checkpoint = circuit.GetWaypoint(next);
        if (checkpoint == null) return;
        Vector3 position = racer.sphereRb != null ? racer.sphereRb.position : racer.transform.position;
        if (Vector3.Distance(position, checkpoint.position) > circuit.waypointReachRadius) return;

        // The first visit to the start line arms the lap; every checkpoint must follow in order.
        if (next == 0 && armedRacers.Contains(racer))
        {
            completedLaps[racer]++;
            LapCompleted?.Invoke(racer, completedLaps[racer]);
            if (completedLaps[racer] >= Mathf.Max(1, totalLaps)) RaceFinished?.Invoke(racer);
        }
        armedRacers.Add(racer);
        nextCheckpoints[racer] = (next + 1) % circuit.waypoints.Count;
    }

    private readonly HashSet<ArcadeDriftController> armedRacers = new HashSet<ArcadeDriftController>();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RespawnRacerOnTrack(ArcadeDriftController racer)
    {
        Vector3 respawnPos = spawnPositions.ContainsKey(racer) ? spawnPositions[racer] : new Vector3(0f, 0.5f, 0f);
        Quaternion respawnRot = spawnRotations.ContainsKey(racer) ? spawnRotations[racer] : Quaternion.identity;

        if (racer.sphereRb != null)
        {
            racer.sphereRb.linearVelocity = Vector3.zero;
            racer.sphereRb.angularVelocity = Vector3.zero;
            racer.sphereRb.position = respawnPos + new Vector3(0f, 0.4f, 0f);
        }
        racer.transform.SetPositionAndRotation(respawnPos, respawnRot);
    }
}
