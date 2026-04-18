using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class WeaponPickup : MonoBehaviourPunCallbacks
{
    private const string SharedPickupPrefix = "weaponPickup";

    [Header("Weapon Data")]
    [SerializeField] private WeaponData weaponData;

    [Header("Optional Info Card")]
    [SerializeField] private GameObject infoCard;

    [Header("UI")]
    [SerializeField] private GameObject pickupButtonObject;

    [Header("Multiplayer")]
    [SerializeField] private bool syncPickupAcrossRoom = true;

    private Button pickupButton;
    private bool playerInRange;
    private Inventory inventory;
    private bool autoPickupWithoutButton;
    private float autoPickupReadyTime;
    private float nextAutoPickupAttemptTime;
    private string sharedPickupKey;
    private bool pickupRequestPending;
    private bool pickupResolved;
    private bool pickupGrantedLocally;

    private void Awake()
    {
        sharedPickupKey = BuildSharedPickupKey();

        if (infoCard != null)
            infoCard.SetActive(false);

        if (pickupButtonObject != null)
        {
            pickupButtonObject.SetActive(false);
            pickupButton = pickupButtonObject.GetComponent<Button>();
            if (pickupButton != null)
                pickupButton.onClick.AddListener(Pickup);
        }

        SyncVisual();
        ApplyExistingSharedPickupState();
    }

    private void Start()
    {
        inventory = Inventory.Instance != null ? Inventory.Instance : FindObjectOfType<Inventory>();
        if (inventory == null)
            Debug.LogError("WeaponPickupButton: Inventory not found in scene!");

        ApplyExistingSharedPickupState();
    }

    public override void OnJoinedRoom()
    {
        ApplyExistingSharedPickupState();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!IsSharedMultiplayerPickup() ||
            propertiesThatChanged == null ||
            string.IsNullOrEmpty(sharedPickupKey) ||
            !propertiesThatChanged.ContainsKey(sharedPickupKey))
        {
            return;
        }

        pickupRequestPending = false;

        if (WasPickupClaimedByLocalPlayer(propertiesThatChanged[sharedPickupKey]))
        {
            GrantPickupToLocalInventory();
        }

        pickupResolved = true;
        ConsumePickupVisual();
    }

    public void SetWeaponData(WeaponData weapon)
    {
        weaponData = weapon;

        if (weaponData != null && !string.IsNullOrWhiteSpace(weaponData.weaponName))
            gameObject.name = weaponData.weaponName + " Pickup";

        SyncVisual();
    }

    public void ConfigureDroppedPickup(float repickDelay)
    {
        autoPickupWithoutButton = true;
        syncPickupAcrossRoom = false;
        sharedPickupKey = string.Empty;
        autoPickupReadyTime = Time.time + Mathf.Max(0f, repickDelay);
        nextAutoPickupAttemptTime = autoPickupReadyTime;
        SyncVisual();
    }

    private void Pickup()
    {
        if (!playerInRange)
            return;

        if (inventory == null)
            return;

        if (weaponData == null)
        {
            Debug.LogError("WeaponPickupButton: weaponData is missing!");
            return;
        }

        if (IsSharedMultiplayerPickup())
        {
            TryClaimSharedPickup();
            return;
        }

        if (!inventory.AddWeapon(weaponData))
            return;

        if (infoCard != null)
            infoCard.SetActive(false);

        if (pickupButtonObject != null)
            pickupButtonObject.SetActive(false);

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(other, out _))
            return;

        playerInRange = true;

        if (infoCard != null)
            infoCard.SetActive(true);

        if (pickupButtonObject != null)
            pickupButtonObject.SetActive(true);
        else
            TryAutoPickup();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(other, out _))
            return;

        playerInRange = true;

        if (pickupButtonObject == null)
            TryAutoPickup();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(other, out _))
            return;

        playerInRange = false;

        if (infoCard != null)
            infoCard.SetActive(false);

        if (pickupButtonObject != null)
            pickupButtonObject.SetActive(false);
    }

    private void TryAutoPickup()
    {
        if (!autoPickupWithoutButton)
            return;

        if (Time.time < autoPickupReadyTime)
            return;

        if (Time.time < nextAutoPickupAttemptTime)
            return;

        nextAutoPickupAttemptTime = Time.time + 1f;
        Pickup();
    }

    private void SyncVisual()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && weaponData != null && weaponData.weaponSprite != null)
            spriteRenderer.sprite = weaponData.weaponSprite;
    }

    private void TryClaimSharedPickup()
    {
        if (pickupResolved || pickupRequestPending || inventory == null || weaponData == null)
            return;

        if (!inventory.CanAddWeapon(weaponData))
            return;

        if (IsSharedPickupClaimed())
        {
            pickupResolved = true;
            ConsumePickupVisual();
            return;
        }

        if (PhotonNetwork.CurrentRoom == null || PhotonNetwork.LocalPlayer == null)
            return;

        pickupRequestPending = true;

        Hashtable roomProperties = new Hashtable
        {
            { sharedPickupKey, PhotonNetwork.LocalPlayer.ActorNumber }
        };

        if (!PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties))
            pickupRequestPending = false;
    }

    private bool GrantPickupToLocalInventory()
    {
        if (pickupGrantedLocally)
            return true;

        if (inventory == null || weaponData == null)
            return false;

        bool added = inventory.AddWeapon(weaponData);
        if (added)
            pickupGrantedLocally = true;

        if (!added)
        {
            Debug.LogWarning($"WeaponPickup: Claimed shared pickup '{name}' but failed to add '{weaponData.weaponName}' to the local inventory.");
        }

        return added;
    }

    private void ApplyExistingSharedPickupState()
    {
        if (!IsSharedMultiplayerPickup() || !IsSharedPickupClaimed())
            return;

        pickupResolved = true;
        ConsumePickupVisual();
    }

    private bool IsSharedPickupClaimed()
    {
        if (!IsSharedMultiplayerPickup() ||
            PhotonNetwork.CurrentRoom == null ||
            PhotonNetwork.CurrentRoom.CustomProperties == null ||
            !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(sharedPickupKey))
        {
            return false;
        }

        return TryReadClaimingActorNumber(PhotonNetwork.CurrentRoom.CustomProperties[sharedPickupKey], out _);
    }

    private bool WasPickupClaimedByLocalPlayer(object claimedByValue)
    {
        if (PhotonNetwork.LocalPlayer == null ||
            !TryReadClaimingActorNumber(claimedByValue, out int claimingActorNumber))
        {
            return false;
        }

        return claimingActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
    }

    private static bool TryReadClaimingActorNumber(object claimedByValue, out int claimingActorNumber)
    {
        switch (claimedByValue)
        {
            case byte byteValue:
                claimingActorNumber = byteValue;
                return claimingActorNumber > 0;
            case short shortValue:
                claimingActorNumber = shortValue;
                return claimingActorNumber > 0;
            case int intValue:
                claimingActorNumber = intValue;
                return claimingActorNumber > 0;
            case long longValue:
                claimingActorNumber = (int)longValue;
                return claimingActorNumber > 0;
            default:
                claimingActorNumber = 0;
                return false;
        }
    }

    private bool IsSharedMultiplayerPickup()
    {
        return syncPickupAcrossRoom &&
               PhotonNetwork.InRoom &&
               !string.IsNullOrEmpty(sharedPickupKey);
    }

    private string BuildSharedPickupKey()
    {
        if (!syncPickupAcrossRoom || !gameObject.scene.IsValid())
            return string.Empty;

        return $"{SharedPickupPrefix}:{gameObject.scene.name}:{BuildHierarchyPath(transform)}";
    }

    private static string BuildHierarchyPath(Transform current)
    {
        if (current == null)
            return string.Empty;

        System.Text.StringBuilder pathBuilder = new System.Text.StringBuilder();
        AppendHierarchySegment(current, pathBuilder);
        return pathBuilder.ToString();
    }

    private static void AppendHierarchySegment(Transform current, System.Text.StringBuilder pathBuilder)
    {
        if (current.parent != null)
            AppendHierarchySegment(current.parent, pathBuilder);

        if (pathBuilder.Length > 0)
            pathBuilder.Append('/');

        pathBuilder.Append(current.name);
        pathBuilder.Append('[');
        pathBuilder.Append(current.GetSiblingIndex());
        pathBuilder.Append(']');
    }

    private void ConsumePickupVisual()
    {
        if (infoCard != null)
            infoCard.SetActive(false);

        if (pickupButtonObject != null)
            pickupButtonObject.SetActive(false);

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.enabled = false;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        Destroy(gameObject);
    }
}
