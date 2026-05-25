using UnityEngine;
using TMPro;

public class UIConnect : MonoBehaviour
{
    [SerializeField] TMP_InputField codeInput;

    public void OnCreateClick()
    {
        NetworkManagerr.Instance.CreateRoom();
    }

    public void OnJoinClick()
    {
        NetworkManagerr.Instance.JoinRoom(codeInput.text);
    }
}