using UnityEngine;
using Photon.Pun;

public class NetworkInterpolation : MonoBehaviourPun, IPunObservable
{
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    [SerializeField] public float positionLerp = 15f;
    [SerializeField] public float rotationLerp = 10f;
    [SerializeField] private float maxPositionError = 1f;
    private Vector3 velocity;
    private float lastUpdateTime;
    private Rigidbody2D rigidbody2d;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    private void Start()
    {
        if (!photonView.IsMine && rigidbody2d != null)
        {
            rigidbody2d.simulated = false;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            targetPosition = (Vector3)stream.ReceiveNext();
            targetRotation = (Quaternion)stream.ReceiveNext();
            lastUpdateTime = Time.time;

            // Резкая коррекция при большой ошибке
            if (Vector3.Distance(transform.position, targetPosition) > maxPositionError)
            {
                transform.position = targetPosition;
            }
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            float timeSinceUpdate = Time.time - lastUpdateTime;
            float factor = Mathf.Clamp01(timeSinceUpdate * positionLerp);

            transform.position = Vector3.Lerp(transform.position, targetPosition, factor);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, factor);
        }
    }
}
