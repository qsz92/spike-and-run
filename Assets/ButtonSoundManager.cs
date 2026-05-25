using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;

    private void Start()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var button in buttons)
            button.onClick.AddListener(() => {
                if (AudioManager.Instance != null && AudioManager.Instance.SfxEnabled)
                    AudioManager.Instance.PlaySfx(clickSound);
            });
    }
}