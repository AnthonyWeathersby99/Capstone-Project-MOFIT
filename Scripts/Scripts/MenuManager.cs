using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
[System.Serializable]
public class MenuButton
{
    public string buttonName;
    public CrossPlatformInputManager buttonInput;
    public string targetSceneName;
}

public class MenuManager : MonoBehaviour
{
    [SerializeField] private List<MenuButton> menuButtons = new List<MenuButton>();
    private SceneTransitionManager transitionManager;

    private void Start()
    {
        transitionManager = FindObjectOfType<SceneTransitionManager>();
        if (transitionManager == null)
        {
            Debug.LogWarning("SceneTransitionManager not found in the scene. Scene transitions may not work.");
        }

        SetupButtons();
    }

    private void SetupButtons()
    {
        foreach (var button in menuButtons)
        {
            if (button.buttonInput != null)
            {
                button.buttonInput.OnPress.AddListener((pos) => LoadSceneWithTransition(button.targetSceneName, pos));
            }
            else
            {
                Debug.LogWarning($"Button input for {button.buttonName} is not assigned.");
            }
        }
    }

    private void LoadSceneWithTransition(string sceneName, Vector2 inputPosition)
    {
        if (transitionManager != null)
        {
            SceneTransitionManager.TransitionDirection direction = DetermineTransitionDirection(inputPosition);
            transitionManager.TransitionToScene(sceneName, direction);
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager not found. Loading scene without transition.");
            SceneManager.LoadScene(sceneName);
        }
    }

    private SceneTransitionManager.TransitionDirection DetermineTransitionDirection(Vector2 inputPosition)
    {
        if (inputPosition.x < Screen.width / 3)
        {
            return SceneTransitionManager.TransitionDirection.RightToLeft;
        }
        else if (inputPosition.x > Screen.width * 2 / 3)
        {
            return SceneTransitionManager.TransitionDirection.LeftToRight;
        }
        else
        {
            return SceneTransitionManager.TransitionDirection.BottomToTop;
        }
    }

    // Optional: Method to add a button at runtime
    public void AddButton(string buttonName, CrossPlatformInputManager buttonInput, string targetSceneName)
    {
        MenuButton newButton = new MenuButton
        {
            buttonName = buttonName,
            buttonInput = buttonInput,
            targetSceneName = targetSceneName
        };
        menuButtons.Add(newButton);

        if (buttonInput != null)
        {
            buttonInput.OnPress.AddListener((pos) => LoadSceneWithTransition(targetSceneName, pos));
        }
    }
}