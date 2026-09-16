using System;
using System.Collections;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public BallFlight ball;
    public Transform hoopTarget;

    [Header("Match TrajectoryInput's values for fairness")]
    public float gravity = 9.8f;
    public float maxSpeed = 14f;

    [Header("Difficulty (0 = worst, 1 = perfect)")]
    [Range(0f, 1f)] public float difficulty = 0.5f;
    public float baseAimErrorRange = 1.5f;
    public float baseAimErrorHeight = 1f;
    public float apexMargin = 2f;
    public float reactionDelay = 0.6f;

    public void TakeShot(Action onShotLaunched)
    {
        StartCoroutine(ShootRoutine(onShotLaunched));
    }

    IEnumerator ShootRoutine(Action onShotLaunched)
    {
        yield return new WaitForSeconds(reactionDelay);

        float idealRange = hoopTarget.position.x - ball.transform.position.x; // signed, keeps shot direction correct
        float idealHeight = Mathf.Abs(hoopTarget.position.y - ball.transform.position.y) + apexMargin;

        float errorScale = 1f - Mathf.Clamp01(difficulty);
        float rangeError = UnityEngine.Random.Range(-baseAimErrorRange, baseAimErrorRange) * errorScale;
        float heightError = UnityEngine.Random.Range(-baseAimErrorHeight, baseAimErrorHeight) * errorScale;

        float shotRange = idealRange + rangeError;
        float shotHeight = Mathf.Max(0.5f, idealHeight + heightError);

        ball.Launch(shotRange, shotHeight, gravity, maxSpeed);
        onShotLaunched?.Invoke();
    }
}