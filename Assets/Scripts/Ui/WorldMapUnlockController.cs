using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class WorldMapUnlockController : MonoBehaviour
{
    private const string WorldsSceneName = "Worlds";
    private const string LockVisualName = "Lock";
    private const string PlayVisualName = "Play";
    private const string CompletedVisualName = "Completed";

// private void Start()
// {
//     PlayerPrefs.SetInt("HighestLevel", 15);
//     PlayerPrefs.SetInt("HighestWorld", 5);
//     PlayerPrefs.DeleteKey("JustUnlockedLevel");
//     PlayerPrefs.DeleteKey("JustUnlockedWorld");
//     PlayerPrefs.Save();

//     BeginRefreshLoop();
// }

    private static readonly WorldButtonConfig[] WorldButtons =
    {
        new WorldButtonConfig("Play CBA", 1, 5),
        new WorldButtonConfig("Play AC", 5, 8),
        new WorldButtonConfig("Play Admin", 8, 12),
        new WorldButtonConfig("Play Pancho", 12, 15),
        new WorldButtonConfig("Play Gate", 15, 16)
    };

    private static bool isSubscribed;

    private readonly Dictionary<string, ButtonAppearance> defaultAppearances = new Dictionary<string, ButtonAppearance>();
    private Coroutine refreshRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!isSubscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isSubscribed = true;
        }

        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (scene.name != WorldsSceneName)
        {
            return;
        }

        if (FindObjectOfType<WorldMapUnlockController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject(nameof(WorldMapUnlockController));
        controllerObject.AddComponent<WorldMapUnlockController>();
    }

    private void Awake()
    {
        BeginRefreshLoop();
    }

    private void Start()
    {
        BeginRefreshLoop();
        
    }

    private void OnEnable()
    {
        BeginRefreshLoop();
    }

    private void OnValidate()
    {
        RefreshInEditorIfNeeded();
    }

    private void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name == WorldsSceneName)
        {
            RefreshWorldButtons();
        }
    }

    private void OnDisable()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    private void RefreshWorldButtons()
    {
        if (gameObject.scene.name != WorldsSceneName)
        {
            return;
        }

        CacheButtonAppearances();

        int highestLevel = ProgressStore.GetHighestLevel();
        int highestWorld = GetHighestWorldUnlocked(highestLevel);

        for (int i = 0; i < WorldButtons.Length; i++)
        {
            WorldButtonConfig config = WorldButtons[i];
            Transform worldRoot = FindWorldRoot(config.ButtonName);

            if (worldRoot == null)
            {
                continue;
            }

            ApplyState(worldRoot, GetState(i, highestWorld, highestLevel));
        }
    }

    private void ApplyState(Transform worldRoot, WorldButtonState state)
    {
        bool isUnlocked = state != WorldButtonState.Locked;
        bool appliedCustomVisuals = ApplyCustomVisuals(worldRoot, state);
        Button[] childButtons = worldRoot.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < childButtons.Length; i++)
        {
            childButtons[i].interactable = isUnlocked;
        }

        Image buttonImage = ResolveDisplayImage(worldRoot);

        if (buttonImage != null && childButtons.Length == 0)
        {
            buttonImage.enabled = !appliedCustomVisuals;

            if (!appliedCustomVisuals &&
                defaultAppearances.TryGetValue(worldRoot.name, out ButtonAppearance originalAppearance) &&
                originalAppearance.IsValid)
            {
                originalAppearance.ApplyTo(buttonImage);
            }
        }
    }

    private bool ApplyCustomVisuals(Transform buttonTransform, WorldButtonState state)
    {
        Transform lockVisual = buttonTransform.Find(LockVisualName);
        Transform playVisual = buttonTransform.Find(PlayVisualName);
        Transform completedVisual = buttonTransform.Find(CompletedVisualName);

        if (lockVisual == null || playVisual == null || completedVisual == null)
        {
            return false;
        }

        lockVisual.gameObject.SetActive(state == WorldButtonState.Locked);
        playVisual.gameObject.SetActive(state == WorldButtonState.Playable);
        completedVisual.gameObject.SetActive(state == WorldButtonState.Completed);
        return true;
    }

    private Transform FindWorldRoot(string buttonName)
    {
        GameObject buttonObject = GameObject.Find(buttonName);
        return buttonObject != null ? buttonObject.transform : null;
    }

    private Image ResolveDisplayImage(Transform worldRoot)
    {
        if (worldRoot == null)
        {
            return null;
        }

        Image rootImage = worldRoot.GetComponent<Image>();
        if (rootImage != null)
        {
            return rootImage;
        }

        Button childButton = worldRoot.GetComponentInChildren<Button>(true);
        return childButton != null ? childButton.GetComponent<Image>() : null;
    }

    private WorldButtonState GetState(int worldIndex, int highestWorld, int highestLevel)
    {
        int worldNumber = worldIndex + 1;

        if (worldNumber < highestWorld)
        {
            return WorldButtonState.Completed;
        }

        if (worldNumber > highestWorld)
        {
            return WorldButtonState.Locked;
        }

        bool isFinalWorld = worldNumber == WorldButtons.Length;
        if (isFinalWorld && highestLevel >= WorldButtons[worldIndex].CompletedAtLevel)
        {
            return WorldButtonState.Completed;
        }

        if (worldNumber == highestWorld)
        {
            return WorldButtonState.Playable;
        }

        return WorldButtonState.Locked;
    }

    public static int GetUnlockedWorldCount(int highestLevel)
    {
        if (highestLevel >= 15)
        {
            return 5;
        }

        if (highestLevel >= 12)
        {
            return 4;
        }

        if (highestLevel >= 8)
        {
            return 3;
        }

        if (highestLevel >= 5)
        {
            return 2;
        }

        return 1;
    }

    private int GetHighestWorldUnlocked(int highestLevel)
    {
        int derivedHighestWorld = GetUnlockedWorldCount(highestLevel);

        if (ProgressStore.HasHighestWorld())
        {
            int storedHighestWorld = ProgressStore.GetHighestWorld();
            int correctedHighestWorld = Mathf.Clamp(
                Mathf.Max(storedHighestWorld, derivedHighestWorld),
                1,
                WorldButtons.Length);

            if (correctedHighestWorld != storedHighestWorld)
            {
                ProgressStore.SetHighestWorld(correctedHighestWorld);
                ProgressStore.Save();
            }

            return correctedHighestWorld;
        }

        return Mathf.Clamp(derivedHighestWorld, 1, WorldButtons.Length);
    }

    private void RefreshInEditorIfNeeded()
    {
        CacheButtonAppearances();
        RefreshWorldButtons();
    }

    private void BeginRefreshLoop()
    {
        RefreshInEditorIfNeeded();

        if (!Application.isPlaying)
        {
            return;
        }

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
        }

        refreshRoutine = StartCoroutine(RefreshForFirstFrames());
    }

    private IEnumerator RefreshForFirstFrames()
    {
        for (int i = 0; i < 10; i++)
        {
            RefreshWorldButtons();
            yield return null;
        }

        refreshRoutine = null;
    }

    [ContextMenu("Reset World Progress")]
    private void ResetWorldProgress()
    {
        ProgressStore.SetHighestLevel(1);
        ProgressStore.SetHighestWorld(1);
        ProgressStore.DeleteJustUnlockedLevel();
        ProgressStore.DeleteJustUnlockedWorld();
        ProgressStore.Save();

        RefreshWorldButtons();
    }

    private void CacheButtonAppearances()
    {
        for (int i = 0; i < WorldButtons.Length; i++)
        {
            WorldButtonConfig config = WorldButtons[i];
            if (defaultAppearances.ContainsKey(config.ButtonName))
            {
                continue;
            }

            Transform worldRoot = FindWorldRoot(config.ButtonName);
            if (worldRoot == null)
            {
                continue;
            }

            Image buttonImage = ResolveDisplayImage(worldRoot);
            if (buttonImage == null)
            {
                continue;
            }

            defaultAppearances[config.ButtonName] = new ButtonAppearance(buttonImage);
        }
    }

    private struct WorldButtonConfig
    {
        public string ButtonName { get; }
        public int RequiredLevel { get; }
        public int CompletedAtLevel { get; }

        public WorldButtonConfig(string buttonName, int requiredLevel, int completedAtLevel)
        {
            ButtonName = buttonName;
            RequiredLevel = requiredLevel;
            CompletedAtLevel = completedAtLevel;
        }
    }

    private enum WorldButtonState
    {
        Locked,
        Playable,
        Completed
    }

    private struct ButtonAppearance
    {
        private readonly Sprite sprite;
        private readonly Image.Type imageType;
        private readonly Color color;
        private readonly bool preserveAspect;

        public bool IsValid => sprite != null;

        public ButtonAppearance(Image sourceImage)
        {
            sprite = sourceImage.sprite;
            imageType = sourceImage.type;
            color = sourceImage.color;
            preserveAspect = sourceImage.preserveAspect;
        }

        public void ApplyTo(Image targetImage)
        {
            targetImage.sprite = sprite;
            targetImage.type = imageType;
            targetImage.color = color;
            targetImage.preserveAspect = preserveAspect;
        }
    }
}
