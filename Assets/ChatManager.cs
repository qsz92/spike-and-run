using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class ChatManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private Transform messagesContainer;
    [SerializeField] private TextMeshProUGUI messagePrefab;
    [SerializeField] private ScrollRect scrollRect;

    private bool isOpen = false;
    private List<string> messages = new List<string>();

    public override void OnEnable()
    {
        base.OnEnable();
        chatInput.onSubmit.AddListener(OnChatSubmit);
    }

    public override void OnDisable()
    {
        chatInput.onSubmit.RemoveListener(OnChatSubmit);
        base.OnDisable();
    }

    void Start()
    {
        chatPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && !chatInput.isFocused)
        {
            ToggleChat();
        }

        if (isOpen && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && chatInput.isFocused)
        {
            SendChatMessage();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleChat();
        }
    }

    void ToggleChat()
    {
        isOpen = !isOpen;
        chatPanel.SetActive(isOpen);
        if (isOpen)
        {
            chatInput.ActivateInputField();
            chatInput.Select();
        }
        else
        {
            chatInput.DeactivateInputField();
        }
    }

    void OnChatSubmit(string _)
    {
        if (isOpen)
        {
            SendChatMessage();
        }
    }

    void SendChatMessage()
    {
        string text = chatInput.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        string sender = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "Player" : PhotonNetwork.NickName;

        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("ReceiveMessage", RpcTarget.All, sender, text);
        }
        else
        {
            ReceiveMessage(sender, text);
        }

        chatInput.text = "";
        chatInput.ActivateInputField();
        chatInput.Select();
    }

    [PunRPC]
    void ReceiveMessage(string sender, string text)
    {
        string msg = $"<b>{sender}:</b> {text}";
        messages.Add(msg);

        var line = Instantiate(messagePrefab, messagesContainer);
        line.text = msg;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
