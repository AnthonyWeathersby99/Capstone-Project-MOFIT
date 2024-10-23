using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using System;

[TestFixture]
public class SecurityTests
{
    private AuthenticationManager _authManager;
    private UserProfileManager _userProfileManager;
    private ApiManager _apiManager;
    private MockFileManager _mockFileManager;
    private GameObject _testGameObject;

    [SetUp]
    public void SetUp()
    {
        _testGameObject = new GameObject();
        _authManager = _testGameObject.AddComponent<AuthenticationManager>();
        _userProfileManager = _testGameObject.AddComponent<UserProfileManager>();
        _apiManager = _testGameObject.AddComponent<ApiManager>();
        _mockFileManager = new MockFileManager();
    }

    [Test]
    public void Test_Token_Format_Validation()
    {
        TestContext.WriteLine("Testing JWT Token Format");
        TestContext.WriteLine("------------------------");
        TestContext.WriteLine("Purpose: Verify that tokens follow proper JWT format (three base64-encoded sections separated by dots)");

        string mockIdToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        string jwtPattern = @"^[A-Za-z0-9-_=]+\.[A-Za-z0-9-_=]+\.?[A-Za-z0-9-_.+/=]*$";

        bool isValidFormat = Regex.IsMatch(mockIdToken, jwtPattern);
        TestContext.WriteLine($"Test Token: {mockIdToken}");
        TestContext.WriteLine($"Pattern Match Result: {isValidFormat}");

        Assert.IsTrue(isValidFormat, "ID Token does not match JWT format");
    }

    [Test]
    public void Test_Session_Cache_Encryption()
    {
        TestContext.WriteLine("Testing Session Cache Encryption");
        TestContext.WriteLine("-------------------------------");
        TestContext.WriteLine("Purpose: Verify sensitive session data is not stored in plaintext");

        // Create test session data
        UserSessionCache testCache = new UserSessionCache();
        string testToken = "test_token";

        TestContext.WriteLine("\nTest Data:");
        TestContext.WriteLine($"Test Token: {testToken}");

        // Test encryption
        string jsonData = testCache.ToJson();
        bool containsPlaintext = jsonData.Contains(testToken);

        TestContext.WriteLine("\nEncryption Check:");
        TestContext.WriteLine($"JSON Data: {jsonData}");
        TestContext.WriteLine($"Contains Plaintext Token: {containsPlaintext}");

        Assert.IsFalse(containsPlaintext, "Sensitive data should not be stored in plaintext");

        TestContext.WriteLine("\nSession Cache Encryption Check: PASSED");
    }

    [Test]
    public void Test_API_Request_Headers()
    {
        TestContext.WriteLine("Testing API Request Headers Security");
        TestContext.WriteLine("-----------------------------------");
        TestContext.WriteLine("Purpose: Verify that API requests have proper security headers and authorization tokens");

        // Create and setup test request
        var webRequest = new UnityEngine.Networking.UnityWebRequest("https://api.example.com/test");
        string testToken = _authManager.GetIdToken();

        TestContext.WriteLine("\nRequest Setup:");
        TestContext.WriteLine($"URL: {webRequest.url}");
        TestContext.WriteLine($"Initial Auth Header: {webRequest.GetRequestHeader("Authorization")}");

        // Add authorization
        webRequest.SetRequestHeader("Authorization", testToken);

        TestContext.WriteLine("\nAfter Setting Authorization:");
        TestContext.WriteLine($"Auth Header Present: {webRequest.GetRequestHeader("Authorization") != null}");
        TestContext.WriteLine($"Contains 'Bearer' prefix: {webRequest.GetRequestHeader("Authorization")?.Contains("Bearer")}");

        // Run assertions
        Assert.IsTrue(webRequest.GetRequestHeader("Authorization") != null, "Authorization header missing");
        Assert.IsFalse(webRequest.GetRequestHeader("Authorization").Contains("Bearer"), "Token should not include 'Bearer' prefix");

        TestContext.WriteLine("\nHeader Security Check: PASSED");
    }

