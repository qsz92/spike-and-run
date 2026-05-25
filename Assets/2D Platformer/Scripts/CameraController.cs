using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Platformer
{
    public class CameraController : MonoBehaviour
    {
        public float damping = 1.5f; // movement speed
        public Vector2 offset = new Vector2(0f, 0f); // special effect if you want the character to be not in center of screen
        public bool faceLeft; //  mirror reflection of OFFSET along the y axis
        private Transform player;
        private float lastX;
        private float zOffset;
        private Camera cameraComponent;
        private AudioListener audioListener;
        private PhotonView ownerPhotonView;

        void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            audioListener = GetComponent<AudioListener>();
            ownerPhotonView = GetComponentInParent<PhotonView>();

            if (ownerPhotonView != null && !ownerPhotonView.IsMine)
            {
                SetLocalCameraState(false);
                enabled = false;
                return;
            }

            SetLocalCameraState(true);
        }

        void Start () {
            offset = new Vector2(Mathf.Abs(offset.x), offset.y);
            FindPlayer(faceLeft);
        }

        public void FindPlayer(bool playerFaceLeft)
        {
            player = ResolvePlayer();
            if (player == null) return;

            faceLeft = playerFaceLeft;
            lastX = player.position.x;
            zOffset = transform.position.z - player.position.z;
            transform.position = GetTargetPosition();
        }

        void LateUpdate () {
            if (player == null)
            {
                player = ResolvePlayer();
                if (player == null) return;

                lastX = player.position.x;
                zOffset = transform.position.z - player.position.z;
            }

            float xDelta = player.position.x - lastX;
            if (Mathf.Abs(xDelta) > 0.01f)
            {
                faceLeft = xDelta < 0f;
            }
            lastX = player.position.x;

            float lerpFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, damping) * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, GetTargetPosition(), lerpFactor);
        }

        private Transform ResolvePlayer()
        {
            if (ownerPhotonView != null) return ownerPhotonView.transform;

            if (transform.parent != null && transform.parent.CompareTag("Player"))
            {
                return transform.parent;
            }

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject candidate in players)
            {
                PhotonView view = candidate.GetComponent<PhotonView>();
                if (view == null || view.IsMine)
                {
                    return candidate.transform;
                }
            }

            return null;
        }

        private Vector3 GetTargetPosition()
        {
            float horizontalOffset = faceLeft ? -offset.x : offset.x;
            return new Vector3(player.position.x + horizontalOffset, player.position.y + offset.y, player.position.z + zOffset);
        }

        private void SetLocalCameraState(bool isLocal)
        {
            if (cameraComponent != null) cameraComponent.enabled = isLocal;
            if (audioListener != null) audioListener.enabled = isLocal;
            gameObject.tag = isLocal ? "MainCamera" : "Untagged";
        }
    }
}

