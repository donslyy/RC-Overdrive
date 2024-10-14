using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKartScript : MonoBehaviour
{
    private Rigidbody rb;
    public float CurrentSpeed = 0;
    public float MaxSpeed = 80f;
    public float accelerationRate = 2f; // Acceleration rate
    private float RealSpeed;
    public float boostSpeed;

    [Header("Tires")]
    public Transform frontLeftTire;
    public Transform frontRightTire;
    public Transform backLeftTire;
    public Transform backRightTire;

    public NavMeshAgent agent;

    private Vector3 velocity = Vector3.zero;
    public float smoothTime = 0.3f; // Time to smooth out movement

    [HideInInspector]
    public float BoostTime = 0;
    [HideInInspector]
    public bool GLIDER_FLY; // Tracks whether the glider is active
    public Animator gliderAnim; // Animator for the glider (assigned in the inspector)

    public Transform boostFire; // Particle system for fire boost effects
    public Transform boostExplosion; // Particle system for boost explosion effect

    private bool isBoostPadActive = false; // Tracks if boost is active

    [Header("Waypoints")]
    private List<Transform> waypoints; // List of waypoints
    private int currentWaypointIndex = 0; // Tracks the current waypoint

    // Define the radius around waypoints for randomness
    public float waypointRadius = 2f; // Adjust this for the size of random offset

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = 0f; // Start with zero speed to simulate acceleration

        agent.radius = 0.75f; // Detects avoidance from a larger distance
        agent.avoidancePriority = Random.Range(30, 50); // Random priority for cars to avoid each other
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; // Sets high-quality avoidance

        // Find all waypoints dynamically
        FindWaypoints();

        if (waypoints.Count > 0)
        {
            SetRandomDestinationNearWaypoint();
        }
    }

    void Update()
    {
        groundNormalRotation(); // Always adjust rotation to ground

        if (GLIDER_FLY)
        {
            EnableNavMeshAgentForGliding();
            HandleGliderMovement(); // Glider movement
        }
        else
        {
            EnableNavMeshAgent(); // Re-enable NavMeshAgent for normal pathfinding
            AccelerateKart(); // Handle acceleration over time
            ApplyNavMeshMovement(); // Move forward along the waypoint path
            tireSteer(); // Handle tire rotation and turning
        }

        boosts();
    }

    void EnableNavMeshAgentForGliding()
    {
        if (!agent.enabled)
        {
            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = true;

            // Calculate the height of the glider object dynamically
            float gliderHeight = CalculateGliderHeight();

            // Debug log to check the height calculation
            Debug.Log("Calculated Glider Height: " + gliderHeight);

            // Set agent's base offset relative to the glider's height to avoid clipping
            agent.baseOffset = gliderHeight + 1.5f;  // Adjust offset with a small buffer to prevent clipping

            // Debug log to check the baseOffset applied
            Debug.Log("NavMeshAgent baseOffset: " + agent.baseOffset);
        }
    }

    float CalculateGliderHeight()
    {
        // Get the Renderer component from the glider object (make sure glider is assigned correctly)
        Renderer gliderRenderer = gliderAnim.gameObject.GetComponent<Renderer>();

        if (gliderRenderer != null)
        {
            // Calculate the height based on the renderer bounds
            return gliderRenderer.bounds.size.y;
        }

        // Return a default value if no renderer is found (you can adjust this as needed)
        return 1f;
    }

    void EnableNavMeshAgent()
    {
        if (!agent.enabled)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            rb.isKinematic = true;  // NavMeshAgent should take over
            agent.enabled = true;
            agent.baseOffset = 0f; // Reset to default ground offset when not gliding
        }
    }


    void AccelerateKart()
    {
        if (CurrentSpeed < MaxSpeed)
        {
            CurrentSpeed += accelerationRate * Time.deltaTime; // Gradually increase speed
            agent.speed = CurrentSpeed; // Apply increased speed to NavMeshAgent
        }
        else
        {
            CurrentSpeed = MaxSpeed; // Clamp to maximum speed
        }
    }

    // Adjust the NavMesh movement to move towards a point near the waypoint
    void ApplyNavMeshMovement()
    {
        if (agent.remainingDistance < 8f && !agent.pathPending)
        {
            // Check if the current waypoint is the last one in the list
            if (currentWaypointIndex >= waypoints.Count - 1)
            {
                // If it's the last waypoint, loop back to the first one
                currentWaypointIndex = 0; // Go back to the first waypoint
            }
            else
            {
                // Otherwise, move to the next waypoint
                currentWaypointIndex++;
            }

            // Set the destination to the new waypoint
            SetRandomDestinationNearWaypoint();
        }
    }

    // Find waypoints dynamically, ensuring each agent has its own reference to the same waypoints.
    void FindWaypoints()
    {
        waypoints = new List<Transform>();
        GameObject waypointParent = GameObject.Find("Waypoints");

        foreach (Transform child in waypointParent.transform)
        {
            waypoints.Add(child);
        }
    }

    // Set a random destination near the current waypoint to add variety
    void SetRandomDestinationNearWaypoint()
    {
        // Get the current waypoint position
        Vector3 waypointPosition = waypoints[currentWaypointIndex].position;

        // Calculate a random offset within the waypoint radius
        Vector2 randomOffset = Random.insideUnitCircle * waypointRadius;
        Vector3 randomDestination = new Vector3(waypointPosition.x + randomOffset.x, waypointPosition.y, waypointPosition.z + randomOffset.y);

        // Set the agent's destination to the random point near the waypoint
        agent.SetDestination(randomDestination);
    }

    private void groundNormalRotation()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = -transform.up;
        float rayLength = 2.5f; // Ray length for ground detection

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayLength))
        {
            Vector3 forwardDirection = Vector3.ProjectOnPlane(transform.forward, hit.normal);
            Quaternion targetRotation = Quaternion.LookRotation(forwardDirection, hit.normal);

            float alignmentSpeed = 7.5f * Time.deltaTime;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, alignmentSpeed);
        }
        else
        {
            // Apply manual fall-off during gliding
            if (!GLIDER_FLY)
            {
                rb.AddForce(Vector3.down * 40f, ForceMode.Acceleration);  // Gravity force when not gliding
            }
        }
    }

    void tireSteer()
    {
        Vector3 steerDirection = agent.steeringTarget - transform.position;
        steerDirection.y = 0; // Keep the rotation flat on the y-axis
        Quaternion targetRotation = Quaternion.LookRotation(steerDirection);

        // Adjust the kart's body rotation smoothly, allowing for sharp turns
        float turnSpeed = agent.angularSpeed * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed);

        // Rotate tires as usual
        frontLeftTire.localRotation = Quaternion.Lerp(frontLeftTire.localRotation, Quaternion.Euler(0, targetRotation.eulerAngles.y - transform.eulerAngles.y, 0), 5f * Time.deltaTime);
        frontRightTire.localRotation = Quaternion.Lerp(frontRightTire.localRotation, Quaternion.Euler(0, targetRotation.eulerAngles.y - transform.eulerAngles.y, 0), 5f * Time.deltaTime);

        float tireSpinSpeed = CurrentSpeed * 0.5f;
        frontLeftTire.GetChild(0).Rotate(-90 * Time.deltaTime * tireSpinSpeed, 0, 0);
        frontRightTire.GetChild(0).Rotate(-90 * Time.deltaTime * tireSpinSpeed, 0, 0);
        backLeftTire.Rotate(90 * Time.deltaTime * tireSpinSpeed, 0, 0);
        backRightTire.Rotate(90 * Time.deltaTime * tireSpinSpeed, 0, 0);
    }

    void boosts()
    {
        // If the boost pad is active, temporarily skip normal boost handling
        if (isBoostPadActive)
        {
            return;
        }

        BoostTime -= Time.deltaTime;

        if (BoostTime > 0)
        {
            // Play the boost particles
            for (int i = 0; i < boostFire.childCount; i++)
            {
                if (!boostFire.GetChild(i).GetComponent<ParticleSystem>().isPlaying)
                {
                    boostFire.GetChild(i).GetComponent<ParticleSystem>().Play();
                }
            }
            agent.speed = 80f; 
        }
        else
        {
            // Stop the boost particles
            for (int i = 0; i < boostFire.childCount; i++)
            {
                boostFire.GetChild(i).GetComponent<ParticleSystem>().Stop();
            }

            // Keep the speed unchanged when boost ends
            agent.speed = CurrentSpeed;  // Sync NavMeshAgent speed to ensure it doesn't change unexpectedly
        }
    }




    public IEnumerator ApplyBoost(float newBoostSpeed, float duration)
    {

        // Temporarily force MaxSpeed to 80f during the boost
        float originalMaxSpeed = MaxSpeed;
        MaxSpeed = 80f;  // Force MaxSpeed to 80f
        CurrentSpeed = 80f;

        // Set the CurrentSpeed to 80f during the boost
        agent.speed = CurrentSpeed;
        isBoostPadActive = true;

        // Activate boost particles
        for (int i = 0; i < boostFire.childCount; i++)
        {
            ParticleSystem boostParticle = boostFire.GetChild(i).GetComponent<ParticleSystem>();
            if (!boostParticle.isPlaying)
            {
                boostParticle.Play();
            }
        }

        // Wait for the boost duration
        yield return new WaitForSeconds(duration);

        // Deactivate boost particles after the boost duration
        for (int i = 0; i < boostFire.childCount; i++)
        {
            ParticleSystem boostParticle = boostFire.GetChild(i).GetComponent<ParticleSystem>();
            boostParticle.Stop();
        }

        // Reset MaxSpeed back to the original value
        MaxSpeed = originalMaxSpeed;

        // Ensure CurrentSpeed doesn't exceed MaxSpeed once the boost is over
        if (CurrentSpeed > MaxSpeed)
        {
            CurrentSpeed = MaxSpeed;
        }

        // End the boost
        isBoostPadActive = false;
    }

    private int waypointsPassedWhileGliding = 0;

    void HandleGliderMovement()
    {
        if (waypoints.Count > 0)
        {
            // Calculate direction towards the next waypoint
            Vector3 direction = (waypoints[currentWaypointIndex].position - transform.position).normalized;

            // Adjust the NavMeshAgent's destination to follow the waypoint but with a higher offset
            Vector3 destination = waypoints[currentWaypointIndex].position + new Vector3(0, 3f, 0); // 3 units above the waypoint
            agent.SetDestination(destination);

            if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                     new Vector3(waypoints[currentWaypointIndex].position.x, 0, waypoints[currentWaypointIndex].position.z)) < 3f)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count; // Move to the next waypoint

                // Increment waypoint counter if gliding
                waypointsPassedWhileGliding++;

                // Disable glider and return to normal kart behavior after 2 waypoints
                if (waypointsPassedWhileGliding >= 2)
                {
                    DisableGliderAndResumeKart();
                }
            }
        }
    }

    void DisableGliderAndResumeKart()
    {
        // Disable the glider mode
        GLIDER_FLY = false;
        gliderAnim.SetBool("GliderOpen", false);
        gliderAnim.SetBool("GliderClose", true);

        // Re-enable the NavMeshAgent for normal ground movement
        EnableNavMeshAgent();

        // Reset the waypoint counter
        waypointsPassedWhileGliding = 0;

        // Set the agent's baseOffset back to 0 for normal ground movement
        agent.baseOffset = 0f;  // Reset baseOffset

        // Set the agent's destination to the next waypoint for normal movement
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Boost"))
        {
            StartCoroutine(ApplyBoost(boostSpeed, 1f)); // Apply boost when hitting the boost pad
        }

        if (other.CompareTag("GliderPanel"))
        {
            GLIDER_FLY = true;
            gliderAnim.SetBool("GliderOpen", true);
            gliderAnim.SetBool("GliderClose", false);

            // Set kart speed to 40 (max speed) when glider starts
            CurrentSpeed = 40f;
            agent.speed = CurrentSpeed;  // Sync NavMeshAgent speed with the set speed

            // Disable the NavMeshAgent for gliding
            agent.enabled = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("OffRoad"))
        {
            GLIDER_FLY = false;
            gliderAnim.SetBool("GliderOpen", false);
            gliderAnim.SetBool("GliderClose", true);

            EnableNavMeshAgent();  // Re-enable NavMeshAgent on ground contact
            agent.SetDestination(waypoints[currentWaypointIndex].position);  // Resume waypoint movement
        }
    }
}
