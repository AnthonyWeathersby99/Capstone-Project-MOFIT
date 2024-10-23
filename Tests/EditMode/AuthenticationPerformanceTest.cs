using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Diagnostics;
using System.IO;

public class AuthenticationPerformanceTest
{
    private AuthenticationManager _authManager;
    private MockFileManager _mockFileManager;
    private MockSaveDataManager _mockSaveDataManager;
    private const int NUM_ITERATIONS = 10;
    private const float MAX_ACCEPTABLE_TIME_MS = 1000; // 1 second
    private const string TEST_AUTH_CODE = "test-valid-auth-code";
    private const string TEST_AUTH_URL = "https://dzi3ny7huab3j.cloudfront.net/?code=test-valid-auth-code";

    [SetUp]
    public void SetUp()
    {
        GameObject authManagerObject = new GameObject();
        _authManager = authManagerObject.AddComponent<AuthenticationManager>();

        // Ensure CachePath is properly set for testing
        AuthenticationManager.CachePath = Application.persistentDataPath;

        // Set up test environment
        _authManager.SetTestEnvironment(true);

        // Setup mocks
        _mockFileManager = new MockFileManager();
        _mockSaveDataManager = new MockSaveDataManager();

        // Inject mocks into AuthenticationManager
        _authManager.SetFileManager(_mockFileManager);
        _authManager.SetSaveDataManager(_mockSaveDataManager);

        // Set mock valid auth code
        _authManager.SetMockValidAuthCode(TEST_AUTH_CODE);
    }

    [UnityTest]
    public IEnumerator TestAuthenticationPerformance()
    {
        Stopwatch stopwatch = new Stopwatch();
        long totalLoginTime = 0;
        long totalRefreshTime = 0;
        int loginSuccessCount = 0;
        int refreshSuccessCount = 0;

        // Test Login Performance
        for (int i = 0; i < NUM_ITERATIONS; i++)
        {
            stopwatch.Reset();
            stopwatch.Start();

            var loginTask = _authManager.ExchangeAuthCodeForAccessToken(TEST_AUTH_URL);
            yield return new WaitUntil(() => loginTask.IsCompleted);

            stopwatch.Stop();
            totalLoginTime += stopwatch.ElapsedMilliseconds;

            if (loginTask.Result)
                loginSuccessCount++;

            yield return null; // Give Unity a frame to process
        }

        // Test Token Refresh Performance
        for (int i = 0; i < NUM_ITERATIONS; i++)
        {
            stopwatch.Reset();
            stopwatch.Start();

            var refreshTask = _authManager.CallRefreshTokenEndpoint();
            yield return new WaitUntil(() => refreshTask.IsCompleted);

            stopwatch.Stop();
            totalRefreshTime += stopwatch.ElapsedMilliseconds;

            if (refreshTask.Result)
                refreshSuccessCount++;

            yield return null;
        }

        // Calculate averages
        long averageLoginTime = totalLoginTime / NUM_ITERATIONS;
        long averageRefreshTime = totalRefreshTime / NUM_ITERATIONS;

        // Log results
        UnityEngine.Debug.Log("Authentication Performance Results:");
        UnityEngine.Debug.Log($"Average login time: {averageLoginTime}ms");
        UnityEngine.Debug.Log($"Login success rate: {(float)loginSuccessCount / NUM_ITERATIONS * 100}%");
        UnityEngine.Debug.Log($"Average refresh time: {averageRefreshTime}ms");
        UnityEngine.Debug.Log($"Refresh success rate: {(float)refreshSuccessCount / NUM_ITERATIONS * 100}%");

        // Verify session cache updates
        UserSessionCache sessionCache = _mockSaveDataManager.GetLastSavedData() as UserSessionCache;
        Assert.IsNotNull(sessionCache, "Session cache should be saved after operations");
        Assert.IsNotNull(sessionCache.getIdToken(), "ID token should be present in session cache");

        // Assert performance metrics
        Assert.That(averageLoginTime, Is.LessThan(MAX_ACCEPTABLE_TIME_MS),
            $"Average login time ({averageLoginTime}ms) exceeds maximum acceptable time ({MAX_ACCEPTABLE_TIME_MS}ms)");
        Assert.That(averageRefreshTime, Is.LessThan(MAX_ACCEPTABLE_TIME_MS),
            $"Average refresh time ({averageRefreshTime}ms) exceeds maximum acceptable time ({MAX_ACCEPTABLE_TIME_MS}ms)");

        // Assert success rates
        Assert.That((float)loginSuccessCount / NUM_ITERATIONS, Is.GreaterThan(0.95f),
            "Login success rate should be above 95%");
        Assert.That((float)refreshSuccessCount / NUM_ITERATIONS, Is.GreaterThan(0.95f),
            "Refresh success rate should be above 95%");
    }

    [TearDown]
    public void TearDown()
    {
        if (_authManager != null)
        {
            Object.DestroyImmediate(_authManager.gameObject);
        }
    }

    public interface IFileManager
    {
        bool WriteToFile(string fileName, string contents);
        bool LoadFromFile(string fileName, out string result);
    }

    public interface ISaveDataManager
    {
        void SaveJsonData(ISaveable saveable);
        void LoadJsonData(ISaveable saveable);
    }

    public interface IAuthenticationManager
    {
        void SetFileManager(IFileManager fileManager);
        void SetSaveDataManager(ISaveDataManager saveDataManager);
        void SetTestEnvironment(bool isTest);
        void SetMockValidAuthCode(string validCode);
    }
}

