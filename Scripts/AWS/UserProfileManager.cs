using UnityEngine;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

public class UserProfileManager : MonoBehaviour
{
    private const string IDENTITY_POOL_ID = "us-west-1:c0888cc2-c5e4-4e2d-a9be-399aa2c62429";
    private const string DYNAMODB_TABLE_NAME = "MOFITUserProfiles";
    private AmazonDynamoDBClient _dynamoDbClient;

    public void Awake()
    {
        // Initialize Unity AWS SDK
        UnityInitializer.AttachToGameObject(this.gameObject);

        // Use Cognito for obtaining credentials
        CognitoAWSCredentials credentials = new CognitoAWSCredentials(
            IDENTITY_POOL_ID,
            RegionEndpoint.USWest1
        );

        // Initialize DynamoDB client
        _dynamoDbClient = new AmazonDynamoDBClient(credentials, RegionEndpoint.USWest1);
        Debug.Log("DynamoDB client initialized successfully.");
    }

    public Task<UserProfile> GetUserProfile(string userId)
    {
        Debug.Log($"Starting GetUserProfile for userId: {userId}");
        var tcs = new TaskCompletionSource<UserProfile>();

        try
        {
            var request = new GetItemRequest
            {
                TableName = DYNAMODB_TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userId } }
                }
            };

            _dynamoDbClient.GetItemAsync(request, (result) =>
            {
                if (result.Exception != null)
                {
                    Debug.LogError($"Error fetching profile: {result.Exception.Message}");
                    tcs.SetException(result.Exception);
                    return;
                }

                var response = result.Response;
                Debug.Log($"GetItemAsync response received. Item count: {response.Item?.Count ?? 0}");

                if (response.Item == null || response.Item.Count == 0)
                {
                    Debug.Log($"User profile not found for UserId: {userId}. Creating new profile.");
                    CreateNewUserProfile(userId).ContinueWith(t => tcs.SetResult(t.Result));
                    return;
                }

                UserProfile profile = new UserProfile
                {
                    UserId = userId,
                    Name = GetStringValue(response.Item, "Name"),
                    HeightFeet = GetStringValue(response.Item, "HeightFeet"),
                    HeightInches = GetStringValue(response.Item, "HeightInches"),
                    CurrentWeight = GetStringValue(response.Item, "CurrentWeight"),
                    Sex = GetStringValue(response.Item, "Sex"),
                    BirthdayDay = GetStringValue(response.Item, "BirthdayDay"),
                    BirthdayMonth = GetStringValue(response.Item, "BirthdayMonth"),
                    BirthdayYear = GetStringValue(response.Item, "BirthdayYear"),
                    StartingWeight = GetStringValue(response.Item, "StartingWeight"),
                    GoalWeight = GetStringValue(response.Item, "GoalWeight")
                };

                Debug.Log($"Profile retrieved: {JsonUtility.ToJson(profile)}");
                tcs.SetResult(profile);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initiating profile fetch: {e.Message}");
            tcs.SetException(e);
        }

        return tcs.Task;
    }

    private string GetStringValue(Dictionary<string, AttributeValue> item, string key)
    {
        return item.ContainsKey(key) ? item[key].S : "";
    }

    private Task<UserProfile> CreateNewUserProfile(string userId)
    {
        var newProfile = new UserProfile
        {
            UserId = userId
        };

        return UpdateUserProfile(newProfile).ContinueWith(t =>
        {
            if (t.Result)
            {
                Debug.Log($"New user profile created for UserId: {userId}");
                return newProfile;
            }
            else
            {
                throw new Exception("Failed to create new user profile");
            }
        });
    }

    public Task<bool> UpdateUserProfile(UserProfile profile)
    {
        var tcs = new TaskCompletionSource<bool>();

        try
        {
            var item = new Dictionary<string, AttributeValue>();
            var expressionAttributeNames = new Dictionary<string, string>();
            var expressionAttributeValues = new Dictionary<string, AttributeValue>();
            var updateExpression = new List<string>();

            // UserId is required
            item["UserId"] = new AttributeValue { S = profile.UserId };

            // Add other attributes only if they have valid values
            AddAttributeIfNotEmpty(profile.Name, "Name", "#N", ":n", expressionAttributeNames, expressionAttributeValues, updateExpression);
            AddAttributeIfNotEmpty(profile.HeightFeet, "HeightFeet", "#HF", ":hf", expressionAttributeNames, expressionAttributeValues, updateExpression);
            AddAttributeIfNotEmpty(profile.HeightInches, "HeightInches", "#HI", ":hi", expressionAttributeNames, expressionAttributeValues, updateExpression);
            AddAttributeIfNotEmpty(profile.CurrentWeight, "CurrentWeight", "#CW", ":cw", expressionAttributeNames, expressionAttributeValues, updateExpression);
            AddAttributeIfNotEmpty(profile.Sex, "Sex", "#S", ":s", expressionAttributeNames, expressionAttributeValues, updateExpression);
            AddAttributeIfNotEmpty(profile.BirthdayDay, "BirthdayDay", "#BD", ":bd", expressionAttributeNames, expressionAttributeValues, updateExpression);
            AddAttributeIfNotEmpty(profile.BirthdayMonth, "BirthdayMonth", "#BM", ":bm", expressionAttributeNames, expressionAttributeValues, updateExpression);
            AddAttributeIfNotEmpty(profile.BirthdayYear, "BirthdayYear", "#BY", ":by", expressionAttributeNames, expressionAttributeValues, updateExpression);
            AddAttributeIfNotEmpty(profile.StartingWeight, "StartingWeight", "#SW", ":sw", expressionAttributeNames, expressionAttributeValues, updateExpression);
            AddAttributeIfNotEmpty(profile.GoalWeight, "GoalWeight", "#GW", ":gw", expressionAttributeNames, expressionAttributeValues, updateExpression);

            // If no fields to update, return success
            if (updateExpression.Count == 0)
            {
                Debug.Log("No fields to update");
                tcs.SetResult(true);
                return tcs.Task;
            }

            var request = new UpdateItemRequest
            {
                TableName = DYNAMODB_TABLE_NAME,
                Key = new Dictionary<string, AttributeValue> { { "UserId", new AttributeValue { S = profile.UserId } } },
                UpdateExpression = "SET " + string.Join(", ", updateExpression),
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = expressionAttributeValues
            };

            Debug.Log($"UpdateItemRequest: {JsonUtility.ToJson(request)}");

            _dynamoDbClient.UpdateItemAsync(request, (result) =>
            {
                if (result.Exception != null)
                {
                    Debug.LogError($"Error updating profile: {result.Exception.Message}");
                    tcs.SetResult(false);
                    return;
                }

                Debug.Log("Profile updated successfully");
                tcs.SetResult(true);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initiating profile update: {e.Message}");
            tcs.SetResult(false);
        }

        return tcs.Task;
    }

    private void AddAttributeIfNotEmpty(string value, string attributeName, string expressionName, string expressionValue,
        Dictionary<string, string> expressionAttributeNames, Dictionary<string, AttributeValue> expressionAttributeValues, List<string> updateExpression)
    {
        if (!string.IsNullOrEmpty(value))
        {
            expressionAttributeNames[expressionName] = attributeName;
            expressionAttributeValues[expressionValue] = new AttributeValue { S = value };
            updateExpression.Add($"{expressionName} = {expressionValue}");
        }
    }
}

[System.Serializable]
public class UserProfile
{
    public string UserId;
    public string Name;
    public string HeightFeet;
    public string HeightInches;
    public string CurrentWeight;
    public string Sex;
    public string BirthdayDay;
    public string BirthdayMonth;
    public string BirthdayYear;
    public string StartingWeight;
    public string GoalWeight;
      
    public UserProfile()
    {
        // Initialize all string fields to empty strings
        UserId = Name = HeightFeet = HeightInches = CurrentWeight = Sex =
        BirthdayDay = BirthdayMonth = BirthdayYear = StartingWeight = GoalWeight = "";
    }
}