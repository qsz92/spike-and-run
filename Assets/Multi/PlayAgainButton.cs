using UnityEngine;

public class PlayAgainButton : MonoBehaviour
{
    public void OnClick()
    {
        NetworkManager manager = NetworkManager.Instance != null
            ? NetworkManager.Instance
            : FindFirstObjectByType<NetworkManager>();

        if (manager != null)
            manager.PlayAgain();
    }
}
