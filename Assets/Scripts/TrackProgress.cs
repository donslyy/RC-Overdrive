using UnityEngine;

public class TrackProgress : MonoBehaviour
{
    public Transform FinishLine;   // Assign the finish line transform in the inspector
    public float DistanceToFinish; // To track distance

    // Update is called once per frame
    void Update()
    {
        // Calculate distance to the finish line
        DistanceToFinish = Vector3.Distance(transform.position, FinishLine.position);
    }
}
