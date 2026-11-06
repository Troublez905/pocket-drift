using System.Collections.Generic;
using UnityEngine;

public class TrackWaypointCircuit : MonoBehaviour
{
    [Header("Circuit Waypoints (Auto-Populated from Child Transforms if Empty)")]
    public List<Transform> waypoints = new List<Transform>();
    public float waypointReachRadius = 4.0f;

    private void Awake()
    {
        RefreshWaypointsFromChildren();
    }

    public void RefreshWaypointsFromChildren()
    {
        if (waypoints.Count == 0)
        {
            foreach (Transform child in transform)
            {
                waypoints.Add(child);
            }
        }
    }

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Count == 0) RefreshWaypointsFromChildren();
        if (waypoints.Count == 0) return transform;
        int safeIdx = ((index % waypoints.Count) + waypoints.Count) % waypoints.Count;
        return waypoints[safeIdx];
    }

    public float GetUpcomingTurnAngle(int currentIndex)
    {
        if (waypoints.Count < 3) return 0f;
        Transform prev = GetWaypoint(currentIndex - 1);
        Transform curr = GetWaypoint(currentIndex);
        Transform next = GetWaypoint(currentIndex + 1);

        Vector3 inVector = (curr.position - prev.position).normalized;
        Vector3 outVector = (next.position - curr.position).normalized;
        inVector.y = 0f;
        outVector.y = 0f;
        return Vector3.Angle(inVector, outVector);
    }

    private void OnDrawGizmos()
    {
        List<Transform> nodes = new List<Transform>();
        foreach (Transform child in transform) nodes.Add(child);
        if (nodes.Count < 2) return;

        Gizmos.color = new Color(0f, 0.95f, 1f, 0.85f);
        for (int i = 0; i < nodes.Count; i++)
        {
            Transform a = nodes[i];
            Transform b = nodes[(i + 1) % nodes.Count];
            if (a != null && b != null)
            {
                Gizmos.DrawWireSphere(a.position, 0.55f);
                Gizmos.DrawLine(a.position, b.position);
            }
        }
    }
}
