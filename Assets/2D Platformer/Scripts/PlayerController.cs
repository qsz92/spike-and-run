using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

namespace Platformer
{
    public class PlayerController : MonoBehaviour
    {
        public float movingSpeed;
        public float jumpForce;
        private float moveInput;

        private bool facingRight = false;
        [HideInInspector]
        public bool deathState = false;

        private bool isGrounded;
        public Transform groundCheck;

        [SerializeField] private TMP_FontAsset nicknameFont;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField, Range(0f, 1f)] private float ownJumpVolume = 0.22f;
        [SerializeField, Range(0f, 1f)] private float otherJumpVolume = 0.08f;
        [SerializeField] private float jumpSoundMinDistance = 2f;
        [SerializeField] private float jumpSoundMaxDistance = 14f;
        [SerializeField] private AudioClip walkSound;
        [SerializeField, Range(0f, 1f)] private float ownWalkVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float otherWalkVolume = 0.25f;
        [SerializeField] private float walkSoundMinDistance = 2f;
        [SerializeField] private float walkSoundMaxDistance = 10f;
        [SerializeField] private float walkFadeSpeed = 4f;
        [SerializeField] private AudioSource jumpAudioSource;
        [SerializeField] private AudioSource walkAudioSource;

        private Rigidbody2D rigidbody;
        private SpriteRenderer spriteRenderer;
        private GameManager gameManager;
        private PhotonView photonView;
        private GameObject nicknameObj;
        private static Transform localPlayerTransform;
        private bool walkingSoundActive;
        private bool IsLocalPlayer => photonView == null || photonView.IsMine;

        private Sprite[] idleSprites;
        private Sprite[] runSprites;
        private Sprite jumpSprite;

        private int currentFrame = 0;
        private float frameTimer = 0f;
        private float frameRate = 0.12f;
        private int playerState = 0;

        void Awake()
        {
            rigidbody = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (jumpAudioSource == null) jumpAudioSource = GetComponent<AudioSource>();
            if (jumpAudioSource == null) jumpAudioSource = gameObject.AddComponent<AudioSource>();
            ConfigureJumpAudioSource();
            if (walkAudioSource == null) walkAudioSource = gameObject.AddComponent<AudioSource>();
            ConfigureWalkAudioSource();
            photonView = GetComponent<PhotonView>();

            if (jumpSound == null)
                jumpSound = Resources.Load<AudioClip>("Audio/SFX/jg-032316-sfx-8-bit-pong-sound_qIf7da96");
            if (walkSound == null)
                walkSound = Resources.Load<AudioClip>("Audio/SFX/sound-18123_fbIRGbDz");

            LoadSkinSprites();

            nicknameObj = new GameObject("Nickname");
            nicknameObj.transform.position = transform.position + new Vector3(0, 1.2f, 0);
            var tmp = nicknameObj.AddComponent<TextMeshPro>();
            tmp.text = photonView != null ? photonView.Owner.NickName : "Player";
            tmp.fontSize = 2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 10;
            if (nicknameFont != null) tmp.font = nicknameFont;
        }

        void Start()
        {
            if (IsLocalPlayer)
            {
                localPlayerTransform = transform;
                gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            }
        }

        private void FixedUpdate()
        {
            if (!IsLocalPlayer) return;
            CheckGround();
        }

        void Update()
        {
            if (nicknameObj != null)
                nicknameObj.transform.position = transform.position + new Vector3(0, 1.2f, 0);

            if (!IsLocalPlayer)
            {
                UpdateWalkingSoundFade();
                return;
            }

            if (Input.GetButton("Horizontal"))
            {
                moveInput = Input.GetAxis("Horizontal");
                Vector3 direction = transform.right * moveInput;
                transform.position = Vector3.MoveTowards(transform.position, transform.position + direction, movingSpeed * Time.deltaTime);
                playerState = 1;
            }
            else
            {
                if (isGrounded) playerState = 0;
            }

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rigidbody.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
                if (photonView != null)
                    photonView.RPC(nameof(RPC_PlayJumpSound), RpcTarget.All);
                else
                    PlayJumpSound();
            }

            if (!isGrounded) playerState = 2;

            if (facingRight == false && moveInput > 0) Flip();
            else if (facingRight == true && moveInput < 0) Flip();

