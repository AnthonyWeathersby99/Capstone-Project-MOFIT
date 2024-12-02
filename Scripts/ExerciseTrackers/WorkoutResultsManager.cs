using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class WorkoutSet
{
    public int setNumber;
    public int reps;
    public float averageFormScore; // 0-100%
    public float duration; // in seconds
    public List<string> formIssues;
    public DateTime timestamp;

    public WorkoutSet(int setNum, int reps, float formScore, float duration, List<string> issues)
    {
        this.setNumber = setNum;
        this.reps = reps;
        this.averageFormScore = formScore;
        this.duration = duration;
        this.formIssues = issues;
        this.timestamp = DateTime.Now;
    }
}

[Serializable]
public class WorkoutSession
{
    public List<WorkoutSet> sets;
    public DateTime sessionDate;
    public float totalDuration;
    public string exerciseType; // e.g., "Hammer Curls"
    public int totalReps;

    public WorkoutSession()
    {
        sets = new List<WorkoutSet>();
        sessionDate = DateTime.Now;
        totalDuration = 0f;
        totalReps = 0;
    }
}

public class WorkoutResultsManager : MonoBehaviour
{
    private static WorkoutSession currentSession;
    private static string SAVE_KEY = "workout_session";

    public static void SaveWorkoutSession(WorkoutSession session)
    {
        string json = JsonUtility.ToJson(session);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        Debug.Log($"Saved workout session: {json}");
    }

    public static WorkoutSession LoadLastSession()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            return JsonUtility.FromJson<WorkoutSession>(json);
        }
        return null;
    }

    public static void StartNewSession(string exerciseType)
    {
        currentSession = new WorkoutSession
        {
            exerciseType = exerciseType,
            sessionDate = DateTime.Now
        };
    }

    public static void AddSet(int reps, float formScore, float duration, List<string> issues)
    {
        if (currentSession == null)
        {
            Debug.LogWarning("No active session - creating new one");
            StartNewSession("Hammer Curls");
        }

        WorkoutSet newSet = new WorkoutSet(
            currentSession.sets.Count + 1,
            reps,
            formScore,
            duration,
            issues
        );

        currentSession.sets.Add(newSet);
        currentSession.totalReps += reps;
        currentSession.totalDuration += duration;

        SaveWorkoutSession(currentSession);
        Debug.Log($"Added set {newSet.setNumber} with {reps} reps and score {formScore}");
    }
}