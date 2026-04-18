using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSkillHolder : MonoBehaviour
{
    public static PlayerSkillHolder Instance { get; private set; }

    public List<SkillCard> Owned = new();
    public static System.Action SkillsChanged;

    public int MaxSkills = 3;
    public bool IsFull => Owned.Count >= MaxSkills;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddSkill(SkillCard skill)
    {
        if (skill == null) return;
        if (Owned.Contains(skill)) return;
        if (IsFull) return;

        Owned.Add(skill);

        if (!GameData.SkillIds.Contains(skill.id))
            GameData.SkillIds.Add(skill.id);

        ApplySkillEffect(skill);

        Debug.Log("Added skill: " + skill.displayName);

        SkillsChanged?.Invoke();
    }

    public void ClearSkills(bool saveToDisk = true)
    {
        Owned.Clear();
        GameData.SkillIds.Clear();

        Debug.Log("Skills cleared.");

        if (saveToDisk)
            SinglePlayerSaveSystem.SaveCheckpoint();
    }

    public void LoadFromGameData()
    {
        Owned.Clear();

        PlayerMovement pm = FindOwnedPlayerComponent<PlayerMovement>();
        if (pm != null) pm.ResetToBaseStats();

        PlayerHealth ph = FindOwnedPlayerComponent<PlayerHealth>();
        if (ph != null) ph.ResetBonusHP();

        foreach (string id in GameData.SkillIds)
        {
            SkillCard card = SkillDatabase.Instance.Get(id);
            if (card != null)
            {
                Owned.Add(card);
                ApplySkillEffect(card);
            }
        }

        SkillsChanged?.Invoke();
    }

    public bool HasSkill(SkillCard skill)
    {
        return Owned.Contains(skill);
    }

    private void ApplySkillEffect(SkillCard skill)
    {
        PlayerMovement pm = FindOwnedPlayerComponent<PlayerMovement>();
        PlayerHealth ph = FindOwnedPlayerComponent<PlayerHealth>();

        switch (skill.type)
        {
            case SkillType.ExtraHP:
                if (ph != null) ph.AddMaxHP((int)skill.value);
                break;

            case SkillType.DoubleJump:
                if (pm != null) pm.EnableDoubleJump();
                break;

            case SkillType.MoveSpeed:
                if (pm != null) pm.AddSpeedBonus(skill.value);
                break;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyAfterSceneLoad());
    }

    private System.Collections.IEnumerator ApplyAfterSceneLoad()
    {
        yield return null;
        LoadFromGameData();
    }

    private void Start()
    {
        LoadFromGameData();
    }

    private static T FindOwnedPlayerComponent<T>() where T : Component
    {
        if (!PhotonNetwork.InRoom)
            return FindFirstObjectByType<T>();

        T[] components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (T component in components)
        {
            if (component == null || !component.gameObject.scene.IsValid())
                continue;

            PhotonView view = component.GetComponent<PhotonView>();
            if (view == null)
                view = component.GetComponentInParent<PhotonView>();

            if (view != null && view.IsMine)
                return component;
        }

        return FindFirstObjectByType<T>();
    }
}
