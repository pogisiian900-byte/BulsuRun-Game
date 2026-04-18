using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public InventorySlot[] slots;

    [Header("UI (scene objects)")]
    public InventorySlotUI[] uiSlots;

    [Header("UI Find")]
    [SerializeField] private string inventoryPanelName = "InventoryContainer";

    [Header("Equipped Weapon (Current Weapon Slot)")]
    public WeaponData equippedWeapon;
    public System.Action<WeaponData> onEquippedWeaponChanged;

    [Header("Dropped Weapon Pickup")]
    [SerializeField] private float droppedWeaponDistance = 1.35f;
    [SerializeField] private float droppedWeaponHeight = 0f;
    [SerializeField] private float droppedWeaponScale = 0.6f;
    [SerializeField] private float droppedWeaponRepickDelay = 0.75f;
    [SerializeField] private Vector2 droppedWeaponColliderSize = new Vector2(1.1f, 1.1f);

    private static Inventory instance;

    private Button dropModeButton;
    private Image dropModeButtonImage;
    private Sprite dropModeSprite;
    private Sprite cancelModeSprite;
    private bool isDropModeActive;

    public static Inventory Instance => instance;
    public bool IsDropModeActive => isDropModeActive;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ApplySavedState();
        BindUIInThisScene();
        RefreshAllUI();
        onEquippedWeaponChanged?.Invoke(equippedWeapon);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetDropMode(false);
        StartCoroutine(BindNextFrame());
    }

    private System.Collections.IEnumerator BindNextFrame()
    {
        yield return null;
        BindUIInThisScene();
        RefreshAllUI();
        onEquippedWeaponChanged?.Invoke(equippedWeapon);
    }

    private void BindUIInThisScene()
    {
        Transform panel = FindInSceneByName(inventoryPanelName);

        if (panel == null)
        {
            uiSlots = null;
            ClearDropButtonBinding();
            Debug.LogWarning($"Inventory: Panel '{inventoryPanelName}' not found in this scene.");
            return;
        }

        BindDropButton(panel);

        InventorySlotUI[] found = panel.GetComponentsInChildren<InventorySlotUI>(true);

        if (found == null || found.Length == 0)
        {
            uiSlots = null;
            Debug.LogWarning("Inventory: No InventorySlotUI found under InventoryContainer in this scene.");
            return;
        }

        System.Array.Sort(found, (a, b) =>
            a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex())
        );

        uiSlots = new InventorySlotUI[slots.Length];

        for (int i = 0; i < slots.Length && i < found.Length; i++)
        {
            uiSlots[i] = found[i];
            uiSlots[i].Init(this, i);
        }

        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] == null)
                Debug.LogWarning($"Inventory: Missing InventorySlotUI for slot index {i} in this scene.");
        }
    }

    private void ApplySavedState()
    {
        if (ItemDatabase.Instance == null)
            return;

        equippedWeapon = ItemDatabase.Instance.GetWeapon(GameData.CurrentWeaponId);

        if (GameData.InventorySlots != null)
            LoadFromSaveData(GameData.InventorySlots);
    }

    private void BindDropButton(Transform panel)
    {
        ClearDropButtonBinding();

        if (panel == null)
            return;

        Transform buttonTransform = panel.Find("Drop Button");
        if (buttonTransform == null)
        {
            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button != null && button.name == "Drop Button")
                {
                    buttonTransform = button.transform;
                    break;
                }
            }
        }

        if (buttonTransform == null)
            return;

        dropModeButton = buttonTransform.GetComponent<Button>();
        dropModeButtonImage = buttonTransform.GetComponent<Image>();

        if (dropModeButton != null)
            dropModeButton.onClick.AddListener(ToggleDropMode);

        RefreshDropButtonVisual();
    }

    private void ClearDropButtonBinding()
    {
        if (dropModeButton != null)
            dropModeButton.onClick.RemoveListener(ToggleDropMode);

        dropModeButton = null;
        dropModeButtonImage = null;
    }

    private Transform FindInSceneByName(string objectName)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in allTransforms)
        {
            if (candidate.name == objectName && candidate.gameObject.scene.IsValid())
                return candidate;
        }

        return null;
    }

    private void RefreshAllUI()
    {
        if (uiSlots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i >= uiSlots.Length)
                break;

            if (uiSlots[i] == null)
                continue;

            uiSlots[i].UpdateSlot(slots[i]);
        }
    }

    public bool AddWeapon(WeaponData weapon)
    {
        if (weapon == null)
            return false;

        if (equippedWeapon == null)
        {
            equippedWeapon = weapon;
            RefreshAllUI();
            onEquippedWeaponChanged?.Invoke(equippedWeapon);
            SinglePlayerSaveSystem.RefreshRuntimeState();
            return true;
        }

        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty())
                continue;

            slot.AddWeapon(weapon);
            RefreshAllUI();
            SinglePlayerSaveSystem.RefreshRuntimeState();
            return true;
        }

        Debug.Log("Inventory Full");
        return false;
    }

    public bool CanAddWeapon(WeaponData weapon)
    {
        if (weapon == null)
            return false;

        if (equippedWeapon == null)
            return true;

        foreach (InventorySlot slot in slots)
        {
            if (slot != null && slot.IsEmpty())
                return true;
        }

        return false;
    }

    public bool AddItem(Item item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        foreach (InventorySlot slot in slots)
        {
            if (!slot.HasItem() || slot.item != item || !item.isStackable)
                continue;

            slot.amount += amount;
            RefreshAllUI();
            SinglePlayerSaveSystem.RefreshRuntimeState();
            return true;
        }

        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty())
                continue;

            slot.AddItem(item, amount);
            RefreshAllUI();
            SinglePlayerSaveSystem.RefreshRuntimeState();
            return true;
        }

        Debug.Log("Inventory Full");
        return false;
    }

    public bool TryAddShopItem(ShopItemData shopItem)
    {
        if (shopItem == null)
            return false;

        if (shopItem.weapon != null)
            return AddWeapon(shopItem.weapon);

        return false;
    }

    public SlotSaveData[] ToSaveData()
    {
        SlotSaveData[] data = new SlotSaveData[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            data[i] = new SlotSaveData
            {
                weaponId = slots[i].weapon != null ? slots[i].weapon.id : "",
                itemId = slots[i].item != null ? slots[i].item.id : "",
                amount = slots[i].amount
            };
        }

        return data;
    }

    public void LoadFromSaveData(SlotSaveData[] data)
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].Clear();

        if (data == null)
        {
            RefreshAllUI();
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (i >= data.Length)
                continue;

            SlotSaveData savedSlot = data[i];

            if (!string.IsNullOrEmpty(savedSlot.weaponId))
            {
                WeaponData savedWeapon = ItemDatabase.Instance.GetWeapon(savedSlot.weaponId);
                if (savedWeapon != null)
                    slots[i].AddWeapon(savedWeapon);
            }

            if (!string.IsNullOrEmpty(savedSlot.itemId))
            {
                Item savedItem = ItemDatabase.Instance.GetItem(savedSlot.itemId);
                if (savedItem != null)
                    slots[i].AddItem(savedItem, savedSlot.amount);
            }
        }

        RefreshAllUI();
    }

    public void HandleSlotClick(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        if (isDropModeActive)
        {
            TryDropWeaponFromSlot(slotIndex);
            return;
        }

        InventorySlot slot = slots[slotIndex];
        if (slot == null)
            return;

        if (slot.HasWeapon())
        {
            EquipWeaponFromSlot(slotIndex);
            return;
        }

        if (slot.IsEmpty() && equippedWeapon != null)
            PlaceCurrentWeaponIntoSlot(slotIndex);
    }

    public void ToggleDropMode()
    {
        SetDropMode(!isDropModeActive);
    }

    public void SetDropMode(bool active)
    {
        isDropModeActive = active;
        RefreshDropButtonVisual();
    }

    public void EquipWeaponFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        InventorySlot slot = slots[slotIndex];
        if (slot == null || !slot.HasWeapon())
            return;

        WeaponData clickedWeapon = slot.weapon;
        WeaponData oldEquipped = equippedWeapon;

        equippedWeapon = clickedWeapon;
        slot.weapon = oldEquipped;

        if (slot.weapon == null && slot.item == null)
            slot.Clear();

        RefreshAllUI();
        onEquippedWeaponChanged?.Invoke(equippedWeapon);
        SinglePlayerSaveSystem.RefreshRuntimeState();
    }

    public void PlaceCurrentWeaponIntoSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        if (equippedWeapon == null)
            return;

        InventorySlot slot = slots[slotIndex];
        if (!slot.IsEmpty())
            return;

        slot.AddWeapon(equippedWeapon);
        equippedWeapon = null;

        RefreshAllUI();
        onEquippedWeaponChanged?.Invoke(null);
        SinglePlayerSaveSystem.RefreshRuntimeState();
    }

    public bool ConsumeEquippedWeapon(WeaponData expectedWeapon = null)
    {
        if (equippedWeapon == null)
            return false;

        if (expectedWeapon != null && equippedWeapon != expectedWeapon)
            return false;

        equippedWeapon = null;
        RefreshAllUI();
        onEquippedWeaponChanged?.Invoke(null);
        SinglePlayerSaveSystem.RefreshRuntimeState();
        return true;
    }

    private void TryDropWeaponFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        InventorySlot slot = slots[slotIndex];
        if (slot == null || !slot.HasWeapon())
            return;

        WeaponData weaponToDrop = slot.weapon;
        if (!SpawnDroppedWeaponPickup(weaponToDrop))
            return;

        slot.Clear();
        RefreshAllUI();
        SetDropMode(false);
        SinglePlayerSaveSystem.RefreshRuntimeState();
    }

    public void ClearAll()
    {
        equippedWeapon = null;

        for (int i = 0; i < slots.Length; i++)
            slots[i].Clear();

        RefreshAllUI();
        onEquippedWeaponChanged?.Invoke(null);
    }

    private bool SpawnDroppedWeaponPickup(WeaponData weapon)
    {
        if (weapon == null)
            return false;

        Transform localPlayer = FindLocalPlayerTransform();
        if (localPlayer == null)
        {
            Debug.LogWarning("Inventory: Unable to find the local player to spawn the dropped weapon.");
            return false;
        }

        float direction = localPlayer.localScale.x < 0f ? -1f : 1f;
        Vector3 spawnPosition = localPlayer.position + new Vector3(droppedWeaponDistance * direction, droppedWeaponHeight, 0f);

        GameObject droppedWeapon = new GameObject(string.IsNullOrWhiteSpace(weapon.weaponName) ? "Dropped Weapon" : weapon.weaponName + " Drop");
        droppedWeapon.transform.position = spawnPosition;
        droppedWeapon.transform.localScale = Vector3.one * droppedWeaponScale;

        SpriteRenderer spriteRenderer = droppedWeapon.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = weapon.weaponSprite;
        spriteRenderer.sortingOrder = 5;

        BoxCollider2D trigger = droppedWeapon.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = GetDroppedWeaponColliderSize(weapon);

        WeaponPickup pickup = droppedWeapon.AddComponent<WeaponPickup>();
        pickup.SetWeaponData(weapon);
        pickup.ConfigureDroppedPickup(droppedWeaponRepickDelay);
        return true;
    }

    private Vector2 GetDroppedWeaponColliderSize(WeaponData weapon)
    {
        if (weapon != null && weapon.weaponSprite != null)
        {
            Vector2 spriteSize = weapon.weaponSprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
                return spriteSize;
        }

        return droppedWeaponColliderSize;
    }

    private Transform FindLocalPlayerTransform()
    {
        PlayerInventory[] players = FindObjectsOfType<PlayerInventory>();
        foreach (PlayerInventory player in players)
        {
            if (player != null && player.isActiveAndEnabled)
                return player.transform;
        }

        GameObject fallbackPlayer = GameObject.FindGameObjectWithTag("Player");
        return fallbackPlayer != null ? fallbackPlayer.transform : null;
    }

    private void RefreshDropButtonVisual()
    {
        if (dropModeButtonImage == null)
            return;

        EnsureDropButtonSprites();
        dropModeButtonImage.sprite = isDropModeActive ? cancelModeSprite : dropModeSprite;
        dropModeButtonImage.type = Image.Type.Simple;
        dropModeButtonImage.preserveAspect = true;
        dropModeButtonImage.color = Color.white;
    }

    private void EnsureDropButtonSprites()
    {
        if (dropModeSprite != null && cancelModeSprite != null)
            return;

        dropModeSprite = CreateModeButtonSprite(new Color32(40, 84, 122, 255), new Color32(245, 248, 255, 255), false);
        cancelModeSprite = CreateModeButtonSprite(new Color32(128, 50, 50, 255), new Color32(255, 244, 244, 255), true);
    }

    private static Sprite CreateModeButtonSprite(Color backgroundColor, Color iconColor, bool cancel)
    {
        const int width = 192;
        const int height = 64;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                texture.SetPixel(x, y, transparent);
        }

        Color borderColor = Color.Lerp(backgroundColor, Color.black, 0.35f);
        FillRect(texture, 4, 4, width - 8, height - 8, backgroundColor);
        DrawRectOutline(texture, 4, 4, width - 8, height - 8, borderColor, 3);

        if (cancel)
        {
            DrawLine(texture, new Vector2(66f, 18f), new Vector2(126f, 46f), iconColor, 7);
            DrawLine(texture, new Vector2(126f, 18f), new Vector2(66f, 46f), iconColor, 7);
        }
        else
        {
            FillRect(texture, 90, 22, 12, 20, iconColor);
            DrawLine(texture, new Vector2(96f, 16f), new Vector2(78f, 31f), iconColor, 6);
            DrawLine(texture, new Vector2(96f, 16f), new Vector2(114f, 31f), iconColor, 6);
            FillRect(texture, 71, 48, 50, 6, iconColor);
            FillRect(texture, 71, 42, 6, 12, iconColor);
            FillRect(texture, 115, 42, 6, 12, iconColor);
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void FillRect(Texture2D texture, int startX, int startY, int width, int height, Color color)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
                SetPixelSafe(texture, x, y, color);
        }
    }

    private static void DrawRectOutline(Texture2D texture, int startX, int startY, int width, int height, Color color, int thickness)
    {
        FillRect(texture, startX, startY, width, thickness, color);
        FillRect(texture, startX, startY + height - thickness, width, thickness, color);
        FillRect(texture, startX, startY, thickness, height, color);
        FillRect(texture, startX + width - thickness, startY, thickness, height, color);
    }

    private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, int thickness)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, end) * 2f));
        int halfThickness = Mathf.Max(1, thickness / 2);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(start, end, t);
            FillRect(
                texture,
                Mathf.RoundToInt(point.x) - halfThickness,
                Mathf.RoundToInt(point.y) - halfThickness,
                thickness,
                thickness,
                color
            );
        }
    }

    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            return;

        texture.SetPixel(x, y, color);
    }
}
