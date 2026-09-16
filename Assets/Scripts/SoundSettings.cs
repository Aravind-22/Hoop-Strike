using UnityEngine;

public static class SoundSettings
{
    const string Key = "SoundOn";
    public static bool IsSoundOn => PlayerPrefs.GetInt(Key, 1) == 1;

    public static void SetSoundOn(bool on)
    {
        PlayerPrefs.SetInt(Key, on ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = on ? 1f : 0f;
    }

    public static void ApplyOnLoad() => AudioListener.volume = IsSoundOn ? 1f : 0f;
}