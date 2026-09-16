using System;
using System.Collections;
using UnityEngine;

public class TrajectoryInput : MonoBehaviour
{
    [Header("Refs")]
    public BallFlight ball;
    public LineRenderer line;
    public Transform triangleMarker;
    public TurnManager turnManager;

    [Header("Shot Range/Height Base & Max (max reached at screen edge)")]
    public float baseRange = -4f;
    public float baseHeight = 2f;

    [Header("Speed")]
    public float maxSpeed = 14f;
    public float gravity = 9.8f;

    [Header("Idle Rotation (visual only, before launch)")]
    public float idleRotationSpeed = 60f;

    [Header("First-Shot Intro Animation")]
    public float introDuration = 1f;

    [HideInInspector] public bool inputEnabled = true;
    public event Action OnBallLaunched;

    private bool aimingEnabled;
    private bool wasInputEnabled;
    private float currentRange;
    private float currentHeight;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (line == null) line = GetComponent<LineRenderer>();
        if (triangleMarker != null) triangleMarker.gameObject.SetActive(false);
    }

    void Update()
    {
        // dummy idle rotation while the ball waits to be launched — stops the instant Launch() fires
        if (!ball.IsInFlight())
            ball.transform.Rotate(Vector3.forward, idleRotationSpeed * Time.deltaTime);

        // detect the moment this player's turn to aim begins
        if (inputEnabled && !wasInputEnabled)
            BeginShotSequence();
        wasInputEnabled = inputEnabled;

        if (!inputEnabled || ball.IsInFlight() || !aimingEnabled) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        UpdateAim(mouseWorld);

        if (Input.GetMouseButtonDown(0) && mouseWorld.x < ball.transform.position.x)
            LaunchShot();
    }

    void BeginShotSequence()
    {
        aimingEnabled = false;
        StartCoroutine(PlayIntroThenEnableAiming());
    }

    IEnumerator PlayIntroThenEnableAiming()
    {
        line.enabled = true;
        float elapsed = 0f;

        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;
            float growT = Mathf.Clamp01(elapsed / introDuration);
            Vector3 ballPos = ball.transform.position;

            // Current mouse position every frame
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

            // Default/minimum trajectory midpoint
            float defaultTriangleX = ballPos.x + baseRange * 0.5f;
            float defaultTriangleY = ballPos.y + baseHeight;

            // Screen limits - ONLY for triangle/control point
            Vector3 leftScreenWorld = cam.ScreenToWorldPoint(new Vector3(0f, Input.mousePosition.y, 0f));
            Vector3 topScreenWorld = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Screen.height, 0f));

            float leftLimitX = leftScreenWorld.x;
            float topLimitY = topScreenWorld.y;

            float triangleX;

            if (mouseWorld.x < defaultTriangleX)
                triangleX = Mathf.Max(mouseWorld.x, leftLimitX);
            else
                triangleX = defaultTriangleX;

            float triangleY;

            if (mouseWorld.y > defaultTriangleY)
                triangleY = Mathf.Min(mouseWorld.y, topLimitY);
            else
                triangleY = defaultTriangleY;

            if (triangleMarker != null)
            {
                triangleMarker.position = new Vector3(triangleX, triangleY, triangleMarker.position.z);
            }

            currentRange = (triangleX - ballPos.x) * 2f;
            currentHeight = triangleY - ballPos.y;

            Vector2[] fullArc = BallFlight.SampleArc(ballPos, currentRange, currentHeight, gravity, maxSpeed, 20);
            int targetCount = Mathf.Max(2, Mathf.RoundToInt(fullArc.Length * GetVisibilityPercent()));

            int count = Mathf.Max(2, Mathf.RoundToInt(targetCount * growT));
            line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                line.SetPosition(i, fullArc[i]);
            }
            if (growT >= 1f && Mathf.Approximately(GetVisibilityPercent(), 0.5f) && triangleMarker != null)
            {
                line.SetPosition(line.positionCount - 1, triangleMarker.position);
            }

            yield return null;
        }
        
        if (triangleMarker != null)
            triangleMarker.gameObject.SetActive(true);

        aimingEnabled = true;
    }

    void UpdateAim(Vector3 mouseWorld)
    {
        Vector3 ballPos = ball.transform.position;

        // Default/minimum trajectory midpoint
        float defaultTriangleX = ballPos.x + baseRange * 0.5f;
        float defaultTriangleY = ballPos.y + baseHeight;

        // Screen limits ONLY for the triangle/mouse control point
        Vector3 leftScreenWorld = cam.ScreenToWorldPoint(new Vector3(0f, Input.mousePosition.y, 0f));
        Vector3 topScreenWorld = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Screen.height, 0f));

        float leftLimitX = leftScreenWorld.x;
        float topLimitY = topScreenWorld.y;

        float triangleX;

        if (mouseWorld.x < defaultTriangleX)
            triangleX = Mathf.Max(mouseWorld.x, leftLimitX);
        else
            triangleX = defaultTriangleX;

        float triangleY;

        if (mouseWorld.y > defaultTriangleY)
            triangleY = Mathf.Min(mouseWorld.y, topLimitY);
        else
            triangleY = defaultTriangleY;

        // =========================================================
        // TRIANGLE = EXACT CONTROL POINT
        // =========================================================

        if (triangleMarker != null)
        {
            triangleMarker.position = new Vector3(triangleX, triangleY, triangleMarker.position.z);
        }

        currentRange = (triangleX - ballPos.x) * 2f;
        currentHeight = triangleY - ballPos.y;

        Vector2[] pts = BallFlight.SampleArc(ballPos, currentRange, currentHeight, gravity, maxSpeed, 20);

        int visibleCount = Mathf.Max(2, Mathf.RoundToInt(pts.Length * GetVisibilityPercent()));
        line.positionCount = visibleCount;

        for (int i = 0; i < visibleCount; i++)
        {
            line.SetPosition(i, pts[i]);
        }

        if (Mathf.Approximately(GetVisibilityPercent(), 0.5f) && triangleMarker != null)
        {
            line.SetPosition(line.positionCount - 1, triangleMarker.position);
        }
    }

    void LaunchShot()
    {
        ball.Launch(currentRange, currentHeight, gravity, maxSpeed);
        OnBallLaunched?.Invoke();
        aimingEnabled = false;
        line.enabled = false;
        line.positionCount = 0;
        if (triangleMarker != null) triangleMarker.gameObject.SetActive(false);
    }

    float GetVisibilityPercent()
    {
        if (BracketManager.Instance == null) return 1f;

        if (BracketManager.Instance.currentBracketRound == BracketRound.Round16)
        {
            int shotIndex = turnManager.playerShotsTaken;
            if (shotIndex <= 2) return 1f;
            if (shotIndex == 3) return 0.75f;
            return 0.5f;
        }

        return 0.5f;
    }

}