using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using YOLOv8WithOpenCVForUnityExample;
using System.Linq;
using System.Collections.Generic;

public class WorkoutSceneUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button finishButton;
    [SerializeField] private TextMeshProUGUI repCountText;
    [SerializeField] private TextMeshProUGUI setCountText;
    [SerializeField] private TextMeshProUGUI formFeedbackText;
    [SerializeField] private TextMeshProUGUI angleText;
    [SerializeField] private TextMeshProUGUI currentArmText;

    [Header("References")]
    [SerializeField] private HammerCurlTracker curlTracker;
    [SerializeField] private YOLOv8PoseEstimationExample poseEstimation;

    private float setStartTime;
    private string currentArm = "Left"; // Start with left arm
    private bool isFirstArmComplete = false;

    [Header("Countdown")]
    [SerializeField] private GameObject countdownPanel;  
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private float beginMessageDuration = 1f;  // How long "Begin!" shows
    private bool isCountingDown = false;
    private float countdownTimer;

    public bool IsWorkoutActive { get; private set; }

    private void Start()
    {
        IsWorkoutActive = false;
        SetupUI();
        LoadExistingSession();
    }

    private void SetupUI()
    {
        finishButton.gameObject.SetActive(false);
        SetStatsVisibility(false);

        // Make sure countdown panel starts disabled
        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        startButton.onClick.AddListener(StartCountdown);
        finishButton.onClick.AddListener(FinishSet);

        if (currentArmText != null)
            currentArmText.text = $"Current Arm: {currentArm}";
    }

    private void StartCountdown()
    {
        isCountingDown = true;
        countdownTimer = countdownDuration;
        startButton.gameObject.SetActive(false);

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
            countdownText.text = countdownDuration.ToString("0");
        }
    }

    private void LoadExistingSession()
    {
        var session = WorkoutResultsManager.LoadLastSession();
        if (session != null && curlTracker != null)
        {
            curlTracker.ResetTracker();

            // Calculate correct set number based on completed pairs
            int newSetNumber = session.completedSetPairs + 1;
            curlTracker.currentSet = newSetNumber;

            // Reset first arm completion status
            isFirstArmComplete = false;
            currentArm = "Left";

            Debug.Log($"Loading session with set number: {newSetNumber}");
        }
    }

    private void StartWorkout()
    {
        IsWorkoutActive = true;
        startButton.gameObject.SetActive(false);
        finishButton.gameObject.SetActive(true);
        SetStatsVisibility(true);
        setStartTime = Time.time;

        if (WorkoutResultsManager.LoadLastSession() == null)
        {
            WorkoutResultsManager.StartNewSession("Hammer Curls");
        }

        if (curlTracker != null)
        {
            curlTracker.ResetTracker(false);
        }

        if (currentArmText != null)
            currentArmText.text = $"Current Arm: {currentArm}";
    }

    private void FinishSet()
    {
        if (curlTracker != null && curlTracker.CurrentReps > 0)
        {
            // Get all rep scores from the current set
            List<float> repScores = curlTracker.GetCurrentSetRepScores();
            float avgFormScore = repScores.Any() ? repScores.Average() : 100f;

            float duration = Time.time - setStartTime;
            var issues = curlTracker.GetCurrentFormIssues();

            Debug.Log($"Finishing set with Average Score: {avgFormScore}, Duration: {duration}, Rep Scores: {string.Join(", ", repScores)}");

            WorkoutResultsManager.AddSet(
                curlTracker.CurrentReps,
                currentArm,
                avgFormScore,
                duration,
                issues,
                repScores  // Add the rep scores parameter
            );
        }

        if (!isFirstArmComplete)
        {
            isFirstArmComplete = true;
            currentArm = "Right";
            startButton.gameObject.SetActive(true);
            finishButton.gameObject.SetActive(false);
            SetStatsVisibility(false);
        }
        else
        {
            CompleteSetPair();
        }
    }

    private void CompleteSetPair()
    {
        var session = WorkoutResultsManager.LoadLastSession();
        if (session != null)
        {
            session.completedSetPairs++;
            WorkoutResultsManager.SaveWorkoutSession(session);
        }

        IsWorkoutActive = false;
        SceneManager.LoadScene("Results");
    }

    private void Update()
    {
        if (isCountingDown)
        {
            countdownTimer -= Time.deltaTime;

            if (countdownTimer <= 0)
            {
                if (countdownText != null && countdownText.text != "Begin!")
                {
                    countdownText.text = "Begin!";
                    StartCoroutine(BeginWorkout());
                }
            }
            else
            {
                if (countdownText != null)
                    countdownText.text = Mathf.Ceil(countdownTimer).ToString("0");
            }
        }
        else if (IsWorkoutActive && curlTracker != null)
        {
            UpdateUI();
        }
    }
    private IEnumerator BeginWorkout()
    {
        yield return new WaitForSeconds(beginMessageDuration);
        isCountingDown = false;
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
        StartWorkout();
    }

    private void UpdateUI()
    {
        if (repCountText != null)
            repCountText.text = $"Reps: {curlTracker.CurrentReps}/{curlTracker.MaxRepsPerSet}";

        if (setCountText != null)
            setCountText.text = $"Set: {curlTracker.CurrentSet}";

        if (formFeedbackText != null)
            formFeedbackText.text = curlTracker.FormFeedback;

        if (angleText != null)
            angleText.text = $"Angle: {curlTracker.CurrentAngle:F1}‹";

        if (currentArmText != null)
            currentArmText.text = $"Current Arm: {currentArm}";
    }

    private void SetStatsVisibility(bool visible)
    {
        repCountText?.gameObject.SetActive(visible);
        setCountText?.gameObject.SetActive(visible);
        formFeedbackText?.gameObject.SetActive(visible);
        angleText?.gameObject.SetActive(visible);
        currentArmText?.gameObject.SetActive(visible);
    }
}