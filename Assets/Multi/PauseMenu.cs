using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI resumeText;
    [SerializeField] private TextMeshProUGUI menuText;

    private bool isOpen;

    private void Start()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
            resumeButton.onClick.AddListener(Resume);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(GoToMenu);
            menuButton.onClick.AddListener(GoToMenu);
        }

        ApplyText();
        SetOpen(false);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetOpen(!isOpen);
    }

    public void Resume()
    {
        SetOpen(false);
    }

    public void GoToMenu()
    {
        NetworkManager manager = NetworkManager.Instance != null
            ? NetworkManager.Instance
            : FindFirstObjectByType<NetworkManager>();

        if (manager != null)
            manager.GoToMenu();
    }

    private void OnSelectedLocaleChanged(Locale _)
    {
        ApplyText();
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        if (panel != null)
            panel.SetActive(open);
    }

    private void ApplyText()
    {
        if (titleText != null) titleText.text = UILocalization.Get("pause_title");
        if (resumeText != null) resumeText.text = UILocalization.Get("pause_resume");
        if (menuText != null) menuText.text = UILocalization.Get("pause_exit_menu");
    }
}