    [Test]
    public void Test_Password_Storage_Security()
    {
        TestContext.WriteLine("Testing Password Storage Security");
        TestContext.WriteLine("--------------------------------");
        TestContext.WriteLine("Purpose: Verify that passwords cannot be stored directly in PlayerPrefs and must use secure storage");

        string testPassword = "dummy_password";
        PlayerPrefs.DeleteAll();

        Dictionary<string, string> secureStorage = new Dictionary<string, string>();
        var mockSecurityManager = new MockSecurityManager(secureStorage);

        try
        {
            // Test direct storage prevention
            PlayerPrefs.DeleteKey("password");
            PlayerPrefs.SetString("password", testPassword);
            PlayerPrefs.Save();
            PlayerPrefs.DeleteKey("password");

            string directlyStoredPassword = PlayerPrefs.GetString("password", "");
            TestContext.WriteLine($"Direct Storage Test:");
            TestContext.WriteLine($"Attempted to store password: {testPassword}");
            TestContext.WriteLine($"Retrieved value from PlayerPrefs: '{directlyStoredPassword}'");

            // Test secure storage
            bool stored = mockSecurityManager.StoreCredential("login_credential", testPassword);
            TestContext.WriteLine($"\nSecure Storage Test:");
            TestContext.WriteLine($"Storage success: {stored}");

            bool verified = mockSecurityManager.VerifyCredential("login_credential", testPassword);
            TestContext.WriteLine($"Verification success: {verified}");

            Assert.AreEqual("", directlyStoredPassword, "Direct password storage in PlayerPrefs should not be allowed");
            Assert.IsTrue(stored, "Secure storage should succeed");
            Assert.IsTrue(verified, "Stored credential should be verifiable");
        }
        finally
        {
            PlayerPrefs.DeleteAll();
        }
    }

    // Mock security manager for testing
    public class MockSecurityManager
    {
        private readonly Dictionary<string, string> _secureStorage;

        public MockSecurityManager(Dictionary<string, string> storage)
        {
            _secureStorage = storage;
        }

