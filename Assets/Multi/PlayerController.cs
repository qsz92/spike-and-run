using UnityEngine;
using Photon.Pun;
using System.Collections;
using Photon.Realtime;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class PlayerController : MonoBehaviourPun
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float mouseSensitivity = 800f;
    [SerializeField] private Camera playerCamera;

    [Header("Camera Settings")]
    [SerializeField] private bool isThirdPerson = false;
    [SerializeField] private float distanceFromPlayer = 5.0f;
    [SerializeField] private float heightOffset = 1.7f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 70f;



    private CharacterController _controller;
    private Vector3 _velocity;
    private float _xRotation = 0f;
    private Vector3 _firstPersonCameraLocalPosition;
    private Vector3 _exitPosition;
    private AudioListener _playerAudioListener;

    private void Start()
    {
        if (photonView.IsMine)
        {
            float savedValue = PlayerPrefs.GetFloat("MouseSensitivity",
                PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("MouseSensitivity", out var val)
                    ? (float)val
                    : 800f);

            mouseSensitivity = savedValue;
            _firstPersonCameraLocalPosition = playerCamera.transform.localPosition;

            // Получаем AudioListener
            _playerAudioListener = playerCamera.GetComponent<AudioListener>();
            if (_playerAudioListener == null)
            {
                _playerAudioListener = playerCamera.gameObject.AddComponent<AudioListener>();
            }
        }
    }

    public void SetMouseSensitivity(float value)
    {
        if (!photonView.IsMine) return;

        mouseSensitivity = Mathf.Clamp(value, 100f, 2000f);
        Debug.Log($"Sensitivity applied: {mouseSensitivity}");
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (photonView.IsMine)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                playerCamera.tag = "MainCamera";
            }

            // Гарантируем наличие AudioListener
            if (playerCamera.GetComponent<AudioListener>() == null)
            {
                playerCamera.gameObject.AddComponent<AudioListener>();
            }

            Cursor.lockState = CursorLockMode.Locked;
            _firstPersonCameraLocalPosition = playerCamera.transform.localPosition;
        }
        else
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom || !photonView.IsMine) return;

        // Движение игрока
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        _controller.Move(move * moveSpeed * Time.deltaTime);

        // Прыжок
        if (Input.GetButtonDown("Jump") && _controller.isGrounded)
            _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);

        // Обработка вращения камеры
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        if (isThirdPerson)
        {
            // Вращение игрока по горизонтали
            transform.Rotate(Vector3.up * mouseX);

            // Вертикальный угол камеры
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, minVerticalAngle, maxVerticalAngle);
        }
        else
        {
            // Режим первого лица
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }
    }

    private void LateUpdate()
    {
        if (!PhotonNetwork.InRoom || !photonView.IsMine) return;
    }

    private void EnablePlayerComponents(bool enable)
    {
        _controller.enabled = enable;
        GetComponent<MeshRenderer>().enabled = enable;

        Collider[] playerColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in playerColliders)
        {
            col.enabled = enable;
        }
    }
}