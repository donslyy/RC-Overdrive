using UnityEngine;
using Cinemachine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerScript playerScript; // Reference to the PlayerController script
    [SerializeField] private CinemachineVirtualCamera virtualCamera; // Reference to the Cinemachine virtual camera

    [Header("FOV Settings")]
    [SerializeField] private float minFOV = 60f; // Minimum FOV value
    [SerializeField] private float maxFOV = 100f; // Maximum FOV value
    [SerializeField] private float maxSpeed = 100f; // Speed at which FOV will be at its maximum
    [SerializeField] private float fovSmoothTime = 0.2f; // Smoothing time for FOV adjustment

    private float currentFOV;
    private float fovVelocity;

    void Start()
    {
        currentFOV = virtualCamera.m_Lens.FieldOfView; // Initialize current FOV
    }

    void Update()
    {
        AdjustFOV();
    }

    private void AdjustFOV()
    {
        // Get the player's current speed
        float speed = Mathf.Abs(playerScript.CurrentSpeed); // Reference the CurrentSpeed from PlayerController

        // Calculate the target FOV based on the player's speed
        float targetFOV = Mathf.Lerp(minFOV, maxFOV, speed / maxSpeed);

        // Smoothly adjust the FOV
        currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovVelocity, fovSmoothTime);

        // Apply the FOV to the Cinemachine camera
        virtualCamera.m_Lens.FieldOfView = currentFOV;
    }
}
