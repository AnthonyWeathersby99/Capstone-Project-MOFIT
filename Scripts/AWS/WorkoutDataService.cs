using System;
using System.Collections.Generic;
using UnityEngine;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.CognitoIdentity;
using Newtonsoft.Json;
using Amazon;
using System.Linq;

[Serializable]
public class WorkoutHistory
{
    public string UserId { get; set; }
    public string WorkoutId { get; set; }  // UserId + Timestamp
    public DateTime Date { get; set; }
    public string ExerciseType { get; set; }
    public int TotalReps { get; set; }
    public float TotalDuration { get; set; }
    public float AverageFormScore { get; set; }
    public List<WorkoutSet> Sets { get; set; }
}

public class WorkoutDataService : MonoBehaviour
{
    private const string WORKOUT_TABLE_NAME = "MOFITWorkoutHistory";
    private AmazonDynamoDBClient _dynamoDbClient;
    private AuthenticationManager _authManager;

    private void Awake()
    {
        _authManager = FindObjectOfType<AuthenticationManager>();
        InitializeDynamoDb();
    }

    private void InitializeDynamoDb()
    {
        UnityInitializer.AttachToGameObject(gameObject);

        var credentials = new CognitoAWSCredentials(
            "us-west-1:c0888cc2-c5e4-4e2d-a9be-399aa2c62429",  // Identity Pool ID
            RegionEndpoint.USWest1
        );

        _dynamoDbClient = new AmazonDynamoDBClient(credentials, RegionEndpoint.USWest1);
    }

    public void SaveWorkoutHistory(WorkoutSession session, Action<bool> callback)
    {
        try
        {
            string userId = _authManager.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("No user ID found. Cannot save workout history.");
                callback?.Invoke(false);
                return;
            }

            var workoutHistory = new WorkoutHistory
            {
                UserId = userId,
                WorkoutId = $"{userId}_{DateTime.Now.Ticks}",
                Date = session.sessionDate,
                ExerciseType = session.exerciseType,
                TotalReps = session.totalReps,
                TotalDuration = session.totalDuration,
                AverageFormScore = session.sets.Count > 0 ?
                    session.sets.Average(s => s.averageFormScore) : 0,
                Sets = session.sets
            };

            var request = new PutItemRequest
            {
                TableName = WORKOUT_TABLE_NAME,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = workoutHistory.UserId }},
                    { "WorkoutId", new AttributeValue { S = workoutHistory.WorkoutId }},
                    { "Date", new AttributeValue { S = workoutHistory.Date.ToString("o") }},
                    { "ExerciseType", new AttributeValue { S = workoutHistory.ExerciseType }},
                    { "TotalReps", new AttributeValue { N = workoutHistory.TotalReps.ToString() }},
                    { "TotalDuration", new AttributeValue { N = workoutHistory.TotalDuration.ToString() }},
                    { "AverageFormScore", new AttributeValue { N = workoutHistory.AverageFormScore.ToString() }},
                    { "Sets", new AttributeValue { S = JsonConvert.SerializeObject(workoutHistory.Sets) }}
                }
            };

            _dynamoDbClient.PutItemAsync(request, (result) =>
            {
                if (result.Exception != null)
                {
                    Debug.LogError($"Error saving workout history: {result.Exception.Message}");
                    callback?.Invoke(false);
                    return;
                }

                Debug.Log($"Successfully saved workout history for user {userId}");
                callback?.Invoke(true);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving workout history: {e.Message}");
            callback?.Invoke(false);
        }
    }

    public void GetUserWorkoutHistory(string userId, Action<List<WorkoutHistory>> callback, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = WORKOUT_TABLE_NAME,
                KeyConditionExpression = "UserId = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":userId", new AttributeValue { S = userId }}
                }
            };

            if (startDate.HasValue && endDate.HasValue)
            {
                request.FilterExpression = "Date BETWEEN :startDate AND :endDate";
                request.ExpressionAttributeValues.Add(":startDate", new AttributeValue { S = startDate.Value.ToString("o") });
                request.ExpressionAttributeValues.Add(":endDate", new AttributeValue { S = endDate.Value.ToString("o") });
            }

            _dynamoDbClient.QueryAsync(request, (result) =>
            {
                if (result.Exception != null)
                {
                    Debug.LogError($"Error retrieving workout history: {result.Exception.Message}");
                    callback?.Invoke(new List<WorkoutHistory>());
                    return;
                }

                var workoutHistory = new List<WorkoutHistory>();

                foreach (var item in result.Response.Items)
                {
                    workoutHistory.Add(new WorkoutHistory
                    {
                        UserId = item["UserId"].S,
                        WorkoutId = item["WorkoutId"].S,
                        Date = DateTime.Parse(item["Date"].S),
                        ExerciseType = item["ExerciseType"].S,
                        TotalReps = int.Parse(item["TotalReps"].N),
                        TotalDuration = float.Parse(item["TotalDuration"].N),
                        AverageFormScore = float.Parse(item["AverageFormScore"].N),
                        Sets = JsonConvert.DeserializeObject<List<WorkoutSet>>(item["Sets"].S)
                    });
                }

                callback?.Invoke(workoutHistory);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error setting up workout history query: {e.Message}");
            callback?.Invoke(new List<WorkoutHistory>());
        }
    }
}