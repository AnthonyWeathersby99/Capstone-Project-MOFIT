using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class UserProfileUIManager : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField heightFeetInput;
    [SerializeField] private TMP_InputField heightInchesInput;
    [SerializeField] private TMP_InputField sexInput;
    [SerializeField] private TMP_InputField birthdayDayInput;
    [SerializeField] private TMP_InputField birthdayMonthInput;
    [SerializeField] private TMP_InputField birthdayYearInput;
    [SerializeField] private TMP_InputField startingWeightInput;
    [SerializeField] private TMP_InputField currentWeightInput;
    [SerializeField] private TMP_InputField goalWeightInput;

    [Header("UI Panels")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private Button retryButton;
    [SerializeField] private float refreshInterval = 5f;

    private UserProfileManager userProfileManager;
    private AuthenticationManager authenticationManager;
    private UserProfile currentProfile;
    private Coroutine refreshCoroutine;

    private const int MAX_RETRIES = 3;
    private int currentRetryCount = 0;

    private async void Start()
    {
        ShowLoadingState(true);

        userProfileManager = FindObjectOfType<UserProfileManager>();
        authenticationManager = FindObjectOfType<AuthenticationManager>();

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryLoading);
        }

        await LoadProfileWithRetry();
    }

    private async Task LoadProfileWithRetry()
    {
        while (currentRetryCount < MAX_RETRIES)
        {
            try
            {
                await LoadProfileAsync();
                AddListenersToInputFields();
                StartRefreshCoroutine();
                ShowLoadingState(false);
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"Profile load attempt {currentRetryCount + 1} failed: {e.Message}");
                currentRetryCount++;

                if (currentRetryCount >= MAX_RETRIES)
                {
                    ShowErrorState("Failed to load profile. Please check your internet connection and try again.");
                    return;
                }

                await Task.Delay(2000); // Wait 2 seconds between retries
            }
        }
    }

    private void ShowLoadingState(bool isLoading)
    {
        if (loadingPanel != null) loadingPanel.SetActive(isLoading);
        if (errorPanel != null) errorPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(!isLoading);
    }

    private void ShowErrorState(string message)
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(false);
        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
            if (errorText != null)
                errorText.text = message;
        }
    }

    public void RetryLoading()
    {
        currentRetryCount = 0;
        ShowLoadingState(true);
        _ = LoadProfileWithRetry();
    }

    private async Task LoadProfileAsync()
    {
        if (userProfileManager == null || authenticationManager == null)
        {
            throw new Exception("Required managers not found!");
        }

        string userId = authenticationManager.GetUserId();
        Debug.Log($"Loading profile for userId: {userId}");

        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("User ID is null or empty");
        }

        currentProfile = await userProfileManager.GetUserProfile(userId);

        if (currentProfile != null)
        {
            Debug.Log($"Profile loaded successfully: {JsonUtility.ToJson(currentProfile)}");
            UpdateUIWithProfile();
        }
        else
        {
            throw new Exception("Failed to load user profile");
        }
    }

    private void StartRefreshCoroutine()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }
        refreshCoroutine = StartCoroutine(RefreshUICoroutine());
    }

    private IEnumerator RefreshUICoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            RefreshUI();
        }
    }

    private async void RefreshUI()
    {
        try
        {
            string userId = authenticationManager.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("User ID is null or empty during refresh.");
                return;
            }

            UserProfile refreshedProfile = await userProfileManager.GetUserProfile(userId);
            if (refreshedProfile != null)
            {
                currentProfile = refreshedProfile;
                UpdateUIWithProfile();
                Debug.Log("Profile refreshed successfully.");
            }
            else
            {
                Debug.LogWarning("Failed to refresh profile: Profile not found.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error refreshing profile: {e.Message}");
        }
    }

    private void UpdateUIWithProfile()
    {
        Debug.Log("Updating UI with profile data:");
        SetTextSafely(nameInput, currentProfile.Name);
        SetTextSafely(heightFeetInput, currentProfile.HeightFeet);
        SetTextSafely(heightInchesInput, currentProfile.HeightInches);
        SetTextSafely(currentWeightInput, currentProfile.CurrentWeight);
        SetTextSafely(sexInput, currentProfile.Sex);
        SetTextSafely(birthdayDayInput, currentProfile.BirthdayDay);
        SetTextSafely(birthdayMonthInput, currentProfile.BirthdayMonth);
        SetTextSafely(birthdayYearInput, currentProfile.BirthdayYear);
        SetTextSafely(startingWeightInput, currentProfile.StartingWeight);
        SetTextSafely(goalWeightInput, currentProfile.GoalWeight);
    }

    private void SetTextSafely(TMP_InputField inputField, string value)
    {
        if (inputField != null)
        {
            inputField.text = value ?? "";
        }
    }

    private void AddListenersToInputFields()
    {
        AddListenerSafely(nameInput, "Name");
        AddListenerSafely(heightFeetInput, "HeightFeet");
        AddListenerSafely(heightInchesInput, "HeightInches");
        AddListenerSafely(currentWeightInput, "CurrentWeight");
        AddListenerSafely(sexInput, "Sex");
        AddListenerSafely(birthdayDayInput, "BirthdayDay");
        AddListenerSafely(birthdayMonthInput, "BirthdayMonth");
        AddListenerSafely(birthdayYearInput, "BirthdayYear");
        AddListenerSafely(startingWeightInput, "StartingWeight");
        AddListenerSafely(goalWeightInput, "GoalWeight");
    }

    private void AddListenerSafely(TMP_InputField inputField, string fieldName)
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener((value) => SaveFieldAsync(fieldName, value));
        }
    }

    private async void SaveFieldAsync(string fieldName, string value)
    {
        if (currentProfile == null)
        {
            Debug.LogError("Cannot save: current profile is null");
            return;
        }

        var updatedProfile = new UserProfile
        {
            UserId = currentProfile.UserId
        };

        // Set only the field being updated
        switch (fieldName)
        {
            case "Name": updatedProfile.Name = value; break;
            case "HeightFeet": updatedProfile.HeightFeet = value; break;
            case "HeightInches": updatedProfile.HeightInches = value; break;
            case "CurrentWeight": updatedProfile.CurrentWeight = value; break;
            case "Sex": updatedProfile.Sex = value; break;
            case "BirthdayDay": updatedProfile.BirthdayDay = value; break;
            case "BirthdayMonth": updatedProfile.BirthdayMonth = value; break;
            case "BirthdayYear": updatedProfile.BirthdayYear = value; break;
            case "StartingWeight": updatedProfile.StartingWeight = value; break;
            case "GoalWeight": updatedProfile.GoalWeight = value; break;
        }

        try
        {
            bool success = await userProfileManager.UpdateUserProfile(updatedProfile);
            if (success)
            {
                Debug.Log($"Successfully updated {fieldName}");
                // Update the current profile field
                switch (fieldName)
                {
                    case "Name": currentProfile.Name = value; break;
                    case "HeightFeet": currentProfile.HeightFeet = value; break;
                    case "HeightInches": currentProfile.HeightInches = value; break;
                    case "CurrentWeight": currentProfile.CurrentWeight = value; break;
                    case "Sex": currentProfile.Sex = value; break;
                    case "BirthdayDay": currentProfile.BirthdayDay = value; break;
                    case "BirthdayMonth": currentProfile.BirthdayMonth = value; break;
                    case "BirthdayYear": currentProfile.BirthdayYear = value; break;
                    case "StartingWeight": currentProfile.StartingWeight = value; break;
                    case "GoalWeight": currentProfile.GoalWeight = value; break;
                }
            }
            else
            {
                Debug.LogError($"Failed to update {fieldName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving {fieldName}: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }
    }
}