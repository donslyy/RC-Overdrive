using UnityEngine;

public class BoostPad : MonoBehaviour
{
    public float boostSpeed = 80f; // The speed to set when the player hits the boost pad
    public float boostDuration = 20f; // Duration of the boost in seconds

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag("Player"))
        {
            // Get the PlayerScript component from the player
            PlayerScript playerScript = other.GetComponent<PlayerScript>();

            if (playerScript != null)
            {
                // Apply the boost to the player and start the particle effect
                playerScript.StartCoroutine(playerScript.ApplyBoost(boostSpeed, boostDuration));
            }
        }
    }
}
