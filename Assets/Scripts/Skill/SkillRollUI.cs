using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SkillRollUI : MonoBehaviour
{
    [Header("Deck & UI")]
    [SerializeField] private List<SkillCard> deck;
    [SerializeField] private SkillCardView[] slots; // 3 slots
    [SerializeField] private PlayerSkillHolder playerSkill;
    [SerializeField] private GameObject root;
    [SerializeField] private CurrentSkillsUI currentSkillsUI;

    [Header("Stage -> Scene Mapping (index = stage number)")]
    private readonly string[] stageScenes =
    {
        "",
        "Level 1 CBA Classroom",     // 1
        "Level 2 CBA",               // 2
        "Level 3 CBA",               // 3
        "CBA Mini Boss",             // 4
        "AC Level 1",                // 5
        "AC Level 2",                // 6
        "AC Mini Boss",              // 7
        "Level 1 Admin Outside",     // 8
        "Level 2nd Floor Admin",     // 9
        "Level 3rd Floor Admin",     // 10
        "Admin Mini Boss",           // 11
        "Level 1 Pancho 1st Floor",  // 12
        "Level 2 Pancho 2nd Floor",  // 13
        "Pancho Mini Boss",          // 14
        "Gate Final Boss"            // 15
    };

    private bool didExit; // prevents double click (Pick + Skip)
    private bool hasUsedReroll;
    private bool hasOpenedInitialRoll;
    private int rollVersion;
    private Button rerollButton;

    private List<SkillCard> currentRoll = new();

    private void OnEnable()
    {
        if (playerSkill == null)
            playerSkill = PlayerSkillHolder.Instance;

        if (currentSkillsUI == null)
            currentSkillsUI = FindFirstObjectByType<CurrentSkillsUI>();

        if (root == null)
            root = gameObject;

        didExit = false;
        hasUsedReroll = false;
        hasOpenedInitialRoll = false;
        rollVersion = 0;
        currentRoll.Clear();

        OpenRoll();
    }

    public void OpenRoll()
    {
        bool isRerollRequest = hasOpenedInitialRoll;

        if (playerSkill == null)
        {
            Debug.LogError("SkillRollUI: PlayerSkillHolder not found.");
            return;
        }

        if (playerSkill.IsFull)
        {
            UpdateRerollButtonState();
            return;
        }

        if (isRerollRequest && hasUsedReroll)
        {
            UpdateRerollButtonState();
            return;
        }

        didExit = false;
        Time.timeScale = 0f;

        if (root != null)
            root.SetActive(true);

        currentRoll = GetRandomUniqueNotOwned(3);
        hasOpenedInitialRoll = true;

        if (isRerollRequest)
            hasUsedReroll = true;

        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;

            if (i < currentRoll.Count)
                slots[i].Set(currentRoll[i], () => Pick(index));
            else
                slots[i].Set(null, null);
        }

        UpdateRerollButtonState();
    }

    public void Pick(int index)
    {
        if (didExit)
            return;

        didExit = true;

        if (playerSkill == null)
        {
            Debug.LogError("SkillRollUI: PlayerSkillHolder is null on Pick().");
            return;
        }

        if (index < 0 || index >= currentRoll.Count)
            return;

        SkillCard picked = currentRoll[index];
        if (picked == null)
            return;

        playerSkill.AddSkill(picked);

        if (currentSkillsUI != null)
            currentSkillsUI.Refresh();

        if (root != null)
            root.SetActive(false);

        Time.timeScale = 1f;
        LoadNextStageScene();
    }

    public void Skip()
    {
        if (didExit)
            return;

        didExit = true;

        if (root != null)
            root.SetActive(false);

        Time.timeScale = 1f;
        LoadNextStageScene();
    }

    private void LoadNextStageScene()
    {
        int stage = RunManager.Instance.StageIndex;

        if (stageScenes == null || stage < 0 || stage >= stageScenes.Length)
        {
            Debug.LogError($"No scene mapping for stage {stage}. Check stageScenes array.");
            return;
        }

        string sceneName = stageScenes[stage];

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"Scene name is empty for stage {stage}.");
            return;
        }

        SinglePlayerSaveSystem.PrepareFreshLevelStart();
        SinglePlayerSaveSystem.ClearQueuedCheckpointRestore();
        SinglePlayerSaveSystem.SaveCheckpoint(false);
        SceneManager.LoadScene(sceneName);
    }

    private List<SkillCard> GetRandomUniqueNotOwned(int count)
    {
        List<SkillCard> pool = new();

        foreach (SkillCard card in deck)
        {
            if (card != null && !playerSkill.HasSkill(card))
                pool.Add(card);
        }

        List<SkillCard> result = new();
        System.Random random = new(BuildRollSeed());

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int pickedIndex = random.Next(0, pool.Count);
            result.Add(pool[pickedIndex]);
            pool.RemoveAt(pickedIndex);
        }

        rollVersion++;
        return result;
    }

    private int BuildRollSeed()
    {
        int actorNumber = PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber : 1;
        int stageIndex = RunManager.Instance != null ? RunManager.Instance.StageIndex : 0;

        unchecked
        {
            int seed = Environment.TickCount;
            seed = (seed * 31) + actorNumber;
            seed = (seed * 31) + stageIndex;
            seed = (seed * 31) + rollVersion;

            for (int i = 0; i < playerSkill.Owned.Count; i++)
            {
                SkillCard ownedSkill = playerSkill.Owned[i];
                seed = (seed * 31) + (ownedSkill != null ? ownedSkill.id.GetHashCode() : 0);
            }

            return seed;
        }
    }

    private void UpdateRerollButtonState()
    {
        Button button = GetRerollButton();
        if (button != null)
            button.interactable = !hasUsedReroll;
    }

    private Button GetRerollButton()
    {
        if (rerollButton != null)
            return rerollButton;

        Button[] buttons = (root != null ? root : gameObject).GetComponentsInChildren<Button>(true);

        foreach (Button candidate in buttons)
        {
            if (candidate == null)
                continue;

            int persistentEventCount = candidate.onClick.GetPersistentEventCount();
            for (int i = 0; i < persistentEventCount; i++)
            {
                if (!ReferenceEquals(candidate.onClick.GetPersistentTarget(i), this))
                    continue;

                if (candidate.onClick.GetPersistentMethodName(i) != nameof(OpenRoll))
                    continue;

                rerollButton = candidate;
                return rerollButton;
            }
        }

        return null;
    }
}
