using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private TextMeshProUGUI playersCountText;
    private bool _isSceneLoaded   = false;
    private bool _isPlayerCreated = false;
    private bool _playAgain       = false;
    public static NetworkManager Instance;

    private void Awake() => Instance = this;

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
        _isSceneLoaded = true;
        if (PhotonNetwork.InRoom && !_isPlayerCreated)
        {
            _isPlayerCreated = true;
            StartCoroutine(CreatePlayerDelayed());
        }
        UpdatePlayerCount();
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        _isSceneLoaded = true;

        if (PhotonNetwork.InRoom && !_isPlayerCreated)
        {
            _isPlayerCreated = true;
            StartCoroutine(CreatePlayerDelayed());
        }
    }

    public void PlayAgain()
    {
        if (!PhotonNetwork.InRoom) return;
        _isPlayerCreated = true;
        var gameManager = FindFirstObjectByType<Platformer.GameManager>();
        GameObject oldPlayer = gameManager != null ? gameManager.CurrentPlayerGameObject : null;
        if (oldPlayer != null)
        {
            oldPlayer.SetActive(true);
            PhotonNetwork.Destroy(oldPlayer);
        }
        if (gameManager != null)
            gameManager.PrepareForRespawn();
        StartCoroutine(CreatePlayerDelayed());
    }

    public void GoToMenu()
    {
        _playAgain       = false;
        _isPlayerCreated = false;
        _isSceneLoaded   = false;
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        else
            StartCoroutine(LoadMenuDelayed());
    }

    public override void OnConnectedToMaster()
    {
        if (_playAgain)
        {
            _playAgain = false;
            var options = new RoomOptions { MaxPlayers = 4 };
            string room = Random.Range(1000, 9999).ToString();
            PhotonNetwork.CreateRoom(room, options);
        }
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.InRoom && _isSceneLoaded && !_isPlayerCreated)
        {
            _isPlayerCreated = true;
            StartCoroutine(CreatePlayerDelayed());
        }
        UpdatePlayerCount();
    }

    private IEnumerator CreatePlayerDelayed()
    {
        yield return new WaitForSeconds(1f);
        CreatePlayer();
    }

    private IEnumerator LoadMenuDelayed()
    {
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Menu");
    }

    private void CreatePlayer()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("Попытка создать игрока вне комнаты!");
            return;
        }
        Vector3 spawnPos;
        if (LevelGenerator.PlatformPositions != null && LevelGenerator.PlatformPositions.Count > 0)
            spawnPos = LevelGenerator.PlatformPositions[Random.Range(0, LevelGenerator.PlatformPositions.Count)];
        else
            spawnPos = new Vector3(Random.Range(-3f, 3f), 2f, 0f);
        PlayerSkinNetwork.ApplyLocalPlayerProperties();
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
        Debug.Log($"Игрок создан: {PhotonNetwork.NickName} на {spawnPos}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => UpdatePlayerCount();
    public override void OnPlayerLeftRoom(Player otherPlayer)  => UpdatePlayerCount();

    private void UpdatePlayerCount()
    {
        if (playersCountText != null && PhotonNetwork.CurrentRoom != null)
            playersCountText.text =
                $"{PhotonNetwork.PlayerList.Length}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
    }

    public override void OnLeftRoom()
    {
        _isPlayerCreated = false;
        _isSceneLoaded   = false;
        if (!_playAgain)
            StartCoroutine(LoadMenuDelayed());
    }
}
