using System;
using System.Text;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

public static class AccountManager
{
    private const string CurrentAccountKey = "Account_Current";
    private const string SessionKey = "Account_Session";

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Session);
    public static string CurrentUsername => PlayerPrefs.GetString(CurrentAccountKey, "");
    public static event Action ProgressChanged;

    private static string Session => PlayerPrefs.GetString(SessionKey, "");
    private static string serverUrl = "YOUR_ACCOUNT_SERVER_URL_HERE";
    private static MonoBehaviour runner;

    public static void Configure(string accountServerUrl, MonoBehaviour coroutineRunner)
    {
        serverUrl = string.IsNullOrWhiteSpace(accountServerUrl) ? serverUrl : accountServerUrl.Trim().TrimEnd('/');
        if (runner == null)
        {
            GameObject runnerObject = new GameObject("AccountManagerRunner");
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            runner = runnerObject.AddComponent<AccountManagerRunner>();
        }
        EnsureRuntimeDefaults();
    }

    public static void AutoLoad() => EnsureRuntimeDefaults();

    public static void Register(string username, string password, Action<bool, string> done)
    {
        username = NormalizeUsername(username);
        if (!CanSend(username, password, done)) return;

        string json = "{\"username\":\"" + JsonEscape(username) + "\",\"password\":\"" + JsonEscape(password) + "\",\"progress\":" + JsonUtility.ToJson(ReadRuntimeProgress()) + "}";
        runner.StartCoroutine(Post("/register", json, response =>
        {
            if (!ApplyAuthResponse(response, done)) return;
            done(true, UILocalization.Get("account_created"));
        }));
    }

    public static void Login(string username, string password, Action<bool, string> done)
    {
        username = NormalizeUsername(username);
        if (!CanSend(username, password, done)) return;

        string json = "{\"username\":\"" + JsonEscape(username) + "\",\"password\":\"" + JsonEscape(password) + "\"}";
        runner.StartCoroutine(Post("/login", json, response =>
        {
            if (!ApplyAuthResponse(response, done)) return;
            done(true, UILocalization.Get("account_login_done"));
        }));
    }

    public static void Logout()
    {
        SaveProgress();
        ResetGuestProgress();
        PlayerPrefs.DeleteKey(SessionKey);
        PlayerPrefs.DeleteKey(CurrentAccountKey);
        PlayerPrefs.Save();
        NotifyProgressChanged();
    }

    public static void SaveProgress()
    {
        if (!IsLoggedIn || runner == null) return;

        string json = "{\"session\":\"" + JsonEscape(Session) + "\",\"progress\":" + JsonUtility.ToJson(ReadRuntimeProgress()) + "}";
        runner.StartCoroutine(Post("/progress/save", json, _ => { }));
    }

    private static bool ApplyAuthResponse(ServerResponse response, Action<bool, string> done)
    {
        if (!response.ok)
        {
            done(false, response.message);
            return false;
        }

        AuthResponse auth = JsonUtility.FromJson<AuthResponse>(response.body);
        if (auth == null || string.IsNullOrEmpty(auth.session))
        {
            done(false, UILocalization.Get("account_server_no_session"));
            return false;
        }

        PlayerPrefs.SetString(CurrentAccountKey, auth.username);
        PlayerPrefs.SetString(SessionKey, auth.session);
        ApplyProgress(auth.progress);
        PlayerPrefs.Save();
        NotifyProgressChanged();
        return true;
    }

    private static bool CanSend(string username, string password, Action<bool, string> done)
    {
        if (runner == null)
        {
            done(false, UILocalization.Get("account_ui_not_configured"));
            return false;
        }

        if (string.IsNullOrWhiteSpace(serverUrl) || serverUrl.StartsWith("YOUR_"))
        {
            done(false, UILocalization.Get("account_ui_not_configured"));
            return false;
        }

        if (username.Length < 3)
        {
            done(false, UILocalization.Get("account_login_min"));
            return false;
        }

        if (password.Length < 6)
        {
            done(false, UILocalization.Get("account_password_min"));
            return false;
        }

        return true;
    }

    private static System.Collections.IEnumerator Post(string path, string json, Action<ServerResponse> done)
    {
        UnityWebRequest request = new UnityWebRequest(serverUrl + path, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = 6;
        request.SetRequestHeader("Content-Type", "application/json");

        UnityWebRequestAsyncOperation operation;
        try
        {
            operation = request.SendWebRequest();
        }
        catch (Exception exception)
        {
            request.Dispose();
            done(new ServerResponse { ok = false, body = "", message = LocalizeServerMessage(exception.Message) });
            yield break;
        }

        yield return operation;

        string body = request.downloadHandler != null ? request.downloadHandler.text : "";
        bool networkOk = request.result == UnityWebRequest.Result.Success;
        BasicResponse parsed = string.IsNullOrEmpty(body) ? null : JsonUtility.FromJson<BasicResponse>(body);
        bool ok = networkOk && (parsed == null || parsed.ok);
        string message = ok ? "" : (!string.IsNullOrEmpty(parsed?.message) ? parsed.message : request.error);
        request.Dispose();
        done(new ServerResponse { ok = ok, body = body, message = string.IsNullOrEmpty(message) ? UILocalization.Get("account_server_error") : LocalizeServerMessage(message) });
    }

    private static string LocalizeServerMessage(string message)
    {
        switch (message)
        {
            case "Аккаунт уже существует":
                return UILocalization.Get("account_exists");
            case "Аккаунт не найден":
                return UILocalization.Get("account_not_found");
            case "Неверный пароль":
                return UILocalization.Get("account_wrong_password");
            case "Сессия истекла":
                return UILocalization.Get("account_session_expired");
            case "Not found":
            case "Bad JSON":
                return UILocalization.Get("account_server_error");
            default:
                return string.IsNullOrEmpty(message) ? UILocalization.Get("account_server_error") : message;
        }
    }

    private static ProgressData ReadRuntimeProgress()
    {
        return new ProgressData
        {
            TotalCoins = PlayerPrefs.GetInt("TotalCoins", 0),
            SelectedSkin = PlayerPrefs.GetInt("SelectedSkin", 1),
            SkinOwned2 = PlayerPrefs.GetInt("SkinOwned_2", 0),
            SkinOwned3 = PlayerPrefs.GetInt("SkinOwned_3", 0),
            SkinBoughtAt2 = PlayerPrefs.GetString("SkinBoughtAt_2", ""),
            SkinBoughtAt3 = PlayerPrefs.GetString("SkinBoughtAt_3", ""),
            SelectedWorkshopSkinSlot = PlayerPrefs.GetInt(CustomSkinUtility.SelectedSlotKey, 0),
            WorkshopSkins = CustomSkinUtility.ExportSkins(),
            PromoTestUsed = PlayerPrefs.GetInt("Promo_TEST_Used", 0),
            PlayerName = PlayerPrefs.GetString("PlayerName", CurrentUsername)
        };
    }

    private static void ApplyProgress(ProgressData progress)
    {
        if (progress == null)
        {
            EnsureRuntimeDefaults();
            return;
        }

        PlayerPrefs.SetInt("TotalCoins", progress.TotalCoins);
        ApplyWorkshopProgress(progress);
        int selectedSkin = Mathf.Max(1, progress.SelectedSkin);
        if (selectedSkin == CustomSkinUtility.CustomSkinIndex && CustomSkinUtility.GetSlotCount() == 0)
            selectedSkin = 1;
        PlayerPrefs.SetInt("SelectedSkin", selectedSkin);
        PlayerPrefs.SetInt(CustomSkinUtility.SelectedSlotKey, Mathf.Clamp(progress.SelectedWorkshopSkinSlot, 0, Mathf.Max(0, CustomSkinUtility.GetSlotCount() - 1)));
        PlayerPrefs.SetInt("SkinOwned_1", 1);
        PlayerPrefs.SetInt("SkinOwned_2", progress.SkinOwned2);
        PlayerPrefs.SetInt("SkinOwned_3", progress.SkinOwned3);
        PlayerPrefs.SetInt("Promo_TEST_Used", progress.PromoTestUsed);
        SetOrDelete("SkinBoughtAt_2", progress.SkinBoughtAt2);
        SetOrDelete("SkinBoughtAt_3", progress.SkinBoughtAt3);
        PlayerPrefs.SetString("PlayerName", string.IsNullOrWhiteSpace(progress.PlayerName) ? CurrentUsername : progress.PlayerName);
        PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName", CurrentUsername);
        PlayerSkinNetwork.ApplyLocalPlayerProperties();
    }

    private static void EnsureRuntimeDefaults()
    {
        PlayerPrefs.SetInt("SkinOwned_1", 1);
        if (!PlayerPrefs.HasKey("SelectedSkin"))
            PlayerPrefs.SetInt("SelectedSkin", 1);
    }

    private static void ResetGuestProgress()
    {
        PlayerPrefs.SetInt("TotalCoins", 0);
        PlayerPrefs.SetInt("SelectedSkin", 1);
        PlayerPrefs.SetInt("SkinOwned_1", 1);
        PlayerPrefs.DeleteKey("SkinOwned_2");
        PlayerPrefs.DeleteKey("SkinOwned_3");
        PlayerPrefs.DeleteKey("SkinBoughtAt_2");
        PlayerPrefs.DeleteKey("SkinBoughtAt_3");
        CustomSkinUtility.ClearSkins();
        PlayerPrefs.DeleteKey("Promo_TEST_Used");
        PlayerPrefs.DeleteKey("PlayerName");
        PhotonNetwork.NickName = "";
        PlayerSkinNetwork.ApplyLocalPlayerProperties();
    }

    private static void ApplyWorkshopProgress(ProgressData progress)
    {
        if (!string.IsNullOrWhiteSpace(progress.WorkshopSkins))
        {
            CustomSkinUtility.ImportSkins(progress.WorkshopSkins);
            return;
        }

        if (progress.WorkshopSkinOwned == 1)
        {
            CustomSkinUtility.ClearSkins();
            CustomSkinUtility.AddSkin(
                CustomSkinUtility.DecodeColor(progress.WorkshopSkinBodyA, CustomSkinUtility.DefaultBodyA),
                CustomSkinUtility.DecodeColor(progress.WorkshopSkinBodyB, CustomSkinUtility.DefaultBodyB),
                CustomSkinUtility.DecodeColor(progress.WorkshopSkinAccent, CustomSkinUtility.DefaultAccent),
                progress.WorkshopSkinBoughtAt);
            return;
        }

        CustomSkinUtility.ClearSkins();
    }

    public static void NotifyProgressChanged()
    {
        ProgressChanged?.Invoke();
    }

    private static void SetOrDelete(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
            PlayerPrefs.DeleteKey(key);
        else
            PlayerPrefs.SetString(key, value);
    }

    private static string NormalizeUsername(string username) => (username ?? "").Trim().ToLowerInvariant();

    private static string JsonEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private struct ServerResponse
    {
        public bool ok;
        public string body;
        public string message;
    }

    [Serializable] private class BasicResponse { public bool ok; public string message; }
    [Serializable] private class AuthResponse { public bool ok; public string username; public string session; public ProgressData progress; }
    private class AccountManagerRunner : MonoBehaviour { }

    [Serializable]
    private class ProgressData
    {
        public int TotalCoins;
        public int SelectedSkin;
        public int SkinOwned2;
        public int SkinOwned3;
        public string SkinBoughtAt2;
        public string SkinBoughtAt3;
        public int SelectedWorkshopSkinSlot;
        public string WorkshopSkins;
        public int WorkshopSkinOwned;
        public string WorkshopSkinBodyA;
        public string WorkshopSkinBodyB;
        public string WorkshopSkinAccent;
        public string WorkshopSkinBoughtAt;
        public int PromoTestUsed;
        public string PlayerName;
    }
}
