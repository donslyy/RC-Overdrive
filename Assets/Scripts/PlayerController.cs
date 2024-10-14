using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour
{
    private Rigidbody rb;


    public float CurrentSpeed = 0;
    public float MaxSpeed;
    public float boostSpeed;
    private float RealSpeed; //not the applied speed
    private float lapStartTime;  // Time when the lap started
    private bool canCompleteLap = true; // To control if the player can complete a lap

    [HideInInspector]
    public bool GLIDER_FLY;

    public Animator gliderAnim;

    [Header("Tires")]
    public Transform frontLeftTire;
    public Transform frontRightTire;
    public Transform backLeftTire;
    public Transform backRightTire;

    //drift and steering
    private float steerDirection;
    private float driftTime;

    bool driftLeft = false;
    bool driftRight = false;
    float outwardsDriftForce = 25000;

    public bool isSliding = false;

    private bool touchingGround;


    [Header("Particles Drift Sparks")]
    public Transform leftDrift;
    public Transform rightDrift;
    public Color drift1;
    public Color drift2;
    public Color drift3;

    [HideInInspector]
    public float BoostTime = 0;


    public Transform boostFire;
    public Transform boostExplosion;

    private bool isBoostPadActive = false;

    [Header("Deflector UI")]
    public Slider deflectorCooldownSlider;  // Slider UI for cooldown
    public Text deflectorLabel;  // Text above the slider to show "Deflector"
    private bool deflectorOnCooldown = false;
    public float deflectorCooldownTime = 5f;  // Cooldown duration in seconds
    private float deflectorCooldownProgress = 0f;  // Progress of the cooldo

    public GameObject deflector;  
    private bool deflectorActive = false;
    public float deflectorDuration = 2f;
    private int flickerFrames = 5;  // Number of frames between flickers

    [Header("Laps")]
    public Text lapCounterText;   // Reference to the UI Text object to display lap count
    public Text raceFinishedText; // Reference to the UI Text object for displaying "Race Finished!"
    public int currentLap = 1;    // Start on lap 1
    public int maxLaps = 3;       // Maximum number of laps
    private bool raceFinished = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        deflector.SetActive(false);  // Make sure the deflector is invisible initially
        lapStartTime = Time.time; // Initialize the lap start time

        // Hide the "Race Finished!" text at the start
        raceFinishedText.gameObject.SetActive(false);
        deflectorCooldownSlider.maxValue = deflectorCooldownTime;
        deflectorCooldownSlider.value = deflectorCooldownTime;  // Start with full cooldown ready
        deflectorLabel.text = "Deflector Charge";  // Set the label
        deflectorCooldownSlider.gameObject.SetActive(true);  // Ensure the slider is visible
    }

    // Update is called once per frame
    void Update()
    {
        move();
        tireSteer();
        steer();
        groundNormalRotation();
        drift();
        boosts();
        handleDeflector();

        if (deflectorOnCooldown)
        {
            deflectorCooldownProgress -= Time.deltaTime;
            deflectorCooldownSlider.value = deflectorCooldownProgress;

            // If cooldown has completed, reset the flag
            if (deflectorCooldownProgress <= 0f)
            {
                deflectorOnCooldown = false;
                deflectorCooldownSlider.value = deflectorCooldownTime;  // Reset the slider for next use
            }
        }
    }

    void move()
    {
        RealSpeed = transform.InverseTransformDirection(rb.velocity).z; //real velocity before setting the value. This can be useful if say you want to have hair moving on the player, but don't want it to move if you are accelerating into a wall, since checking velocity after it has been applied will always be the applied value, and not real

        if (Input.GetKey(KeyCode.Space))
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxSpeed, Time.deltaTime * 0.5f); //speed
        }
        else if (Input.GetKey(KeyCode.S))
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, -MaxSpeed / 1.75f, 1f * Time.deltaTime);
        }
        else
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.deltaTime * 1.5f); //speed
        }

        if (!GLIDER_FLY)
        {
            Vector3 vel = transform.forward * CurrentSpeed;
            vel.y = rb.velocity.y; //gravity
            rb.velocity = vel;
        }
        else
        {
            Vector3 vel = transform.forward * (CurrentSpeed * 1.25f); // Multiply speed to increase forward momentum
            vel.y = rb.velocity.y * 0.6f; // Keep a slightly reduced gravity effect
            rb.velocity = vel;
        }


    }
    private void steer()
    {
        steerDirection = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
        Vector3 steerDirVect; //this is used for the final rotation of the kart for steering

        float steerAmount;


        if (driftLeft && !driftRight)
        {
            steerDirection = Input.GetAxis("Horizontal") < 0 ? -1.5f : -0.5f;
            transform.GetChild(0).localRotation = Quaternion.Lerp(transform.GetChild(0).localRotation, Quaternion.Euler(0, -20f, 0), 8f * Time.deltaTime);


            if (isSliding && touchingGround)
                rb.AddForce(transform.right * outwardsDriftForce * Time.deltaTime, ForceMode.Acceleration);
        }
        else if (driftRight && !driftLeft)
        {
            steerDirection = Input.GetAxis("Horizontal") > 0 ? 1.5f : 0.5f;
            transform.GetChild(0).localRotation = Quaternion.Lerp(transform.GetChild(0).localRotation, Quaternion.Euler(0, 20f, 0), 8f * Time.deltaTime);

            if (isSliding && touchingGround)
                rb.AddForce(transform.right * -outwardsDriftForce * Time.deltaTime, ForceMode.Acceleration);
        }
        else
        {
            transform.GetChild(0).localRotation = Quaternion.Lerp(transform.GetChild(0).localRotation, Quaternion.Euler(0, 0f, 0), 8f * Time.deltaTime);
        }

        //since handling is supposed to be stronger when car is moving slower, we adjust steerAmount depending on the real speed of the kart, and then rotate the kart on its y axis with steerAmount
        steerAmount = RealSpeed > 30 ? RealSpeed / 4 * steerDirection : steerAmount = RealSpeed / 1.5f * steerDirection;



        //glider movements

        if (Input.GetKey(KeyCode.LeftArrow) && GLIDER_FLY)  //left
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 50), 2 * Time.deltaTime);
        } // left 
        else if (Input.GetKey(KeyCode.RightArrow) && GLIDER_FLY) //right
        {

            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, -50), 2 * Time.deltaTime);
        } //right
        else //nothing
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0), 2 * Time.deltaTime);
        } //nothing

        if (Input.GetKey(KeyCode.UpArrow) && GLIDER_FLY)
        {

            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(25, transform.eulerAngles.y, transform.eulerAngles.z), 2 * Time.deltaTime);

            rb.AddForce(Vector3.down * 8000 * Time.deltaTime, ForceMode.Acceleration);
        } //moving down
        else if (Input.GetKey(KeyCode.DownArrow) && GLIDER_FLY)
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(-50, transform.eulerAngles.y, transform.eulerAngles.z), 2 * Time.deltaTime);
            rb.AddForce(Vector3.up * 12000 * Time.deltaTime, ForceMode.Acceleration);

        } //rotating up
        else
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z), 2 * Time.deltaTime);
        }








        steerDirVect = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + steerAmount, transform.eulerAngles.z);
        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, steerDirVect, 6 * Time.deltaTime);

    }
    private void groundNormalRotation()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = -transform.up;
        float rayLength = 2.5f; // Ray length for ground detection

        // Draw the ray in the Scene view for debugging purposes
        Debug.DrawRay(rayOrigin, rayDirection * rayLength, Color.red);

        // Perform the raycast to detect ground
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayLength))
        {
            // Calculate target rotation based on the ground normal
            Vector3 forwardDirection = Vector3.ProjectOnPlane(transform.forward, hit.normal);
            Quaternion targetRotation = Quaternion.LookRotation(forwardDirection, hit.normal);

            // Rotate the kart towards the target rotation smoothly
            float alignmentSpeed = 7.5f * Time.deltaTime;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, alignmentSpeed);
            touchingGround = true;

        }
        else
        {
            touchingGround = false;

            // Rotate the kart to a natural downward tilt when airborne
            float fallTiltSpeed = 8f * Time.deltaTime;
            Vector3 forwardTilt = Vector3.Lerp(transform.forward, Vector3.down, fallTiltSpeed);
            Quaternion targetRotation = Quaternion.LookRotation(forwardTilt, transform.up);

            // Apply downward force to help stabilize rotation
            rb.AddForce(Vector3.down * 40f, ForceMode.Acceleration);

            // Smoothly apply the target rotation to the kart
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, fallTiltSpeed);

        }
    }



    void drift()
    {
        if (Input.GetKeyDown(KeyCode.V) && touchingGround)
        {
            transform.GetChild(0).GetComponent<Animator>().SetTrigger("Hop");
            if (steerDirection > 0)
            {
                driftRight = true;
                driftLeft = false;
            }
            else if (steerDirection < 0)
            {
                driftRight = false;
                driftLeft = true;
            }
        }


        if (Input.GetKey(KeyCode.V) && touchingGround && CurrentSpeed > 25 && Input.GetAxis("Horizontal") != 0)
        {
            driftTime += Time.deltaTime;

            //particle effects (sparks)
            if (driftTime >= 1 && driftTime < 3)
            {

                for (int i = 0; i < leftDrift.childCount; i++)
                {
                    ParticleSystem DriftPS = rightDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //right wheel particles
                    ParticleSystem.MainModule PSMAIN = DriftPS.main;

                    ParticleSystem DriftPS2 = leftDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //left wheel particles
                    ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;

                    PSMAIN.startColor = drift1;
                    PSMAIN2.startColor = drift1;

                    if (!DriftPS.isPlaying)
                    {
                        DriftPS.Play();
                    }

                    if (!DriftPS2.isPlaying)
                    {
                        DriftPS2.Play();
                    }

                }
            }
            if (driftTime >= 3 && driftTime < 6)
            {
                //drift color particles
                for (int i = 0; i < leftDrift.childCount; i++)
                {
                    ParticleSystem DriftPS = rightDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule PSMAIN = DriftPS.main;
                    ParticleSystem DriftPS2 = leftDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;
                    PSMAIN.startColor = drift2;
                    PSMAIN2.startColor = drift2;


                }

            }
            if (driftTime >= 6)
            {
                for (int i = 0; i < leftDrift.childCount; i++)
                {

                    ParticleSystem DriftPS = rightDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule PSMAIN = DriftPS.main;
                    ParticleSystem DriftPS2 = leftDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;
                    PSMAIN.startColor = drift3;
                    PSMAIN2.startColor = drift3;

                }
            }
        }

        if (!Input.GetKey(KeyCode.V) || RealSpeed < 25)
        {
            driftLeft = false;
            driftRight = false;
            isSliding = false; /////////



            //give a boost
            if (driftTime > 1 && driftTime < 3)
            {
                BoostTime = 0.75f;
            }
            if (driftTime >= 3 && driftTime < 6)
            {
                BoostTime = 1.5f;

            }
            if (driftTime >= 6)
            {
                BoostTime = 2.5f;

            }

            //reset everything
            driftTime = 0;
            //stop particles
            for (int i = 0; i < 5; i++)
            {
                ParticleSystem DriftPS = rightDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //right wheel particles
                ParticleSystem.MainModule PSMAIN = DriftPS.main;

                ParticleSystem DriftPS2 = leftDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //left wheel particles
                ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;

                DriftPS.Stop();
                DriftPS2.Stop();

            }
        }



    }
    void boosts()
    {
        if (isBoostPadActive)
        {
            // Skip the usual boost handling while the boost pad effect is active
            return;
        }

        BoostTime -= Time.deltaTime;
        if (BoostTime > 0)
        {
            for (int i = 0; i < boostFire.childCount; i++)
            {
                if (!boostFire.GetChild(i).GetComponent<ParticleSystem>().isPlaying)
                {
                    boostFire.GetChild(i).GetComponent<ParticleSystem>().Play();
                }
            }
            MaxSpeed = boostSpeed + 20;
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxSpeed, 1 * Time.deltaTime);
        }
        else
        {
            for (int i = 0; i < boostFire.childCount; i++)
            {
                boostFire.GetChild(i).GetComponent<ParticleSystem>().Stop();
            }
            MaxSpeed = boostSpeed - 40;
            if (CurrentSpeed > MaxSpeed)
            {
                CurrentSpeed = MaxSpeed;
            }
        }
    }


    void tireSteer()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            frontLeftTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 155, 0), 5 * Time.deltaTime);
            frontRightTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 155, 0), 5 * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            frontLeftTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 205, 0), 5 * Time.deltaTime);
            frontRightTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 205, 0), 5 * Time.deltaTime);
        }
        else
        {
            frontLeftTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 180, 0), 5 * Time.deltaTime);
            frontRightTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 180, 0), 5 * Time.deltaTime);
        }

        //tire spinning

        if (CurrentSpeed > 30)
        {
            frontLeftTire.GetChild(0).Rotate(-90 * Time.deltaTime * CurrentSpeed * 0.5f, 0, 0);
            frontRightTire.GetChild(0).Rotate(-90 * Time.deltaTime * CurrentSpeed * 0.5f, 0, 0);
            backLeftTire.Rotate(90 * Time.deltaTime * CurrentSpeed * 0.5f, 0, 0);
            backRightTire.Rotate(90 * Time.deltaTime * CurrentSpeed * 0.5f, 0, 0);
        }
        else
        {
            frontLeftTire.GetChild(0).Rotate(-90 * Time.deltaTime * RealSpeed * 0.5f, 0, 0);
            frontRightTire.GetChild(0).Rotate(-90 * Time.deltaTime * RealSpeed * 0.5f, 0, 0);
            backLeftTire.Rotate(90 * Time.deltaTime * RealSpeed * 0.5f, 0, 0);
            backRightTire.Rotate(90 * Time.deltaTime * RealSpeed * 0.5f, 0, 0);
        }
    }

    void handleDeflector()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !deflectorActive)  // Activate on Shift press
        {
            StartCoroutine(ActivateDeflector());
        }
    }

    void UpdateLapCounter()
    {
        lapCounterText.text = "Lap: " + currentLap + " / " + maxLaps;
    }

    void EnableLapCompletion()
    {
        canCompleteLap = true;
    }

    private IEnumerator ActivateDeflector()
    {
        if (!deflectorOnCooldown)
        {
            deflectorActive = true;
            deflector.SetActive(true);  // Show the deflector immediately

            // Start the cooldown
            deflectorOnCooldown = true;
            deflectorCooldownProgress = deflectorCooldownTime;

            // Wait for the first second of deflector being fully active
            yield return new WaitForSeconds(deflectorDuration - 1f);

            // Flicker the deflector during the last second
            float flickerTime = 1f;
            while (flickerTime > 0)
            {
                deflector.SetActive(!deflector.activeSelf);  // Toggle visibility
                yield return new WaitForSeconds(flickerFrames / 60f);  // Flicker based on frames
                flickerTime -= flickerFrames / 60f;  // Decrease the flicker time
            }

            // Ensure deflector is turned off at the end
            deflector.SetActive(false);
            deflectorActive = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the kart collided with a boost pad
        if (other.CompareTag("Boost"))
        {
            // Start the boost for this specific kart only
            StartCoroutine(ApplyBoost(boostSpeed, 2f)); // 2 seconds boost duration
        }
        if (other.gameObject.CompareTag("FinishLine"))
        {
            if (canCompleteLap)
            {
                if (currentLap < maxLaps)
                {
                    currentLap++; // Increment the lap number
                    UpdateLapCounter(); // Update the lap counter on the UI
                    lapStartTime = Time.time; // Reset lap start time
                }
                else if (currentLap == maxLaps)
                {
                    raceFinished = true; // Stop the player from moving after the race finishes
                    raceFinishedText.gameObject.SetActive(true); // Show "Race Finished!" text
                }

                // Prevent immediate lap completion again
                canCompleteLap = false;
                Invoke(nameof(EnableLapCompletion), 1f); // Reset lap completion ability after 1 second
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "GliderPanel")
        {
            GLIDER_FLY = true;
            gliderAnim.SetBool("GliderOpen", true);
            gliderAnim.SetBool("GliderClose", false);

        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ground" || collision.gameObject.tag == "OffRoad")
        {
            GLIDER_FLY = false;
            gliderAnim.SetBool("GliderOpen", false);
            gliderAnim.SetBool("GliderClose", true);
        }
    }

    public IEnumerator ApplyBoost(float newBoostSpeed, float duration)
    {
        Debug.Log("Boost coroutine started");
        float originalSpeed = MaxSpeed;
        MaxSpeed = newBoostSpeed;
        CurrentSpeed = newBoostSpeed;
        isBoostPadActive = true; // Set the flag

        for (int i = 0; i < boostFire.childCount; i++)
        {
            ParticleSystem boostParticle = boostFire.GetChild(i).GetComponent<ParticleSystem>();
            if (!boostParticle.isPlaying)
            {
                boostParticle.Play();
            }
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < boostFire.childCount; i++)
        {
            ParticleSystem boostParticle = boostFire.GetChild(i).GetComponent<ParticleSystem>();
            boostParticle.Stop();
        }

        MaxSpeed = originalSpeed;
        if (CurrentSpeed > MaxSpeed)
        {
            CurrentSpeed = MaxSpeed;
        }

        isBoostPadActive = false; // Clear the flag
    }



}