using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class WorkoutHistoryController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject setDetailsPrefab;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject noWorkoutsPanel;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Canvas Management")]
    [SerializeField] private GameObject libraryCanvas;
    [SerializeField] private GameObject historyCanvas;

    private WorkoutAWSManager awsManager;
    private AuthenticationManager authManager;
    private bool isLoadingData = false;

    private void Start()
    {
        awsManager = FindObjectOfType<WorkoutAWSManager>();
        authManager = FindObjectOfType<AuthenticationManager>();

        // Start with library canvas active
        ShowLibraryView();

        // Subscribe to AWS manager's callback
        if (awsManager != null)
        {
            // You'll need to add this event to WorkoutAWSManager
            awsManager.OnWorkoutHistoryReceived += HandleWorkoutHistory;
        }
    }

    public void ShowHistoryView()
    {
        historyCanvas.SetActive(true);
        libraryCanvas.SetActive(false);
        LoadWorkoutHistory();
    }

    public void ShowLibraryView()
    {
        libraryCanvas.SetActive(true);
        historyCanvas.SetActive(false);
    }

    private async void LoadWorkoutHistory()
    {
        if (isLoadingData) return;

        isLoadingData = true;
        loadingPanel.SetActive(true);
        noWorkoutsPanel.SetActive(false);

        // Clear existing content
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        try
        {
            string userId = authManager.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("User ID not found");
                return;
            }

            // This will trigger OnWorkoutHistoryReceived when complete
            awsManager.GetUserWorkoutHistory(userId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading workout history: {e.Message}");
            ShowError();
        }
    }

    private void HandleWorkoutHistory(List<WorkoutSession> sessions)
    {
        isLoadingData = false;
        loadingPanel.SetActive(false);

        if (sessions == null || sessions.Count == 0)
        {
            noWorkoutsPanel.SetActive(true);
            return;
        }

        foreach (var session in sessions)
        {
            Debug.Log($"Session Data: Date: {session.sessionDate}, Sets Count: {session.sets?.Count ?? 0}");
            if (session.sets != null)
            {
                foreach (var set in session.sets)
                {
                    Debug.Log($"Set Data:" +
                        $"\n  Set Number: {set.setNumber}" +
                        $"\n  Arm: {set.arm}" +
                        $"\n  Reps: {set.reps}" +
                        $"\n  Form Score: {set.averageFormScore}" +
                        $"\n  Duration: {set.duration}" +
                        $"\n  Form Issues Count: {set.formIssues?.Count ?? 0}");
                }
            }
        }

        // Sort sessions by date (newest first)
        sessions.Sort((a, b) => b.sessionDate.CompareTo(a.sessionDate));

        foreach (var session in sessions)
        {
            CreateSessionGroup(session);
        }

        // Reset scroll position to top
        scrollRect.normalizedPosition = new Vector2(0, 1);
    }

    private void CreateSessionGroup(WorkoutSession session)
    {
        // Now create set details for each set with the date included
        foreach (var set in session.sets)
        {
            GameObject setPanel = Instantiate(setDetailsPrefab, contentContainer);
            SetDetailsPanelHistory detailsPanel = setPanel.GetComponent<SetDetailsPanelHistory>();
            if (detailsPanel != null)
            {
                detailsPanel.PopulateSetDetails(set, session.sessionDate);
            }
        }
    }

    private void ShowError()
    {
        isLoadingData = false;
        loadingPanel.SetActive(false);
        noWorkoutsPanel.SetActive(true);

        // Modify the no workouts text to show error
        TextMeshProUGUI errorText = noWorkoutsPanel.GetComponent<TextMeshProUGUI>();
        if (errorText != null)
        {
            errorText.text = "Error loading workout history.\nPlease try again later.";
        }
    }

    private void OnDestroy()
    {
        if (awsManager != null)
        {
            awsManager.OnWorkoutHistoryReceived -= HandleWorkoutHistory;
        }
    }
}