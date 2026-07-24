using UnityEngine;

/// <summary>Persistent (DontDestroyOnLoad) BGM/SFX volume controller. Volumes are stored in
/// PlayerPrefs so they persist independently of any save game.</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string BgmVolumeKey = "BgmVolume";
    private const string SfxVolumeKey = "SfxVolume";

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    public float BgmVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        bgmSource.volume = BgmVolume;
    }

    public void SetBgmVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = BgmVolume;
        PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
    }

    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip)
            return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, SfxVolume);
    }
}
