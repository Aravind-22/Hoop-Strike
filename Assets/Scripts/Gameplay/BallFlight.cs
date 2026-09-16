using System;
using UnityEngine;

public class BallFlight : MonoBehaviour
{
    [Header("Ball")]
    public float ballRadius = 0.25f;
    private float colliderRadius = 0.51f;

    [Header("Layer Masks (each surface on its own layer)")]
    public LayerMask rimMask;
    public LayerMask backboardMask;
    public LayerMask verticalStandMask;
    public LayerMask floorMask;
    public LayerMask scoreZoneMask;

    private LayerMask allCollidableMask;
    private ContactFilter2D collidableFilter;
    private ContactFilter2D scoreZoneFilter;
    private readonly Collider2D[] overlapResults = new Collider2D[4];
    private readonly Collider2D[] scoreOverlapResults = new Collider2D[2];
    private readonly Collider2D[] multiOverlapResults = new Collider2D[4];

    [Header("Rebound Tuning")]
    public float minReboundSpeed = 1.5f;

    [Header("Bounce Limits (separate so floor doesn't bounce forever)")]
    public int maxRimBounces = 9;
    public int maxFloorBounces = 2;
    [Range(0f, 1f)] public float rimDamping = 0.55f;
    [Range(0f, 1f)]public float backboardDamping = 0.6f;
    [Range(0f, 1f)]public float floorDamping = 0.7f;
    [Range(0f, 1f)] public float verticalDamping = 0.75f;

    [Header("Rotation")]
    public float rotationDirectionSign = -1f;

    [Header("Substep Safety (prevents tunneling through thin colliders)")]
    public float maxStepDistance = 0.1f;

    [Header("Playback Speed")]
    public float speedMultiplier = 1.6f;

    [Header("Net Fall Constraint")]
    public float netEdgeMargin = 0.05f;

    [Header("Anti-Stuck")]
    public float minHorizontalAfterBounce = 0.4f;

    [Header("Manual Position/Velocity Nudge Per Surface")]
    public float rimHorizontalNudge = 0.4f;       // reduce this from current value
    public float verticalStandHorizontalNudge = 1.2f; // increase this from current value

    [Header("Safety")]
    [Tooltip("If the ball somehow clears everything and falls below this Y, force-end the shot instead of flying forever.")]
    public float killY = -10f;

    public event Action<bool> OnShotResolved; // bool = scored

    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;
    private Vector2 velocity;
    private float gravity;
    private bool inFlight;
    private bool hasScoredThisShot;
    private int rimBounceCount;
    private int floorBounceCount;

    private Collider2D lastHitCollider;
    private float lastHitFrameTime;
    private const float hitCooldown = 0.05f;

    private Vector2 prevPos;

    private bool inNetFall;
    private float netMinX, netMaxX, netBottomY;

    public NetRippleEffect netRipple;
    private GameObject activeShadow;

    [Header("Sound/VFX Throttling")]
    public float sfxCooldown = 0.12f; // separate from physics hitCooldown, prevents rapid-fire audio spam
    private float lastSfxTime = -999f;

    [Header("Rim Stuck Prevention")]
    public int rimStuckThreshold = 4; // if this many rim bounces happen without clearing, force resolution
    public float forcedEscapeVelocityX = 2f;

    [Header("Debug")]
    public BallTrajectoryGizmo trajectoryGizmo;

    [Header("Net Exit")]
    public float netExitVelocityRestoreSpeed = 4f;
    private float storedNetVelocityX;
    private bool restoringNetVelocityX;
    private float scoreLineY;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();

        allCollidableMask = rimMask | backboardMask | verticalStandMask | floorMask;

        collidableFilter = new ContactFilter2D();
        collidableFilter.SetLayerMask(allCollidableMask);
        collidableFilter.useTriggers = false;

        scoreZoneFilter = new ContactFilter2D();
        scoreZoneFilter.SetLayerMask(scoreZoneMask);
        scoreZoneFilter.useTriggers = true;

