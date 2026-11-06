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

    private Dictionary<ArcadeDriftController, Vector3> spawnPositions = new Dictionary<ArcadeDriftController, Vector3>();
    private Dictionary<ArcadeDriftController, Quaternion> spawnRotations = new Dictionary<ArcadeDriftController, Quaternion>();

    private void Awake()
    {
        Instance = this;
        if (circuit == null) circuit = FindAnyObjectByType<TrackWaypointCircuit>();
        if (racers.Count == 0)
        {
            racers.AddRange(FindObjectsByType<ArcadeDriftController>(FindObjectsInactive.Exclude));
        }

        foreach (var r in racers)
        {
            if (r != null)
            {
                spawnPositions[r] = r.transform.position;
                spawnRotations[r] = r.transform.rotation;
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
            }
        }
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
