using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class InventoryButtonBinder : MonoBehaviour
{
    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(BindNextFrame());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (btn != null) btn.onClick.RemoveAllListeners();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(BindNextFrame());
    }

  private IEnumerator BindNextFrame()
    {
        if (EventSystem.current == null)
            Debug.LogWarning("No EventSystem in this scene! UI buttons won't click.");

        btn.onClick.RemoveAllListeners();

        bool bound = false;

        while (!bound)
        {
            var allInventories = FindObjectsOfType<PlayerInventory>();
            
            foreach (var inv in allInventories)
            {
                    btn.onClick.AddListener(inv.ToggleInventory);
                    bound = true;
                    break;
            }

            // If we didn't find them, wait a tiny bit and check again
            if (!bound)
            {
                yield return new WaitForSeconds(0.1f); 
            }
        }
    }
}