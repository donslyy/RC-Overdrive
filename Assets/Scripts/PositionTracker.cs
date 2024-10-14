using System.Collections.Generic;
using UnityEngine;

public class PositionTracker : MonoBehaviour
{
    public List<TrackProgress> racers; // List of all racers
    public UnityEngine.UI.Text positionText; // UI to display player position
    public TrackProgress playerProgress; // Player's TrackProgress component

    void Start()
    {
        // Automatically find all objects with TrackProgress components and add them to the list
        racers = new List<TrackProgress>(FindObjectsOfType<TrackProgress>());
        playerProgress = GameObject.FindWithTag("Player").GetComponent<TrackProgress>();
    }

    void Update()
    {
        // Sort racers based on DistanceToFinish
        racers.Sort((r1, r2) => r1.DistanceToFinish.CompareTo(r2.DistanceToFinish));

        // Find the player's position in the race
        int playerPosition = racers.IndexOf(playerProgress) + 1;

        // Update the UI text with the player's position
        positionText.text = "Position: " + playerPosition + " / " + racers.Count;
    }
}
