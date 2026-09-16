using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource sfxSource2; // second channel, was ambienceSource

    [Header("Music")]
    public AudioClip bgm;
    public float bgmGapSeconds = 5f;

    [Header("UI")]
    public AudioClip buttonClick;

    [Header("Ball")]
    public AudioClip hoopSwish;
    public AudioClip rimHit;
    public AudioClip backboardHit;
    public AudioClip courtBounce;

    [Header("Crowd")]
    public AudioClip crowdCheer;
    public AudioClip crowdGroan;

    [Header("Match")]
    public AudioClip whistle;
    public AudioClip winJingle;
    public AudioClip loseJingle;
    public AudioClip victoryFanfare;

    private Coroutine bgmRoutine;
    private bool useSecondChannel;

    [Header("Volume Fade")]
    public float mainMenuVolume = 0.3f;
    public float gameplayVolume = 0.1f;
    public float fadeDuration = 1.5f;
    public float initialFadeStartVolume = 0.1f;

    private Coroutine fadeRoutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        SoundSettings.ApplyOnLoad();
        musicSource.volume = initialFadeStartVolume;
        PlayMusic();
        FadeToVolume(mainMenuVolume); // initial fade-in on game start, 0.1 -> 0.3
    }

    public void FadeToVolume(float target)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    IEnumerator FadeRoutine(float target)
    {
        float start = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            musicSource.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }

        musicSource.volume = target;
    }

    public void OnEnteredGameplayScene() => FadeToVolume(gameplayVolume);
    public void OnEnteredMainMenuScene() => FadeToVolume(mainMenuVolume);

    public void PlayMusic()
    {
        if (bgm == null) return;
        if (bgmRoutine != null) StopCoroutine(bgmRoutine);
        bgmRoutine = StartCoroutine(BgmLoopRoutine());
    }

    IEnumerator BgmLoopRoutine()
    {
        musicSource.loop = false;
        musicSource.clip = bgm;

        while (true)
        {
            musicSource.Play();
            yield return new WaitForSeconds(bgm.length);
            yield return new WaitForSeconds(bgmGapSeconds);
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        // alternate between the two channels so overlapping SFX don't fight for one source
        AudioSource target = useSecondChannel ? sfxSource2 : sfxSource;
        useSecondChannel = !useSecondChannel;

        target.PlayOneShot(clip, volumeScale);
    }

    public void PlayButtonClick() => PlaySFX(buttonClick);
    public void PlayHoopSwish() => PlaySFX(hoopSwish);
    public void PlayRimHit() => PlaySFX(rimHit);
    public void PlayBackboardHit() => PlaySFX(backboardHit);
    public void PlayCourtBounce() => PlaySFX(courtBounce);
    public void PlayCrowdCheer() => PlaySFX(crowdCheer);
    public void PlayCrowdGroan() => PlaySFX(crowdGroan);
    public void PlayWhistle() => PlaySFX(whistle);
    public void PlayWinJingle() => PlaySFX(winJingle);
    public void PlayLoseJingle() => PlaySFX(loseJingle);
    public void PlayVictoryFanfare() => PlaySFX(victoryFanfare);
}