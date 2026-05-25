using UnityEngine;
using UnityEngine.UI;

public class CoinsDisplay : MonoBehaviour
{
    [SerializeField] private Text coinsText;

    void Start()
    {
        UpdateCoinsText();
    }

    void OnEnable()
    {
        AccountManager.ProgressChanged += UpdateCoinsText;
        UpdateCoinsText();
    }

    void OnDisable()
    {
        AccountManager.ProgressChanged -= UpdateCoinsText;
    }

    void Update()
    {
        UpdateCoinsText();
    }

    void UpdateCoinsText()
    {
        if (coinsText == null) return;
        coinsText.text = PlayerPrefs.GetInt("TotalCoins", 0).ToString();
    }
}
