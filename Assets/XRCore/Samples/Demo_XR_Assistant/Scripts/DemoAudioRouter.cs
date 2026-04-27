using System.Collections;
using UnityEngine;
using XRCore.Core;
using XRCore.Tasks;

public class DemoAudioRouter : MonoBehaviour
{
    public AudioSource audioSource;

    public AudioClip lookAtCubeClip;
    public AudioClip objectDetectedClip;
    public AudioClip taskCompletedClip;
    public float completionAudioDelaySeconds = 0f;

    private bool _objectDetectedPlayed;
    private float _lastObjectDetectedTime = -999f;
    private Coroutine _completionRoutine;
    private float _lastLookCueTime = -999f;
    private bool _receivedTaskStartedEvent;

    void OnEnable()
    {
        XRCoreEventBus.Subscribe<HeadGazeTargetEnteredEvent>(OnTargetEntered);
        XRCoreEventBus.Subscribe<XRTaskCompletedEvent>(OnTaskCompleted);
        XRCoreEventBus.Subscribe<XRTaskStartedEvent>(OnTaskStarted);
    }

    void OnDisable()
    {
        XRCoreEventBus.Unsubscribe<HeadGazeTargetEnteredEvent>(OnTargetEntered);
        XRCoreEventBus.Unsubscribe<XRTaskCompletedEvent>(OnTaskCompleted);
        XRCoreEventBus.Unsubscribe<XRTaskStartedEvent>(OnTaskStarted);

        if (_completionRoutine != null)
        {
            StopCoroutine(_completionRoutine);
            _completionRoutine = null;
        }
    }

    void Start()
    {
        if (!_receivedTaskStartedEvent)
        {
            PlayLookCue();
        }
    }

    void OnTaskStarted(XRTaskStartedEvent e)
    {
        _receivedTaskStartedEvent = true;
        _objectDetectedPlayed = false;
        _lastObjectDetectedTime = -999f;

        if (_completionRoutine != null)
        {
            StopCoroutine(_completionRoutine);
            _completionRoutine = null;
        }

        // Prevent duplicate intro if Start() already played it this frame.
        if (Time.time - _lastLookCueTime > 0.2f)
        {
            PlayLookCue();
        }
    }

    void OnTargetEntered(HeadGazeTargetEnteredEvent e)
    {
        if (_objectDetectedPlayed)
        {
            return;
        }

        _objectDetectedPlayed = true;
        _lastObjectDetectedTime = Time.time;
        Play(objectDetectedClip);
    }

    void OnTaskCompleted(XRTaskCompletedEvent e)
    {
        if (_completionRoutine != null)
        {
            StopCoroutine(_completionRoutine);
        }

        float wait = 0f;
        if (_objectDetectedPlayed)
        {
            float elapsed = Time.time - _lastObjectDetectedTime;
            wait = Mathf.Max(0f, completionAudioDelaySeconds - elapsed);
        }

        _completionRoutine = StartCoroutine(PlayCompletedAfterDelay(wait));
    }

    private IEnumerator PlayCompletedAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        Play(taskCompletedClip);
        _completionRoutine = null;
    }

    void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    void PlayLookCue()
    {
        _lastLookCueTime = Time.time;
        Play(lookAtCubeClip);
    }
}
