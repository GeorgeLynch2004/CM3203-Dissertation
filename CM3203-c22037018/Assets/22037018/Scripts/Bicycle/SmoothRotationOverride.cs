using UnityEngine;

public class SmoothRotationOverride : MonoBehaviour
{
    public float smoothingSpeed = 5f;
    private Quaternion previousRotation;
    public Quaternion desiredRotation;
    [SerializeField] private Transform bikeParent;

    private void Start()
    {
        // Initialize _previousRotation with the current rotation of the object
        previousRotation = transform.rotation;
    }

    private void Update()
    {
        desiredRotation = bikeParent.rotation;

        // Compute a smoothed rotation by interpolating between _previousRotation and desiredRotation
        Quaternion smoothedRotation = Quaternion.Slerp(
            _previousRotation,
            desiredRotation,
            Time.deltaTime * smoothingSpeed
        );

        // Apply that smoothed rotation
        transform.rotation = smoothedRotation;

        // Update previousRotation so that next frame, we keep blending from this new final rotation
        previousRotation = smoothedRotation;
    }
}
