using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PlayerQuestionVideoScreen : MonoBehaviour
{
    [Header("Video Elements")]
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;

    [Header("Video Clips")]
    public VideoClip playerQuestionVideo1; // Round 3 (1st player question)
    public VideoClip playerQuestionVideo2; // Round 6 (2nd player question)
    public VideoClip playerQuestionVideo3; // Round 9 (3rd player question)

    [Header("Settings")]
    public bool skipOnClick = true;
    public bool autoAdvanceOnVideoEnd = true;

    void Start()
    {
        // Setup video player
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;

            // Set the appropriate video clip based on current round
            SetVideoClipForCurrentRound();

            // Start preparing the video
            videoPlayer.Prepare();
        }
    }

    void SetVideoClipForCurrentRound()
    {
        if (videoPlayer == null || GameManager.Instance == null) return;

        int currentRound = GameManager.Instance.GetCurrentRound();

        // Player questions are at rounds 3, 6, and 9
        VideoClip selectedClip = null;

        switch (currentRound)
        {
            case 3: // 1st player question
                selectedClip = playerQuestionVideo1;
                Debug.Log("Loading player question video 1 for round 3");
                break;
            case 6: // 2nd player question
                selectedClip = playerQuestionVideo2;
                Debug.Log("Loading player question video 2 for round 6");
                break;
            case 9: // 3rd player question
                selectedClip = playerQuestionVideo3;
                Debug.Log("Loading player question video 3 for round 9");
                break;
            default:
                Debug.LogWarning("Unexpected round for player question video: " + currentRound);
                selectedClip = playerQuestionVideo1; // Fallback
                break;
        }

        if (selectedClip != null)
        {
            videoPlayer.clip = selectedClip;
        }
        else
        {
            Debug.LogError("No video clip assigned for round " + currentRound);
        }
    }

    void Update()
    {
        // Check for skip input
        if (skipOnClick && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
        {
            SkipVideo();
        }
    }

    void OnVideoPrepared(VideoPlayer source)
    {
        Debug.Log("Player question video prepared and ready to play");

        // Auto-play when ready
        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
    }

    void OnVideoFinished(VideoPlayer source)
    {
        Debug.Log("Player question video finished");

        if (autoAdvanceOnVideoEnd)
        {
            AdvanceToNextScreen();
        }
    }

    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("Video error: " + message);

        // If video fails, just advance
        AdvanceToNextScreen();
    }

    void SkipVideo()
    {
        Debug.Log("Skipping player question video");

        // Stop video
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        AdvanceToNextScreen();
    }

    void AdvanceToNextScreen()
    {
        // Only server can advance scenes
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            // Use NetworkSceneManager via GameManager
            GameManager.Instance.LoadScene("QuestionScreen");

            // Start the question timer (server-only)
            GameManager.Instance.StartTimer(GameManager.Instance.questionTimer);
        }
        else
        {
            Debug.LogWarning("[PlayerQuestionVideo] Non-server tried to advance scene - this should be server-driven");
        }
    }

    void OnDestroy()
    {
        // Clean up video listeners
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
}
