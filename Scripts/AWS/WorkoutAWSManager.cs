using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon.CognitoIdentity;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Newtonsoft.Json;
using Amazon;

public class WorkoutAWSManager : MonoBehaviour
{
    private const string IDENTITY_POOL_ID = "us-west-1:c0888cc2-c5e4-4e2d-a9be-399aa2c62429";
    private const string DYNAMODB_TABLE_NAME = "MOFITWorkouts";
    private AmazonDynamoDBClient _dynamoDbClient;
    private AuthenticationManager _authManager;
    public event System.Action<List<WorkoutSession>> OnWorkoutHistoryReceived;

    private void Awake()
    {
        UnityInitializer.AttachToGameObject(this.gameObject);

        CognitoAWSCredentials credentials = new CognitoAWSCredentials(
            IDENTITY_POOL_ID,
            RegionEndpoint.USWest1
        );

        _dynamoDbClient = new AmazonDynamoDBClient(credentials, RegionEndpoint.USWest1);
        _authManager = FindObjectOfType<AuthenticationManager>();
    }

    public void GetUserWorkoutHistory(string userId)
    {
        var request = new QueryRequest
        {
            TableName = DYNAMODB_TABLE_NAME,
            KeyConditionExpression = "UserId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":userId", new AttributeValue { S = userId } }
        },
            ScanIndexForward = false
        };