            AnimateSprite();
            SetWalkingSoundActive(Input.GetButton("Horizontal") && Mathf.Abs(moveInput) > 0.01f && isGrounded && !deathState);
            UpdateWalkingSoundFade();
        }

        void OnDestroy()
        {
            if (nicknameObj != null)
                Destroy(nicknameObj);
        }

        private void AnimateSprite()
        {
            if (playerState == 2)
            {
                if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
                return;
            }

            Sprite[] frames = playerState == 1 ? runSprites : idleSprites;
            if (frames == null || frames.Length == 0) return;

            frameTimer += Time.deltaTime;
            if (frameTimer >= frameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % frames.Length;
                spriteRenderer.sprite = frames[currentFrame];
            }
        }

        private void LoadSkinSprites()
        {
            int skinIndex = PlayerSkinNetwork.GetSkinIndex(photonView);
            if (skinIndex == CustomSkinUtility.CustomSkinIndex)
            {
                Color bodyA = PlayerSkinNetwork.GetBodyA(photonView);
                Color bodyB = PlayerSkinNetwork.GetBodyB(photonView);
                Color accent = PlayerSkinNetwork.GetAccent(photonView);
                idleSprites = CustomSkinUtility.BuildCustomSprites(Resources.LoadAll<Sprite>("Skin 1/idle"), bodyA, bodyB, accent);
                runSprites = CustomSkinUtility.BuildCustomSprites(Resources.LoadAll<Sprite>("Skin 1/run"), bodyA, bodyB, accent);
                Sprite[] jumpArr = CustomSkinUtility.BuildCustomSprites(Resources.LoadAll<Sprite>("Skin 1/jump"), bodyA, bodyB, accent);
                jumpSprite = jumpArr.Length > 0 ? jumpArr[0] : null;
                return;
            }

            string skinFolder = $"Skin {skinIndex}";
            idleSprites = Resources.LoadAll<Sprite>($"{skinFolder}/idle");
            runSprites = Resources.LoadAll<Sprite>($"{skinFolder}/run");
            Sprite[] defaultJumpArr = Resources.LoadAll<Sprite>($"{skinFolder}/jump");
            jumpSprite = defaultJumpArr.Length > 0 ? defaultJumpArr[0] : null;
        }

        private void Flip()
        {
            facingRight = !facingRight;
            Vector3 Scaler = transform.localScale;
            Scaler.x *= -1;
            transform.localScale = Scaler;
        }

        private void CheckGround()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.transform.position, 0.2f);
            isGrounded = colliders.Length > 1;
        }

        private void PlayJumpSound()
        {
            if (jumpSound == null) return;
            if (AudioManager.Instance != null && !AudioManager.Instance.SfxEnabled) return;

            float volume = IsLocalPlayer ? ownJumpVolume : GetDistanceAdjustedJumpVolume();
            if (volume <= 0f) return;

            if (jumpAudioSource != null)
                jumpAudioSource.PlayOneShot(jumpSound, volume);
        }

        [PunRPC]
        private void RPC_PlayJumpSound()
        {
            PlayJumpSound();
        }

        private void ConfigureJumpAudioSource()
        {
            jumpAudioSource.playOnAwake = false;
            jumpAudioSource.loop = false;
            jumpAudioSource.spatialBlend = 0f;
        }

        private float GetDistanceAdjustedJumpVolume()
        {
            if (localPlayerTransform == null) return otherJumpVolume;

            float distance = Vector2.Distance(transform.position, localPlayerTransform.position);
            if (distance >= jumpSoundMaxDistance) return 0f;
            if (distance <= jumpSoundMinDistance) return otherJumpVolume;

            float t = Mathf.InverseLerp(jumpSoundMinDistance, jumpSoundMaxDistance, distance);
            return otherJumpVolume * (1f - t);
        }

        private void ConfigureWalkAudioSource()
        {
            walkAudioSource.playOnAwake = false;
            walkAudioSource.loop = true;
            walkAudioSource.spatialBlend = 0f;
            walkAudioSource.volume = 0f;
        }

        private void SetWalkingSoundActive(bool active)
        {
            if (walkingSoundActive == active) return;

            ApplyWalkingSoundActive(active);

            if (photonView != null)
                photonView.RPC(nameof(RPC_SetWalkingSound), RpcTarget.Others, active);
        }

        [PunRPC]
        private void RPC_SetWalkingSound(bool active)
        {
            ApplyWalkingSoundActive(active);
        }

        private void ApplyWalkingSoundActive(bool active)
        {
            walkingSoundActive = active;

            if (active && walkSound != null && walkAudioSource != null)
            {
                walkAudioSource.clip = walkSound;
                walkAudioSource.time = 0f;
                walkAudioSource.Play();
            }
        }

        private void UpdateWalkingSoundFade()
        {
            if (walkAudioSource == null) return;

            float targetVolume = 0f;
            if (walkingSoundActive && (AudioManager.Instance == null || AudioManager.Instance.SfxEnabled))
                targetVolume = IsLocalPlayer ? ownWalkVolume : GetDistanceAdjustedVolume(otherWalkVolume, walkSoundMinDistance, walkSoundMaxDistance);

            if (walkingSoundActive && !walkAudioSource.isPlaying && walkSound != null)
            {
                walkAudioSource.clip = walkSound;
                walkAudioSource.time = 0f;
                walkAudioSource.Play();
            }

            walkAudioSource.volume = Mathf.MoveTowards(walkAudioSource.volume, targetVolume, walkFadeSpeed * Time.deltaTime);

            if (!walkingSoundActive && walkAudioSource.isPlaying && walkAudioSource.volume <= 0.001f)
                walkAudioSource.Stop();
        }

        private float GetDistanceAdjustedVolume(float baseVolume, float minDistance, float maxDistance)
        {
            if (localPlayerTransform == null) return baseVolume;

            float distance = Vector2.Distance(transform.position, localPlayerTransform.position);
            if (distance >= maxDistance) return 0f;
            if (distance <= minDistance) return baseVolume;

            float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
            return baseVolume * (1f - t);
        }

        // Коллизия (если враг не триггер)
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!IsLocalPlayer) return;
            if (other.gameObject.CompareTag("Enemy")) deathState = true;
        }

        // Триггер (если враг — триггер)
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsLocalPlayer) return;

            if (other.CompareTag("Enemy"))
            {
                deathState = true;
                return;
            }

            if (other.CompareTag("Coin"))
            {
                gameManager.coinsCounter += 1;
                Destroy(other.gameObject);
            }
        }
    }
}
