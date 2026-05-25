using System.Collections.Generic;
using UnityEngine;

public static class CustomSkinUtility
{
    public const int CustomSkinIndex = 100;
    public const int WorkshopPrice = 300;
    public const string OwnedKey = "WorkshopSkinOwned";
    public const string BodyAKey = "WorkshopSkinBodyA";
    public const string BodyBKey = "WorkshopSkinBodyB";
    public const string AccentKey = "WorkshopSkinAccent";
    public const string BoughtAtKey = "WorkshopSkinBoughtAt";
    public const string CountKey = "WorkshopSkinCount";
    public const string SelectedSlotKey = "SelectedWorkshopSkinSlot";

    public static readonly Color DefaultBodyA = new Color(0.86f, 0.12f, 0.22f);
    public static readonly Color DefaultBodyB = new Color(0.72f, 0.78f, 0.73f);
    public static readonly Color DefaultAccent = new Color(1f, 0.56f, 0.05f);

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<int, Texture2D> ReadableTextureCache = new Dictionary<int, Texture2D>();

    public static bool IsOwned => GetSlotCount() > 0;

    public static Color GetBodyA() => GetSlotBodyA(GetSelectedSlot());
    public static Color GetBodyB() => GetSlotBodyB(GetSelectedSlot());
    public static Color GetAccent() => GetSlotAccent(GetSelectedSlot());

    public static int AddSkin(Color bodyA, Color bodyB, Color accent, string boughtAt)
    {
        MigrateLegacySingleSkin();

        int slot = GetSlotCount();
        PlayerPrefs.SetString(SlotBodyAKey(slot), EncodeColor(bodyA));
        PlayerPrefs.SetString(SlotBodyBKey(slot), EncodeColor(bodyB));
        PlayerPrefs.SetString(SlotAccentKey(slot), EncodeColor(accent));
        PlayerPrefs.SetString(SlotBoughtAtKey(slot), boughtAt);
        PlayerPrefs.SetInt(CountKey, slot + 1);
        PlayerPrefs.SetInt(OwnedKey, 1);
        PlayerPrefs.SetInt(SelectedSlotKey, slot);
        return slot;
    }

    public static int GetSlotCount()
    {
        MigrateLegacySingleSkin();
        return Mathf.Max(0, PlayerPrefs.GetInt(CountKey, 0));
    }

    public static int GetSelectedSlot()
    {
        int count = GetSlotCount();
        if (count <= 0) return 0;
        return Mathf.Clamp(PlayerPrefs.GetInt(SelectedSlotKey, 0), 0, count - 1);
    }

    public static bool IsSelectedSlot(int slot) => PlayerPrefs.GetInt("SelectedSkin", 1) == CustomSkinIndex && GetSelectedSlot() == slot;

    public static void SelectSlot(int slot)
    {
        if (!IsValidSlot(slot)) return;
        PlayerPrefs.SetInt("SelectedSkin", CustomSkinIndex);
        PlayerPrefs.SetInt(SelectedSlotKey, slot);
    }

    public static bool IsValidSlot(int slot) => slot >= 0 && slot < GetSlotCount();

    public static Color GetSlotBodyA(int slot) => GetColor(SlotBodyAKey(slot), DefaultBodyA);
    public static Color GetSlotBodyB(int slot) => GetColor(SlotBodyBKey(slot), DefaultBodyB);
    public static Color GetSlotAccent(int slot) => GetColor(SlotAccentKey(slot), DefaultAccent);
    public static string GetSlotBoughtAt(int slot) => PlayerPrefs.GetString(SlotBoughtAtKey(slot), "");

    public static string SlotBodyAKey(int slot) => $"WorkshopSkin_{slot}_BodyA";
    public static string SlotBodyBKey(int slot) => $"WorkshopSkin_{slot}_BodyB";
    public static string SlotAccentKey(int slot) => $"WorkshopSkin_{slot}_Accent";
    public static string SlotBoughtAtKey(int slot) => $"WorkshopSkin_{slot}_BoughtAt";

    public static string ExportSkins()
    {
        MigrateLegacySingleSkin();

        int count = GetSlotCount();
        string[] rows = new string[count];
        for (int slot = 0; slot < count; slot++)
        {
            rows[slot] = string.Join("|",
                PlayerPrefs.GetString(SlotBodyAKey(slot), EncodeColor(DefaultBodyA)),
                PlayerPrefs.GetString(SlotBodyBKey(slot), EncodeColor(DefaultBodyB)),
                PlayerPrefs.GetString(SlotAccentKey(slot), EncodeColor(DefaultAccent)),
                PlayerPrefs.GetString(SlotBoughtAtKey(slot), ""));
        }
        return string.Join(";", rows);
    }

