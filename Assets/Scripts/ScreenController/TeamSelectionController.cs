using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamSelectionController : MonoBehaviour
{
    public TeamRoster roster;
    public Transform teamGrid;
    public GameObject teamButtonPrefab; // Button root, child "FlagImage" (Image), child "NameText" (TMP_Text)
    public RectTransform highlightImage;
    public Button nextButton;
    public GameObject teamSelectionPanel;
    public GameObject knockoutScreenPanel;

    int selectedIndex = -1;

    void Start()
    {
        nextButton.interactable = false;
        highlightImage.gameObject.SetActive(false);

        for (int i = 0; i < roster.teamNames.Length; i++)
        {
            int idx = i;
            GameObject go = Instantiate(teamButtonPrefab, teamGrid);
            go.transform.Find("TeamName").GetComponent<TMP_Text>().text = roster.teamNames[i];
            go.transform.Find("Flag").GetComponent<Image>().sprite = roster.teamFlags[i];
            go.GetComponent<Button>().onClick.AddListener(() => 
            {
                AudioManager.Instance.PlayButtonClick();
                SelectTeam(idx, go.GetComponent<RectTransform>());
            });
        }

        nextButton.onClick.AddListener(() => { AudioManager.Instance.PlayButtonClick(); OnNext(); });
    }

    void SelectTeam(int idx, RectTransform buttonRect)
    {
        selectedIndex = idx;
        highlightImage.gameObject.SetActive(true);
        highlightImage.SetParent(buttonRect, false);
        highlightImage.anchoredPosition = Vector2.zero;
        nextButton.interactable = true;
    }

    void OnNext()
    {
        BracketManager.Instance.StartTournament(selectedIndex);
        teamSelectionPanel.SetActive(false);
        knockoutScreenPanel.SetActive(true);
    }
}