using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class HammerCurlSettings
{
    // Existing settings remain the same
    [Header("Angle Thresholds")]
    [Range(0, 45)]
    public float minAngle = 10f;

    [Range(90, 160)]
    public float maxAngle = 130f;

    [Range(5, 45)]
    public float angleThreshold = 20f;

    [Header("Rep Settings")]
    [Range(1, 20)]
    public int minRepsPerSet = 8;

    [Range(1, 30)]
    public int maxRepsPerSet = 12;

    [Range(0.1f, 3f)]
    public float repHoldTime = 1f;

    [Header("Form Detection")]
    [Range(10f, 200f)]
    public float maxCurlSpeed = 100f;

    [Range(5f, 50f)]
    public float minCurlSpeed = 20f;
}

public class HammerCurlTracker : MonoBehaviour
{
    [SerializeField]
    private HammerCurlSettings settings = new HammerCurlSettings();

    // Existing state variables
    private int currentReps;
    public int currentSet = 1;
    private bool isMovingUp;
    private float lastCheckedAngle;
    private float holdTimer;
    private float currentElbowAngle;
    private float lastAngleCheckTime;
    private bool isHolding;
    private float repStartTime;

    // New variables for form feedback averaging
    private List<string> currentRepIssues = new List<string>();
    private List<float> repFormScores = new List<float>();
    private string previousRepFeedback = "Start your first rep";
    private float previousRepFormScore = 100f;
    private int formChecksThisRep = 0;

    // Properties
    public float CurrentFormScore => currentFormScore;
    public int CurrentReps => currentReps;
    public int CurrentSet => currentSet;
    public float CurrentAngle => currentElbowAngle;
    public string FormFeedback { get; private set; }
    public int MaxRepsPerSet => settings.maxRepsPerSet;

    private List<string> currentFormIssues = new List<string>();
    private float setStartTime;
    private float currentFormScore = 100f;

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
        currentRepIssues.Clear();
        repFormScores.Clear();
        currentFormScore = 100f;
        formChecksThisRep = 0;
        FormFeedback = "Ready to start";
        previousRepFeedback = "Start your first rep";
        previousRepFormScore = 100f;
    }

    public void UpdateTracking(Vector3 shoulder, Vector3 elbow, Vector3 wrist)
    {
        float previousAngle = currentElbowAngle;
        currentElbowAngle = CalculateElbowAngle(shoulder, elbow, wrist);

        float deltaTime = Time.time - lastAngleCheckTime;
        if (deltaTime > 0)
        {
            float angleSpeed = Mathf.Abs(currentElbowAngle - previousAngle) / deltaTime;
            CheckForm(angleSpeed);
        }

        lastAngleCheckTime = Time.time;
        CheckRepCompletion();

        // Show previous rep's feedback during current rep
        FormFeedback = previousRepFeedback;
    }

    private float CalculateElbowAngle(Vector3 shoulder, Vector3 elbow, Vector3 wrist)
    {
        Vector3 upperArmVector = shoulder - elbow;
        Vector3 forearmVector = wrist - elbow;
        return Vector3.Angle(upperArmVector, forearmVector);
    }

    private void CheckForm(float angleSpeed)
    {
        List<string> currentIssues = new List<string>();
        float formDeduction = 0f;

        float angleDiff = Mathf.Abs(currentElbowAngle - lastCheckedAngle);
        if (angleDiff > settings.angleThreshold)
        {
            isMovingUp = currentElbowAngle > lastCheckedAngle;
            lastCheckedAngle = currentElbowAngle;
        }

        // Accumulate form issues for this check
        if (currentElbowAngle < settings.minAngle)
        {
            currentIssues.Add("Incomplete range of motion at bottom");
            formDeduction += 15f;
        }
        else if (currentElbowAngle > settings.maxAngle)
        {
            currentIssues.Add("Excessive swing at top");
            formDeduction += 20f;
        }
        else if (angleSpeed > settings.maxCurlSpeed)
        {
            currentIssues.Add("Movement too fast");
            formDeduction += 10f;
        }
        else if (angleSpeed < settings.minCurlSpeed && angleSpeed > 0.1f)
        {
            currentIssues.Add("Movement too slow/hesitant");
            formDeduction += 5f;
        }

        // Add issues to the current rep's collection
        currentRepIssues.AddRange(currentIssues);
        repFormScores.Add(100f - formDeduction);
        formChecksThisRep++;
    }

    private void CompleteRep()
    {
        if (isHolding && holdTimer >= settings.repHoldTime)
        {
            // Calculate average form score for the completed rep
            float averageFormScore = repFormScores.Count > 0 ? repFormScores.Average() : 100f;

            // Get most common form issues
            var commonIssues = currentRepIssues
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .Take(2)
                .Select(g => g.Key)
                .ToList();

            // Create feedback message for next rep
            if (commonIssues.Any())
            {
                previousRepFeedback = $"Last rep ({averageFormScore:F0}%): {string.Join(", ", commonIssues)}";
            }
            else
            {
                previousRepFeedback = $"Good form! ({averageFormScore:F0}%)";
            }

            previousRepFormScore = averageFormScore;

            // Reset rep tracking
            currentReps++;
            isHolding = false;
            holdTimer = 0f;
            repStartTime = Time.time;
            currentRepIssues.Clear();
            repFormScores.Clear();
            formChecksThisRep = 0;
        }
    }

    private void CheckRepCompletion()
    {
        if (currentElbowAngle >= settings.maxAngle * 0.9f)
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

        if (currentReps >= settings.maxRepsPerSet)
        {
            CompleteSet();
        }
    }

    private void CompleteSet()
    {
        if (currentReps >= settings.maxRepsPerSet)
        {
            float setDuration = Time.time - setStartTime;
            currentSet++;
            currentReps = 0;
            setStartTime = Time.time;
            currentFormIssues.Clear();
            currentRepIssues.Clear();
            repFormScores.Clear();
            currentFormScore = 100f;
            FormFeedback = "Set complete! Take a rest";
            previousRepFeedback = "Ready for next set";
            formChecksThisRep = 0;
        }
    }

    public List<string> GetCurrentFormIssues()
    {
        return new List<string>(currentRepIssues);
    }

    // Test methods
    public void ForceCompleteRep()
    {
        CompleteRep();
    }

    public void ForceCompleteSet()
    {
        CompleteSet();
    }
}