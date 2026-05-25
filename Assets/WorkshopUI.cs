using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class WorkshopUI : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset uiFont;

    private readonly Color[] palette =
    {
        new Color(0.86f, 0.12f, 0.22f),
        new Color(0.95f, 0.32f, 0.63f),
        new Color(0.22f, 0.42f, 0.93f),
        new Color(0.11f, 0.68f, 0.42f),
        new Color(0.98f, 0.75f, 0.12f),
        new Color(0.97f, 0.45f, 0.18f),
        new Color(0.52f, 0.25f, 0.82f),
        new Color(0.83f, 0.86f, 0.9f),
        new Color(0.16f, 0.18f, 0.28f),
        new Color(0.52f, 0.18f, 0.28f),
        new Color(0.1f, 0.78f, 0.86f),
        new Color(0.95f, 0.95f, 0.95f)
    };

    private Image previewImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI bodyAText;
    private TextMeshProUGUI bodyBText;
    private TextMeshProUGUI accentText;
    private TextMeshProUGUI createButtonText;
    private TextMeshProUGUI statusText;
    private Button createButton;
    private Color bodyA;
    private Color bodyB;
    private Color accent;

    private void OnEnable()
    {
        EnsureUI();
        LoadColors();
        ApplyLocalization();
        UpdatePreview();
        UpdateButtons();
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        AccountManager.ProgressChanged += OnProgressChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        AccountManager.ProgressChanged -= OnProgressChanged;
    }

    private void OnSelectedLocaleChanged(Locale _) => ApplyLocalization();

    private void OnProgressChanged()
    {
        LoadColors();
        UpdatePreview();
        UpdateButtons();
    }

    private void EnsureUI()
    {
        Transform existingRoot = transform.Find("WorkshopRoot");
        RectTransform root;
        if (existingRoot == null)
        {
            root = GetOrCreateRect("WorkshopRoot", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateDefaultUI(root);
            root.SetAsFirstSibling();
            return;
        }

        root = existingRoot as RectTransform;
        BindExistingUI(root);
        root.SetAsFirstSibling();
    }

    private void CreateDefaultUI(RectTransform root)
    {
        titleText = GetOrCreateText("Title", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(520f, 54f), new Vector2(320f, -88f), 34f);
        hintText = GetOrCreateText("Hint", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(560f, 74f), new Vector2(338f, -148f), 18f);
        hintText.alignment = TextAlignmentOptions.Left;

        Image previewBack = GetOrCreateImage("PreviewBack", root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(230f, 230f), new Vector2(185f, -10f));
        previewBack.color = new Color(0.22f, 0.36f, 0.55f, 0.72f);
        previewImage = GetOrCreateImage("Preview", previewBack.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(152f, 152f), Vector2.zero);
        previewImage.preserveAspect = true;

        bodyAText = CreateColorRow(root, "BodyA", 430f, -238f, color => { bodyA = color; UpdatePreview(); });
        bodyBText = CreateColorRow(root, "BodyB", 430f, -326f, color => { bodyB = color; UpdatePreview(); });
        accentText = CreateColorRow(root, "Accent", 430f, -414f, color => { accent = color; UpdatePreview(); });

        createButton = GetOrCreateButton("CreateButton", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(250f, 54f), new Vector2(260f, 96f));
        createButton.onClick.RemoveListener(CreateOrSaveSkin);
        createButton.onClick.AddListener(CreateOrSaveSkin);
        createButtonText = GetOrCreateText("Text", createButton.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22f);

        statusText = GetOrCreateText("Status", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(600f, 34f), new Vector2(396f, 48f), 18f);
        statusText.alignment = TextAlignmentOptions.Left;
    }

    private void BindExistingUI(Transform root)
    {
        titleText = FindText(root, "Title");
        hintText = FindText(root, "Hint");
        bodyAText = BindColorRow(root, "BodyA", color => { bodyA = color; UpdatePreview(); });
        bodyBText = BindColorRow(root, "BodyB", color => { bodyB = color; UpdatePreview(); });
        accentText = BindColorRow(root, "Accent", color => { accent = color; UpdatePreview(); });

        Transform previewBack = root.Find("PreviewBack");
        previewImage = previewBack != null ? FindImage(previewBack, "Preview") : null;

        createButton = FindButton(root, "CreateButton");
        if (createButton != null)
        {
            createButton.onClick.RemoveListener(CreateOrSaveSkin);
            createButton.onClick.AddListener(CreateOrSaveSkin);
            createButtonText = createButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        statusText = FindText(root, "Status");

        Button oldSelectButton = FindButton(root, "SelectButton");
        if (oldSelectButton != null)
            oldSelectButton.gameObject.SetActive(false);
    }

    private TextMeshProUGUI CreateColorRow(Transform parent, string rowName, float x, float y, Action<Color> onColor)
    {
        RectTransform row = GetOrCreateRect(rowName, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(620f, 72f), new Vector2(x, y));
        TextMeshProUGUI label = GetOrCreateText("Label", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(130f, 34f), new Vector2(65f, 0f), 20f);
        label.alignment = TextAlignmentOptions.Left;

        for (int i = 0; i < palette.Length; i++)
        {
            int index = i;
            Button swatch = GetOrCreateButton("Color " + i, row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, 34f), new Vector2(158f + i * 39f, 0f));
            Image image = swatch.GetComponent<Image>();
            image.color = palette[i];
            swatch.onClick.RemoveAllListeners();
            swatch.onClick.AddListener(() => onColor(palette[index]));
        }

        return label;
    }

    private TextMeshProUGUI BindColorRow(Transform parent, string rowName, Action<Color> onColor)
    {
        Transform row = parent.Find(rowName);
        if (row == null) return null;

        for (int i = 0; i < palette.Length; i++)
        {
            int index = i;
            Button swatch = FindButton(row, "Color " + i);
            if (swatch == null) continue;

            swatch.onClick.RemoveAllListeners();
            swatch.onClick.AddListener(() => onColor(palette[index]));
        }

        return FindText(row, "Label");
    }

    private void LoadColors()
    {
        bodyA = CustomSkinUtility.GetBodyA();
        bodyB = CustomSkinUtility.GetBodyB();
        accent = CustomSkinUtility.GetAccent();
    }

    private void CreateOrSaveSkin()
    {
        int coins = PlayerPrefs.GetInt("TotalCoins", 0);
        if (coins < CustomSkinUtility.WorkshopPrice)
        {
            SetStatus(UILocalization.Get("workshop_not_enough"));
            return;
        }

        PlayerPrefs.SetInt("TotalCoins", coins - CustomSkinUtility.WorkshopPrice);
        int slot = CustomSkinUtility.AddSkin(bodyA, bodyB, accent, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        PlayerPrefs.SetInt("SelectedSkin", CustomSkinUtility.CustomSkinIndex);
        PlayerPrefs.SetInt(CustomSkinUtility.SelectedSlotKey, slot);
        PlayerPrefs.Save();
        PlayerSkinNetwork.ApplyLocalPlayerProperties();
        AccountManager.SaveProgress();
        AccountManager.NotifyProgressChanged();
        SetStatus(UILocalization.Get("workshop_created"));
        UpdateButtons();
    }

    private void ApplyLocalization()
    {
        if (titleText != null) titleText.text = UILocalization.Get("workshop_title");
        if (hintText != null) hintText.text = UILocalization.Get("workshop_hint");
        if (bodyAText != null) bodyAText.text = UILocalization.Get("workshop_body_a");
        if (bodyBText != null) bodyBText.text = UILocalization.Get("workshop_body_b");
        if (accentText != null) accentText.text = UILocalization.Get("workshop_accent");
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (createButtonText != null)
            createButtonText.text = UILocalization.Format("workshop_create", CustomSkinUtility.WorkshopPrice);
    }

    private void UpdatePreview()
    {
        if (previewImage == null) return;

        Sprite[] idle = Resources.LoadAll<Sprite>("Skin 1/idle");
        Sprite preview = idle.Length > 0 ? CustomSkinUtility.BuildCustomSprite(idle[0], bodyA, bodyB, accent) : null;
        previewImage.sprite = preview;
        previewImage.enabled = preview != null;
    }

    private void SetStatus(string value)
    {
        if (statusText != null)
            statusText.text = value;
    }

    private RectTransform GetOrCreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        Transform existing = parent.Find(name);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private Image GetOrCreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        Image image = GetOrAdd<Image>(GetOrCreateRect(name, parent, anchorMin, anchorMax, size, position).gameObject);
        image.raycastTarget = false;
        return image;
    }

    private Button GetOrCreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        Image image = GetOrAdd<Image>(GetOrCreateRect(name, parent, anchorMin, anchorMax, size, position).gameObject);
        image.raycastTarget = true;
        Button button = GetOrAdd<Button>(image.gameObject);
        button.targetGraphic = image;
        if (name.StartsWith("Color "))
            return button;
        image.color = new Color(0.8039216f, 0.40784314f, 0.23921569f, 1f);
        return button;
    }

    private TextMeshProUGUI GetOrCreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position, float fontSize)
    {
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(GetOrCreateRect(name, parent, anchorMin, anchorMax, size, position).gameObject);
        if (uiFont != null)
            text.font = uiFont;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }

    private TextMeshProUGUI FindText(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    private Image FindImage(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private Button FindButton(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.GetComponent<Button>() : null;
    }
}
