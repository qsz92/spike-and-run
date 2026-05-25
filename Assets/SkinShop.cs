using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class SkinShop : MonoBehaviour
{
    [Header("Skin 1")]
    [SerializeField] private Button skin1SelectBtn;

    [Header("Skin 2 (50 coins)")]
    [SerializeField] private Button skin2BuyBtn;
    [SerializeField] private Button skin2SelectBtn;

    [Header("Skin 3 (100 coins)")]
    [SerializeField] private Button skin3BuyBtn;
    [SerializeField] private Button skin3SelectBtn;

    private const int SKIN2_PRICE = 50;
    private const int SKIN3_PRICE = 100;

    private string LabelSelected => UILocalization.Get("shop_selected");
    private string LabelSelect => UILocalization.Get("shop_select");

    void Start()
    {
        PlayerPrefs.SetInt("SkinOwned_1", 1);
        UpdateUI();
        skin1SelectBtn.onClick.AddListener(() => SelectSkin(1));
        skin2BuyBtn.onClick.AddListener(() => BuySkin(2, SKIN2_PRICE));
        skin2SelectBtn.onClick.AddListener(() => SelectSkin(2));
        skin3BuyBtn.onClick.AddListener(() => BuySkin(3, SKIN3_PRICE));
        skin3SelectBtn.onClick.AddListener(() => SelectSkin(3));
    }

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        AccountManager.ProgressChanged += UpdateUI;
        UpdateUI();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        AccountManager.ProgressChanged -= UpdateUI;
    }

    void OnSelectedLocaleChanged(Locale _) => UpdateUI();

    void UpdateUI()
    {
        int coins = PlayerPrefs.GetInt("TotalCoins", 0);
        int selected = PlayerPrefs.GetInt("SelectedSkin", 1);

        UpdateSkinUI(skin1SelectBtn, null, 1, selected, coins, 0, true);
        UpdateSkinUI(skin2SelectBtn, skin2BuyBtn, 2, selected, coins, SKIN2_PRICE, PlayerPrefs.GetInt("SkinOwned_2", 0) == 1);
        UpdateSkinUI(skin3SelectBtn, skin3BuyBtn, 3, selected, coins, SKIN3_PRICE, PlayerPrefs.GetInt("SkinOwned_3", 0) == 1);
    }

    void UpdateSkinUI(Button selectBtn, Button buyBtn, int skinIndex, int selected, int coins, int price, bool owned)
    {
        if (buyBtn != null)
        {
            buyBtn.gameObject.SetActive(!owned);
            buyBtn.interactable = coins >= price;
            TextMeshProUGUI buyLabel = buyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (buyLabel != null)
                buyLabel.text = UILocalization.Get("shop_buy");
        }
        selectBtn.gameObject.SetActive(owned);
        if (owned)
        {
            selectBtn.interactable = selected != skinIndex;
            TextMeshProUGUI selectLabel = selectBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (selectLabel != null)
                selectLabel.text = selected == skinIndex ? LabelSelected : LabelSelect;
        }
    }

    void BuySkin(int index, int price)
    {
        int coins = PlayerPrefs.GetInt("TotalCoins", 0);
        if (coins < price) return;
        PlayerPrefs.SetInt("TotalCoins", coins - price);
        PlayerPrefs.SetInt($"SkinOwned_{index}", 1);
        if (!PlayerPrefs.HasKey($"SkinBoughtAt_{index}"))
            PlayerPrefs.SetString($"SkinBoughtAt_{index}", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        PlayerPrefs.Save();
        AccountManager.SaveProgress();
        AccountManager.NotifyProgressChanged();
        UpdateUI();
    }

    void SelectSkin(int index)
    {
        PlayerPrefs.SetInt("SelectedSkin", index);
        PlayerPrefs.DeleteKey(CustomSkinUtility.SelectedSlotKey);
        PlayerSkinNetwork.ApplyLocalPlayerProperties();
        AccountManager.SaveProgress();
        AccountManager.NotifyProgressChanged();
        UpdateUI();
    }
}
