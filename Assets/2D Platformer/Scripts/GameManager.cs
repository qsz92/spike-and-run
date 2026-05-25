using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

namespace Platformer
{
    public class GameManager : MonoBehaviour
    {
        public int coinsCounter = 0;
        public GameObject playerGameObject;
        private PlayerController player;
        public GameObject deathPlayerPrefab;
        public Text coinText;

        [Header("Death UI")]
        public GameObject deathPanel;
        public GameObject[] hideOnDeathObjects;
        private GameObject deathPlayerObject;
        public GameObject CurrentPlayerGameObject => playerGameObject;

        void Start()
        {
            if (deathPanel != null)
                deathPanel.SetActive(false);

            SetHideOnDeathObjects(true);
        }

        void Update()
        {
            if (player == null)
            {
                player = FindLocalPlayer();
                if (player != null)
                    playerGameObject = player.gameObject;
                return;
            }

            if (coinText != null)
                coinText.text = coinsCounter.ToString();

            if (player.deathState == true)
            {
                playerGameObject.SetActive(false);
                deathPlayerObject = (GameObject)Instantiate(
                    deathPlayerPrefab,
                    playerGameObject.transform.position,
                    playerGameObject.transform.rotation
                );
                deathPlayerObject.transform.localScale = playerGameObject.transform.localScale;
                player.deathState = false;
                Invoke("ShowDeathPanel", 1.5f);
            }
        }

        private void ShowDeathPanel()
        {
            // Сохраняем монеты
            int saved = PlayerPrefs.GetInt("TotalCoins", 0);
            PlayerPrefs.SetInt("TotalCoins", saved + coinsCounter);
            AccountManager.SaveProgress();
            AccountManager.NotifyProgressChanged();

            if (deathPanel != null)
                deathPanel.SetActive(true);

            SetHideOnDeathObjects(false);
        }

        public void PrepareForRespawn()
        {
            CancelInvoke("ShowDeathPanel");
            if (deathPanel != null)
                deathPanel.SetActive(false);

            SetHideOnDeathObjects(true);

            if (deathPlayerObject != null)
                Destroy(deathPlayerObject);

            player = null;
            playerGameObject = null;
        }

        private void SetHideOnDeathObjects(bool visible)
        {
            if (hideOnDeathObjects == null) return;
            foreach (var obj in hideOnDeathObjects)
                if (obj) obj.SetActive(visible);
        }

        private PlayerController FindLocalPlayer()
        {
            PlayerController[] players = FindObjectsOfType<PlayerController>();
            foreach (PlayerController candidate in players)
            {
                PhotonView view = candidate.GetComponent<PhotonView>();
                if (view == null || view.IsMine)
                    return candidate;
            }
            return null;
        }
    }
}
