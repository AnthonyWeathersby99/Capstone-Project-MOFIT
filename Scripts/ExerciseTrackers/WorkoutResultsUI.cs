using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using static PlasticPipe.PlasticProtocol.Messages.Serialization.ItemHandlerMessagesSerialization;
using System.Net.NetworkInformation;
using UnityEditor;



public class WorkoutResultsUI : MonoBehaviour
{
    [Header("Session Overview")]
    [SerializeField] private TextMeshProUGUI sessionDateText;
    [SerializeField] private TextMeshProUGUI totalTimeText;
    [SerializeField] private TextMeshProUGUI totalRepsText;
    [SerializeField] private TextMeshProUGUI averageFormText;

    [Header("Set Details Panel")]
    [SerializeField] private GameObject setDetailsPrefab;
    [SerializeField] private Transform scrollContent;

    [Header("Navigation")]
    [SerializeField] private CrossPlatformInputManager nextSetButton;
    [SerializeField] private CrossPlatformInputManager completeWorkoutButton;

    private MenuManager menuManager;

    private void Start()
    {
        menuManager = FindObjectOfType<MenuManager>();
        if (menuManager != null)
        {
            menuManager.AddButton("NextSet", nextSetButton, "BodyTracker");
            menuManager.AddButton("CompleteWorkout", completeWorkoutButton, "MainMenu");
        }

        LoadAndDisplayResults();
    }

    private void LoadAndDisplayResults()
    {
        WorkoutSession session = WorkoutResultsManager.LoadLastSession();
        if (session == null)
        {
            Debug.LogWarning("No workout session found!");
            return;
        }

        // Display session overview
        DisplaySessionOverview(session);

        // Clear and populate set details
        PopulateSetDetails(session);
    }

    private void DisplaySessionOverview(WorkoutSession session)
    {
        if (sessionDateText != null)
            sessionDateText.text = $"Workout Date: {session.sessionDate:g}";

        if (totalTimeText != null)
            totalTimeText.text = $"Total Time: {FormatTime(session.totalDuration)}";

        if (totalRepsText != null)
            totalRepsText.text = $"Total Reps: {session.totalReps}";

        if (averageFormText != null && session.sets.Count > 0)
        {
            float averageForm = session.sets.Average(s => s.averageFormScore);
            averageFormText.text = $"Average Form: {averageForm:F1}%";
        }
    }

    private void PopulateSetDetails(WorkoutSession session)
    {
        if (scrollContent == null || setDetailsPrefab == null)
        {
            Debug.LogError("Missing scrollContent or setDetailsPrefab reference");
            return;
        }

        Debug.Log($"Populating {session.sets.Count} sets");

        // Clear existing content
        foreach (Transform child in scrollContent)
        {
            Destroy(child.gameObject);
        }

        // Create set detail panels for each set, sorting by set number
        foreach (var set in session.sets.OrderBy(s => s.setNumber))
        {
            // Skip if the arm is missing or invalid
            if (string.IsNullOrEmpty(set.arm))
                continue;

            GameObject setPanel = Instantiate(setDetailsPrefab, scrollContent);
            Debug.Log($"Created panel for Set {set.setNumber} - {set.arm} Arm");
            PopulateSetPanel(setPanel, set);
        }
    }

    private void PopulateSetPanel(GameObject panel, WorkoutSet set)
    {
        Debug.Log($"===== Populating Set Panel =====");
        Debug.Log($"Set Number: {set.setNumber}");
        Debug.Log($"Arm: {set.arm}");
        Debug.Log($"Form Score: {set.averageFormScore}");
        Debug.Log($"Duration: {set.duration}");
        Debug.Log($"Timestamp: {set.timestamp}");
        Debug.Log($"Issues Count: {set.formIssues?.Count ?? 0}");
        if (set.formIssues != null)
            Debug.Log($"Issues: {string.Join(", ", set.formIssues)}");

        // Get references to components
        TextMeshProUGUI setNumberText = panel.transform.Find("Header/SetNumber (Text)")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timestampText = panel.transform.Find("Header/Timestamp (Text)")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI repsValue = panel.transform.Find("Details/Reps/Value (Text)")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI formScoreValue = panel.transform.Find("Details/FormScore/Value (Text)")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI durationValue = panel.transform.Find("Details/Duration/Value (Text)")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI formIssuesList = panel.transform.Find("FormIssues/Lists (Text)")?.GetComponent<TextMeshProUGUI>();

        Debug.Log($"Found Components: SetNumber:{setNumberText != null}, Timestamp:{timestampText != null}, " +
                  $"Reps:{repsValue != null}, FormScore:{formScoreValue != null}, " +
                  $"Duration:{durationValue != null}, FormIssues:{formIssuesList != null}");

        // Populate data with null checks and log the values being set
        if (setNumberText != null)
        {
            setNumberText.text = $"Set {set.setNumber} - {set.arm} Arm";
            Debug.Log($"Set Text: {setNumberText.text}");
        }

        if (timestampText != null)
        {
            timestampText.text = set.timestamp.ToString("HH:mm:ss");
            Debug.Log($"Set timestamp: {set.timestamp:HH:mm:ss.fff}");
        }

        if (repsValue != null)
        {
            repsValue.text = set.reps.ToString();
            Debug.Log($"Reps Text: {repsValue.text}");
        }

        if (formScoreValue != null)
        {
            formScoreValue.text = $"{set.averageFormScore:F1}%";
            Debug.Log($"Form Score Text: {formScoreValue.text}");
        }

        if (durationValue != null)
        {
            durationValue.text = FormatTime(set.duration);
            Debug.Log($"Duration Text: {durationValue.text}");
        }

        if (formIssuesList != null && set.formIssues != null)
        {
            var uniqueIssues = set.formIssues.Distinct().Take(4).ToList();
            formIssuesList.text = uniqueIssues.Any() ?
                $"• {string.Join("\n• ", uniqueIssues)}" :
                "No form issues";
            Debug.Log($"Form Issues Text: {formIssuesList.text}");
        }
    }

    private string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
    }
}