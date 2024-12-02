using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using YOLOv8WithOpenCVForUnityExample;

public class WorkoutSceneUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button finishButton;
    [SerializeField] private TextMeshProUGUI repCountText;
    [SerializeField] private TextMeshProUGUI setCountText;
    [SerializeField] private TextMeshProUGUI formFeedbackText;
    [SerializeField] private TextMeshProUGUI angleText;

    [Header("References")]
    [SerializeField] private HammerCurlTracker curlTracker;
    [SerializeField] private YOLOv8PoseEstimationExample poseEstimation;

    private float setStartTime;

    public bool IsWorkoutActive { get; private set; }

    private void Start()
    {
        IsWorkoutActive = false;

        // Initially hide finish button and workout stats
        finishButton.gameObject.SetActive(false);
        SetStatsVisibility(false);

        // Setup button listeners
        if (startButton != null)
            startButton.onClick.AddListener(StartWorkout);

        if (finishButton != null)
            finishButton.onClick.AddListener(FinishWorkout);

        // Check if this is continuing a session
        var session = WorkoutResultsManager.LoadLastSession();
        if (session != null)
        {
            // Don't create a new session since we're continuing an existing one
            if (curlTracker != null)
            {
                curlTracker.ResetTracker();
                // Set the current set number based on existing sets
                for (int i = 0; i < session.sets.Count; i++)
                {
                    curlTracker.currentSet++;
                }
            }
        }
    }

    private void SetStatsVisibility(bool visible)
    {
        if (repCountText != null) repCountText.gameObject.SetActive(visible);
        if (setCountText != null) setCountText.gameObject.SetActive(visible);
        if (formFeedbackText != null) formFeedbackText.gameObject.SetActive(visible);
        if (angleText != null) angleText.gameObject.SetActive(visible);
    }


    private void StartWorkout()
    {
        IsWorkoutActive = true;
        startButton.gameObject.SetActive(false);
        finishButton.gameObject.SetActive(true);
        SetStatsVisibility(true);
        setStartTime = Time.time;

        // Start tracking
        if (curlTracker != null)
        {
            // Only start new session if there isn't an existing one
            if (WorkoutResultsManager.LoadLastSession() == null)
            {
                WorkoutResultsManager.StartNewSession("Hammer Curls");
            }
            curlTracker.ResetTracker();
        }
    }

    private void FinishWorkout()
    {
        if (curlTracker != null)
        {
            // Save current set regardless of completion status
            if (curlTracker.CurrentReps > 0)
            {
                // Save the partial set
                WorkoutResultsManager.AddSet(
                    curlTracker.CurrentReps,
                    curlTracker.CurrentFormScore,
                    Time.time - setStartTime,
                    curlTracker.GetCurrentFormIssues()
                );
            }

            // Ensure we save the final session state
            var session = WorkoutResultsManager.LoadLastSession();
            if (session != null)
            {
                if (curlTracker != null)
                {
                    // Set the current set number based on existing sets
                    curlTracker.currentSet = session.sets.Count + 1;
                    curlTracker.ResetTracker(false); // Don't reset set number
                }
            }
        }

        IsWorkoutActive = false;
        SceneManager.LoadScene("Results"); 
    }


    private void Update()
    {
        if (!IsWorkoutActive || curlTracker == null) return;

        // Update UI with current tracking info
        if (repCountText != null)
            repCountText.text = $"Reps: {curlTracker.CurrentReps}/{curlTracker.MaxRepsPerSet}";

        if (setCountText != null)
            setCountText.text = $"Set: {curlTracker.CurrentSet}";

        if (formFeedbackText != null)
            formFeedbackText.text = curlTracker.FormFeedback;

        if (angleText != null)
            angleText.text = $"Angle: {curlTracker.CurrentAngle:F1}Åã";
    }
}