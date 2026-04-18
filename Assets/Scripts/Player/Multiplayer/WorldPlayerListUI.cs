using System;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class WorldPlayerListUI : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text[] playerNameTexts;
    [SerializeField] private GameObject[] playerSlots;
    [SerializeField] private string emptySlotText = string.Empty;
    [SerializeField] private bool showHostLabel = true;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        RefreshPlayerList();
    }

    private void Start()
    {
        RefreshPlayerList();
    }

    public override void OnJoinedRoom()
    {
        RefreshPlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        RefreshPlayerList();
    }

    public void RefreshPlayerList()
    {
        CacheReferences();

        if (playerNameTexts == null || playerNameTexts.Length == 0)
        {
            return;
        }

        bool showEmptySlots = !string.IsNullOrEmpty(emptySlotText);

        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            UpdateSlot(i, emptySlotText, showEmptySlots);
        }

        if (!PhotonNetwork.InRoom)
        {
            UpdateSlot(0, PlayerNameStore.GetSavedName(), true);

            return;
        }

        Player[] players = PhotonNetwork.PlayerList;
        Array.Sort(players, ComparePlayers);

        int count = Mathf.Min(players.Length, playerNameTexts.Length);
        for (int i = 0; i < count; i++)
        {
            UpdateSlot(i, FormatPlayerName(players[i], i + 1), true);
        }
    }

    private void CacheReferences()
    {
        if (playerNameTexts == null || playerNameTexts.Length == 0)
        {
            playerNameTexts = GetComponentsInChildren<TMP_Text>(true);
        }

        if (playerNameTexts == null || playerNameTexts.Length == 0)
        {
            return;
        }

        if (playerSlots == null || playerSlots.Length != playerNameTexts.Length)
        {
            Array.Resize(ref playerSlots, playerNameTexts.Length);
        }

        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            if (playerSlots[i] != null || playerNameTexts[i] == null)
            {
                continue;
            }

            Transform parent = playerNameTexts[i].transform.parent;
            playerSlots[i] = parent != null ? parent.gameObject : playerNameTexts[i].gameObject;
        }
    }

    private void UpdateSlot(int index, string playerLabel, bool isVisible)
    {
        if (index < 0 || index >= playerNameTexts.Length)
        {
            return;
        }

        TMP_Text nameText = playerNameTexts[index];
        GameObject slot = playerSlots != null && index < playerSlots.Length
            ? playerSlots[index]
            : null;

        if (slot != null)
        {
            slot.SetActive(isVisible);
        }
        else if (nameText != null)
        {
            nameText.gameObject.SetActive(isVisible);
        }

        if (nameText != null)
        {
            nameText.text = isVisible ? playerLabel : string.Empty;
        }
    }

    private int ComparePlayers(Player left, Player right)
    {
        if (left.IsMasterClient && !right.IsMasterClient)
        {
            return -1;
        }

        if (!left.IsMasterClient && right.IsMasterClient)
        {
            return 1;
        }

        return left.ActorNumber.CompareTo(right.ActorNumber);
    }

    private string FormatPlayerName(Player player, int slotNumber)
    {
        string playerName = string.IsNullOrWhiteSpace(player.NickName)
            ? "Player " + player.ActorNumber
            : player.NickName;

        if (showHostLabel && player.IsMasterClient)
        {
            return "Host: " + playerName;
        }

        return "Player " + slotNumber + ": " + playerName;
    }
}
