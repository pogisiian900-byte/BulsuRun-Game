using TMPro;
using UnityEngine;

public class PlayerNameInputUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private bool loadSavedNameOnStart = true;
    [SerializeField] private bool saveOnEndEdit = true;

    private void Awake()
    {
        if (nameInput == null)
        {
            nameInput = GetComponent<TMP_InputField>();
        }
    }

    private void Start()
    {
        if (loadSavedNameOnStart && nameInput != null)
        {
            nameInput.text = PlayerNameStore.GetSavedName();
        }

        PlayerNameStore.ApplySavedName();

        if (saveOnEndEdit && nameInput != null)
        {
            nameInput.onEndEdit.AddListener(SaveName);
        }
    }

    private void OnDestroy()
    {
        if (saveOnEndEdit && nameInput != null)
        {
            nameInput.onEndEdit.RemoveListener(SaveName);
        }
    }

    public void SaveNameFromInput()
    {
        if (nameInput == null)
        {
            return;
        }

        SaveName(nameInput.text);
    }

    public void SaveName(string value)
    {
        PlayerNameStore.SaveName(value);

        if (nameInput != null)
        {
            nameInput.SetTextWithoutNotify(PlayerNameStore.GetSavedName());
        }
    }
}
