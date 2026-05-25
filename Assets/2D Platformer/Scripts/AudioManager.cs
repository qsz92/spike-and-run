using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float MasterVolume
    {
        get => PlayerPrefs.GetFloat("MasterVolume", 1f);
        set
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            ApplyVolume();
            PlayerPrefs.Save();
        }
    }

    public bool MusicEnabled
    {
        get => PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("MusicEnabled", value ? 1 : 0);
            ApplyVolume();
            PlayerPrefs.Save();
        }
    }

    public bool SfxEnabled
    {
        get => PlayerPrefs.GetInt("SfxEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("SfxEnabled", value ? 1 : 0);
            ApplyVolume();
            PlayerPrefs.Save();
        }
    }

    private void ApplyVolume()
    {
        float master = MasterVolume;
        AudioListener.volume = master;
        musicSource.volume = MusicEnabled ? 1f : 0f;
        sfxSource.volume = SfxEnabled ? 1f : 0f;
    }

    public void PlayMusic(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySfx(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    private void LoadSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        ApplyVolume();
    }
}