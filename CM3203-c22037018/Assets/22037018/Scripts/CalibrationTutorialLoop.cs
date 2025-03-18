using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CalibrationTutorialLoop : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;   // Assign in Inspector
    [SerializeField] private VideoPlayer videoPlayer; // Assign in Inspector

    void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        if (rawImage == null)
        {
            rawImage = GetComponent<RawImage>();
        }

        // Set the video output to the RawImage texture
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = new RenderTexture(1920, 1080, 0);
        rawImage.texture = videoPlayer.targetTexture;

        videoPlayer.isLooping = true; // Enable looping

        videoPlayer.Play(); // Start the video
    }
}
