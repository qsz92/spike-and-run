using UnityEngine;

public class PlayerDeathDetector : MonoBehaviour
{
    private Platformer.PlayerController _pc;

    void Start()
    {
        _pc = GetComponent<Platformer.PlayerController>();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (_pc == null) return;

        if (other.gameObject.CompareTag("Enemy"))
        {
            _pc.deathState = true;
        }
    }
}