using System.Collections.Generic;
using UnityEngine;

public class BallTrajectoryGizmo : MonoBehaviour
{
    [Header("References")]
    public BallFlight ball;

    [Header("Gizmo")]
    public bool drawTrajectory = true;

    [Tooltip("Minimum distance before storing another point")]
    public float pointSpacing = 0.05f;

    private readonly List<Vector3> trajectoryPoints = new List<Vector3>();

    void Awake()
    {
        if (ball == null)
            ball = GetComponent<BallFlight>();
    }

    // Called when a new shot starts
    public void BeginTrajectory(Vector3 startPosition)
    {
        trajectoryPoints.Clear();
        trajectoryPoints.Add(startPosition);
    }

    // Called continuously while the ball moves
    public void AddPoint(Vector3 position)
    {
        if (trajectoryPoints.Count == 0)
        {
            trajectoryPoints.Add(position);
            return;
        }

        Vector3 lastPoint = trajectoryPoints[trajectoryPoints.Count - 1];

        if (Vector3.Distance(lastPoint, position) >= pointSpacing)
        {
            trajectoryPoints.Add(position);
        }
    }

    // Optional: keeps the final point exact
    public void EndTrajectory(Vector3 finalPosition)
    {
        if (trajectoryPoints.Count == 0 ||
            trajectoryPoints[trajectoryPoints.Count - 1] != finalPosition)
        {
            trajectoryPoints.Add(finalPosition);
        }
    }

    void OnDrawGizmos()
    {
        if (!drawTrajectory || trajectoryPoints == null || trajectoryPoints.Count < 2)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < trajectoryPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(
                trajectoryPoints[i],
                trajectoryPoints[i + 1]
            );
        }
    }

    public void ClearTrajectory()
    {
        trajectoryPoints.Clear();
    }
}