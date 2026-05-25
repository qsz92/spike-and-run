using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerr : MonoBehaviourPunCallbacks
{
    public static NetworkManagerr Instance;
    private string generatedRoomCode;
    private bool pendingCreate = false;
    private bool _loadingGame = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!PlayerPrefs.HasKey("PlayerName"))
            PlayerPrefs.SetString("PlayerName", GenerateNickname());
        PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName");
        PhotonNetwork.AutomaticallySyncScene = true;
        ConnectToPhoton();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDisable();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _loadingGame = false;
    }

    void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRoom()
    {
        generatedRoomCode = GenerateRoomCode();
        pendingCreate = true;
        Debug.Log($"CreateRoom called. State: {PhotonNetwork.NetworkClientState}, Server: {PhotonNetwork.Server}");

        if (!PhotonNetwork.IsConnected)
        {
            ConnectToPhoton();
        }
        else if (PhotonNetwork.IsConnectedAndReady &&
                 PhotonNetwork.NetworkClientState != ClientState.Joining &&
                 PhotonNetwork.NetworkClientState != ClientState.Leaving &&
                 PhotonNetwork.NetworkClientState != ClientState.ConnectingToGameServer &&
                 PhotonNetwork.NetworkClientState != ClientState.ConnectingToMasterServer)
        {
            DoCreateRoom();
        }
        // иначе ждём OnConnectedToMaster
    }

    private void DoCreateRoom()
    {
        pendingCreate = false;
        Debug.Log("DoCreateRoom: " + generatedRoomCode);
        RoomOptions options = new RoomOptions { MaxPlayers = 6 };
        PhotonNetwork.CreateRoom(generatedRoomCode, options);
    }

    public void JoinRoom(string code)
    {
        if (string.IsNullOrEmpty(code)) return;
        PhotonNetwork.JoinRoom(code.Trim());
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster, pendingCreate: " + pendingCreate);
        if (pendingCreate)
            DoCreateRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        if (PhotonNetwork.IsMasterClient && !_loadingGame)
        {
            _loadingGame = true;
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Join failed: " + message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Create failed: " + message);
        generatedRoomCode = GenerateRoomCode();
        DoCreateRoom();
    }

    private string GenerateRoomCode()
    {
        System.Random rand = new System.Random();
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
            code[i] = (char)('0' + rand.Next(0, 10));
        return new string(code);
    }

    private string GenerateNickname()
    {
        string[] adj  = { "Swift", "Dark", "Iron", "Ghost", "Wild", "Red", "Silent", "Mad" };
        string[] noun = { "Wolf", "Fox", "Bear", "Hawk", "Snake", "Tiger", "Eagle", "Shark" };
        System.Random r = new System.Random();
        return adj[r.Next(adj.Length)] + noun[r.Next(noun.Length)] + r.Next(10, 99);
    }
}
