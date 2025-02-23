using UnityEngine;

/// <summary>
/// This script overrides the rotation of the attached GameObject by 
/// smoothly blending between the new desired rotation each frame 
/// and the final rotation from the previous frame.
/// Attach it to any GameObject in your scene.
/// </summary>
public class SmoothRotationOverride : MonoBehaviour
{
    [Tooltip("Controls how quickly the rotation blends.")]
    public float smoothingSpeed = 5f;

    // Stores the final rotation we applied in the previous frame
    private Quaternion _previousRotation;

    // Example: we might read this from an AI system, input, or some other logic each frame
    // Here, it's just a placeholder you can set from the Inspector or assign externally.
    [Tooltip("The desired rotation this frame (can be set from code or Inspector).")]
    public Quaternion desiredRotation;
    [SerializeField] private Transform bikeParent;

    private void Start()
    {
        // Initialize _previousRotation with the current rotation of the object
        _previousRotation = transform.rotation;
    }

    private void Update()
    {
        desiredRotation = bikeParent.rotation;

        // 1. Compute a smoothed rotation by interpolating between _previousRotation and desiredRotation
        Quaternion smoothedRotation = Quaternion.Slerp(
            _previousRotation,
            desiredRotation,
            Time.deltaTime * smoothingSpeed
        );

        // 2. Apply that smoothed rotation
        transform.rotation = smoothedRotation;

        // 3. Update _previousRotation so that next frame, we keep blending from this new final rotation
        _previousRotation = smoothedRotation;
    }
}
