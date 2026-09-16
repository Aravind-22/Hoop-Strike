using UnityEngine;
using UnityEngine.UI;

public class SoundToggleUI : MonoBehaviour
{
    public Image iconImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    void Start()
    {
        SoundSettings.ApplyOnLoad();
        Refresh();
        GetComponent<Button>().onClick.AddListener(() => { AudioManager.Instance.PlayButtonClick(); Toggle(); });
    }

    void Toggle()
    {
        SoundSettings.SetSoundOn(!SoundSettings.IsSoundOn);
        Refresh();
    }

    void Refresh() => iconImage.sprite = SoundSettings.IsSoundOn ? soundOnSprite : soundOffSprite;
}