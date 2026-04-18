using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerWeaponBinder : MonoBehaviourPunCallbacks
{
    private const string EquippedWeaponPropertyKey = "equippedWeaponId";
    private const float RemoteRefreshIntervalSeconds = 0.5f;

    private Inventory inventory;
    private PlayerWeapon playerWeapon;
    private float nextRemoteRefreshTime;

    private void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBind();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindInventory();
    }

    private void Update()
    {
        if (ShouldUseLocalInventoryBinding() || Time.unscaledTime < nextRemoteRefreshTime)
        {
            return;
        }

        nextRemoteRefreshTime = Time.unscaledTime + RemoteRefreshIntervalSeconds;
        ApplyOwnerWeaponState();
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        TryBind();
    }

    public void RefreshBinding()
    {
        nextRemoteRefreshTime = 0f;
        TryBind();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!PhotonNetwork.InRoom || changedProps == null || !changedProps.ContainsKey(EquippedWeaponPropertyKey))
        {
            return;
        }

        if (photonView == null || photonView.Owner == null || targetPlayer == null)
        {
            return;
        }

        if (targetPlayer.ActorNumber != photonView.Owner.ActorNumber)
        {
            return;
        }

        ApplyOwnerWeaponState();
    }

    private void TryBind()
    {
        UnbindInventory();

        if (playerWeapon == null)
        {
            return;
        }

        if (!ShouldUseLocalInventoryBinding())
        {
            ApplyOwnerWeaponState();
            return;
        }

        inventory = Inventory.Instance != null ? Inventory.Instance : FindObjectOfType<Inventory>();
        if (inventory == null)
        {
            return;
        }

        inventory.onEquippedWeaponChanged += OnEquippedChanged;
        OnEquippedChanged(inventory.equippedWeapon);
    }

    private void OnEquippedChanged(WeaponData weapon)
    {
        if (playerWeapon == null)
        {
            return;
        }

        playerWeapon.EquipWeapon(weapon);
        PublishWeaponState(weapon);
    }

    private void UnbindInventory()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.onEquippedWeaponChanged -= OnEquippedChanged;
        inventory = null;
    }

    private bool ShouldUseLocalInventoryBinding()
    {
        return !PhotonNetwork.InRoom || (photonView != null && photonView.IsMine);
    }

    private void ApplyOwnerWeaponState()
    {
        if (playerWeapon == null)
        {
            return;
        }

        if (ShouldUseLocalInventoryBinding())
        {
            WeaponData localWeapon = inventory != null ? inventory.equippedWeapon : null;
            playerWeapon.EquipWeapon(localWeapon);
            PublishWeaponState(localWeapon);
            return;
        }

        playerWeapon.EquipWeapon(ResolveOwnerWeapon());
    }

    private WeaponData ResolveOwnerWeapon()
    {
        if (ItemDatabase.Instance == null || photonView == null || photonView.Owner == null)
        {
            return null;
        }

        Hashtable customProperties = photonView.Owner.CustomProperties;
        if (customProperties == null || !customProperties.ContainsKey(EquippedWeaponPropertyKey))
        {
            return null;
        }

        object weaponIdValue = customProperties[EquippedWeaponPropertyKey];
        string weaponId = weaponIdValue as string;
        if (weaponId == null && weaponIdValue != null)
        {
            weaponId = weaponIdValue.ToString();
        }

        return ItemDatabase.Instance.GetWeapon(weaponId);
    }

    private void PublishWeaponState(WeaponData weapon)
    {
        if (!PhotonNetwork.InRoom || photonView == null || !photonView.IsMine || PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        string weaponId = weapon != null ? weapon.id ?? string.Empty : string.Empty;
        object existingValue = null;

        if (PhotonNetwork.LocalPlayer.CustomProperties != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(EquippedWeaponPropertyKey))
        {
            existingValue = PhotonNetwork.LocalPlayer.CustomProperties[EquippedWeaponPropertyKey];
        }

        string existingWeaponId = existingValue as string;
        if (existingWeaponId == null && existingValue != null)
        {
            existingWeaponId = existingValue.ToString();
        }

        if (string.Equals(existingWeaponId ?? string.Empty, weaponId, System.StringComparison.Ordinal))
        {
            return;
        }

        Hashtable properties = new Hashtable
        {
            { EquippedWeaponPropertyKey, weaponId }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
    }
}