        if (trajectoryGizmo == null)
            trajectoryGizmo = GetComponent<BallTrajectoryGizmo>();
    }

    public void Launch(float range, float height, float g, float maxSpeed)
    {
        gravity = g;
        var (v, _) = SolveVelocity(range, height, g, maxSpeed);
        velocity = v;
        inFlight = true;
        hasScoredThisShot = false;
        rimBounceCount = 0;
        floorBounceCount = 0;
        lastHitCollider = null; 
        prevPos = transform.position;
        inNetFall = false;
        spriteRenderer.enabled = true;
        trailRenderer.enabled = true;
        activeShadow = VFXManager.Instance.ShowFloorShadow(transform.position);
        if (trajectoryGizmo != null)
        {
            trajectoryGizmo.BeginTrajectory(transform.position);
        }
        restoringNetVelocityX = false;
        storedNetVelocityX = 0f;
    }

    public void ResetForNextShot(Vector3 startPos)
    {
        transform.position = startPos;
        transform.rotation = Quaternion.identity;
        velocity = Vector2.zero;
        inFlight = false;
        hasScoredThisShot = false;
        rimBounceCount = 0;
        floorBounceCount = 0;
        lastHitCollider = null;
        inNetFall = false;
        spriteRenderer.enabled = true;
    }

    public static (Vector2 velocity, float gUsed) SolveVelocity(float range, float height, float gravity, float maxSpeed)
    {
        float tUp = Mathf.Sqrt(2f * height / gravity);
        float totalTime = 2f * tUp;

        float vx = range / totalTime;
        float vy0 = gravity * tUp;

        Vector2 v = new Vector2(vx, vy0);
        return (v, gravity);
    }

    public static Vector2[] SampleArc(Vector2 start, float range, float height, float gravity, float maxSpeed, int steps)
    {
        var (v, g) = SolveVelocity(range, height, gravity, maxSpeed);
        float totalTime = 2f * v.y / g;
        Vector2[] pts = new Vector2[steps];
        for (int i = 0; i < steps; i++)
        {
            float t = (i / (float)(steps - 1)) * totalTime;
            float x = start.x + v.x * t;
            float y = start.y + v.y * t - 0.5f * g * t * t;
            pts[i] = new Vector2(x, y);
        }
        return pts;
    }

    void Update()
    {
        if (!inFlight) return;

        float dt = Time.deltaTime * speedMultiplier;
        Vector2 acceleration = Vector2.down * gravity;

        Vector2 frameDisplacement = velocity * dt + 0.5f * acceleration * dt * dt;
        float frameDistance = frameDisplacement.magnitude;
        int steps = Mathf.Max(1, Mathf.CeilToInt(frameDistance / maxStepDistance));
        float stepDt = dt / steps;
        Debug.Log(" step count : "+steps);
        for (int i = 0; i < steps; i++)
        {
            Debug.Log("no of steps : "+i);
            if (!inFlight) break;

            if (restoringNetVelocityX && !inNetFall)
            {
                velocity.x = Mathf.MoveTowards(velocity.x, storedNetVelocityX, netExitVelocityRestoreSpeed * stepDt);

                if (Mathf.Approximately(velocity.x, storedNetVelocityX))
                {
                    velocity.x = storedNetVelocityX;
                    restoringNetVelocityX = false;
                }
            }

            Vector2 displacement = velocity * stepDt + 0.5f * acceleration * stepDt * stepDt;
            Vector2 stepPos = (Vector2)transform.position + displacement;

            velocity += acceleration * stepDt;

            if (activeShadow != null)
            {
                activeShadow.transform.position = new Vector3(stepPos.x, -3.9f, 0f);

                float xScale = Mathf.Lerp(0.8f, 0.0f, Mathf.InverseLerp(-3.9f, 1f, stepPos.y));

                Vector3 scale = activeShadow.transform.localScale;
                scale.x = xScale;
                activeShadow.transform.localScale = scale;
            }

            if (inNetFall && stepPos.y <= netBottomY)
            {
                inNetFall = false;
                velocity.x = 0f;
                restoringNetVelocityX = true;
            }
                
            if (inNetFall)
            {
                stepPos.x = Mathf.Clamp(stepPos.x, netMinX + netEdgeMargin + ballRadius, netMaxX - netEdgeMargin - ballRadius);
                velocity.x = 0f;
            }

            RotateBall(displacement.x);

            CheckScoreZone(stepPos);
            transform.position = stepPos;
            CheckCollision(stepPos);

            if (trajectoryGizmo != null)
            {
                trajectoryGizmo.AddPoint(transform.position);
            }
            prevPos = stepPos;

            if (inFlight && stepPos.y < killY)
            {
                EndFlight();
                break;
            }
        }
    }

    void RotateBall(float dist)
    {
        float angularSpeedDeg = (dist / colliderRadius) * Mathf.Rad2Deg;
        transform.Rotate(Vector3.forward, rotationDirectionSign * angularSpeedDeg);
    }

    void CheckCollision(Vector2 pos)
    {
        int hitCount = Physics2D.OverlapCircle(pos, ballRadius, collidableFilter, overlapResults);
        if (hitCount == 0) return;

        int multiHitCount = Physics2D.OverlapCircle(pos, ballRadius * 1.1f, collidableFilter, multiOverlapResults);
        if (multiHitCount >= 2)
        {
            ResolvePinchedBall(pos, multiOverlapResults, multiHitCount);
            return;
        }

        Collider2D hit = overlapResults[0]; // first overlap this frame; fine since substepping keeps overlaps rare/singular per step
        if (hit == lastHitCollider && Time.time - lastHitFrameTime < hitCooldown) return;

        restoringNetVelocityX = false;

        int hitLayer = 1 << hit.gameObject.layer;
        bool isFloor = (hitLayer & floorMask) != 0;
        bool isBackboard = (hitLayer & backboardMask) != 0;
        bool isVerticalStand = (hitLayer & verticalStandMask) != 0;
        bool isRim = (hitLayer & rimMask) != 0;

        bool canPlaySfx = Time.time - lastSfxTime >= sfxCooldown;

        float damping;
        if (isFloor)
        {
            damping = floorDamping;
            AudioManager.Instance.PlayCourtBounce();
        }
        else if (isBackboard)
        {
            damping = backboardDamping;
            AudioManager.Instance.PlayBackboardHit();
            VFXManager.Instance.PlayImpactSpark(new Vector3(pos.x - 0.5f, pos.y - 0.1f, 0f));
        }
        else if (isVerticalStand)
        {
            damping = verticalDamping;
            AudioManager.Instance.PlayBackboardHit(); // reuse or assign a dedicated clip if you have one
        }
        else // isRim
        {
            damping = rimDamping;
            if (canPlaySfx)
            {
                AudioManager.Instance.PlayRimHit();
                VFXManager.Instance.PlayImpactSpark(new Vector3(pos.x - 0.3f, pos.y - 0.4f, 0f));
                lastSfxTime = Time.time;
            }
        }

        Vector2 closest = hit.ClosestPoint(pos);
        Vector2 normal = (pos - closest);
        if (normal.sqrMagnitude < 0.0001f) normal = Vector2.up;
        normal.Normalize();

        if (isVerticalStand)
        {
            normal = Vector2.right;
        }

        Vector2 correctedPos = closest + normal * ballRadius;
        float maxCorrectionDistance = ballRadius * 2f; // never snap further than roughly 2 ball-widths in one resolve
        if (Vector2.Distance(pos, correctedPos) > maxCorrectionDistance)
            correctedPos = pos + (correctedPos - pos).normalized * maxCorrectionDistance;

        transform.position = correctedPos;

        Vector2 surfaceVelocity = Vector2.zero;
        MovingSurfaceVelocity mover = hit.GetComponent<MovingSurfaceVelocity>();
        if (mover == null) mover = hit.GetComponentInParent<MovingSurfaceVelocity>();
        if (mover != null) surfaceVelocity = mover.Velocity;

        Vector2 relativeVelocity = velocity - surfaceVelocity;
        Vector2 reflectedRelative = Vector2.Reflect(relativeVelocity, normal) * damping;
        velocity = reflectedRelative + surfaceVelocity;

        if (isVerticalStand)
        {
            velocity.x = Mathf.Max(Mathf.Abs(velocity.x), verticalStandHorizontalNudge);
        }
        else if (isRim && Mathf.Abs(velocity.x) < rimHorizontalNudge)
        {
            float pushDir = normal.x != 0f ? Mathf.Sign(normal.x) : Mathf.Sign(velocity.x != 0f ? velocity.x : 1f);
            velocity.x = pushDir * rimHorizontalNudge;
        }

        lastHitCollider = hit;
        lastHitFrameTime = Time.time;

        if (isFloor)
        {
            floorBounceCount++;
            if (floorBounceCount >= maxFloorBounces - 1)
            {
                velocity = Vector2.zero;
                EndFlight();
                return;
            }
        }
        else
        {
            rimBounceCount++;

            float escapeDir = normal.x != 0f ? Mathf.Sign(normal.x) : Mathf.Sign(velocity.x != 0f ? velocity.x : 1f);

            if (rimBounceCount >= rimStuckThreshold && Mathf.Abs(velocity.x) < rimHorizontalNudge * 1.5f)
            {
                velocity.x = escapeDir * forcedEscapeVelocityX;
                velocity.y = Mathf.Min(velocity.y, -1f);
            }

            if (rimBounceCount >= maxRimBounces)
            {
                velocity.y = Mathf.Min(velocity.y, -2f);
                velocity.x = escapeDir * Mathf.Max(Mathf.Abs(velocity.x), forcedEscapeVelocityX);
                rimBounceCount = maxRimBounces - 1;
            }
        }

        if (velocity.magnitude < minReboundSpeed && velocity != Vector2.zero)
            velocity = normal * minReboundSpeed;
    }

    void ResolvePinchedBall(Vector2 pos, Collider2D[] hits, int count)
    {
        bool involvesVerticalStand = false;
        Vector2 combinedNormal = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            if (((1 << hits[i].gameObject.layer) & verticalStandMask) != 0)
                involvesVerticalStand = true;

            Vector2 closest = hits[i].ClosestPoint(pos);
            Vector2 n = (pos - closest);
            if (n.sqrMagnitude > 0.0001f)
                combinedNormal += n.normalized;
        }

        if (involvesVerticalStand)
        {
            combinedNormal = Vector2.right; // same fixed rule applies even in a pinch situation
        }
        else if (combinedNormal.sqrMagnitude < 0.0001f)
        {
            combinedNormal = Vector2.up;
        }

        combinedNormal.Normalize();

        transform.position = pos + combinedNormal * ballRadius * 1.5f;
        velocity = combinedNormal * Mathf.Max(minReboundSpeed * 1.5f, velocity.magnitude * 0.5f);

        rimBounceCount++;
    }

    void CheckScoreZone(Vector2 pos)
    {
        int hitCount = Physics2D.OverlapCircle(pos, ballRadius * 0.5f, scoreZoneFilter, scoreOverlapResults);
        if (hitCount == 0) return;

        Collider2D zone = scoreOverlapResults[0];

        if (!inNetFall)
        {
            inNetFall = true;

            storedNetVelocityX = velocity.x;
            restoringNetVelocityX = false;
            
            netMinX = zone.bounds.min.x;
            netMaxX = zone.bounds.max.x;
            netBottomY = -2.75f;
            scoreLineY = zone.bounds.max.y;
        }

        if (hasScoredThisShot) return;

        bool crossedScoreLine = prevPos.y >= scoreLineY && pos.y < scoreLineY;
        bool movingDown = velocity.y < 0f;

        if (crossedScoreLine && movingDown)
        {
            hasScoredThisShot = true;

            AudioManager.Instance.PlayHoopSwish();
            netRipple.PlayRipple();

            VFXManager.Instance.PlayScoreHighlight(pos);
            VFXManager.Instance.PlayFireworks();
        }
    }

    void EndFlight()
    {
        if (trajectoryGizmo != null)
        {
            trajectoryGizmo.EndTrajectory(transform.position);
        }
        inFlight = false;
        inNetFall = false;
        VFXManager.Instance.PlaySmoke(transform.position);
        spriteRenderer.enabled = false;
        trailRenderer.enabled = false;
        OnShotResolved?.Invoke(hasScoredThisShot);
        if (activeShadow != null) { VFXManager.Instance.HideFloorShadow(activeShadow);; activeShadow = null; }
    }

    public bool IsInFlight() => inFlight;
}