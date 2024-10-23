using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class UserProfilePerformanceTest
{
    private MockUserProfileManager _profileManager;
    private const int NUM_ITERATIONS = 50;
    private const float MAX_ACCEPTABLE_TIME_MS = 500;

    // Simple mock manager that doesn't depend on AWS
    private class MockUserProfileManager : MonoBehaviour
    {
        private Dictionary<string, UserProfile> _profileStorage = new Dictionary<string, UserProfile>();

        public async Task<UserProfile> GetUserProfile(string userId)
        {
            // Simulate some processing time
            await Task.Delay(5);

            if (_profileStorage.TryGetValue(userId, out UserProfile profile))
            {
                return profile;
            }
            return new UserProfile { UserId = userId };
        }

        public async Task<bool> UpdateUserProfile(UserProfile profile)
        {
            try
            {
                // Simulate some processing time
                await Task.Delay(5);

                _profileStorage[profile.UserId] = profile;
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Mock Update Error: {ex.Message}");
                return false;
            }
        }
    }

    [SetUp]
    public void SetUp()
    {
        GameObject profileManagerObject = new GameObject();
        _profileManager = profileManagerObject.AddComponent<MockUserProfileManager>();
    }

    [UnityTest]
    public IEnumerator TestUserProfileLoadSavePerformance()
    {
        Stopwatch stopwatch = new Stopwatch();
        long totalLoadTime = 0;
        long totalSaveTime = 0;
        int successfulSaves = 0;
        int successfulLoads = 0;

        for (int i = 0; i < NUM_ITERATIONS; i++)
        {
            string userId = $"test_user_{i}";
            UserProfile testProfile = CreateTestProfile(userId);

            // Test Save Performance
            stopwatch.Reset();
            stopwatch.Start();
            var saveTask = _profileManager.UpdateUserProfile(testProfile);
            yield return new WaitUntil(() => saveTask.IsCompleted);
            stopwatch.Stop();

            if (saveTask.Result)
            {
                totalSaveTime += stopwatch.ElapsedMilliseconds;
                successfulSaves++;
            }

            // Test Load Performance
            stopwatch.Reset();
            stopwatch.Start();
            var loadTask = _profileManager.GetUserProfile(userId);
            yield return new WaitUntil(() => loadTask.IsCompleted);
            stopwatch.Stop();

            if (loadTask.Result != null)
            {
                totalLoadTime += stopwatch.ElapsedMilliseconds;
                successfulLoads++;

                // Verify loaded profile
                Assert.AreEqual(testProfile.Name, loadTask.Result.Name,
                    $"Loaded profile does not match saved profile for user {userId}");
            }

            yield return null;
        }

        // Calculate averages only if we had successful operations
        float averageLoadTime = successfulLoads > 0 ? totalLoadTime / (float)successfulLoads : 0;
        float averageSaveTime = successfulSaves > 0 ? totalSaveTime / (float)successfulSaves : 0;

        UnityEngine.Debug.Log($"Profile Performance Results:");
        UnityEngine.Debug.Log($"Successful saves: {successfulSaves}/{NUM_ITERATIONS}");
        UnityEngine.Debug.Log($"Successful loads: {successfulLoads}/{NUM_ITERATIONS}");
        UnityEngine.Debug.Log($"Average save time: {averageSaveTime}ms");
        UnityEngine.Debug.Log($"Average load time: {averageLoadTime}ms");

        // Assert performance metrics
        Assert.That(successfulSaves, Is.EqualTo(NUM_ITERATIONS), "Not all save operations were successful");
        Assert.That(successfulLoads, Is.EqualTo(NUM_ITERATIONS), "Not all load operations were successful");
        Assert.That(averageSaveTime, Is.LessThan(MAX_ACCEPTABLE_TIME_MS),
            $"Average save time ({averageSaveTime}ms) exceeds maximum acceptable time ({MAX_ACCEPTABLE_TIME_MS}ms)");
        Assert.That(averageLoadTime, Is.LessThan(MAX_ACCEPTABLE_TIME_MS),
            $"Average load time ({averageLoadTime}ms) exceeds maximum acceptable time ({MAX_ACCEPTABLE_TIME_MS}ms)");
    }

    private UserProfile CreateTestProfile(string userId)
    {
        return new UserProfile
        {
            UserId = userId,
            Name = "Test User",
            HeightFeet = "5",
            HeightInches = "10",
            CurrentWeight = "70",
            Sex = "Male",
            BirthdayDay = "1",
            BirthdayMonth = "1",
            BirthdayYear = "1990",
            StartingWeight = "75",
            GoalWeight = "65"
        };
    }

    [TearDown]
    public void TearDown()
    {
        if (_profileManager != null)
        {
            UnityEngine.Object.DestroyImmediate(_profileManager.gameObject);
        }
    }
}