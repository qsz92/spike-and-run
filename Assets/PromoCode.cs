using UnityEngine;
using TMPro;
using System.Collections;

public class PromoCode : MonoBehaviour
{
    [SerializeField] private TMP_InputField promoInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private CanvasGroup statusPanel;

    public void ApplyPromo()
    {
        string code = promoInput.text.Trim().ToUpper();
        string usedKey = $"Promo_{code}_Used";

        if (PlayerPrefs.GetInt(usedKey, 0) == 1)
        {
            ShowStatus(UILocalization.Get("promo_already_used"));
            return;
        }

        if (code == "TEST")
        {
            PlayerPrefs.SetInt("TotalCoins", PlayerPrefs.GetInt("TotalCoins", 0) + 1000);
            PlayerPrefs.SetInt(usedKey, 1);
            AccountManager.SaveProgress();
            AccountManager.NotifyProgressChanged();
            ShowStatus(UILocalization.Get("promo_reward"));
        }
        else
        {
            ShowStatus(UILocalization.Get("promo_invalid_code"));
        }
    }

    void ShowStatus(string message)
    {
        statusText.text = message;
        StopAllCoroutines();
        StartCoroutine(FadeStatus());
    }

    IEnumerator FadeStatus()
    {
        statusPanel.gameObject.SetActive(true);

        // Если уже видна — не фейдим заново, просто сбрасываем таймер
        if (statusPanel.alpha < 1f)
        {
            statusPanel.alpha = 0f;
            while (statusPanel.alpha < 1f)
            {
                statusPanel.alpha += Time.deltaTime * 3f;
                yield return null;
            }
        }

        statusPanel.alpha = 1f;
        yield return new WaitForSeconds(5f);

        while (statusPanel.alpha > 0f)
        {
            statusPanel.alpha -= Time.deltaTime * 3f;
            yield return null;
        }
        statusPanel.gameObject.SetActive(false);
    }
}
