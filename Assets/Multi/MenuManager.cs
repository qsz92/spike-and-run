using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class MenuManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panels")]
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject Kodpanel;

    [Header("Input")]
    [SerializeField] private TMP_InputField roomCodeInput;

    [Header("Nickname")]
    [SerializeField] private TMP_InputField nicknameInput;

    private Dictionary<string, RoomInfo> _cachedRooms = new Dictionary<string, RoomInfo>();

    private void Start()
    {
        settings.SetActive(false);
        Kodpanel.SetActive(false);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = "1.0";
        }

        PhotonNetwork.AutomaticallySyncScene = true;

        if (nicknameInput != null)
            nicknameInput.text = PlayerPrefs.GetString("PlayerName", "");
    }

    public void ShowSettings()
    {
        settings.SetActive(true);
    }

    public void CancelShowSettings()
    {
        settings.SetActive(false);
    }

    public void ToggleKodpanel()
    {
        if (!Kodpanel.activeSelf)
        {
            Kodpanel.SetActive(true);
        }
        else
        {
            bool inputIsEmpty = roomCodeInput == null || string.IsNullOrWhiteSpace(roomCodeInput.text);
            if (inputIsEmpty)
                Kodpanel.SetActive(false);
        }
    }

    public void SaveNickname()
    {
        string nick = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nick)) return;
        PlayerPrefs.SetString("PlayerName", nick);
        PhotonNetwork.NickName = nick;
        AccountManager.SaveProgress();
    }
}