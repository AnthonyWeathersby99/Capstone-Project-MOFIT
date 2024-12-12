using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class HammerCurlSettings
{
    [Header("Starting Position Range")]
    [Range(120, 180)]
    public float minStartAngle = 140f;  // Minimum angle to count as starting position
    [Range(120, 180)]
    public float maxStartAngle = 180f;  // Maximum angle to count as starting position

    [Header("Peak Curl Position Range")]
    [Range(60, 120)]
    public float minPeakAngle = 80f;    // Minimum angle at peak of curl
    [Range(60, 120)]
    public float maxPeakAngle = 110f;   // Maximum angle at peak of curl

    [Header("Rep Requirements")]
    [Range(30, 90)]
    public float minRangeOfMotion = 45f;
    [Range(0.5f, 3f)]
    public float minRepDuration = 1f;
    [Range(0.1f, 3f)]
    public float repHoldTime = 0.5f;

    [Header("Rep Settings")]
    [Range(1, 20)]
    public int minRepsPerSet = 8;
    [Range(1, 30)]
    public int maxRepsPerSet = 12;

    [Header("Form Detection")]
    [Range(10f, 200f)]
    public float maxCurlSpeed = 100f;
    [Range(5f, 50f)]
    public float minCurlSpeed = 20f;
    [Range(5, 45)]
    public float angleThreshold = 20f;
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
    private float repStartAngle;
    private float peakAngle;
    private bool isValidRepInProgress;

    // Properties
    public float CurrentFormScore => currentFormScore;
    public int CurrentReps => currentReps;
    public int CurrentSet => currentSet;
    public float CurrentAngle => currentElbowAngle;
    public string FormFeedback { get; private set; }
    public int MaxRepsPerSet => settings.maxRepsPerSet;

    private List<string> currentFormIssues = new List<string>();
    private List<string> currentRepIssues = new List<string>();
    private List<float> repFormScores = new List<float>();
    private float setStartTime;
    private float currentFormScore = 100f;
    private string previousRepFeedback = "Start your first rep";
    private float previousRepFormScore = 100f;
    private int formChecksThisRep = 0;

    private const float PERFECT_CURL_ANGLE = 90f;
    private const float GOOD_RANGE_THRESHOLD = 5f;
    private const float PERFECT_RANGE_THRESHOLD = 1f;

    private List<float> currentSetRepScores = new List<float>();
    private float lastRepScore = 0f;


    private void Start()
    {
        ResetTracker();
    }

    public void ResetTracker(bool resetSetNumber = false)
    {
        currentSetRepScores.Clear();
        lastRepScore = 0f;
        currentReps = 0;
        if (resetSetNumber)
            currentSet = 1;
        isMovingUp = false;
        isHolding = false;
        holdTimer = 0f;
        lastCheckedAngle = 0f;
        repStartTime = Time.time;
        repStartAngle = 0f;
        peakAngle = 0f;
        isValidRepInProgress = false;
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

            // Track rep progress - Changed to use minStartAngle
            if (!isValidRepInProgress && currentElbowAngle >= settings.minStartAngle)
            {
                // Start tracking a new potential rep
                isValidRepInProgress = true;
                repStartTime = Time.time;
                repStartAngle = currentElbowAngle;
                peakAngle = currentElbowAngle;
                Debug.Log($"Started tracking new rep at angle: {currentElbowAngle}");
            }
            else if (isValidRepInProgress)
            {
                // Track the highest point reached during the curl
                if (currentElbowAngle < peakAngle)
                {
                    peakAngle = currentElbowAngle;
                    Debug.Log($"New peak curl angle: {peakAngle}");
                }
            }
        }

        lastAngleCheckTime = Time.time;
        CheckRepCompletion();

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
        if (currentElbowAngle < settings.minPeakAngle)
        {
            currentIssues.Add("Incomplete range of motion at bottom");
            formDeduction += 15f;
        }
        else if (currentElbowAngle > settings.maxStartAngle)
        {
            currentIssues.Add("Excessive swing at top");
            formDeduction += 20f;
        }
        else if (angleSpeed > settings.maxCurlSpeed)
        {
            currentIssues.Add("Movement too fast");
            formDeduction += 10f;
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
            float formScore = lastRepScore; // Use the last calculated score
            currentReps++;

            // Reset rep tracking but keep the scores list
            isHolding = false;
            holdTimer = 0f;
            repStartTime = Time.time;
            currentRepIssues.Clear();
            repFormScores.Clear();
            formChecksThisRep = 0;
            previousRepFormScore = formScore;
        }
    }

    private void CheckRepCompletion()
    {
        // Check if we're in starting position range
        bool isInStartPosition = currentElbowAngle >= settings.minStartAngle &&
                               currentElbowAngle <= settings.maxStartAngle;

        // Check if we're in peak curl position range
        bool isInPeakPosition = currentElbowAngle >= settings.minPeakAngle &&
                              currentElbowAngle <= settings.maxPeakAngle;

        // Start tracking when in starting position
        if (!isValidRepInProgress && isInStartPosition)
        {
            isValidRepInProgress = true;
            repStartTime = Time.time;
            repStartAngle = currentElbowAngle;
            peakAngle = currentElbowAngle;
            isHolding = false;
            Debug.Log($"Started tracking new rep at angle: {currentElbowAngle}");
        }
        else if (isValidRepInProgress)
        {
            // Track the curl peak (smallest angle)
            if (currentElbowAngle < peakAngle)
            {
                peakAngle = currentElbowAngle;
                Debug.Log($"New peak curl angle: {peakAngle}");
            }

            // Check if we're in the peak position range
            if (isInPeakPosition)
            {
                if (!isHolding)
                {
                    isHolding = true;
                    holdTimer = 0f;
                    Debug.Log("Started hold timer at peak");
                }

                holdTimer += Time.deltaTime;
            }
            else
            {
                // If we've held at the peak and now returning to start position
                if (isHolding && holdTimer >= settings.repHoldTime)
                {
                    // Check if we've returned to starting position range
                    if (isInStartPosition)
                    {
                        Debug.Log("Attempting to validate rep on return to start");
                        ValidateAndCompleteRep();
                    }
                }
            }
        }

        if (currentReps >= settings.maxRepsPerSet)
        {
            CompleteSet();
        }
    }

    private void ValidateAndCompleteRep()
    {
        if (!isValidRepInProgress)
            return;

        float repDuration = Time.time - repStartTime;
        float formScore = CalculateFormScore(peakAngle);

        Debug.Log($"Validating rep - Peak angle: {peakAngle:F1}‹, Form score: {formScore:F1}%");

        bool hasMinimumDuration = repDuration >= settings.minRepDuration;
        bool hasHeldPeak = holdTimer >= settings.repHoldTime;

        // Build feedback message
        if (!hasMinimumDuration)
        {
            previousRepFeedback = "Movement too quick - control the motion";
        }
        else if (!hasHeldPeak)
        {
            previousRepFeedback = "Hold the curl at the top longer";
        }
        else
        {
            // Provide angle-specific feedback
            if (Mathf.Abs(peakAngle - PERFECT_CURL_ANGLE) <= PERFECT_RANGE_THRESHOLD)
            {
                previousRepFeedback = $"Perfect form! ({formScore:F1}%)";
            }
            else if (Mathf.Abs(peakAngle - PERFECT_CURL_ANGLE) <= GOOD_RANGE_THRESHOLD)
            {
                previousRepFeedback = $"Good form ({formScore:F1}%)";
            }
            else if (peakAngle > PERFECT_CURL_ANGLE + GOOD_RANGE_THRESHOLD)
            {
                previousRepFeedback = $"Not deep enough: {peakAngle:F1}‹ ({formScore:F1}%), target: {PERFECT_CURL_ANGLE}‹";
            }
            else
            {
                previousRepFeedback = $"Too deep: {peakAngle:F1}‹ ({formScore:F1}%), target: {PERFECT_CURL_ANGLE}‹";
            }
        }

        // Always complete the rep if minimum requirements are met
        if (hasMinimumDuration && hasHeldPeak)
        {
            Debug.Log("Completing rep with score: " + formScore);
            CompleteRep();
        }

        Debug.Log(previousRepFeedback);

        // Reset tracking for next rep
        isValidRepInProgress = false;
        repStartAngle = currentElbowAngle;
        peakAngle = currentElbowAngle;
        isHolding = false;
        holdTimer = 0f;
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

    private float CalculateFormScore(float peakAngle)
    {
        float score;

        // Perfect form (89-91 degrees)
        if (Mathf.Abs(peakAngle - PERFECT_CURL_ANGLE) <= PERFECT_RANGE_THRESHOLD)
        {
            score = 100f;
        }
        // Good form (85-95 degrees)
        else if (Mathf.Abs(peakAngle - PERFECT_CURL_ANGLE) <= GOOD_RANGE_THRESHOLD)
        {
            score = 90f;
        }
        else
        {
            // Calculate percentage based on how far off from 90 degrees
            float deviation = Mathf.Abs(peakAngle - PERFECT_CURL_ANGLE);
            float maxDeviation = 30f; // Maximum degrees off before 0%
            score = Mathf.Max(0f, 100f * (1f - deviation / maxDeviation));
        }

        // Apply penalties for form issues
        if (currentRepIssues.Any())
        {
            // Deduct points for each unique form issue
            float penalty = currentRepIssues.Distinct().Count() * 10f;
            score = Mathf.Max(0f, score - penalty);
        }

        lastRepScore = score;
        currentSetRepScores.Add(score);

        return score;
    }

    public float GetCurrentSetAverageScore()
    {
        if (currentSetRepScores.Count == 0)
            return 0f;
        return currentSetRepScores.Average();
    }

    public List<float> GetCurrentSetRepScores()
    {
        return new List<float>(currentSetRepScores);
    }

    private string GetAngleFeedback(float peakAngle)
    {
        float deviation = peakAngle - PERFECT_CURL_ANGLE;
        if (Mathf.Abs(deviation) <= PERFECT_RANGE_THRESHOLD)
        {
            return "Perfect form!";
        }
        else if (Mathf.Abs(deviation) <= GOOD_RANGE_THRESHOLD)
        {
            return "Good form";
        }
        else if (deviation > GOOD_RANGE_THRESHOLD)
        {
            return "Curl not deep enough";
        }
        else
        {
            return "Curl too deep";
        }
    }
}