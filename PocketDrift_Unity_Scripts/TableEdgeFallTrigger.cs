using System.Collections;
using UnityEngine;

public class TableEdgeFallTrigger : MonoBehaviour
{
    [Header("Fall & Respawn Settings")]
    public float respawnDelay = 1.2f;
    public float respawnHeightAboveTrack = 1.5f;

    private void OnTriggerEnter(Collider other)
    {
        ArcadeDriftController car = other.GetComponentInParent<ArcadeDriftController>();
        if (car != null)
        {
            RespawnCar(car);
        }
    }

    public void RespawnCar(ArcadeDriftController car)
    {
        if (car != null)
        {
            StartCoroutine(RespawnCarRoutine(car));
        }
    }

    private IEnumerator RespawnCarRoutine(ArcadeDriftController car)
    {
        // Zero out velocities and stun temporarily
        if (car.sphereRb != null)
        {
            car.sphereRb.linearVelocity = Vector3.zero;
            car.sphereRb.angularVelocity = Vector3.zero;
            car.sphereRb.isKinematic = true;
        }

        yield return new WaitForSeconds(respawnDelay);

        // Find nearest waypoint or track center to drop car back
        TrackWaypointCircuit circuit = FindAnyObjectByType<TrackWaypointCircuit>();
        Vector3 respawnPos = Vector3.zero;
        Quaternion respawnRot = Quaternion.identity;

        if (circuit != null && circuit.waypoints.Count > 0)
        {
            Transform closestWp = circuit.GetWaypoint(0);
            float closestDist = float.MaxValue;
            foreach (var wp in circuit.waypoints)
            {
                float d = Vector3.Distance(car.transform.position, wp.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestWp = wp;
                }
            }
            respawnPos = closestWp.position + Vector3.up * respawnHeightAboveTrack;
            respawnRot = closestWp.rotation;
        }
        else
        {
            respawnPos = new Vector3(0f, respawnHeightAboveTrack, 0f);
        }

        car.transform.position = respawnPos;
        car.transform.rotation = respawnRot;

        if (car.sphereRb != null)
        {
            car.sphereRb.transform.position = respawnPos + new Vector3(0f, 0.35f, 0f);
            car.sphereRb.isKinematic = false;
            car.sphereRb.linearVelocity = respawnRot * Vector3.forward * 4f; // Small push start
        }
    }
}
