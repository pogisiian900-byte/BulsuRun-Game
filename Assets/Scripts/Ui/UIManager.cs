using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private const string PlayerOnePortraitResource = "Player boy";
    private const string PlayerTwoPortraitResource = "Player Girl";

    public static UIManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Image profileImage;

    private Sprite playerOnePortrait;
    private Sprite playerTwoPortrait;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 🔥 persists across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 🔁 Called when scene changes
    public void BindUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            // Debug.LogError("Canvas not found!");
            return;
        }

        // Find Inventory
        Transform inv = canvas.transform.Find("InventoryUI/InventoryContainer");
        if (inv != null)
            inventoryUI = inv.gameObject;
        // else
        //     Debug.LogError("InventoryContainer not found!");

        // Find Coin Text
        GameObject coinObj = GameObject.Find("CoinText");
        if (coinObj != null)
            coinText = coinObj.GetComponent<TextMeshProUGUI>();
        // else
        //     Debug.LogError("CoinText not found!");

        profileImage = FindChildByName(canvas.transform, "Profile")?.GetComponent<Image>();
        UpdateProfileIcon();

        UpdateCoins(GameData.Coins);
    }

    // 💰 Update Coins
    public void UpdateCoins(int amount)
    {
        if (coinText != null)
            coinText.text = amount.ToString();
    }

    // 🎒 Toggle Inventory
    public void ToggleInventory()
    {
        if (inventoryUI != null)
            inventoryUI.SetActive(!inventoryUI.activeSelf);
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindUI();
    }

    private void UpdateProfileIcon()
    {
        if (profileImage == null)
            return;

        LoadPortraitSpritesIfNeeded();

        Sprite portraitToUse = ResolveLocalPlayerPortrait();
        if (portraitToUse == null)
            return;

        profileImage.sprite = portraitToUse;
        profileImage.type = Image.Type.Simple;
        profileImage.preserveAspect = true;
    }

    private void LoadPortraitSpritesIfNeeded()
    {
        if (playerOnePortrait == null)
            playerOnePortrait = LoadFirstSprite(PlayerOnePortraitResource);

        if (playerTwoPortrait == null)
            playerTwoPortrait = LoadFirstSprite(PlayerTwoPortraitResource);
    }

    private Sprite ResolveLocalPlayerPortrait()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return playerOnePortrait;

        Player[] players = PhotonNetwork.PlayerList;
        System.Array.Sort(players, ComparePlayersByActorNumber);

        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];
            if (player == null || player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                continue;

            return i == 1 ? playerTwoPortrait : playerOnePortrait;
        }

        return playerOnePortrait;
    }

    private static int ComparePlayersByActorNumber(Player left, Player right)
    {
        if (left == null && right == null)
            return 0;

        if (left == null)
            return 1;

        if (right == null)
            return -1;

        return left.ActorNumber.CompareTo(right.ActorNumber);
    }

    private static Sprite LoadFirstSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildByName(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
