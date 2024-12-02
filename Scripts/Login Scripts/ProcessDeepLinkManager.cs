using System;
using UnityEngine;

// Handles deep link events into the Unity app on a mobile device.  Not tested or supported on desktop applications.
// Class is linked to ProcessDeepLinkContainer in Unity Editor
// source: https://docs.unity3d.com/Manual/enabling-deep-linking.html
public class ProcessDeepLinkManager : MonoBehaviour
{
   public static ProcessDeepLinkManager Instance { get; private set; }

   private string deeplinkURL;
   private UIInputManager _uiInputManager;

    private void onDeepLinkActivated(string url)
    {
        Debug.Log("Deep link activated: " + url);
        deeplinkURL = url;

        _uiInputManager = FindObjectOfType<UIInputManager>();
        if (_uiInputManager != null)
        {
            _uiInputManager.ProcessDeepLink(deeplinkURL);
        }
        else
        {
            Debug.LogError("UIInputManager not found. Make sure it exists in the scene.");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            Application.deepLinkActivated += onDeepLinkActivated;

            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                Debug.Log("Deep link on app start: " + Application.absoluteURL);
                onDeepLinkActivated(Application.absoluteURL);
            }
            else
            {
                deeplinkURL = "NONE";
            }

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetDeepLink()
   {
        Debug.Log("DeeplinkURL: "+ deeplinkURL);
        return deeplinkURL;
        
   }
}