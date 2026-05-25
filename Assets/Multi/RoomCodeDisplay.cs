using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class RoomCodeDisplay : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text roomCodeText;

    public override void OnEnable()
    {
        base.OnEnable();
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        UpdateDisplay();
    }

    public override void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        base.OnDisable();
    }

    public override void OnJoinedRoom() => UpdateDisplay();

    public override void OnLeftRoom() => UpdateDisplay();

    private void OnSelectedLocaleChanged(Locale _) => UpdateDisplay();

    private void UpdateDisplay()
    {
        if (!roomCodeText) return;

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            roomCodeText.text = $"{UILocalization.Get("room_code_prefix")} {PhotonNetwork.CurrentRoom.Name}";
        else
            roomCodeText.text = UILocalization.Get("room_no_room");
    }
}
