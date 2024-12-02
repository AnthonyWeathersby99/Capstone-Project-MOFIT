using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;


public class AuthenticationManager : MonoBehaviour
{

    public static string CachePath;

    private string _appClientId;
    private string _cognitoDomain;
    private string _redirectUrl;

    private IFileManager _fileManager;
    private ISaveDataManager _saveDataManager;
    private string _mockValidAuthCode;
    private bool _isTestEnvironment;



    // In production, should probably keep these in a config file
    private const string AppClientID = "54k2n4fo3jspbo4aah0cmfaukn"; // App client ID, found under App Client Settings
   private const string AuthCognitoDomainPrefix = "mofitlogin"; // Found under App Integration -> Domain Name. Changing this means it must be updated in all linked Social providers redirect and javascript origins

   private const string Region = "us-west-1"; // Update with the AWS Region that contains your services
#if UNITY_EDITOR
    private const string RedirectUrl = "https://dzi3ny7huab3j.cloudfront.net";
#else
private const string RedirectUrl = "https://dzi3ny7huab3j.cloudfront.net";
#endif
    private const string AuthCodeGrantType = "authorization_code";
   private const string RefreshTokenGrantType = "refresh_token";
   private const string CognitoAuthUrl = ".auth." + Region + ".amazoncognito.com";
   private const string TokenEndpointPath = "/oauth2/token";
   private const string CognitoDomain = "https://" + AuthCognitoDomainPrefix + ".auth." + Region + ".amazoncognito.com";

    private static string _userid = "";


    public void SetTestEnvironment(bool isTest)
    {
        _isTestEnvironment = isTest;
        if (isTest)
        {
            _appClientId = AWSTestConfig.AppClientId;
            _cognitoDomain = AWSTestConfig.CognitoDomain;
            _redirectUrl = "https://dzi3ny7huab3j.cloudfront.net";
        }
        else
        {
            _appClientId = AppClientID;
            _cognitoDomain = CognitoDomain;
            _redirectUrl = RedirectUrl;
        }
    }

