using Photon.Pun;
using UnityEngine;

public static class PlayerNameStore
{
    private const string PlayerNameKey = "PlayerName";
    private const int MaxNameLength = 20;

    public static bool HasSavedName()
    {
        return !string.IsNullOrEmpty(GetSavedName());
    }

    public static string GetSavedName()
    {
        return Sanitize(PlayerPrefs.GetString(PlayerNameKey, string.Empty));
    }

    public static void SaveName(string playerName)
    {
        string sanitizedName = Sanitize(playerName);

        PlayerPrefs.SetString(PlayerNameKey, sanitizedName);
        PlayerPrefs.Save();
        PhotonNetwork.NickName = sanitizedName;
    }

    public static void ApplySavedName()
    {
        PhotonNetwork.NickName = GetSavedName();
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        return trimmed.Length <= MaxNameLength ? trimmed : trimmed.Substring(0, MaxNameLength);
    }
}
