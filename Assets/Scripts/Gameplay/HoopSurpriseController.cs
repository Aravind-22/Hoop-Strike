using DG.Tweening;
using UnityEngine;

public class HoopSurpriseController : MonoBehaviour
{
    public Transform backboard;
    public Transform hoopRig; // usually this GameObject itself

    [Header("Backboard Vertical Movement (Round 8, Final)")]
    public float backboardMoveDistance = 0.5f;
    public float backboardMoveDuration = 1.2f;

    [Header("Rig Forward/Back Movement (Round 4, Final)")]
    public float rigMoveDistance = 1f;
    public float rigMoveDuration = 1.5f;

    private Vector3 backboardRestPos;
    private Vector3 rigRestPos;

    void Awake()
    {
        backboardRestPos = backboard.localPosition;
        rigRestPos = hoopRig.localPosition;
    }

    void Start()
    {
        BracketRound round = BracketManager.Instance != null
            ? BracketManager.Instance.currentBracketRound
            : BracketRound.Round16;

        ApplySurpriseForRound(round);
    }

    public void ApplySurpriseForRound(BracketRound round)
    {
        DOTween.Kill(backboard);
        DOTween.Kill(hoopRig);
        backboard.localPosition = backboardRestPos;
        hoopRig.localPosition = rigRestPos;

        bool backboardMoves = round == BracketRound.Round8 || round == BracketRound.Final;
        bool rigMoves = round == BracketRound.Round4 || round == BracketRound.Final;

        if (backboardMoves)
        {
            backboard.DOLocalMoveY(backboardRestPos.y + backboardMoveDistance, backboardMoveDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        if (rigMoves)
        {
            hoopRig.DOLocalMoveX(rigRestPos.x + rigMoveDistance, rigMoveDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }
}