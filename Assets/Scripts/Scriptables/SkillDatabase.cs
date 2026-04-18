using System.Collections.Generic;
using UnityEngine;

public class SkillDatabase : MonoBehaviour
{
    public static SkillDatabase Instance { get; private set; }

    [SerializeField] private List<SkillCard> allSkills;

    private Dictionary<string, SkillCard> byId;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        byId = new Dictionary<string, SkillCard>();
        foreach (var s in allSkills)
        {
            if (s != null && !string.IsNullOrEmpty(s.id) && !byId.ContainsKey(s.id))
                byId.Add(s.id, s);
        }
    }

    public SkillCard Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        byId.TryGetValue(id, out var skill);
        return skill;
    }
}
