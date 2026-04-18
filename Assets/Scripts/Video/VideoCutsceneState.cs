using UnityEngine.Video;

public static class VideoCutsceneState
{
    public const string CutsceneSceneName = "Video Scene";
    public const string DefaultReturnSceneName = "Worlds";

    public static bool HasPendingRequest { get; private set; }
    public static VideoClip PendingClip { get; private set; }
    public static string ReturnSceneName { get; private set; } = DefaultReturnSceneName;

    public static void Queue(VideoClip clip, string returnSceneName)
    {
        HasPendingRequest = true;
        PendingClip = clip;
        ReturnSceneName = string.IsNullOrWhiteSpace(returnSceneName)
            ? DefaultReturnSceneName
            : returnSceneName;
    }

    public static void Clear()
    {
        HasPendingRequest = false;
        PendingClip = null;
        ReturnSceneName = DefaultReturnSceneName;
    }
}
