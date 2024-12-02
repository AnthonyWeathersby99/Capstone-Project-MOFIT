using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class HammerCurlSettings
{
    [Header("Angle Thresholds")]
    [Tooltip("Minimum angle when arm is fully extended")]
    [Range(0, 45)]
    public float minAngle = 10f;

    [Tooltip("Maximum angle at top of curl")]
    [Range(90, 160)]
    public float maxAngle = 130f;

    [Tooltip("Minimum angle change to count as movement")]
    [Range(5, 45)]
    public float angleThreshold = 20f;

    [Header("Rep Settings")]
    [Tooltip("Minimum reps required for a set")]
    [Range(1, 20)]
    public int minRepsPerSet = 8;

    [Tooltip("Maximum reps for a set")]
    [Range(1, 30)]
    public int maxRepsPerSet = 12;

    [Tooltip("Time in seconds to hold at top of movement")]
    [Range(0.1f, 3f)]
    public float repHoldTime = 1f;

    [Header("Form Detection")]
    [Tooltip("Maximum speed for curl (degrees per second)")]
    [Range(10f, 200f)]
    public float maxCurlSpeed = 100f;

    [Tooltip("Minimum speed for curl (degrees per second)")]
    [Range(5f, 50f)]
    public float minCurlSpeed = 20f;
}

public class HammerCurlTracker : MonoBehaviour
{
    [SerializeField]
    private HammerCurlSettings settings = new HammerCurlSettings();

    private int currentReps;
    public int currentSet = 1;
    private bool isMovingUp;
    private float lastCheckedAngle;
    private float holdTimer;
    private float currentElbowAngle;
    private float lastAngleCheckTime;
    private bool isHolding;
    private float repStartTime;
    public float CurrentFormScore => currentFormScore;

    [Header("Debug Visualization")]
    public bool showDebugGizmos = true;
    public Color gizmoColor = Color.green;

    // Current state properties
    public int CurrentReps => currentReps;
    public int CurrentSet => currentSet;
    public float CurrentAngle => currentElbowAngle;
    public string FormFeedback { get; private set; }
    public int MaxRepsPerSet => settings.maxRepsPerSet;

    private List<string> currentFormIssues = new List<string>();
    private float setStartTime;
    private float currentFormScore = 100f; // Start perfect, deduct for issues



    private void Start()
    {
        ResetTracker();
    }


    public void ResetTracker(bool resetSetNumber = false)
    {
        currentReps = 0;
        if (resetSetNumber)
            currentSet = 1;
        isMovingUp = false;
        isHolding = false;
        holdTimer = 0f;
        lastCheckedAngle = 0f;
        repStartTime = Time.time;
        setStartTime = Time.time;
        currentFormIssues.Clear();
        currentFormScore = 100f;
        FormFeedback = "Ready to start";
    }

    public List<string> GetCurrentFormIssues()
    {
        return new List<string>(currentFormIssues);
    }

    public void UpdateTracking(Vector3 shoulder, Vector3 elbow, Vector3 wrist)
    {
        float previousAngle = currentElbowAngle;
        currentElbowAngle = CalculateElbowAngle(shoulder, elbow, wrist);

        // Calculate movement speed
        float deltaTime = Time.time - lastAngleCheckTime;
        if (deltaTime > 0)  // Prevent division by zero
        {
            float angleSpeed = Mathf.Abs(currentElbowAngle - previousAngle) / deltaTime;
            CheckForm(angleSpeed);
        }

        lastAngleCheckTime = Time.time;
        CheckRepCompletion();

        // Debug logging
        //Debug.Log($"Current Angle: {currentElbowAngle:F1}‹ | Reps: {currentReps} | Set: {currentSet}");
    }

    private float CalculateElbowAngle(Vector3 shoulder, Vector3 elbow, Vector3 wrist)
    {
        Vector3 upperArmVector = shoulder - elbow;
        Vector3 forearmVector = wrist - elbow;

        // Calculate angle between vectors
        float angle = Vector3.Angle(upperArmVector, forearmVector);

        // Debug visualization
        if (showDebugGizmos)
        {
            Debug.DrawLine(shoulder, elbow, Color.red, Time.deltaTime);
            Debug.DrawLine(elbow, wrist, Color.blue, Time.deltaTime);
        }

        return angle;
    }

