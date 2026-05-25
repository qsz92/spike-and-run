using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class SensitivitySettings : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityText;
    [SerializeField] private PlayerController playerController;

    private const string SENSITIVITY_KEY = "MouseSensitivity";

    private void Awake()
    {
        // 1. Принудительная инициализация связей
        if (!sensitivitySlider) sensitivitySlider = GetComponentInChildren<Slider>();
        if (!sensitivityText) sensitivityText = GetComponentInChildren<TMP_Text>();
        if (!playerController) playerController = FindLocalPlayer();

        // 2. Настройка слайдера
        sensitivitySlider.minValue = 100f;
        sensitivitySlider.maxValue = 2000f;
        sensitivitySlider.wholeNumbers = true;

        // 3. Подписка на события
        sensitivitySlider.onValueChanged.AddListener(UpdateSensitivity);
    }

    private void Start()
    {
        // 4. Загрузка настроек
        LoadSettings();
    }

    private PlayerController FindLocalPlayer()
    {
        foreach (PlayerController pc in FindObjectsOfType<PlayerController>())
        {
            if (pc.photonView.IsMine) return pc;
        }
        return null;
    }

    private void LoadSettings()
    {
        float savedValue = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 800f);
        sensitivitySlider.value = savedValue;
        UpdateUI(savedValue);
        ApplySensitivity(savedValue);
    }

    private void UpdateSensitivity(float value)
    {
        UpdateUI(value);
        ApplySensitivity(value);
        SaveSettings(value);
    }

    private void UpdateUI(float value)
    {
        // 5. Принудительное обновление текста
        sensitivityText.text = Mathf.RoundToInt(value).ToString();
        LayoutRebuilder.ForceRebuildLayoutImmediate(sensitivityText.rectTransform);
    }

    private void ApplySensitivity(float value)
    {
        if (playerController != null && playerController.photonView.IsMine)
        {
            playerController.SetMouseSensitivity(value);
        }
    }

    private void SaveSettings(float value)
    {
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, value);
        PlayerPrefs.Save();
    }
}