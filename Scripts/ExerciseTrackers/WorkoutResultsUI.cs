using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public class WorkoutResultsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI sessionDateText;
    [SerializeField] private TextMeshProUGUI totalTimeText;
    [SerializeField] private TextMeshProUGUI totalRepsText;
    [SerializeField] private TextMeshProUGUI averageFormText;

    // References for the single set detail section
    [SerializeField] private TextMeshProUGUI setNumberText;
    [SerializeField] private TextMeshProUGUI repsText;
    [SerializeField] private TextMeshProUGUI formScoreText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI formIssuesText;

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
        Debug.Log("LoadAndDisplayResults called");
        WorkoutSession session = WorkoutResultsManager.LoadLastSession();
        if (session == null)
        {
            Debug.LogWarning("No workout session found!");
            return;
        }

        Debug.Log($"Found session with {session.sets.Count} sets and {session.totalReps} total reps");

        // Display session overview
        if (sessionDateText != null)
            sessionDateText.text = $"Workout Date: {session.sessionDate:g}";
        else
            Debug.LogWarning("sessionDateText is null");

        if (totalTimeText != null)
            totalTimeText.text = $"Total Time: {FormatTime(session.totalDuration)}";
        else
            Debug.LogWarning("totalTimeText is null");

        if (totalRepsText != null)
            totalRepsText.text = $"Total Reps: {session.totalReps}";
        else
            Debug.LogWarning("totalRepsText is null");

        if (averageFormText != null && session.sets.Count > 0)
        {
            float averageForm = session.sets.Average(s => s.averageFormScore);
            averageFormText.text = $"Average Form: {averageForm:F1}%";
        }
        else
            Debug.LogWarning("averageFormText is null or no sets available");

        // Display the last set's details
        if (session.sets.Count > 0)
        {
            var lastSet = session.sets[session.sets.Count - 1];

            if (setNumberText != null)
                setNumberText.text = $"Set {lastSet.setNumber}";
            else
                Debug.LogWarning("setNumberText is null");

            if (repsText != null)
                repsText.text = $"Reps: {lastSet.reps}";
            else
                Debug.LogWarning("repsText is null");

            if (formScoreText != null)
                formScoreText.text = $"Form Score: {lastSet.averageFormScore:F1}%";
            else
                Debug.LogWarning("formScoreText is null");

            if (durationText != null)
                durationText.text = $"Duration: {FormatTime(lastSet.duration)}";
            else
                Debug.LogWarning("durationText is null");

            if (formIssuesText != null && lastSet.formIssues != null && lastSet.formIssues.Count > 0)
            {
                string issues = string.Join("\n", lastSet.formIssues.Take(3));
                formIssuesText.text = $"Form Issues:\n{issues}";
            }
            else if (formIssuesText != null)
            {
                formIssuesText.text = "No form issues";
            }
            else
                Debug.LogWarning("formIssuesText is null");
        }
        else
        {
            Debug.LogWarning("No sets found in session");
        }
    }

    private string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
    }
}