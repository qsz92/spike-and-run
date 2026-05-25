using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public static class PlayerSkinNetwork
{
    private const string SkinKey = "skin";
    private const string BodyAKey = "ws_body_a";
    private const string BodyBKey = "ws_body_b";
    private const string AccentKey = "ws_accent";

    public static void ApplyLocalPlayerProperties()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;

        Hashtable properties = new Hashtable();
        properties[SkinKey] = PlayerPrefs.GetInt("SelectedSkin", 1);
        int slot = CustomSkinUtility.GetSelectedSlot();
        properties[BodyAKey] = CustomSkinUtility.EncodeColor(CustomSkinUtility.GetSlotBodyA(slot));
        properties[BodyBKey] = CustomSkinUtility.EncodeColor(CustomSkinUtility.GetSlotBodyB(slot));
        properties[AccentKey] = CustomSkinUtility.EncodeColor(CustomSkinUtility.GetSlotAccent(slot));

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
    }

    public static int GetSkinIndex(PhotonView view)
    {
        Player owner = view != null ? view.Owner : null;
        if (TryGetProperty(owner, SkinKey, out object value))
            return ToInt(value, PlayerPrefs.GetInt("SelectedSkin", 1));

        return view == null || view.IsMine ? PlayerPrefs.GetInt("SelectedSkin", 1) : 1;
    }

    public static Color GetBodyA(PhotonView view) => GetColor(view, BodyAKey, CustomSkinUtility.DefaultBodyA, CustomSkinUtility.GetBodyA);
    public static Color GetBodyB(PhotonView view) => GetColor(view, BodyBKey, CustomSkinUtility.DefaultBodyB, CustomSkinUtility.GetBodyB);
    public static Color GetAccent(PhotonView view) => GetColor(view, AccentKey, CustomSkinUtility.DefaultAccent, CustomSkinUtility.GetAccent);

    private static Color GetColor(PhotonView view, string propertyKey, Color fallback, System.Func<Color> localGetter)
    {
        Player owner = view != null ? view.Owner : null;
        if (TryGetProperty(owner, propertyKey, out object value))
            return CustomSkinUtility.DecodeColor(value as string, fallback);

        return view == null || view.IsMine ? localGetter() : fallback;
    }

    private static int ToInt(object value, int fallback)
    {
        if (value is int intValue) return intValue;
        if (value is long longValue) return (int)longValue;
        if (value is string stringValue && int.TryParse(stringValue, out int parsed)) return parsed;
        return fallback;
    }

    private static bool TryGetProperty(Player owner, string key, out object value)
    {
        value = null;
        if (owner == null || owner.CustomProperties == null || !owner.CustomProperties.ContainsKey(key))
            return false;

        value = owner.CustomProperties[key];
        return true;
    }
}
