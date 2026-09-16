// MovingSurfaceVelocity.cs — attach to Backboard, Rim edges, or HoopRig itself (whichever object actually tweens)
using UnityEngine;

public class MovingSurfaceVelocity : MonoBehaviour
{
    public Vector2 Velocity { get; private set; }
    private Vector3 lastPos;

    void Awake() => lastPos = transform.position;

    void LateUpdate()
    {
        Velocity = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;
    }
}