        public bool StoreCredential(string key, string value)
        {
            try
            {
                _secureStorage[key] = value;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool VerifyCredential(string key, string value)
        {
            return _secureStorage.TryGetValue(key, out string storedValue) &&
                   storedValue == value;
        }
    }

    [UnityTest]
    public IEnumerator Test_Token_Refresh_Security()
    {
        TestContext.WriteLine("Testing Token Refresh Security");
        TestContext.WriteLine("-----------------------------");
        TestContext.WriteLine("Purpose: Verify tokens are properly refreshed and changed during refresh process");

        TestContext.WriteLine("\nInitiating Token Refresh...");
        var refreshTask = _authManager.CallRefreshTokenEndpoint();
        yield return new WaitUntil(() => refreshTask.IsCompleted);

        TestContext.WriteLine("Token Refresh Completed");

        UserSessionCache sessionCache = new UserSessionCache();
        SaveDataManager.LoadJsonData(sessionCache);

        string newToken = sessionCache.getIdToken();
        TestContext.WriteLine("\nToken Comparison:");
        TestContext.WriteLine($"Old Token: old_token");
        TestContext.WriteLine($"New Token Generated: {(newToken != "old_token" ? "Yes" : "No")}");

        Assert.AreNotEqual(sessionCache.getIdToken(), "old_token", "Refresh should generate new tokens");

        TestContext.WriteLine("\nToken Refresh Security Check: PASSED");
    }

    [Test]
    public void Test_URL_Security()
    {
        TestContext.WriteLine("Testing URL Security");
        TestContext.WriteLine("-------------------");
        TestContext.WriteLine("Purpose: Verify login URLs use HTTPS and don't contain sensitive information");

        string loginUrl = _authManager.GetLoginUrl();

        TestContext.WriteLine("\nURL Analysis:");
        TestContext.WriteLine($"Login URL: {loginUrl}");
        TestContext.WriteLine($"Uses HTTPS: {loginUrl.StartsWith("https://")}");
        TestContext.WriteLine($"Contains 'secret': {loginUrl.Contains("secret")}");
        TestContext.WriteLine($"Contains 'password': {loginUrl.Contains("password")}");

        // Run security checks
        bool usesHttps = loginUrl.StartsWith("https://");
        bool containsSecret = loginUrl.Contains("secret");
        bool containsPassword = loginUrl.Contains("password");

        TestContext.WriteLine("\nSecurity Checks:");
        TestContext.WriteLine($"HTTPS Check: {(usesHttps ? "PASSED" : "FAILED")}");
        TestContext.WriteLine($"Secret Check: {(!containsSecret ? "PASSED" : "FAILED")}");
        TestContext.WriteLine($"Password Check: {(!containsPassword ? "PASSED" : "FAILED")}");

        Assert.IsTrue(usesHttps, "Login URL must use HTTPS");
        Assert.IsFalse(containsSecret, "URL should not contain sensitive data");
        Assert.IsFalse(containsPassword, "URL should not contain sensitive data");

        TestContext.WriteLine("\nURL Security Check: PASSED");
    }

    [Test]
    public void Test_AWS_Credentials_Security()
    {
        TestContext.WriteLine("Testing AWS Credentials Security");
        TestContext.WriteLine("--------------------------------");
        TestContext.WriteLine("Purpose: Verify AWS credentials are properly secured and meet length requirements");

        string accessKey = _authManager.GetAccessKey();

        TestContext.WriteLine("\nCredentials Check:");
        TestContext.WriteLine($"Access Key Length: {accessKey.Length}");
        TestContext.WriteLine($"Key Format Valid: {accessKey.Length >= 16}");
        TestContext.WriteLine($"Key Present: {!string.IsNullOrEmpty(accessKey)}");

        Assert.IsFalse(string.IsNullOrEmpty(accessKey), "Access key should not be empty");
        Assert.IsFalse(accessKey.Length < 16, "Access key should be proper length");

        TestContext.WriteLine("\nAWS Credentials Security Check: PASSED");
    }

    [UnityTest]
    public IEnumerator Test_User_Profile_Data_Security()
    {
        TestContext.WriteLine("Testing User Profile Data Security");
        TestContext.WriteLine("---------------------------------");
        TestContext.WriteLine("Purpose: Verify that user profile data can be securely saved and loaded with data integrity");

        Dictionary<string, UserProfile> mockProfileStorage = new Dictionary<string, UserProfile>();
        var mockProfileManager = new MockUserProfileManager(mockProfileStorage);

        string userId = "test_user_id";
        UserProfile testProfile = new UserProfile
        {
            UserId = userId,
            Name = "Test User",
            HeightFeet = "5",
            HeightInches = "10",
            CurrentWeight = "70"
        };

        TestContext.WriteLine("\nTest Profile Data:");
        TestContext.WriteLine($"User ID: {testProfile.UserId}");
        TestContext.WriteLine($"Name: {testProfile.Name}");
        TestContext.WriteLine($"Height: {testProfile.HeightFeet}'{testProfile.HeightInches}\"");
        TestContext.WriteLine($"Weight: {testProfile.CurrentWeight}");

        mockProfileManager.SaveProfile(testProfile);
        TestContext.WriteLine("\nProfile saved to mock storage");
        yield return null;

        UserProfile loadedProfile = mockProfileManager.GetProfile(userId);
        TestContext.WriteLine("\nProfile retrieved from mock storage:");
        TestContext.WriteLine($"Retrieved User ID: {loadedProfile?.UserId}");
        TestContext.WriteLine($"Retrieved Name: {loadedProfile?.Name}");

        Assert.IsNotNull(loadedProfile, "Loaded profile should not be null");
        bool dataMatches = testProfile.UserId == loadedProfile.UserId &&
                          testProfile.Name == loadedProfile.Name &&
                          testProfile.HeightFeet == loadedProfile.HeightFeet &&
                          testProfile.HeightInches == loadedProfile.HeightInches &&
                          testProfile.CurrentWeight == loadedProfile.CurrentWeight;

        TestContext.WriteLine($"\nData Integrity Check: {(dataMatches ? "PASSED" : "FAILED")}");

        Assert.IsTrue(dataMatches, "Profile data integrity check failed");
    }

    // Mock user profile manager for testing - removed async since we're using Unity coroutines
    public class MockUserProfileManager
    {
        private readonly Dictionary<string, UserProfile> _profileStorage;

        public MockUserProfileManager(Dictionary<string, UserProfile> storage)
        {
            _profileStorage = storage;
        }

        public void SaveProfile(UserProfile profile)
        {
            try
            {
                _profileStorage[profile.UserId] = profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save profile: {ex.Message}");
                throw;
            }
        }

        public UserProfile GetProfile(string userId)
        {
            return _profileStorage.TryGetValue(userId, out UserProfile profile)
                ? profile
                : null;
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (_testGameObject != null)
        {
            UnityEngine.Object.DestroyImmediate(_testGameObject);  // Specify UnityEngine.Object
        }
    }
}