    public void SetFileManager(IFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public void SetSaveDataManager(ISaveDataManager saveDataManager)
    {
        _saveDataManager = saveDataManager;
    }

    public void SetMockValidAuthCode(string mockValidAuthCode)
    {
        _mockValidAuthCode = mockValidAuthCode;
    }

    public async Task<bool> ExchangeAuthCodeForAccessToken(string rawUrlWithGrantCode)
    {
        string allQueryParams = rawUrlWithGrantCode.Split('?')[1];
        string[] paramsSplit = allQueryParams.Split('&');
        string grantCode = "";

        foreach (string param in paramsSplit)
        {
            if (param.StartsWith("code"))
            {
                grantCode = param.Split('=')[1];
                grantCode = grantCode.removeAllNonAlphanumericCharsExceptDashes();
                break;
            }
        }

        if (string.IsNullOrEmpty(grantCode))
        {
            Debug.Log("Code not found");
            return false;
        }

        if (_isTestEnvironment && grantCode == _mockValidAuthCode)
        {
            // Simulate successful login for test environment
            var mockTokenResponse = new BADAuthenticationResultType
            {
                id_token = "mock-id-token",
                refresh_token = "mock-refresh-token",
                access_token = "mock-access-token"
            };

            _userid = "mock-user-id";
            if (_saveDataManager != null)
            {
                _saveDataManager.SaveJsonData(new UserSessionCache(mockTokenResponse, _userid));
            }
            else
            {
                SaveDataManager.SaveJsonData(new UserSessionCache(mockTokenResponse, _userid));
            }
            return true;
        }

        return await CallCodeExchangeEndpoint(grantCode);
    }

    // exchanges grant code for tokens
    private async Task<bool> CallCodeExchangeEndpoint(string grantCode)
   {
        WWWForm form = new WWWForm();
        form.AddField("grant_type", AuthCodeGrantType);
        form.AddField("client_id", AppClientID);
        form.AddField("code", grantCode);
        form.AddField("redirect_uri", RedirectUrl);

        // DOCS: https://docs.aws.amazon.com/cognito/latest/developerguide/token-endpoint.html
        string requestPath = "https://" + AuthCognitoDomainPrefix + CognitoAuthUrl + TokenEndpointPath;

      UnityWebRequest webRequest = UnityWebRequest.Post(requestPath, form);
      await webRequest.SendWebRequest();

      if (webRequest.result != UnityWebRequest.Result.Success)
      {
         Debug.Log("Code exchange failed: " + webRequest.error + "\n" + webRequest.result + "\n" + webRequest.responseCode);
         webRequest.Dispose();
      }
      else
      {
         Debug.Log("Success, Code exchange complete!");

         BADAuthenticationResultType authenticationResultType = JsonUtility.FromJson<BADAuthenticationResultType>(webRequest.downloadHandler.text);
         // Debug.Log("ID token: " + authenticationResultType.id_token);

         _userid = AuthUtilities.GetUserSubFromIdToken(authenticationResultType.id_token);

         // update session cache
         SaveDataManager.SaveJsonData(new UserSessionCache(authenticationResultType, _userid));
         webRequest.Dispose();
         return true;
      }
      return false;
   }

   public async Task<bool> CallRefreshTokenEndpoint()
   {
      UserSessionCache userSessionCache = new UserSessionCache();
      SaveDataManager.LoadJsonData(userSessionCache);

      string preservedRefreshToken = "";

      if (userSessionCache != null && userSessionCache._refreshToken != null && userSessionCache._refreshToken != "")
      {
         // DOCS: https://docs.aws.amazon.com/cognito/latest/developerguide/token-endpoint.html
         string refreshTokenUrl = "https://" + AuthCognitoDomainPrefix + CognitoAuthUrl + TokenEndpointPath;
          Debug.Log(refreshTokenUrl);

         preservedRefreshToken = userSessionCache._refreshToken;

         WWWForm form = new WWWForm();
         form.AddField("grant_type", RefreshTokenGrantType);
         form.AddField("client_id", AppClientID);
         form.AddField("refresh_token", userSessionCache._refreshToken);

         UnityWebRequest webRequest = UnityWebRequest.Post(refreshTokenUrl, form);
         webRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

         await webRequest.SendWebRequest();


         if (webRequest.result != UnityWebRequest.Result.Success)
         {
            Debug.Log("Refresh token call failed: " + webRequest.error + "\n" + webRequest.result + "\n" + webRequest.responseCode);
            // clear out invalid user session data to force re-authentication
            ClearUserSessionData();
            webRequest.Dispose();
         }
         else
         {
            Debug.Log("Success, Refresh token call complete!");
            // Debug.Log(webRequest.downloadHandler.text);

            BADAuthenticationResultType authenticationResultType = JsonUtility.FromJson<BADAuthenticationResultType>(webRequest.downloadHandler.text);

            // token endpoint to get refreshed access token does NOT return the refresh token, so manually save it from before.
            authenticationResultType.refresh_token = preservedRefreshToken;

            _userid = AuthUtilities.GetUserSubFromIdToken(authenticationResultType.id_token);

            // update session cache
            SaveDataManager.SaveJsonData(new UserSessionCache(authenticationResultType, _userid));
            webRequest.Dispose();
            return true;
         }
      }
      return false;
   }

   // Revokes refresh token and any access tokens issued from the refresh token.  Forces user to re-authenticate.
   private async Task<bool> RevokeRefreshToken()
   {
      UserSessionCache userSessionCache = new UserSessionCache();
      SaveDataManager.LoadJsonData(userSessionCache);

      if (userSessionCache != null && userSessionCache._refreshToken != null && userSessionCache._refreshToken != "")
      {
         // DOCS (WARNING these docs are not accurate at the time of this implementation): https://docs.aws.amazon.com/cognito/latest/developerguide/revocation-endpoint.html
         // These were more accurate: https://docs.aws.amazon.com/cognito-user-identity-pools/latest/APIReference/API_RevokeToken.html
         // Also, the Enable token revocation option must be enabled for this to work under User Pool -> App Clients tab.
         string revokeTokenEndpoint = "https://" + AuthCognitoDomainPrefix + CognitoAuthUrl + "/oauth2/revoke";
         // Debug.Log(revokeTokenEndpoint);

         WWWForm form = new WWWForm();
         form.AddField("client_id", AppClientID);
         form.AddField("token", userSessionCache._refreshToken);

         UnityWebRequest webRequest = UnityWebRequest.Post(revokeTokenEndpoint, form);
         webRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

         await webRequest.SendWebRequest();

         if (webRequest.result != UnityWebRequest.Result.Success)
         {
            Debug.Log("Revoke token call failed: " + webRequest.error + "\n" + webRequest.result + "\n" + webRequest.responseCode);
            webRequest.Dispose();
         }
         else
         {
            Debug.Log("Success, Revoke token call complete!");
            webRequest.Dispose();
            return true;
         }
      }
      return false;
   }

   public async void Logout()
   {
      bool logoutSuccess = await RevokeRefreshToken();

      // Important! Make sure to remove the local stored tokens.
      ClearUserSessionData();
      Debug.Log("user logged out.");
   }

   // Saves an empty user session object that will clear out all locally saved tokens.
   private void ClearUserSessionData()
   {
      UserSessionCache userSessionCache = new UserSessionCache();
      SaveDataManager.SaveJsonData(userSessionCache);
   }

   public string GetUsersId()
   {
      // Debug.Log("GetUserId: [" + _userid + "]");
      if (_userid == null || _userid == "")
      {
         // load userid from cached session 
         UserSessionCache userSessionCache = new UserSessionCache();
         SaveDataManager.LoadJsonData(userSessionCache);
         _userid = userSessionCache.getUserId();
      }
      return _userid;
   }



    public string GetIdToken()
    {
        UserSessionCache userSessionCache = new UserSessionCache();
        SaveDataManager.LoadJsonData(userSessionCache);
        string token = userSessionCache.getIdToken();
        Debug.Log($"Retrieved ID Token: {token.Substring(0, 10)}..."); // Log first 10 chars of token
        return token;
    }

    public string GetUserId()
   {
      UserSessionCache userSessionCache = new UserSessionCache();
      SaveDataManager.LoadJsonData(userSessionCache);
      return userSessionCache.getUserId();
   }

    public string GetLoginUrl()
    {
        string loginUrl = CognitoDomain + "/login?response_type=code&client_id="
            + AppClientID + "&redirect_uri=" + UnityWebRequest.EscapeURL(RedirectUrl);
        Debug.Log("Login URL: " + loginUrl);
        return loginUrl;
    }

    public string GetAccessKey()
    {
        // Replace this with your actual method of securely retrieving the access key
        return "AKIASKOLIUAQYVLGMSXI";
    }

   // public string GetSecretKey()
    //{
        // Replace this with your actual method of securely retrieving the secret key
       // return "yAFW0msLafrC7D6UbRxgBHO4/R150kq+6XK5IyMJ";
    //}
    void Awake()
   {
      CachePath = Application.persistentDataPath;
      // Debug.Log("CachePath: " + CachePath);
   }

}

#region Testing classes

public static class AWSTestConfig
{
    public const string UserPoolId = "us-west-1_hbbG1U0QG";
    public const string AppClientId = "4ival0j8qnkcrm9ifmpt2qrl6n";
    public const string CognitoDomain = "https://mofittestpool.auth.us-west-1.amazoncognito.com";
}

public interface IFileManager
{
    bool WriteToFile(string fileName, string contents);
    bool LoadFromFile(string fileName, out string contents);
}

public interface ISaveDataManager
{
    void SaveJsonData(ISaveable saveable);
}

#endregion Testing classes
