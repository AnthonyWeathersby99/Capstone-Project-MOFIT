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
using System.Linq;
using System.Threading;

public class WorkoutAWSManager : MonoBehaviour
{
    private const string IDENTITY_POOL_ID = "us-west-1:c0888cc2-c5e4-4e2d-a9be-399aa2c62429";
    private const string DYNAMODB_TABLE_NAME = "MOFITWorkouts";
    private AmazonDynamoDBClient _dynamoDbClient;
    private AuthenticationManager _authManager;

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

    public async Task UploadWorkoutSession(WorkoutSession session)
    {
        string userId = _authManager.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID not found. User must be authenticated.");
            return;
        }

        if (string.IsNullOrEmpty(session.workoutId))
        {
            session.workoutId = $"{userId}-{DateTime.Now.Ticks}";
        }

        var item = new Dictionary<string, AttributeValue>
    {
        { "UserId", new AttributeValue { S = userId } },
        { "workoutId", new AttributeValue { S = session.workoutId } },
        { "completedSetPairs", new AttributeValue { N = session.completedSetPairs.ToString() } },
        { "exerciseType", new AttributeValue { S = session.exerciseType ?? "Unknown" } },
        { "sessionDate", new AttributeValue { S = session.sessionDate.ToString("o") } },
        { "sets", new AttributeValue { S = JsonUtility.ToJson(session.sets) } },
        { "timestamp", new AttributeValue { S = DateTime.UtcNow.ToString("o") } },
        { "totalDuration", new AttributeValue { N = session.totalDuration.ToString() } },
        { "totalReps", new AttributeValue { N = session.totalReps.ToString() } }
    };

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
                return;
            }

            var response = result.Response;
            var workouts = new List<WorkoutSession>();
            foreach (var item in response.Items)
            {
                var session = new WorkoutSession
                {
                    workoutId = item.ContainsKey("WorkoutId") ? item["WorkoutId"].S : string.Empty,
                    workoutType = item.ContainsKey("WorkoutType") ? item["WorkoutType"].S : string.Empty,
                    Duration = item.ContainsKey("Duration") ? int.Parse(item["Duration"].S) : 0,
                };
                workouts.Add(session);
            }
            Debug.Log("Workout history retrieved successfully.");
        });
    }
}
