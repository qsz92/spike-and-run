using Photon.Pun;
using UnityEngine;

public class CoinSync : MonoBehaviourPunCallbacks
{
    [HideInInspector] public int coinIndex;
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;
        var pv = other.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) return;
        collected = true;

        // Если photonView есть — RPC, если нет — просто локально
        if (photonView != null)
            photonView.RPC(nameof(RPC_Collect), RpcTarget.All);
        else
            Destroy(gameObject);
    }

    [PunRPC]
    private void RPC_Collect()
    {
        Destroy(gameObject);
    }
}