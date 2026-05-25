using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;

    private AudioSource musicSource;

    private void Start()
    {
        // Находим AudioSource MusicPlayer
        var musicPlayer = FindObjectOfType<MusicPlayer>();
        if (musicPlayer != null)
            musicSource = musicPlayer.GetComponent<AudioSource>();

        // Загружаем сохранённые настройки
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bool savedMusic = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;

        masterSlider.value = savedVolume;
        musicToggle.isOn = savedMusic;
        sfxToggle.isOn = PlayerPrefs.GetInt("SfxEnabled", 1) == 1;

        AudioListener.volume = savedVolume;
        if (musicSource != null) musicSource.mute = !savedMusic;

        // Подписываемся
        masterSlider.onValueChanged.AddListener(val =>
        {
            AudioListener.volume = val;
            PlayerPrefs.SetFloat("MasterVolume", val);
            PlayerPrefs.Save();
        });

        musicToggle.onValueChanged.AddListener(val =>
        {
            if (musicSource != null) musicSource.mute = !val;
            PlayerPrefs.SetInt("MusicEnabled", val ? 1 : 0);
            PlayerPrefs.Save();
        });

        sfxToggle.onValueChanged.AddListener(val =>
        {
            PlayerPrefs.SetInt("SfxEnabled", val ? 1 : 0);
            PlayerPrefs.Save();
        });
    }
}