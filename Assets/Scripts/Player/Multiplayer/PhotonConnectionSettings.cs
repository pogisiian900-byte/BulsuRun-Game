using Photon.Pun;

public static class PhotonConnectionSettings
{
    public const string FixedRegionCode = "asia";

    public static void ApplyRuntimeSettings()
    {
        if (PhotonNetwork.PhotonServerSettings == null || PhotonNetwork.PhotonServerSettings.AppSettings == null)
        {
            return;
        }

        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = FixedRegionCode;
    }

    public static string DescribeConnectionBucket()
    {
        string region = !string.IsNullOrWhiteSpace(PhotonNetwork.CloudRegion)
            ? PhotonNetwork.CloudRegion
            : PhotonNetwork.PhotonServerSettings?.AppSettings?.FixedRegion;

        string appVersion = !string.IsNullOrWhiteSpace(PhotonNetwork.AppVersion)
            ? PhotonNetwork.AppVersion
            : PhotonNetwork.PhotonServerSettings?.AppSettings?.AppVersion;

        return "region=" + (region ?? "unknown") + ", appVersion=" + (appVersion ?? "unknown");
    }
}
