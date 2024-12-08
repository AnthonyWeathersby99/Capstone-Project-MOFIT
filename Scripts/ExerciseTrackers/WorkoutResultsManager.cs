using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class WorkoutSet
{
    public int setNumber;
    public string arm;
    public int reps;
    public float averageFormScore;
    public float duration;
    public List<string> formIssues;
    // Store these components separately for JSON serialization
    public int year;
    public int month;
    public int day;
    public int hour;
    public int minute;
    public int second;

    [NonSerialized] // This won't be serialized
    private DateTime _timestamp;

    public DateTime timestamp
    {
        get
        {
            if (_timestamp == default(DateTime))
            {
                _timestamp = new DateTime(year, month, day, hour, minute, second);
            }
            return _timestamp;
        }
        set
        {
            _timestamp = value;
            year = value.Year;
            month = value.Month;
            day = value.Day;
            hour = value.Hour;
            minute = value.Minute;
            second = value.Second;
        }
    }

    public WorkoutSet(int setNum, string arm, int reps, float formScore, float duration, List<string> issues)
    {
        this.setNumber = setNum;
        this.arm = arm;
        this.reps = reps;
        this.averageFormScore = formScore;
        this.duration = duration;
        this.formIssues = issues;
        this.timestamp = DateTime.Now;

        Debug.Log($"Created WorkoutSet with timestamp: {this.timestamp:yyyy-MM-dd HH:mm:ss}");
    }

    public string GetFormattedTimestamp()
    {
        return timestamp.ToString("HH:mm:ss");
    }
}

[Serializable]
public class WorkoutSession
{
    public List<WorkoutSet> sets;
    public DateTime sessionDate;
    public float totalDuration;
    public string exerciseType;
    public int totalReps;
    public int completedSetPairs; // Added to track completed left/right pairs

    public WorkoutSession()
    {
        sets = new List<WorkoutSet>();
        sessionDate = DateTime.Now;
        totalDuration = 0f;
        totalReps = 0;
        completedSetPairs = 0;
    }

    // Helper method to get average form score
    public float GetAverageFormScore()
    {
        if (sets.Count == 0) return 0f;
        return sets.Average(s => s.averageFormScore);
    }

    // Helper method to get sets for a specific pair number
    public List<WorkoutSet> GetSetPair(int pairNumber)
    {
        return sets.Where(s => s.setNumber == pairNumber).ToList();
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
            sessionDate = DateTime.Now,
            completedSetPairs = 0
        };
    }

    public static void AddSet(int reps, string arm, float formScore, float duration, List<string> issues)
    {
        if (currentSession == null)
        {
            Debug.LogWarning("No active session - creating new one");
            StartNewSession("Hammer Curls");
        }

        // Log the incoming data
        Debug.Log($"Adding set - Arm: {arm}, Form Score: {formScore}, Duration: {duration}");

        // Calculate the correct set number - if there are 2 sets, next is set 2
        int currentSetNumber = (int)Math.Ceiling((float)(currentSession.sets.Count + 1) / 2);
        Debug.Log($"Calculated set number: {currentSetNumber} from {currentSession.sets.Count} sets");

        WorkoutSet newSet = new WorkoutSet(
            currentSetNumber,
            arm,
            reps,
            formScore,
            duration,
            issues
        );

        currentSession.sets.Add(newSet);
        currentSession.totalReps += reps;
        currentSession.totalDuration += duration;

        SaveWorkoutSession(currentSession);
        Debug.Log($"Added set {newSet.setNumber} - {arm} arm with form score {formScore} at {newSet.timestamp:HH:mm:ss}");
    }
}