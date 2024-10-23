using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;

[TestFixture]
public class AuthenticationManagerTests
{
    private AuthenticationManager _authManager;
    private MockFileManager _mockFileManager;
    private MockSaveDataManager _mockSaveDataManager;


    [SetUp]
    public void SetUp()
    {
        GameObject authManagerObject = new GameObject();
        _authManager = authManagerObject.AddComponent<AuthenticationManager>();
        _authManager.SetTestEnvironment(true);

        // Setup mocks
        _mockFileManager = new MockFileManager();
        _mockSaveDataManager = new MockSaveDataManager();

        // Inject mocks into AuthenticationManager
        _authManager.SetFileManager(_mockFileManager);
        _authManager.SetSaveDataManager(_mockSaveDataManager);

        // Set a mock valid auth code
        _authManager.SetMockValidAuthCode("test-valid-auth-code");
    }

    [UnityTest]
    public IEnumerator Login_User_WithValidAuthCode_ShouldReturnTrue()
    {
        // Use a known valid auth code for testing
        string validAuthCode = "test-valid-auth-code";
        string validAuthCodeUrl = $"https://dzi3ny7huab3j.cloudfront.net/?code={validAuthCode}";

        // Set up the mock to expect this auth code
        _authManager.SetMockValidAuthCode(validAuthCode);

        var loginTask = _authManager.ExchangeAuthCodeForAccessToken(validAuthCodeUrl);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError($"Exception occurred: {loginTask.Exception}");
        }

        Assert.IsTrue(loginTask.Result, "Expected login with valid authorization code to succeed.");

        // Verify that session cache was updated
        UserSessionCache sessionCache = _mockSaveDataManager.GetLastSavedData() as UserSessionCache;
        Assert.IsNotNull(sessionCache, "Session cache should be saved after successful login");
        Assert.IsNotNull(sessionCache.getIdToken(), "ID token should be present in session cache");
    }

    [UnityTest]
    public IEnumerator Login_User_WithInvalidAuthCode_ShouldReturnFalse()
    {
        string invalidAuthCodeUrl = "https://dzi3ny7huab3j.cloudfront.net/?code=invalid-code";

        var loginTask = _authManager.ExchangeAuthCodeForAccessToken(invalidAuthCodeUrl);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        Assert.IsFalse(loginTask.Result, "Expected login with invalid authorization code to fail.");
    }

    private string GenerateValidAuthCode()
    {
        // Implement this method to generate a valid auth code for testing
        return "valid-auth-code";
    }

    private string GenerateValidIdToken()
    {
        // Implement this method to generate a valid ID token for testing
        return "valid.id.token";
    }

    [TearDown]
    public void TearDown()
    {
        if (_authManager != null)
        {
            Object.DestroyImmediate(_authManager.gameObject);
        }
    }
}

public class MockFileManager : IFileManager
{
    public bool WriteToFile(string fileName, string contents)
    {
        // Simulate successful file write
        return true;
    }

    public bool LoadFromFile(string fileName, out string contents)
    {
        // Simulate successful file read
        contents = "{}";
        return true;
    }
}

public class MockSaveDataManager : ISaveDataManager
{
    private ISaveable _lastSavedData;

    public void SaveJsonData(ISaveable saveable)
    {
        _lastSavedData = saveable;
    }

    public ISaveable GetLastSavedData()
    {
        return _lastSavedData;
    }
}


public class AuthCodeGenerator
{
    public static async Task<string> GenerateAuthCode(AuthenticationManager authManager)
    {
        string loginUrl = authManager.GetLoginUrl();

        // Open the login URL in a browser
        Application.OpenURL(loginUrl);

        // Wait for user to complete login and get redirected
        await Task.Delay(5000); // Wait 5 seconds

        return "simulated-auth-code";
    }
}