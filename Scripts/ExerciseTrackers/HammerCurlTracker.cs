using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class HammerCurlSettings
{
    [Header("Starting Position Range")]
    [Range(120, 180)]
    public float minStartAngle = 140f;
    [Range(120, 180)]
    public float maxStartAngle = 180f;

    [Header("Peak Curl Position Range")]
    [Range(60, 120)]
    public float minPeakAngle = 80f;
    [Range(60, 120)]
    public float maxPeakAngle = 110f;

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

    private const float PERFECT_CURL_ANGLE = 90f;
    private const float START_ANGLE = 165f;
    private const float MIN_SPEED = 20f;
    private const float MAX_SPEED = 100f;

    private int currentReps;
    public int currentSet = 1;
    private bool isMovingUp;
    private float lastCheckedAngle;
    private float holdTimer;
    private float currentElbowAngle;
    private float lastAngleCheckTime;
    private bool isHolding;
    private float repStartTime;
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
    private List<float> speedSamples = new List<float>();
    private float setStartTime;
    private float currentFormScore = 100f;
    private string previousRepFeedback = "Start your first rep";
    private float previousRepFormScore = 100f;
    private int formChecksThisRep = 0;
    private RepMetrics currentRepMetrics;

    private List<float> currentSetRepScores = new List<float>();
    private float lastRepScore = 0f;

    private struct RepMetrics
    {
        public float startAngle;
        public float peakAngle;
        public float averageSpeed;
        public float speedConsistency;
        public bool properPause;
        public List<string> formIssues;
        public float rangeOfMotion;
        public float duration;
    }

    private void Start()
    {
        ResetTracker();
    }

    public void ResetTracker(bool resetSetNumber = false)
    {
        currentSetRepScores.Clear();
        speedSamples.Clear();
        lastRepScore = 0f;
        currentReps = 0;
        if (resetSetNumber)
            currentSet = 1;
        isMovingUp = false;
        isHolding = false;
        holdTimer = 0f;
        lastCheckedAngle = 0f;
        repStartTime = Time.time;
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

        currentRepMetrics = new RepMetrics
        {
            formIssues = new List<string>()
        };
    }

    public void UpdateTracking(Vector3 shoulder, Vector3 elbow, Vector3 wrist)
    {
        float previousAngle = currentElbowAngle;
        currentElbowAngle = CalculateElbowAngle(shoulder, elbow, wrist);

        float deltaTime = Time.time - lastAngleCheckTime;
        if (deltaTime > 0)
        {
            float angleSpeed = Mathf.Abs(currentElbowAngle - previousAngle) / deltaTime;
            speedSamples.Add(angleSpeed);

            if (!isValidRepInProgress && currentElbowAngle >= settings.minStartAngle)
            {
                isValidRepInProgress = true;
                repStartTime = Time.time;
                currentRepMetrics = new RepMetrics
                {
                    startAngle = currentElbowAngle,
                    peakAngle = currentElbowAngle,
                    formIssues = new List<string>()
                };
                speedSamples.Clear();
            }
            else if (isValidRepInProgress)
            {
                if (currentElbowAngle < currentRepMetrics.peakAngle)
                {
                    currentRepMetrics.peakAngle = currentElbowAngle;
                    currentRepMetrics.properPause = CheckProperPause();
                }

                // Update metrics
                currentRepMetrics.duration = Time.time - repStartTime;
                currentRepMetrics.rangeOfMotion = currentRepMetrics.startAngle - currentRepMetrics.peakAngle;

                // Calculate speed metrics
                if (speedSamples.Count > 0)
                {
                    float avgSpeed = speedSamples.Average();
                    float stdDev = CalculateStandardDeviation(speedSamples, avgSpeed);
                    currentRepMetrics.averageSpeed = avgSpeed;
                    currentRepMetrics.speedConsistency = 1f - (stdDev / avgSpeed);
                }
            }

            CheckForm(angleSpeed);
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

    private bool CheckProperPause()
    {
        return isHolding && holdTimer >= settings.repHoldTime;
    }

    private float CalculateStandardDeviation(List<float> samples, float mean)
    {
        if (samples.Count < 2) return 0f;
        float variance = samples.Sum(x => Mathf.Pow(x - mean, 2)) / (samples.Count - 1);
        return Mathf.Sqrt(variance);
    }

    private void CheckForm(float angleSpeed)
    {
        float angleDiff = Mathf.Abs(currentElbowAngle - lastCheckedAngle);
        if (angleDiff > settings.angleThreshold)
        {
            isMovingUp = currentElbowAngle > lastCheckedAngle;
            lastCheckedAngle = currentElbowAngle;
        }

        // Check for form issues
        if (currentElbowAngle < settings.minPeakAngle)
            currentRepMetrics.formIssues.Add("Incomplete range of motion");
        else if (currentElbowAngle > settings.maxStartAngle)
            currentRepMetrics.formIssues.Add("Excessive swing at top");

        if (angleSpeed > settings.maxCurlSpeed)
            currentRepMetrics.formIssues.Add("Movement too fast");
        else if (angleSpeed < settings.minCurlSpeed && isValidRepInProgress)
            currentRepMetrics.formIssues.Add("Movement too slow");

        formChecksThisRep++;
    }

    private void CompleteRep()
    {
        if (isHolding && holdTimer >= settings.repHoldTime)
        {
            float formScore = CalculateFormScore(currentRepMetrics);
            currentReps++;

            // Store the score
            lastRepScore = formScore;
            currentSetRepScores.Add(formScore);

            // Reset tracking
            isHolding = false;
            holdTimer = 0f;
            repStartTime = Time.time;
            currentRepIssues.Clear();
            repFormScores.Clear();
            speedSamples.Clear();
            formChecksThisRep = 0;
            previousRepFormScore = formScore;

            UpdateFeedback(currentRepMetrics, formScore);
        }
    }

    private void UpdateFeedback(RepMetrics metrics, float score)
    {
        if (metrics.duration < settings.minRepDuration)
        {
            previousRepFeedback = "Movement too quick - control the motion";
        }
        else if (!metrics.properPause)
        {
            previousRepFeedback = "Hold the curl at the top longer";
        }
        else
        {
            if (Mathf.Abs(metrics.peakAngle - PERFECT_CURL_ANGLE) <= 1f)
                previousRepFeedback = $"Perfect form! ({score:F1}%)";
            else if (Mathf.Abs(metrics.peakAngle - PERFECT_CURL_ANGLE) <= 5f)
                previousRepFeedback = $"Good form ({score:F1}%)";
            else if (metrics.peakAngle > PERFECT_CURL_ANGLE + 5f)
                previousRepFeedback = $"Not deep enough: {metrics.peakAngle:F1}Åã ({score:F1}%), target: {PERFECT_CURL_ANGLE}Åã";
            else
                previousRepFeedback = $"Too deep: {metrics.peakAngle:F1}Åã ({score:F1}%), target: {PERFECT_CURL_ANGLE}Åã";
        }
    }

    private float CalculateFormScore(RepMetrics metrics)
    {
        float score = 100f;

        // Starting position score (25% weight)
        float startScore = CalculateAngleScore(metrics.startAngle, START_ANGLE, 10f);

        // Peak curl position score (35% weight)
        float peakScore = CalculateAngleScore(metrics.peakAngle, PERFECT_CURL_ANGLE, 5f);

        // Movement quality score (20% weight)
        float speedScore = CalculateSpeedScore(metrics.averageSpeed, metrics.speedConsistency);

        // Form maintenance score (20% weight)
        float formScore = CalculateFormMaintenanceScore(metrics.formIssues);

        // Calculate weighted total
        score = (startScore * 0.25f) + (peakScore * 0.35f) +
                (speedScore * 0.20f) + (formScore * 0.20f);

        // Bonus for proper pause at peak
        if (metrics.properPause) score = Mathf.Min(100f, score + 5f);

        // Range of motion requirement
        if (metrics.rangeOfMotion < settings.minRangeOfMotion)
        {
            score *= (metrics.rangeOfMotion / settings.minRangeOfMotion);
        }

        return Mathf.Max(0f, score);
    }

    private float CalculateAngleScore(float actual, float target, float tolerance)
    {
        float deviation = Mathf.Abs(actual - target);
        if (deviation <= tolerance)
            return 100f;

        float maxDeviation = tolerance * 3f;
        return Mathf.Max(0f, 100f * (1f - (deviation - tolerance) / maxDeviation));
    }

    private float CalculateSpeedScore(float avgSpeed, float consistency)
    {
        float speedScore = 100f;

        // Speed within target range check
        if (avgSpeed < MIN_SPEED)
            speedScore *= (avgSpeed / MIN_SPEED);
        else if (avgSpeed > MAX_SPEED)
            speedScore *= (MAX_SPEED / avgSpeed);

        // Apply consistency factor
        speedScore *= consistency;

        return speedScore;
    }

    private float CalculateFormMaintenanceScore(List<string> issues)
    {
        if (!issues.Any()) return 100f;

        Dictionary<string, float> issuePenalties = new Dictionary<string, float>
        {
            {"Movement too fast", 15f},
            {"Movement too slow", 10f},
            {"Excessive swing", 20f},
            {"Incomplete range of motion", 25f},
            {"Improper starting position", 15f},
            {"No pause at peak", 10f}
        };

        float totalPenalty = issues.Distinct().Sum(issue =>
            issuePenalties.ContainsKey(issue) ? issuePenalties[issue] : 10f);

        return Mathf.Max(0f, 100f - totalPenalty);
    }

    private void CheckRepCompletion()
    {
        bool isInStartPosition = currentElbowAngle >= settings.minStartAngle &&
                               currentElbowAngle <= settings.maxStartAngle;

        bool isInPeakPosition = currentElbowAngle >= settings.minPeakAngle &&
                              currentElbowAngle <= settings.maxPeakAngle;

        if (!isValidRepInProgress && isInStartPosition)
        {
            isValidRepInProgress = true;
            repStartTime = Time.time;
            currentRepMetrics = new RepMetrics
            {
                startAngle = currentElbowAngle,
                peakAngle = currentElbowAngle,
                formIssues = new List<string>()
            };
            speedSamples.Clear();
            Debug.Log($"Started tracking new rep at angle: {currentElbowAngle}");
        }
        else if (isValidRepInProgress)
        {
            if (isInPeakPosition)
            {
                if (!isHolding)
                {
                    isHolding = true;
                    holdTimer = 0f;
                }
                holdTimer += Time.deltaTime;
            }
            else if (isHolding && holdTimer >= settings.repHoldTime && isInStartPosition)
            {
                ValidateAndCompleteRep();
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

        currentRepMetrics.duration = Time.time - repStartTime;
        float formScore = CalculateFormScore(currentRepMetrics);

        // Only check minimum duration
        bool hasMinimumDuration = currentRepMetrics.duration >= settings.minRepDuration;

        // Build feedback message
        if (!currentRepMetrics.properPause)
        {
            previousRepFeedback = "Try holding at the peak for better form";
        }
        else if (currentRepMetrics.duration < settings.minRepDuration)
        {
            previousRepFeedback = "Slow down the movement for better control";
        }
        else
        {
            // Angle-specific feedback
            float angleDiff = Mathf.Abs(currentRepMetrics.peakAngle - PERFECT_CURL_ANGLE);
            if (angleDiff <= 2f)
                previousRepFeedback = $"Perfect form! ({formScore:F1}%)";
            else if (angleDiff <= 5f)
                previousRepFeedback = $"Good form ({formScore:F1}%)";
            else if (currentRepMetrics.peakAngle > PERFECT_CURL_ANGLE)
                previousRepFeedback = $"Try curling deeper: {currentRepMetrics.peakAngle:F1}Åã ({formScore:F1}%)";
            else
                previousRepFeedback = $"Don't curl too deep: {currentRepMetrics.peakAngle:F1}Åã ({formScore:F1}%)";
        }

        // Complete rep if it meets minimum duration
        if (hasMinimumDuration)
        {
            Debug.Log($"Completing rep with score: {formScore}");
            CompleteRep();
        }

        // Reset tracking for next rep
        isValidRepInProgress = false;
        currentRepMetrics = new RepMetrics
        {
            formIssues = new List<string>()
        };
        isHolding = false;
        holdTimer = 0f;
    }

    private void CompleteSet()
    {
        float setDuration = Time.time - setStartTime;
        Debug.Log($"Completing set. Duration: {setDuration}s, Average Score: {GetCurrentSetAverageScore():F1}%");

        currentReps = 0;
        setStartTime = Time.time;
        currentFormIssues.Clear();
        currentRepIssues.Clear();
        repFormScores.Clear();
        speedSamples.Clear();
        currentFormScore = 100f;
        FormFeedback = "Set complete! Take a rest";
        previousRepFeedback = "Ready for next set";
        formChecksThisRep = 0;
    }

    public List<string> GetCurrentFormIssues()
    {
        return currentRepMetrics.formIssues.Distinct().ToList();
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

    // Test methods
    public void ForceCompleteRep()
    {
        CompleteRep();
    }

    public void ForceCompleteSet()
    {
        CompleteSet();
    }

    private string GetAngleFeedback(float peakAngle)
    {
        float deviation = peakAngle - PERFECT_CURL_ANGLE;
        if (Mathf.Abs(deviation) <= 1f)
            return "Perfect form!";
        else if (Mathf.Abs(deviation) <= 5f)
            return "Good form";
        else if (deviation > 5f)
            return "Curl not deep enough";
        else
            return "Curl too deep";
    }
}