        _dynamoDbClient.QueryAsync(request, (result) =>
        {
            if (result.Exception != null)
            {
                Debug.LogError($"Error getting workout history: {result.Exception.Message}");
                OnWorkoutHistoryReceived?.Invoke(new List<WorkoutSession>());
                return;
            }

            var response = result.Response;
            var workoutSessions = new List<WorkoutSession>();

            foreach (var item in response.Items)
            {
                try
                {
                    var session = new WorkoutSession();

                    // Parse basic session data
                    session.workoutId = GetAttributeValue(item, "workoutId");
                    session.exerciseType = GetAttributeValue(item, "exerciseType");
                    session.totalReps = int.Parse(GetAttributeValue(item, "totalReps", "0"));
                    session.totalDuration = float.Parse(GetAttributeValue(item, "totalDuration", "0"));
                    session.completedSetPairs = int.Parse(GetAttributeValue(item, "completedSetPairs", "0"));

                    if (DateTime.TryParse(GetAttributeValue(item, "sessionDate"), out DateTime sessionDate))
                    {
                        // Convert from UTC to local time if it's not already
                        if (sessionDate.Kind == DateTimeKind.Utc)
                        {
                            session.sessionDate = sessionDate.ToLocalTime();
                        }
                        else
                        {
                            session.sessionDate = sessionDate;
                        }
                    }

                    // Parse sets from DynamoDB List
                    session.sets = new List<WorkoutSet>();
                    if (item.ContainsKey("sets") && item["sets"].L != null)
                    {
                        foreach (var setItem in item["sets"].L)
                        {
                            if (setItem.M != null)
                            {
                                var set = new WorkoutSet();
                                var setMap = setItem.M;

                                // Parse set data
                                set.setNumber = int.Parse(GetAttributeValue(setMap, "setNumber", "0"));
                                set.arm = GetAttributeValue(setMap, "arm", "Unknown");
                                set.reps = int.Parse(GetAttributeValue(setMap, "reps", "0"));
                                set.averageFormScore = float.Parse(GetAttributeValue(setMap, "averageFormScore", "0"));
                                set.duration = float.Parse(GetAttributeValue(setMap, "duration", "0"));

                                // Parse form issues
                                set.formIssues = new List<string>();
                                if (setMap.ContainsKey("formIssues") && setMap["formIssues"].L != null)
                                {
                                    foreach (var issue in setMap["formIssues"].L)
                                    {
                                        if (issue.S != null)
                                            set.formIssues.Add(issue.S);
                                    }
                                }

                                // Parse timestamp components
                                set.year = int.Parse(GetAttributeValue(setMap, "year", "2024"));
                                set.month = int.Parse(GetAttributeValue(setMap, "month", "1"));
                                set.day = int.Parse(GetAttributeValue(setMap, "day", "1"));
                                set.hour = int.Parse(GetAttributeValue(setMap, "hour", "0"));
                                set.minute = int.Parse(GetAttributeValue(setMap, "minute", "0"));
                                set.second = int.Parse(GetAttributeValue(setMap, "second", "0"));

                                Debug.Log($"Parsed set: Number={set.setNumber}, Arm={set.arm}, Reps={set.reps}, Score={set.averageFormScore}");
                                session.sets.Add(set);
                            }
                        }
                    }

                    workoutSessions.Add(session);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing workout session: {e.Message}");
                    continue;
                }
            }

            Debug.Log($"Successfully retrieved {workoutSessions.Count} workout sessions");
            foreach (var session in workoutSessions)
            {
                Debug.Log($"Session: {session.sessionDate}, Sets count: {session.sets.Count}");
                foreach (var set in session.sets)
                {
                    Debug.Log($"Set: {set.setNumber}, Arm: {set.arm}, Reps: {set.reps}, Score: {set.averageFormScore}");
                }
            }

            OnWorkoutHistoryReceived?.Invoke(workoutSessions);
        });
    }

    private string GetAttributeValue(Dictionary<string, AttributeValue> item, string key, string defaultValue = "")
    {
        if (item.TryGetValue(key, out AttributeValue value))
        {
            if (value.S != null) return value.S;
            if (value.N != null) return value.N;
        }
        return defaultValue;
    }

    public async Task UploadWorkoutSession(WorkoutSession session)
    {
        string userId = _authManager.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID not found. User must be authenticated.");
            return;
        }

        // Debug logging
        Debug.Log($"Uploading session with {session.sets?.Count ?? 0} sets");
        foreach (var set in session.sets ?? new List<WorkoutSet>())
        {
            Debug.Log($"Set {set.setNumber}: Arm={set.arm}, Reps={set.reps}, Score={set.averageFormScore}, " +
                      $"Issues Count={set.formIssues?.Count ?? 0}, RepScores Count={set.repScores?.Count ?? 0}");
        }

        if (string.IsNullOrEmpty(session.workoutId))
        {
            session.workoutId = $"{userId}-{DateTime.Now.Ticks}";
        }

        try
        {
            var setsList = new List<AttributeValue>();

            if (session.sets != null)
            {
                foreach (var set in session.sets)
                {
                    // Initialize empty lists to avoid null reference
                    var formIssuesList = new List<AttributeValue>();
                    if (set.formIssues != null)
                    {
                        foreach (var issue in set.formIssues)
                        {
                            if (!string.IsNullOrEmpty(issue))
                            {
                                formIssuesList.Add(new AttributeValue { S = issue });
                            }
                        }
                    }

                    var repScoresList = new List<AttributeValue>();
                    if (set.repScores != null)
                    {
                        foreach (var score in set.repScores)
                        {
                            repScoresList.Add(new AttributeValue { N = score.ToString("F2") });
                        }
                    }

                    var setMap = new Dictionary<string, AttributeValue>
                {
                    { "setNumber", new AttributeValue { N = Math.Max(1, set.setNumber).ToString() } },
                    { "arm", new AttributeValue { S = string.IsNullOrEmpty(set.arm) ? "Unknown" : set.arm } },
                    { "reps", new AttributeValue { N = Math.Max(0, set.reps).ToString() } },
                    { "averageFormScore", new AttributeValue { N = set.averageFormScore.ToString("F2") } },
                    { "duration", new AttributeValue { N = Math.Max(0, set.duration).ToString("F2") } },
                    { "year", new AttributeValue { N = DateTime.Now.Year.ToString() } },
                    { "month", new AttributeValue { N = DateTime.Now.Month.ToString() } },
                    { "day", new AttributeValue { N = DateTime.Now.Day.ToString() } },
                    { "hour", new AttributeValue { N = DateTime.Now.Hour.ToString() } },
                    { "minute", new AttributeValue { N = DateTime.Now.Minute.ToString() } },
                    { "second", new AttributeValue { N = DateTime.Now.Second.ToString() } }

                };

                    // Only add these if they have content
                    if (formIssuesList.Count > 0)
                    {
                        setMap.Add("formIssues", new AttributeValue { L = formIssuesList });
                    }

                    if (repScoresList.Count > 0)
                    {
                        setMap.Add("repScores", new AttributeValue { L = repScoresList });
                    }

                    setsList.Add(new AttributeValue { M = setMap });
                }
            }

            var item = new Dictionary<string, AttributeValue>
        {
            { "UserId", new AttributeValue { S = userId } },
            { "workoutId", new AttributeValue { S = session.workoutId } },
            { "completedSetPairs", new AttributeValue { N = Math.Max(0, session.completedSetPairs).ToString() } },
            { "exerciseType", new AttributeValue { S = string.IsNullOrEmpty(session.exerciseType) ? "Hammer Curls" : session.exerciseType } },
            { "sessionDate", new AttributeValue { S = DateTime.Now.ToString("o") } }, 
            { "timestamp", new AttributeValue { S = DateTime.UtcNow.ToString("o") } },
            { "totalDuration", new AttributeValue { N = Math.Max(0, session.totalDuration).ToString("F2") } },
            { "totalReps", new AttributeValue { N = Math.Max(0, session.totalReps).ToString() } }
        };

            // Only add sets if we have any
            if (setsList.Count > 0)
            {
                item.Add("sets", new AttributeValue { L = setsList });
            }

            Debug.Log("Prepared DynamoDB item: " + JsonUtility.ToJson(item));

            var request = new PutItemRequest
            {
                TableName = DYNAMODB_TABLE_NAME,
                Item = item
            };

            var taskCompletionSource = new TaskCompletionSource<bool>();

            _dynamoDbClient.PutItemAsync(request, (result) =>
            {
                if (result.Exception != null)
                {
                    Debug.LogError($"Error uploading workout session: {result.Exception.Message}");
                    taskCompletionSource.SetException(result.Exception);
                }
                else
                {
                    Debug.Log("Workout session uploaded successfully.");
                    taskCompletionSource.SetResult(true);
                }
            });

            await taskCompletionSource.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error uploading workout: {e.Message}");
            throw;
        }
    }
}