using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;

public static class UILocalization
{
    private const string Table = "UI";
    private static readonly Dictionary<string, string> FallbackValues = new Dictionary<string, string>
    {
        { "account_title", "Account" },
        { "account_login_placeholder", "Login" },
        { "account_password_placeholder", "Password" },
        { "account_login_button", "Log in" },
        { "account_register_button", "Register" },
        { "account_logout_button", "Log out" },
        { "account_guest", "Guest" },
        { "account_status_logging_in", "Logging in..." },
        { "account_status_registering", "Registering..." }
    };

    public static string Get(string key)
    {
        if (LocalizationSettings.SelectedLocale == null)
            return Fallback(key);

        try
        {
            string value = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
            return string.IsNullOrEmpty(value) ? Fallback(key) : value;
        }
        catch
        {
            return Fallback(key);
        }
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    private static string Fallback(string key)
    {
        return FallbackValues.TryGetValue(key, out string value) ? value : key;
    }
}
