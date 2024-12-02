using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

public class UserProfileUIManager : MonoBehaviour
{
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
    [SerializeField] private float refreshInterval = 5f;

    private UserProfileManager userProfileManager;
    private UserProfile currentProfile;
    private AuthenticationManager authenticationManager;

    private Coroutine refreshCoroutine;

    private void Awake()
    {
        userProfileManager = FindObjectOfType<UserProfileManager>();
        authenticationManager = FindObjectOfType<AuthenticationManager>();
        if (userProfileManager == null)
        {
            Debug.LogError("UserProfileManager not found in the scene.");
        }
        else
        {
            Debug.Log("UserProfileManager found successfully.");
        }
        if (authenticationManager == null)
        {
            Debug.LogError("AuthenticationManager not found in the scene.");
        }
    }

    private async void Start()
    {
        if (userProfileManager != null)
        {
            await LoadProfileAsync();
            AddListenersToInputFields();
            StartRefreshCoroutine();
        }
        else
        {
            Debug.LogError("Cannot start UserProfileUIManager: UserProfileManager is null.");
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
            Debug.LogError($"Error refreshing profile: {e.Message}\nStack Trace: {e.StackTrace}");
        }
    }

    private void OnDisable()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            if (userProfileManager == null)
            {
                Debug.LogError("Cannot load profile: UserProfileManager is null.");
                return;
            }

            if (authenticationManager == null)
            {
                Debug.LogError("Cannot load profile: AuthenticationManager is null.");
                return;
            }

            string userId = authenticationManager.GetUserId();
            Debug.Log($"Attempting to get user profile for userId: {userId}");

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("User ID is null or empty.");
                return;
            }

            currentProfile = await userProfileManager.GetUserProfile(userId);

            if (currentProfile != null)
            {
                Debug.Log("Profile loaded successfully.");
                UpdateUIWithProfile();
            }
            else
            {
                Debug.Log("No existing profile found.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading profile: {e.Message}\nStack Trace: {e.StackTrace}");
        }
    }

    private void AddListenersToInputFields()
    {
        AddListenerSafely(nameInput, "Name");
        AddListenerSafely(currentWeightInput, "CurrentWeight");
        AddListenerSafely(startingWeightInput, "StartingWeight");
        AddListenerSafely(goalWeightInput, "GoalWeight");
        AddListenerSafely(heightFeetInput, "HeightFeet");
        AddListenerSafely(heightInchesInput, "HeightInches");
        AddListenerSafely(birthdayDayInput, "BirthdayDay");
        AddListenerSafely(birthdayMonthInput, "BirthdayMonth");
        AddListenerSafely(birthdayYearInput, "BirthdayYear");
        AddListenerSafely(sexInput, "Sex");
    }

    private void AddListenerSafely(TMP_InputField inputField, string fieldName)
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener((value) => SaveProfileAsync(fieldName, value).ConfigureAwait(false));
        }
        else
        {
            Debug.LogWarning($"Input field for {fieldName} not assigned in UserProfileUIManager");
        }
    }

    private void UpdateUIWithProfile()
    {
        Debug.Log("Updating UI with profile data:");
        Debug.Log($"Name: {currentProfile.Name}");
        Debug.Log($"Height: Feet: {currentProfile.HeightFeet}, Inches: {currentProfile.HeightInches}");
        Debug.Log($"Current Weight: {currentProfile.CurrentWeight}");
        Debug.Log($"Sex: {currentProfile.Sex}");
        Debug.Log($"Birthday: {currentProfile.BirthdayDay}/{currentProfile.BirthdayMonth}/{currentProfile.BirthdayYear}");
        Debug.Log($"Starting Weight: {currentProfile.StartingWeight}");
        Debug.Log($"Goal Weight: {currentProfile.GoalWeight}");

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
            inputField.text = value;
        }
    }

    private async Task SaveProfileAsync(string fieldName, string value)
    {
        if (userProfileManager == null || currentProfile == null)
        {
            Debug.LogError("Cannot save profile: UserProfileManager or currentProfile is null.");
            return;
        }

        var updatedProfile = new UserProfile
        {
            UserId = currentProfile.UserId
        };

        switch (fieldName)
        {
            case "Name":
                updatedProfile.Name = value;
                break;
            case "CurrentWeight":
                updatedProfile.CurrentWeight = value;
                break;
            case "StartingWeight":
                updatedProfile.StartingWeight = value;
                break;
            case "GoalWeight":
                updatedProfile.GoalWeight = value;
                break;
            case "HeightFeet":
                updatedProfile.HeightFeet = value;
                break;
            case "HeightInches":
                updatedProfile.HeightInches = value;
                break;
            case "BirthdayDay":
                updatedProfile.BirthdayDay = value;
                break;
            case "BirthdayMonth":
                updatedProfile.BirthdayMonth = value;
                break;
            case "BirthdayYear":
                updatedProfile.BirthdayYear = value;
                break;
            case "Sex":
                updatedProfile.Sex = value;
                break;
            default:
                Debug.LogWarning($"Unhandled field name: {fieldName}");
                return;
        }

        try
        {
            bool success = await userProfileManager.UpdateUserProfile(updatedProfile);
            if (success)
            {
                Debug.Log($"Field {fieldName} updated successfully");
                UpdateCurrentProfileField(fieldName, value);
            }
            else
            {
                Debug.LogError($"Failed to update field {fieldName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving profile: {e.Message}");
        }
    }

    private void UpdateCurrentProfileField(string fieldName, string value)
    {
        switch (fieldName)
        {
            case "Name":
                currentProfile.Name = value;
                break;
            case "CurrentWeight":
                currentProfile.CurrentWeight = value;
                break;
            case "StartingWeight":
                currentProfile.StartingWeight = value;
                break;
            case "GoalWeight":
                currentProfile.GoalWeight = value;
                break;
            case "HeightFeet":
                currentProfile.HeightFeet = value;
                break;
            case "HeightInches":
                currentProfile.HeightInches = value;
                break;
            case "BirthdayDay":
                currentProfile.BirthdayDay = value;
                break;
            case "BirthdayMonth":
                currentProfile.BirthdayMonth = value;
                break;
            case "BirthdayYear":
                currentProfile.BirthdayYear = value;
                break;
            case "Sex":
                currentProfile.Sex = value;
                break;
            default:
                Debug.LogWarning($"Unhandled field name: {fieldName}");
                break;
        }
    }
}