    public static void ImportSkins(string data)
    {
        ClearSkins();

        if (string.IsNullOrWhiteSpace(data))
            return;

        string[] rows = data.Split(';');
        int count = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i])) continue;

            string[] parts = rows[i].Split('|');
            PlayerPrefs.SetString(SlotBodyAKey(count), parts.Length > 0 ? parts[0] : EncodeColor(DefaultBodyA));
            PlayerPrefs.SetString(SlotBodyBKey(count), parts.Length > 1 ? parts[1] : EncodeColor(DefaultBodyB));
            PlayerPrefs.SetString(SlotAccentKey(count), parts.Length > 2 ? parts[2] : EncodeColor(DefaultAccent));
            PlayerPrefs.SetString(SlotBoughtAtKey(count), parts.Length > 3 ? parts[3] : "");
            count++;
        }

        PlayerPrefs.SetInt(CountKey, count);
        PlayerPrefs.SetInt(OwnedKey, count > 0 ? 1 : 0);
    }

    public static void ClearSkins()
    {
        int count = Mathf.Max(GetSlotCount(), 32);
        for (int slot = 0; slot < count; slot++)
        {
            PlayerPrefs.DeleteKey(SlotBodyAKey(slot));
            PlayerPrefs.DeleteKey(SlotBodyBKey(slot));
            PlayerPrefs.DeleteKey(SlotAccentKey(slot));
            PlayerPrefs.DeleteKey(SlotBoughtAtKey(slot));
        }

        PlayerPrefs.DeleteKey(CountKey);
        PlayerPrefs.DeleteKey(SelectedSlotKey);
        PlayerPrefs.DeleteKey(OwnedKey);
        PlayerPrefs.DeleteKey(BodyAKey);
        PlayerPrefs.DeleteKey(BodyBKey);
        PlayerPrefs.DeleteKey(AccentKey);
        PlayerPrefs.DeleteKey(BoughtAtKey);
    }

    private static void MigrateLegacySingleSkin()
    {
        if (PlayerPrefs.GetInt(CountKey, 0) > 0 || PlayerPrefs.GetInt(OwnedKey, 0) != 1)
            return;

        PlayerPrefs.SetString(SlotBodyAKey(0), PlayerPrefs.GetString(BodyAKey, EncodeColor(DefaultBodyA)));
        PlayerPrefs.SetString(SlotBodyBKey(0), PlayerPrefs.GetString(BodyBKey, EncodeColor(DefaultBodyB)));
        PlayerPrefs.SetString(SlotAccentKey(0), PlayerPrefs.GetString(AccentKey, EncodeColor(DefaultAccent)));
        PlayerPrefs.SetString(SlotBoughtAtKey(0), PlayerPrefs.GetString(BoughtAtKey, ""));
        PlayerPrefs.SetInt(CountKey, 1);
        PlayerPrefs.SetInt(SelectedSlotKey, 0);
    }

    public static string EncodeColor(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);

    public static Color DecodeColor(string value, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(value) && ColorUtility.TryParseHtmlString(value, out Color color))
            return color;
        return fallback;
    }

    public static Color GetColor(string key, Color fallback) => DecodeColor(PlayerPrefs.GetString(key, ""), fallback);

    public static Sprite[] BuildCustomSprites(Sprite[] sourceSprites, Color bodyA, Color bodyB, Color accent)
    {
        if (sourceSprites == null) return new Sprite[0];

        Sprite[] result = new Sprite[sourceSprites.Length];
        for (int i = 0; i < sourceSprites.Length; i++)
            result[i] = BuildCustomSprite(sourceSprites[i], bodyA, bodyB, accent);
        return result;
    }

    public static Sprite BuildCustomSprite(Sprite source, Color bodyA, Color bodyB, Color accent)
    {
        if (source == null) return null;

        string key = $"{source.GetInstanceID()}_{EncodeColor(bodyA)}_{EncodeColor(bodyB)}_{EncodeColor(accent)}";
        if (SpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D readable = GetReadableTexture(source.texture);
        Rect sourceRect = source.textureRect;
        int width = Mathf.RoundToInt(sourceRect.width);
        int height = Mathf.RoundToInt(sourceRect.height);
        int startX = Mathf.RoundToInt(sourceRect.x);
        int startY = Mathf.RoundToInt(sourceRect.y);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = readable.GetPixel(startX + x, startY + y);
                texture.SetPixel(x, y, RecolorPixel(pixel, bodyA, bodyB, accent));
            }
        }

        texture.Apply(false, false);

        Vector2 pivot = new Vector2(source.pivot.x / sourceRect.width, source.pivot.y / sourceRect.height);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot, source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);
        SpriteCache[key] = sprite;
        return sprite;
    }

    private static Texture2D GetReadableTexture(Texture2D source)
    {
        int key = source.GetInstanceID();
        if (ReadableTextureCache.TryGetValue(key, out Texture2D cached) && cached != null)
            return cached;

        RenderTexture previous = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readable.Apply(false, false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        ReadableTextureCache[key] = readable;
        return readable;
    }

    private static Color RecolorPixel(Color pixel, Color bodyA, Color bodyB, Color accent)
    {
        if (pixel.a <= 0.01f) return pixel;

        Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
        Color target;

        if (IsAccent(hue, saturation, value))
            target = accent;
        else if (IsBodyA(hue, saturation, value))
            target = bodyA;
        else if (IsBodyB(pixel, hue, saturation, value))
            target = bodyB;
        else
            return pixel;

        float shade = Mathf.Clamp(value / 0.72f, 0.45f, 1.35f);
        Color shaded = target * shade;
        shaded.a = pixel.a;
        return shaded;
    }

    private static bool IsAccent(float hue, float saturation, float value)
    {
        return saturation > 0.42f && value > 0.32f && hue >= 0.055f && hue <= 0.16f;
    }

    private static bool IsBodyA(float hue, float saturation, float value)
    {
        return saturation > 0.35f && value > 0.24f && (hue <= 0.035f || hue >= 0.92f);
    }

    private static bool IsBodyB(Color pixel, float hue, float saturation, float value)
    {
        bool visorBlue = pixel.b > pixel.r + 0.08f && pixel.b > pixel.g + 0.02f && value > 0.55f;
        if (visorBlue) return false;
        return saturation < 0.26f && value > 0.34f && value < 0.86f;
    }
}
