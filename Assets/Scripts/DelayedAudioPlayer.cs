using System.Collections;
using UnityEngine;

/// <summary>
/// Plays an audio source after a specified delay from scene start.
/// </summary>
public class DelayedAudioPlayer : MonoBehaviour
{
    [Tooltip("The AudioSource component to play")]
    [SerializeField] private AudioSource targetAudioSource;

    [Tooltip("Delay in seconds before playing the audio")]
    [SerializeField] private float delayInSeconds = 5.0f;

    [Tooltip("Should the audio play only once or repeat after the delay?")]
    [SerializeField] private bool playOnce = true;

    private void Start()
    {
        // If no AudioSource is assigned, try to get one from this GameObject
        if (targetAudioSource == null)
        {
            targetAudioSource = GetComponent<AudioSource>();

            // Log warning if still null
            if (targetAudioSource == null)
            {
                Debug.LogWarning("No AudioSource assigned or found on this GameObject!");
                return;
            }
        }

        // Make sure the audio doesn't play automatically
        targetAudioSource.playOnAwake = false;

        // Start the delayed playback coroutine
        if (playOnce)
        {
            StartCoroutine(PlayAudioAfterDelay());
        }
        else
        {
            StartCoroutine(PlayAudioRepeatedlyAfterDelay());
        }
    }

    private IEnumerator PlayAudioAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delayInSeconds);

        // Play the audio
        targetAudioSource.Play();
        Debug.Log($"Playing audio: {targetAudioSource.clip.name} after {delayInSeconds} seconds");
    }

    private IEnumerator PlayAudioRepeatedlyAfterDelay()
    {
        // Initial delay
        yield return new WaitForSeconds(delayInSeconds);

        while (true)
        {
            // Play the audio
            targetAudioSource.Play();
            Debug.Log($"Playing audio: {targetAudioSource.clip.name}");

            // Wait until it's done
            yield return new WaitForSeconds(targetAudioSource.clip.length);

            // Add a small gap between repetitions if needed
            yield return new WaitForSeconds(0.1f);
        }
    }
}