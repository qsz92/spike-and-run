using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MusicPlayer : MonoBehaviour
{
    [System.Serializable]
    public class Track
    {
        public AudioClip clip;
        public string artist;
        public string title;
    }

    [SerializeField] private Track[] tracks;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject trackInfoPanel;
    [SerializeField] private TMP_Text artistText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float showDuration = 5f;
    [SerializeField] private float fadeDuration = 0.5f;

    private List<int> playlist = new List<int>();
    private int currentIndex = 0;

    private void Start()
    {
        if (tracks.Length == 0)
        {
            Debug.LogError("MusicPlayer: нет треков!");
            return;
        }
        Debug.Log($"MusicPlayer: треков {tracks.Length}, клип: {tracks[0].clip}");
        GeneratePlaylist();
        PlayTrack(playlist[currentIndex]);
    }

    private void Update()
    {
        if (!audioSource.isPlaying)
        {
            currentIndex++;
            if (currentIndex >= playlist.Count)
            {
                GeneratePlaylist();
                currentIndex = 0;
            }
            PlayTrack(playlist[currentIndex]);
        }
    }

    private void GeneratePlaylist()
    {
        playlist.Clear();
        List<int> indices = new List<int>();
        for (int i = 0; i < tracks.Length; i++)
            indices.Add(i);

        while (indices.Count > 0)
        {
            int r = Random.Range(0, indices.Count);
            playlist.Add(indices[r]);
            indices.RemoveAt(r);
        }
    }

    private void PlayTrack(int index)
    {
        Track track = tracks[index];
        audioSource.clip = track.clip;
        audioSource.Play();
        Debug.Log($"MusicPlayer: играет {track.title} - {track.artist}");

        artistText.text = track.artist;
        titleText.text = track.title;

        StopAllCoroutines();
        StartCoroutine(ShowAndHidePanel());
    }

    private IEnumerator ShowAndHidePanel()
    {
        trackInfoPanel.SetActive(true);
        panelCanvasGroup.alpha = 0f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(showDuration);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(1f - t / fadeDuration);
            yield return null;
        }

        panelCanvasGroup.alpha = 0f;
        trackInfoPanel.SetActive(false);
    }
}