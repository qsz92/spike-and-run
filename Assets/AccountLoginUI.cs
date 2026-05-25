using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class AccountLoginUI : MonoBehaviour
{
    [SerializeField] private string accountServerUrl = "YOUR_ACCOUNT_SERVER_URL_HERE";
    [SerializeField] private TMP_FontAsset uiFont;

    [SerializeField] private GameObject authPanel;
    [SerializeField] private GameObject accountBar;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI accountText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI usernamePlaceholderText;
    [SerializeField] private TextMeshProUGUI passwordPlaceholderText;
    [SerializeField] private TextMeshProUGUI loginButtonText;
    [SerializeField] private TextMeshProUGUI registerButtonText;
    [SerializeField] private TextMeshProUGUI logoutButtonText;
    [SerializeField] private TextMeshProUGUI closeButtonText;

    void Awake()
    {
        EnsureEditableUI();

        if (!Application.isPlaying) return;

        AccountManager.Configure(accountServerUrl, this);
        AccountManager.AutoLoad();
        Refresh();
    }

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        EnsureEditableUI();
        ApplyLocalizedText();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    void OnSelectedLocaleChanged(Locale _)
    {
        ApplyLocalizedText();
        if (Application.isPlaying)
            Refresh();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        EditorApplication.delayCall -= EnsureEditableUIDelayed;
        EditorApplication.delayCall += EnsureEditableUIDelayed;
    }

    void EnsureEditableUIDelayed()
    {
        if (this == null) return;
        EnsureEditableUI();
        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    void EnsureEditableUI()
    {
        RectTransform root = transform as RectTransform;
        if (root == null) return;

        authPanel = GetOrCreateUIObject("AuthPanel", root, Vector2.zero, Vector2.one, Vector2.zero, authPanel).gameObject;
        Image authBackground = GetOrAdd<Image>(authPanel);
        authBackground.color = new Color(0.04f, 0.04f, 0.04f, 0.88f);

        RectTransform window = GetOrCreateUIObject("AuthWindow", authPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460f, 360f));
        Image windowImage = GetOrAdd<Image>(window.gameObject);
        windowImage.color = new Color(0.61960787f, 0.27058825f, 0.22352941f, 1f);

        titleText = GetOrCreateText("Title", window, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 56f), new Vector2(0f, -52f), 30f);

        Button closeButton = GetOrCreateButton("CloseButton", window, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(38f, 38f), new Vector2(-28f, -28f));
        closeButtonText = GetOrCreateText("Text", closeButton.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18f);
        closeButton.onClick.RemoveListener(CloseAuthPanel);
        closeButton.onClick.AddListener(CloseAuthPanel);

        usernameInput = GetOrCreateInput("LoginInput", window, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-72f, 46f), new Vector2(0f, -122f));
        usernamePlaceholderText = usernameInput.placeholder as TextMeshProUGUI;
        if (Application.isPlaying)
            usernameInput.text = AccountManager.CurrentUsername;

        passwordInput = GetOrCreateInput("PasswordInput", window, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-72f, 46f), new Vector2(0f, -182f));
        passwordPlaceholderText = passwordInput.placeholder as TextMeshProUGUI;
        passwordInput.contentType = TMP_InputField.ContentType.Password;

        Button loginButton = GetOrCreateButton("LoginButton", window, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(172f, 48f), new Vector2(126f, -254f));
        loginButtonText = GetOrCreateText("Text", loginButton.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22f);
        loginButton.onClick.RemoveListener(Login);
        loginButton.onClick.AddListener(Login);

        Button registerButton = GetOrCreateButton("RegisterButton", window, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(172f, 48f), new Vector2(-126f, -254f));
        registerButtonText = GetOrCreateText("Text", registerButton.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22f);
        registerButton.onClick.RemoveListener(Register);
        registerButton.onClick.AddListener(Register);

        statusText = GetOrCreateText("Status", window, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-72f, 42f), new Vector2(0f, 34f), 18f);

        accountBar = GetOrCreateUIObject("AccountBar", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(220f, 34f), new Vector2(150f, -58f), accountBar).gameObject;
        Image barImage = GetOrAdd<Image>(accountBar);
        barImage.color = new Color(0.61960787f, 0.27058825f, 0.22352941f, 0.95f);

        accountText = GetOrCreateText("AccountText", accountBar.transform, Vector2.zero, Vector2.one, new Vector2(-74f, 0f), new Vector2(-37f, 0f), 17f);

        Button logoutButton = GetOrCreateButton("LogoutButton", accountBar.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(74f, 0f), new Vector2(-37f, 0f));
        logoutButtonText = GetOrCreateText("Text", logoutButton.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 15f);
        logoutButton.onClick.RemoveListener(Logout);
        logoutButton.onClick.RemoveListener(AccountAction);
        logoutButton.onClick.AddListener(AccountAction);

        ApplyLocalizedText();
    }

    void ApplyLocalizedText()
    {
        if (titleText != null)
            titleText.text = UILocalization.Get("account_title");
        if (usernamePlaceholderText != null)
            usernamePlaceholderText.text = UILocalization.Get("account_login_placeholder");
        if (passwordPlaceholderText != null)
            passwordPlaceholderText.text = UILocalization.Get("account_password_placeholder");
        if (loginButtonText != null)
            loginButtonText.text = UILocalization.Get("account_login_button");
        if (registerButtonText != null)
            registerButtonText.text = UILocalization.Get("account_register_button");
        if (closeButtonText != null)
            closeButtonText.text = "X";
        if (logoutButtonText != null)
            logoutButtonText.text = AccountManager.IsLoggedIn ? UILocalization.Get("account_logout_button") : UILocalization.Get("account_login_button");
    }

    void Login()
    {
        statusText.text = UILocalization.Get("account_status_logging_in");
        AccountManager.Login(usernameInput.text, passwordInput.text, (ok, message) =>
        {
            if (ok)
            {
                passwordInput.text = "";
                Refresh();
            }

            statusText.text = message;
        });
    }

    void Register()
    {
        statusText.text = UILocalization.Get("account_status_registering");
        AccountManager.Register(usernameInput.text, passwordInput.text, (ok, message) =>
        {
            if (ok)
            {
                passwordInput.text = "";
                Refresh();
            }

            statusText.text = message;
        });
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (authPanel == null || !authPanel.activeSelf) return;
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == usernameInput.gameObject)
            passwordInput.Select();
        else
            usernameInput.Select();
    }

    void Logout()
    {
        AccountManager.Logout();
        statusText.text = "";
        Refresh();
    }

    void AccountAction()
    {
        if (AccountManager.IsLoggedIn)
            Logout();
        else
            OpenAuthPanel();
    }

    void OpenAuthPanel()
    {
        authPanel.SetActive(true);
        statusText.text = "";
        usernameInput.Select();
    }

    void CloseAuthPanel()
    {
        authPanel.SetActive(false);
        statusText.text = "";
    }

    void Refresh()
    {
        bool loggedIn = AccountManager.IsLoggedIn;
        authPanel.SetActive(false);
        accountBar.SetActive(true);
        accountText.text = loggedIn ? AccountManager.CurrentUsername : UILocalization.Get("account_guest");
        if (logoutButtonText != null)
            logoutButtonText.text = loggedIn ? UILocalization.Get("account_logout_button") : UILocalization.Get("account_login_button");
    }

    RectTransform GetOrCreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        return GetOrCreateUIObject(name, parent, anchorMin, anchorMax, size, Vector2.zero, null);
    }

    RectTransform GetOrCreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        return GetOrCreateUIObject(name, parent, anchorMin, anchorMax, size, position, null);
    }

    RectTransform GetOrCreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, GameObject preferredObject)
    {
        return GetOrCreateUIObject(name, parent, anchorMin, anchorMax, size, Vector2.zero, preferredObject);
    }

    RectTransform GetOrCreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position, GameObject preferredObject)
    {
        Transform existing = parent.Find(name);
        if (existing == null && preferredObject != null && preferredObject.name == name)
            existing = preferredObject.transform;
        if (existing == null)
        {
            Transform searchRoot = parent.GetComponentInParent<Canvas>()?.transform ?? parent.root;
            existing = FindDeepChild(searchRoot, name);
        }

        bool created = existing == null;
        GameObject obj = created ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)) : existing.gameObject;
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
            rect = obj.AddComponent<RectTransform>();

        if (created)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
        return rect;
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform found = FindDeepChild(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    TextMeshProUGUI GetOrCreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position, float fontSize)
    {
        RectTransform rect = GetOrCreateUIObject(name, parent, anchorMin, anchorMax, size, position);
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
        if (uiFont != null)
            text.font = uiFont;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        return text;
    }

    TMP_InputField GetOrCreateInput(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        RectTransform inputRect = GetOrCreateUIObject(name, parent, anchorMin, anchorMax, size, position);
        Image image = GetOrAdd<Image>(inputRect.gameObject);
        image.color = new Color(0.8039216f, 0.40784314f, 0.23921569f, 1f);

        TMP_InputField input = GetOrAdd<TMP_InputField>(inputRect.gameObject);
        input.interactable = true;
        input.shouldHideMobileInput = false;
        TextMeshProUGUI text = GetOrCreateText("Text", inputRect, Vector2.zero, Vector2.one, new Vector2(-24f, 0f), new Vector2(12f, 0f), 22f);
        text.alignment = TextAlignmentOptions.MidlineLeft;

        TextMeshProUGUI placeholderText = GetOrCreateText("Placeholder", inputRect, Vector2.zero, Vector2.one, new Vector2(-24f, 0f), new Vector2(12f, 0f), 22f);
        placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
        placeholderText.color = new Color(1f, 1f, 1f, 0.55f);

        input.textComponent = text;
        input.placeholder = placeholderText;
        input.targetGraphic = image;
        return input;
    }

    Button GetOrCreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        Image image = GetOrAdd<Image>(GetOrCreateUIObject(name, parent, anchorMin, anchorMax, size, position).gameObject);
        image.color = new Color(0.8039216f, 0.40784314f, 0.23921569f, 1f);
        Button button = GetOrAdd<Button>(image.gameObject);
        button.targetGraphic = image;
        return button;
    }

    T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }
}
