using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SkinInventory : MonoBehaviour
{
    [SerializeField] private RectTransform storeItemsTemplateRoot;
    [SerializeField] private TMP_FontAsset uiFont;

    private const int SKIN2_PRICE = 50;
    private const int SKIN3_PRICE = 100;
    private const int GRID_COLUMNS = 3;

    private RectTransform inventoryItemsRoot;
    private readonly List<GameObject> customSkinCards = new List<GameObject>();
    private readonly List<Image> customSkinPreviews = new List<Image>();

    private GameObject detailsPanel;
    private Image detailsPreview;
    private TextMeshProUGUI detailsTitle;
    private TextMeshProUGUI detailsPrice;
    private TextMeshProUGUI detailsBoughtAt;
    private TextMeshProUGUI detailsSelectText;
    private TextMeshProUGUI detailsCloseText;
    private Button detailsSelectButton;
    private int currentDetailsSkinIndex;
    private int currentDetailsCustomSlot = -1;

    private string LabelPrice => UILocalization.Get("inventory_price");
    private string LabelBoughtAt => UILocalization.Get("inventory_bought");
    private string LabelUnknown => UILocalization.Get("inventory_unknown");
    private string LabelClose => UILocalization.Get("inventory_close");

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        AccountManager.ProgressChanged += OnProgressChanged;
        UpdateInventoryUI();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        AccountManager.ProgressChanged -= OnProgressChanged;
    }

    void OnProgressChanged()
    {
        UpdateInventoryUI();
        if (detailsPanel != null)
            detailsPanel.SetActive(false);
    }

    void OnSelectedLocaleChanged(Locale _)
    {
        UpdateInventoryUI();
        if (detailsPanel != null && detailsPanel.activeSelf)
            ApplyDetailsText();
    }

    void UpdateInventoryUI()
    {
        EnsureInventoryItems();
        if (inventoryItemsRoot == null) return;

        bool skin2Owned = PlayerPrefs.GetInt("SkinOwned_2", 0) == 1;
        bool skin3Owned = PlayerPrefs.GetInt("SkinOwned_3", 0) == 1;
        int customCount = CustomSkinUtility.GetSlotCount();

        SetInventoryCardVisible(1, false);
        SetInventoryCardVisible(2, skin2Owned);
        SetInventoryCardVisible(3, skin3Owned);
        EnsureCustomSkinCards(customCount);

        List<RectTransform> visibleCards = new List<RectTransform>();
        AddVisibleCard(visibleCards, 2, skin2Owned);
        AddVisibleCard(visibleCards, 3, skin3Owned);

        for (int slot = 0; slot < customSkinCards.Count; slot++)
        {
            bool visible = slot < customCount;
            customSkinCards[slot].SetActive(visible);
            if (!visible) continue;

            customSkinPreviews[slot].sprite = GetCustomSkinPreview(slot);
            visibleCards.Add((RectTransform)customSkinCards[slot].transform);
        }

        LayoutCards(visibleCards);
    }

    void EnsureInventoryItems()
    {
        if (inventoryItemsRoot != null || storeItemsTemplateRoot == null) return;

        inventoryItemsRoot = Instantiate(storeItemsTemplateRoot.gameObject, transform).GetComponent<RectTransform>();
        inventoryItemsRoot.name = "InventoryItems";
        inventoryItemsRoot.anchoredPosition = storeItemsTemplateRoot.anchoredPosition;
        inventoryItemsRoot.gameObject.SetActive(true);

        SetupRegularCard(2);
        SetupRegularCard(3);
    }

    void SetupRegularCard(int skinIndex)
    {
        GameObject card = GetInventoryCard(skinIndex);
        if (card == null) return;

        foreach (Button childButton in card.GetComponentsInChildren<Button>(true))
            childButton.gameObject.SetActive(false);

        Button cardButton = card.GetComponent<Button>();
        if (cardButton == null)
            cardButton = card.AddComponent<Button>();

        cardButton.targetGraphic = card.GetComponent<Graphic>();
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => ShowRegularDetails(skinIndex));
    }

    void EnsureCustomSkinCards(int count)
    {
        while (customSkinCards.Count < count)
            CreateCustomSkinCard(customSkinCards.Count);
    }

    void CreateCustomSkinCard(int slot)
    {
        RectTransform reference = GetReferenceCardRect();
        if (reference == null) return;

        RectTransform cardRect = CreateUIObject($"WorkshopSkinCard_{slot}", inventoryItemsRoot, reference.anchorMin, reference.anchorMax, reference.sizeDelta);
        GameObject card = cardRect.gameObject;
        customSkinCards.Add(card);

        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(1f, 1f, 1f, 0f);
        Button cardButton = card.AddComponent<Button>();
        cardButton.targetGraphic = cardImage;
        int capturedSlot = slot;
        cardButton.onClick.AddListener(() => ShowCustomDetails(capturedSlot));

        Image preview = CreateImage("Preview", cardRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f));
        preview.rectTransform.anchoredPosition = new Vector2(0f, 22.8f);
        preview.sprite = GetCustomSkinPreview(slot);
        customSkinPreviews.Add(preview);

        Image priceBack = CreateImage("PriceBack", cardRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56.983f, 30f));
        priceBack.rectTransform.anchoredPosition = new Vector2(-51.509f, -59f);
        priceBack.color = new Color(0.73f, 0.62f, 0.16f, 1f);
        priceBack.raycastTarget = false;
        TextMeshProUGUI price = CreateText("Price", priceBack.transform, Vector2.zero, Vector2.one, Vector2.zero, 22f);
        price.text = CustomSkinUtility.WorkshopPrice.ToString();
    }

    void LayoutCards(List<RectTransform> visibleCards)
    {
        RectTransform first = GetInventoryCard(1)?.transform as RectTransform;
        RectTransform second = GetInventoryCard(2)?.transform as RectTransform;
        if (first == null || second == null) return;

        Vector2 start = first.anchoredPosition;
        float spacingX = Mathf.Abs(second.anchoredPosition.x - first.anchoredPosition.x);
        if (spacingX < 80f) spacingX = 172f;
        float spacingY = 190f;

        for (int i = 0; i < visibleCards.Count; i++)
        {
            RectTransform rect = visibleCards[i];
            rect.anchorMin = first.anchorMin;
            rect.anchorMax = first.anchorMax;
            rect.anchoredPosition = start + new Vector2((i % GRID_COLUMNS) * spacingX, -(i / GRID_COLUMNS) * spacingY);
        }
    }

    void AddVisibleCard(List<RectTransform> cards, int skinIndex, bool visible)
    {
        if (!visible) return;
        GameObject card = GetInventoryCard(skinIndex);
        if (card != null)
            cards.Add((RectTransform)card.transform);
    }

    GameObject GetInventoryCard(int skinIndex)
    {
        if (inventoryItemsRoot == null) return null;

        int childIndex = skinIndex - 1;
        if (childIndex < 0 || childIndex >= inventoryItemsRoot.childCount) return null;

        return inventoryItemsRoot.GetChild(childIndex).gameObject;
    }

    RectTransform GetReferenceCardRect()
    {
        GameObject card = GetInventoryCard(2) ?? GetInventoryCard(1);
        return card != null ? card.transform as RectTransform : null;
    }

    void SetInventoryCardVisible(int skinIndex, bool visible)
    {
        GameObject card = GetInventoryCard(skinIndex);
        if (card != null)
            card.SetActive(visible);
    }

    void ShowRegularDetails(int skinIndex)
    {
        if (PlayerPrefs.GetInt($"SkinOwned_{skinIndex}", 0) != 1) return;

        currentDetailsSkinIndex = skinIndex;
        currentDetailsCustomSlot = -1;
        ShowDetails(GetSkinPreview(skinIndex));
    }

    void ShowCustomDetails(int slot)
    {
        if (!CustomSkinUtility.IsValidSlot(slot)) return;

        currentDetailsSkinIndex = CustomSkinUtility.CustomSkinIndex;
        currentDetailsCustomSlot = slot;
        ShowDetails(GetCustomSkinPreview(slot));
    }

    void ShowDetails(Sprite preview)
    {
        EnsureDetailsPanel();
        ApplyDetailsText();

        detailsPreview.sprite = preview;
        detailsPreview.enabled = preview != null;
        detailsPanel.SetActive(true);
        detailsPanel.transform.SetAsLastSibling();
    }

    void ApplyDetailsText()
    {
        if (currentDetailsCustomSlot >= 0)
        {
            detailsTitle.text = $"{UILocalization.Get("inventory_workshop_skin_title")} {currentDetailsCustomSlot + 1}";
            detailsPrice.text = $"{LabelPrice}: {CustomSkinUtility.WorkshopPrice}";
            string boughtAt = CustomSkinUtility.GetSlotBoughtAt(currentDetailsCustomSlot);
            detailsBoughtAt.text = $"{LabelBoughtAt}: {(string.IsNullOrEmpty(boughtAt) ? LabelUnknown : boughtAt)}";
            detailsSelectButton.interactable = !CustomSkinUtility.IsSelectedSlot(currentDetailsCustomSlot);
            detailsSelectText.text = CustomSkinUtility.IsSelectedSlot(currentDetailsCustomSlot) ? UILocalization.Get("shop_selected") : UILocalization.Get("shop_select");
        }
        else
        {
            detailsTitle.text = UILocalization.Format("inventory_skin_title", currentDetailsSkinIndex);
            detailsPrice.text = $"{LabelPrice}: {GetSkinPrice(currentDetailsSkinIndex)}";
            detailsBoughtAt.text = $"{LabelBoughtAt}: {PlayerPrefs.GetString($"SkinBoughtAt_{currentDetailsSkinIndex}", LabelUnknown)}";
            detailsSelectButton.interactable = PlayerPrefs.GetInt("SelectedSkin", 1) != currentDetailsSkinIndex;
            detailsSelectText.text = PlayerPrefs.GetInt("SelectedSkin", 1) == currentDetailsSkinIndex ? UILocalization.Get("shop_selected") : UILocalization.Get("shop_select");
        }

        if (detailsCloseText != null)
            detailsCloseText.text = LabelClose;
    }

    void SelectDetailsSkin()
    {
        if (currentDetailsCustomSlot >= 0)
            CustomSkinUtility.SelectSlot(currentDetailsCustomSlot);
        else
            PlayerPrefs.SetInt("SelectedSkin", currentDetailsSkinIndex);

        PlayerPrefs.Save();
        PlayerSkinNetwork.ApplyLocalPlayerProperties();
        AccountManager.SaveProgress();
        AccountManager.NotifyProgressChanged();
        ApplyDetailsText();
    }

    void EnsureDetailsPanel()
    {
        if (detailsPanel != null) return;

        detailsPanel = CreateUIObject("InventoryDetails", transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero).gameObject;
        Image backdrop = detailsPanel.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.82f);

        RectTransform window = CreateUIObject("Window", detailsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(620f, 390f));
        Image windowImage = window.gameObject.AddComponent<Image>();
        windowImage.color = new Color(0.61960787f, 0.27058825f, 0.22352941f, 1f);

        detailsPreview = CreateImage("Preview", window, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(230f, 230f));
        detailsPreview.rectTransform.anchoredPosition = new Vector2(165f, 10f);

        detailsTitle = CreateText("Title", window, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 54f), 34f);
        detailsTitle.rectTransform.anchoredPosition = new Vector2(410f, -82f);

        detailsPrice = CreateText("Price", window, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 38f), 24f);
        detailsPrice.rectTransform.anchoredPosition = new Vector2(410f, -152f);

        detailsBoughtAt = CreateText("BoughtAt", window, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 38f), 24f);
        detailsBoughtAt.rectTransform.anchoredPosition = new Vector2(410f, -205f);

        detailsSelectButton = CreateButton("Select", window, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(132f, 42f));
        detailsSelectButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(415f, 55f);
        detailsSelectButton.onClick.AddListener(SelectDetailsSkin);
        detailsSelectText = CreateText("Text", detailsSelectButton.transform, Vector2.zero, Vector2.one, Vector2.zero, 22f);

        Button closeButton = CreateButton("Close", window, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(132f, 42f));
        closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-88f, -42f);
        closeButton.onClick.AddListener(() => detailsPanel.SetActive(false));
        detailsCloseText = CreateText("Text", closeButton.transform, Vector2.zero, Vector2.one, Vector2.zero, 22f);

        detailsPanel.SetActive(false);
    }

    RectTransform CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        Image image = CreateUIObject(name, parent, anchorMin, anchorMax, size).gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        return image;
    }

    TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, float fontSize)
    {
        TextMeshProUGUI text = CreateUIObject(name, parent, anchorMin, anchorMax, size).gameObject.AddComponent<TextMeshProUGUI>();
        if (uiFont != null)
            text.font = uiFont;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        return text;
    }

    Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        Image image = CreateUIObject(name, parent, anchorMin, anchorMax, size).gameObject.AddComponent<Image>();
        image.color = new Color(0.8039216f, 0.40784314f, 0.23921569f, 1f);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    int GetSkinPrice(int skinIndex)
    {
        if (skinIndex == 2) return SKIN2_PRICE;
        if (skinIndex == 3) return SKIN3_PRICE;
        return 0;
    }

    Sprite GetSkinPreview(int skinIndex)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>($"Skin {skinIndex}/idle");
        return sprites.Length > 0 ? sprites[0] : null;
    }

    Sprite GetCustomSkinPreview(int slot)
    {
        Sprite[] baseSprites = Resources.LoadAll<Sprite>("Skin 1/idle");
        return baseSprites.Length > 0
            ? CustomSkinUtility.BuildCustomSprite(baseSprites[0], CustomSkinUtility.GetSlotBodyA(slot), CustomSkinUtility.GetSlotBodyB(slot), CustomSkinUtility.GetSlotAccent(slot))
            : null;
    }
}