    private void CheckForm(float angleSpeed)
    {
        currentFormIssues.Clear();
        float formDeduction = 0f;

        // Update movement direction when significant movement is detected
        float angleDiff = Mathf.Abs(currentElbowAngle - lastCheckedAngle);
        if (angleDiff > settings.angleThreshold)
        {
            isMovingUp = currentElbowAngle > lastCheckedAngle;
            lastCheckedAngle = currentElbowAngle;
        }

        // Form checks with priority and scoring
        if (currentElbowAngle < settings.minAngle)
        {
            currentFormIssues.Add("Incomplete range of motion at bottom");
            formDeduction += 15f;
            FormFeedback = "Extend arms fully at bottom";
        }
        else if (currentElbowAngle > settings.maxAngle)
        {
            currentFormIssues.Add("Excessive swing at top");
            formDeduction += 20f;
            FormFeedback = "Don't swing! Control the movement";
        }
        else if (angleSpeed > settings.maxCurlSpeed)
        {
            currentFormIssues.Add("Movement too fast");
            formDeduction += 10f;
            FormFeedback = "Too fast! Slow down the movement";
        }
        else if (angleSpeed < settings.minCurlSpeed && angleSpeed > 0.1f)
        {
            currentFormIssues.Add("Movement too slow/hesitant");
            formDeduction += 5f;
            FormFeedback = "Move with more control and purpose";
        }
        else if (isHolding)
        {
            FormFeedback = "Good! Hold at the top";
        }
        else
        {
            FormFeedback = isMovingUp ? "Good form - curling up" : "Good form - lowering down";
        }

        // Update form score
        currentFormScore = Mathf.Max(0f, 100f - formDeduction);
    }

    private void CheckRepCompletion()
    {
        // Check for top position hold
        if (currentElbowAngle >= settings.maxAngle * 0.9f)  // Allow slight variation at top
        {
            if (!isHolding)
            {
                isHolding = true;
                holdTimer = 0f;
            }

            holdTimer += Time.deltaTime;

            if (holdTimer >= settings.repHoldTime)
            {
                CompleteRep();
            }
        }
        else
        {
            isHolding = false;
            holdTimer = 0f;
        }

        // Check for set completion
        if (currentReps >= settings.maxRepsPerSet)
        {
            CompleteSet();

        }
    }

    private void CompleteRep()
    {
        if (isHolding && holdTimer >= settings.repHoldTime)
        {
            float repTime = Time.time - repStartTime;
            currentReps++;
            isHolding = false;
            holdTimer = 0f;
            repStartTime = Time.time;

            FormFeedback = $"Good rep! {currentReps}/{settings.maxRepsPerSet}";

            // Log rep statistics
            Debug.Log($"Rep {currentReps} completed in {repTime:F2} seconds");
        }
    }

    private void CompleteSet()
    {
        if (currentReps >= settings.maxRepsPerSet)
        {
            float setDuration = Time.time - setStartTime;

            // Save the completed set data
            WorkoutResultsManager.AddSet(
                currentReps,
                currentFormScore,
                setDuration,
                new List<string>(currentFormIssues)
            );

            // Reset for next set
            currentSet++;
            currentReps = 0;
            setStartTime = Time.time;
            currentFormIssues.Clear();
            currentFormScore = 100f;
            FormFeedback = "Set complete! Take a rest";
            Debug.Log($"Set {currentSet - 1} completed!");
            Debug.Log($"Saving set data - Reps: {currentReps}, Form Score: {currentFormScore}, Duration: {setDuration}");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw current angle indicator
        Gizmos.color = gizmoColor;
        Vector3 position = transform.position;
        float radius = 0.5f;

        // Draw arc representing the current angle
        int segments = 32;
        float angleStep = currentElbowAngle / segments;
        Vector3 previousPoint = position + new Vector3(Mathf.Cos(0) * radius, Mathf.Sin(0) * radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = position + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }
    }

    // Optional: Method to force a rep completion (for testing)
    public void ForceCompleteRep()
    {
        CompleteRep();
    }

    // Optional: Method to force a set completion (for testing)
    public void ForceCompleteSet()
    {
        CompleteSet();
    }
}