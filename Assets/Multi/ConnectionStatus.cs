using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class ConnectionStatus : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject[] connectedOnlyObjects;

    private void Start()
    {
        UpdateStatus();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        UpdateStatus();
    }

    public override void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        base.OnDisable();
    }

    private void OnSelectedLocaleChanged(Locale _) => UpdateStatus();

    private void UpdateStatus()
    {
        bool connected = false;
        switch (PhotonNetwork.NetworkClientState)
        {
            case ClientState.Disconnected:
                statusText.text = UILocalization.Get("connection_no_connection");
                statusText.color = Color.red;
                break;
            case ClientState.ConnectingToNameServer:
            case ClientState.ConnectingToMasterServer:
            case ClientState.Authenticating:
                statusText.text = UILocalization.Get("status_connecting");
                statusText.color = Color.yellow;
                break;
            case ClientState.ConnectedToMasterServer:
            case ClientState.JoinedLobby:
                statusText.text = UILocalization.Get("connection_connected");
                statusText.color = Color.green;
                connected = true;
                break;
            default:
                statusText.text = "";
                break;
        }

        SetConnectedOnlyObjects(connected);
    }

    public override void OnConnectedToMaster()       => UpdateStatus();
    public override void OnJoinedLobby()             => UpdateStatus();
    public override void OnDisconnected(DisconnectCause cause)
    {
        statusText.text = UILocalization.Get("connection_error");
        statusText.color = Color.red;
        SetConnectedOnlyObjects(false);
    }

    private void SetConnectedOnlyObjects(bool visible)
    {
        if (connectedOnlyObjects == null) return;
        foreach (var obj in connectedOnlyObjects)
            if (obj) obj.SetActive(visible);
    }
}
