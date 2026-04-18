using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PlayerInventory : MonoBehaviourPun
{
    public int coins;

    private void Start()
    {
        if (PhotonNetwork.InRoom && !photonView.IsMine)
        {
            enabled = false;
            return;
        }

        coins = Mathf.Max(0, GameData.Coins);
        StartCoroutine(InitUI());
    }

    private IEnumerator InitUI()
    {
        while (UIManager.Instance == null)
            yield return null;

        UIManager.Instance.UpdateCoins(coins);
    }

    public void AddCoin(int amount)
    {
        coins += amount;
        GameData.Coins = coins;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateCoins(coins);
    }

    public void SetCoins(int amount, bool saveToDisk = true)
    {
        coins = Mathf.Max(0, amount);
        GameData.Coins = coins;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateCoins(coins);

        if (saveToDisk)
            SinglePlayerSaveSystem.SaveCheckpoint();
    }

    public void ToggleInventory()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ToggleInventory();
    }
    
    
//  private void BindUI()
// {
//     GameObject canvas = GameObject.Find("Canvas");

//     if (canvas != null)
//     {
//         Transform t = canvas.transform.Find("InventoryUi/InventoryContainer");
//         if (t != null)
//             inventoryUI = t.gameObject;
//     }

//     if (coinText == null)
//     {
//         var txt = GameObject.Find("CoinText");
//         if (txt != null)
//             coinText = txt.GetComponent<TextMeshProUGUI>();
//     }
// }

}
