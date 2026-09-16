using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class KnockoutScreenController : MonoBehaviour
{
    [Header("Assign transforms positioned on the bracket art for each round")]
    public Transform[] round16Slots; // 16
    public Transform[] round8Slots;  // 8
    public Transform[] round4Slots;  // 4
    public Transform[] finalSlots;   // 2

    public GameObject flagImagePrefab; // simple Image prefab
    public Button nextButton;
    public string gameplaySceneName = "GameplayScene";

    void OnEnable() => Populate();

    void Start() => nextButton.onClick.AddListener(() => { AudioManager.Instance.PlayButtonClick(); SceneManager.LoadScene(gameplaySceneName); });

    void Populate()
    {
        Transform[] slots = BracketManager.Instance.currentBracketRound switch
        {
            BracketRound.Round16 => round16Slots,
            BracketRound.Round8 => round8Slots,
            BracketRound.Round4 => round4Slots,
            BracketRound.Final => finalSlots,
            _ => round16Slots
        };

        var roster = BracketManager.Instance.roster;
        var seeding = BracketManager.Instance.seeding;

        // clear all slot sets first
        foreach (var arr in new[] { round16Slots, round8Slots, round4Slots, finalSlots })
            foreach (var slot in arr)
                foreach (Transform child in slot)
                    Destroy(child.gameObject);

        for (int i = 0; i < slots.Length && i < seeding.Count; i++)
        {
            var img = Instantiate(flagImagePrefab, slots[i]).GetComponent<Image>();
            img.sprite = roster.teamFlags[seeding[i]];
        }
    }
}