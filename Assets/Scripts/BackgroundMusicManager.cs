using UnityEngine;
using System.Collections;

public class BackgroundMusicManager : MonoBehaviour
{
    [Header("Music Tracks")]
    [Tooltip("Array of background music tracks to choose from")]
    public AudioClip[] backgroundTracks;

    [Header("Volume Settings")]
    [Tooltip("Volume level for background music")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    [Tooltip("Fade duration when switching tracks (in seconds)")]
    public float fadeTime = 1.0f;

    // Reference to AudioSource component
    private AudioSource audioSource;

    // Current track index
    private int currentTrackIndex = -1;

    // Singleton instance
    private static BackgroundMusicManager _instance;
    public static BackgroundMusicManager Instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        // Singleton pattern implementation
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Optional: Make this object persist between scenes
        // DontDestroyOnLoad(gameObject);

        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource
        audioSource.loop = true;
        audioSource.volume = musicVolume;
    }

    private void Start()
    {
        // Validate tracks array
        if (backgroundTracks == null || backgroundTracks.Length == 0)
        {
            Debug.LogError("No background tracks assigned to BackgroundMusicManager!");
            return;
        }

        // Play a random track
        PlayRandomTrack();
    }

    /// <summary>
    /// Selects and plays a random track from the available tracks
    /// </summary>
    public void PlayRandomTrack()
    {
        if (backgroundTracks == null || backgroundTracks.Length == 0)
        {
            return;
        }

        // Select a random track (different from current if possible)
        int randomIndex;
        if (backgroundTracks.Length > 1)
        {
            do
            {
                randomIndex = Random.Range(0, backgroundTracks.Length);
            }
            while (randomIndex == currentTrackIndex);
        }
        else
        {
            randomIndex = 0;
        }

        PlayTrack(randomIndex);
    }

    /// <summary>
    /// Plays the track at the specified index
    /// </summary>
    /// <param name="trackIndex">Index of the track to play</param>
    public void PlayTrack(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= backgroundTracks.Length)
        {
            Debug.LogError("Track index out of range: " + trackIndex);
            return;
        }

        // Skip if same track is already playing
        if (trackIndex == currentTrackIndex && audioSource.isPlaying)
        {
            return;
        }

        currentTrackIndex = trackIndex;

        // Start fading if already playing something
        if (audioSource.isPlaying)
        {
            StartCoroutine(FadeAndPlayTrack(backgroundTracks[trackIndex]));
        }
        else
        {
            // Play immediately if nothing is playing
            audioSource.clip = backgroundTracks[trackIndex];
            audioSource.volume = musicVolume;
            audioSource.Play();
        }

        Debug.Log("Now playing track: " + backgroundTracks[trackIndex].name);
    }

    /// <summary>
    /// Fades out current track and fades in the new one
    /// </summary>
    private IEnumerator FadeAndPlayTrack(AudioClip newTrack)
    {
        // Fade out
        float startVolume = audioSource.volume;
        float currentTime = 0;

        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0, currentTime / fadeTime);
            yield return null;
        }

        // Switch tracks
        audioSource.Stop();
        audioSource.clip = newTrack;
        audioSource.Play();

        // Fade in
        currentTime = 0;
        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0, musicVolume, currentTime / fadeTime);
            yield return null;
        }

        // Ensure we end at the target volume
        audioSource.volume = musicVolume;
    }

    /// <summary>
    /// Sets the music volume
    /// </summary>
    /// <param name="volume">New volume (0-1)</param>
    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        audioSource.volume = musicVolume;
    }

    /// <summary>
    /// Pauses the current track
    /// </summary>
    public void PauseMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    /// <summary>
    /// Resumes the current track if paused
    /// </summary>
    public void ResumeMusic()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }

    /// <summary>
    /// Stops the current track
    /// </summary>
    public void StopMusic()
    {
        audioSource.Stop();
    }

    /// <summary>
    /// Handle track ending (in case loop is disabled)
    /// </summary>
    private void Update()
    {
        // If we're not looping tracks automatically, check if track ended
        if (!audioSource.loop && audioSource.clip != null)
        {
            if (!audioSource.isPlaying && audioSource.time >= audioSource.clip.length)
            {
                // Track ended, restart same track
                audioSource.Play();
            }
        }
    }
}