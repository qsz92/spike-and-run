using UnityEngine;
using UnityEngine.Localization.Settings;

public class LanguageSwitcher : MonoBehaviour
{
    public void ToggleLanguage()
    {
        var current = LocalizationSettings.SelectedLocale.Identifier.Code;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        foreach (var locale in locales)
        {
            if (locale.Identifier.Code != current)
            {
                LocalizationSettings.SelectedLocale = locale;
                break;
            }
        }
    }
}