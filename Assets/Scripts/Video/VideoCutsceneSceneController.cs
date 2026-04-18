using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoCutsceneSceneController : MonoBehaviour
{
    private const float PrepareTimeoutSeconds = 20f;
    private const float MinimumPlaybackRealtimeBeforeAutoCompleteSeconds = 0.5f;
    private const float PlaybackProgressStallToleranceSeconds = 0.35f;
    private const double MinimumPlaybackProgressSeconds = 0.05d;
    private const double VideoEndToleranceSeconds = 0.1d;

    private static bool isInitialized;

    private bool hasObservedPlaybackProgress;
    private bool hasPlaybackStarted;
    private bool isTransitioning;
    private double lastObservedVideoTime;
    private float lastObservedPlaybackProgressAtUnscaledTime;
    private long lastObservedFrame;
    private float playbackStartedAtUnscaledTime;
    private Button skipButton;
    private VideoPlayer videoPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureControllerExists(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureControllerExists(scene);
    }

    private static void EnsureControllerExists(Scene scene)
    {
        if (scene.name != VideoCutsceneState.CutsceneSceneName)
            return;

        if (FindFirstObjectByType<VideoCutsceneSceneController>(FindObjectsInactive.Include) != null)
            return;

        GameObject controllerObject = new GameObject(nameof(VideoCutsceneSceneController));
        SceneManager.MoveGameObjectToScene(controllerObject, scene);
        controllerObject.AddComponent<VideoCutsceneSceneController>();
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        hasPlaybackStarted = false;
        hasObservedPlaybackProgress = false;
        lastObservedFrame = -1L;
        lastObservedVideoTime = -1d;
        playbackStartedAtUnscaledTime = 0f;
        lastObservedPlaybackProgressAtUnscaledTime = 0f;

        videoPlayer = FindFirstObjectByType<VideoPlayer>(FindObjectsInactive.Include);
        if (videoPlayer == null)
        {
            Debug.LogWarning("Video Scene has no VideoPlayer. Returning to the requested scene.");
            LoadReturnScene();
            yield break;
        }

        if (VideoCutsceneState.HasPendingRequest && VideoCutsceneState.PendingClip != null)
            videoPlayer.clip = VideoCutsceneState.PendingClip;

        if (videoPlayer.clip == null && string.IsNullOrWhiteSpace(videoPlayer.url))
        {
            Debug.LogWarning("Video Scene has no assigned clip or URL. Returning to the requested scene.", this);
            LoadReturnScene();
            yield break;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.Stop();
        videoPlayer.loopPointReached += HandleVideoFinished;
        videoPlayer.errorReceived += HandleVideoError;

        EnsureSkipButton();
        yield return StartCoroutine(PrepareAndPlay());
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipCutscene);

        VideoCutsceneState.Clear();
    }

    private IEnumerator PrepareAndPlay()
    {
        string clipLabel = GetActiveVideoLabel();
        videoPlayer.Prepare();

        float remainingPrepareTime = PrepareTimeoutSeconds;
        while (!videoPlayer.isPrepared && remainingPrepareTime > 0f)
        {
            remainingPrepareTime -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogWarning(
                $"Video preparation timed out after {PrepareTimeoutSeconds:0.#} seconds for '{clipLabel}'. Returning to '{GetReturnSceneName()}'.",
                this);
            LoadReturnScene();
            yield break;
        }

        lastObservedFrame = videoPlayer.frame;
        lastObservedVideoTime = videoPlayer.time;
        hasPlaybackStarted = true;
        playbackStartedAtUnscaledTime = Time.unscaledTime;
        lastObservedPlaybackProgressAtUnscaledTime = playbackStartedAtUnscaledTime;
        videoPlayer.Play();
        StartCoroutine(WatchForVideoCompletion());
    }

    private void EnsureSkipButton()
    {
        bool canUseSkipButton = CanControlCutsceneTransition();

        Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Cutscene Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas.enabled = true;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null && canvasRect.localScale.sqrMagnitude < 0.001f)
                canvasRect.localScale = Vector3.one;
        }

        EnsureEventSystemExists();

        skipButton = FindExistingSkipButton();
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(canUseSkipButton);
            skipButton.onClick.RemoveListener(SkipCutscene);

            if (canUseSkipButton)
                skipButton.onClick.AddListener(SkipCutscene);

            return;
        }

        if (!canUseSkipButton)
            return;

        GameObject buttonObject = new GameObject("Skip Button");
        buttonObject.transform.SetParent(canvas.transform, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-32f, -32f);
        buttonRect.sizeDelta = new Vector2(180f, 56f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0f, 0f, 0f, 0.75f);

        skipButton = buttonObject.AddComponent<Button>();
        skipButton.targetGraphic = buttonImage;
        skipButton.onClick.AddListener(SkipCutscene);

        ColorBlock colors = skipButton.colors;
        colors.normalColor = buttonImage.color;
        colors.highlightedColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        colors.pressedColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        skipButton.colors = colors;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.AddComponent<Text>();
        label.text = "Skip";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.fontSize = 28;
        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    private Button FindExistingSkipButton()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            string objectName = buttons[i].gameObject.name;
            if (!string.IsNullOrEmpty(objectName) &&
                objectName.IndexOf("skip", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return buttons[i];
            }

            Text legacyLabel = buttons[i].GetComponentInChildren<Text>(true);
            if (legacyLabel != null &&
                !string.IsNullOrEmpty(legacyLabel.text) &&
                legacyLabel.text.IndexOf("skip", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return buttons[i];
            }

            TMP_Text tmpLabel = buttons[i].GetComponentInChildren<TMP_Text>(true);
            if (tmpLabel != null &&
                !string.IsNullOrEmpty(tmpLabel.text) &&
                tmpLabel.text.IndexOf("skip", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return buttons[i];
            }
        }

        return buttons.Length == 1 ? buttons[0] : null;
    }

    private IEnumerator WatchForVideoCompletion()
    {
        while (!isTransitioning && videoPlayer != null)
        {
            UpdatePlaybackProgressState();

            if (HasPlaybackCompleted())
            {
                LoadReturnScene();
                yield break;
            }

            yield return null;
        }
    }

    private bool HasPlaybackCompleted()
    {
        if (!hasPlaybackStarted || videoPlayer == null || !hasObservedPlaybackProgress)
            return false;

        float realtimeSincePlaybackStarted = Time.unscaledTime - playbackStartedAtUnscaledTime;
        if (realtimeSincePlaybackStarted < MinimumPlaybackRealtimeBeforeAutoCompleteSeconds)
            return false;

        bool playbackStoppedAfterStarting = !videoPlayer.isPlaying;
        if (!playbackStoppedAfterStarting)
            return false;

        if (Time.unscaledTime - lastObservedPlaybackProgressAtUnscaledTime < PlaybackProgressStallToleranceSeconds)
            return false;

        if (videoPlayer.clip != null &&
            videoPlayer.frameCount > 0 &&
            videoPlayer.frame >= (long)videoPlayer.frameCount - 1)
        {
            return true;
        }

        if (videoPlayer.length <= 0d)
            return true;

        return videoPlayer.time >= videoPlayer.length - VideoEndToleranceSeconds;
    }

    private void UpdatePlaybackProgressState()
    {
        if (videoPlayer == null)
            return;

        long currentFrame = videoPlayer.frame;
        double currentTime = videoPlayer.time;
        bool frameAdvanced = currentFrame > lastObservedFrame;
        bool timeAdvanced = currentTime > lastObservedVideoTime + MinimumPlaybackProgressSeconds;

        if (frameAdvanced || timeAdvanced)
        {
            hasObservedPlaybackProgress = true;
            lastObservedPlaybackProgressAtUnscaledTime = Time.unscaledTime;
        }

        if (currentFrame > lastObservedFrame)
            lastObservedFrame = currentFrame;

        if (currentTime > lastObservedVideoTime)
            lastObservedVideoTime = currentTime;
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        LoadReturnScene();
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("Video playback failed: " + message, this);
        LoadReturnScene();
    }

    private void SkipCutscene()
    {
        if (!CanControlCutsceneTransition())
            return;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        LoadReturnScene();
    }

    private void LoadReturnScene()
    {
        if (isTransitioning)
            return;

        if (!CanControlCutsceneTransition())
        {
            isTransitioning = true;
            return;
        }

        isTransitioning = true;

        string targetScene = VideoCutsceneState.HasPendingRequest
            ? VideoCutsceneState.ReturnSceneName
            : VideoCutsceneState.DefaultReturnSceneName;

        VideoCutsceneState.Clear();

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LoadLevel(targetScene);
            return;
        }

        SceneManager.LoadScene(targetScene);
    }

    private static bool CanControlCutsceneTransition()
    {
        return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    }

    private string GetActiveVideoLabel()
    {
        if (videoPlayer == null)
            return "<no video player>";

        if (videoPlayer.clip != null)
            return videoPlayer.clip.name;

        if (!string.IsNullOrWhiteSpace(videoPlayer.url))
            return videoPlayer.url;

        return "<unassigned>";
    }

    private static string GetReturnSceneName()
    {
        return VideoCutsceneState.HasPendingRequest
            ? VideoCutsceneState.ReturnSceneName
            : VideoCutsceneState.DefaultReturnSceneName;
    }
}
