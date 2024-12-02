using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] private RectTransform transitionImage;
    [SerializeField] private float transitionTime = 0.5f;

    public enum TransitionDirection
    {
        RightToLeft,
        LeftToRight,
        BottomToTop
    }

    private void Awake()
    {
        if (transitionImage == null)
        {
            Debug.LogError("Transition image is not assigned in the inspector.");
        }
    }

    public void TransitionToScene(string sceneName, TransitionDirection direction)
    {
        if (transitionImage == null)
        {
            Debug.LogError("Transition image is missing. Loading scene without transition.");
            SceneManager.LoadScene(sceneName);
            return;
        }
        StartCoroutine(PerformTransition(sceneName, direction));
    }

    private IEnumerator PerformTransition(string sceneName, TransitionDirection direction)
    {
        SetInitialPosition(direction);
        AnimateCover(direction);

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(sceneName);

        // Wait for the next frame to ensure the new scene is loaded
        yield return null;

        // Find the transition image in the new scene
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            transitionImage = canvas.transform.Find("TransitionImage") as RectTransform;
            if (transitionImage == null)
            {
                Debug.LogError("TransitionImage not found in the new scene.");
                yield break;
            }
        }
        else
        {
            Debug.LogError("Canvas not found in the new scene.");
            yield break;
        }

        AnimateUncover(direction);

        yield return new WaitForSeconds(transitionTime);

        ResetPosition(direction);
    }

    private void SetInitialPosition(TransitionDirection direction)
    {
        switch (direction)
        {
            case TransitionDirection.RightToLeft:
                transitionImage.anchoredPosition = new Vector2(Screen.width, 0);
                break;
            case TransitionDirection.LeftToRight:
                transitionImage.anchoredPosition = new Vector2(-Screen.width, 0);
                break;
            case TransitionDirection.BottomToTop:
                transitionImage.anchoredPosition = new Vector2(0, -Screen.height);
                break;
        }
    }

    private void AnimateCover(TransitionDirection direction)
    {
        switch (direction)
        {
            case TransitionDirection.RightToLeft:
            case TransitionDirection.LeftToRight:
                LeanTween.moveX(transitionImage, 0, transitionTime).setEase(LeanTweenType.easeInOutQuad);
                break;
            case TransitionDirection.BottomToTop:
                LeanTween.moveY(transitionImage, 0, transitionTime).setEase(LeanTweenType.easeInOutQuad);
                break;
        }
    }

    private void AnimateUncover(TransitionDirection direction)
    {
        switch (direction)
        {
            case TransitionDirection.RightToLeft:
                LeanTween.moveX(transitionImage, Screen.width, transitionTime).setEase(LeanTweenType.easeInOutQuad);
                break;
            case TransitionDirection.LeftToRight:
                LeanTween.moveX(transitionImage, -Screen.width, transitionTime).setEase(LeanTweenType.easeInOutQuad);
                break;
            case TransitionDirection.BottomToTop:
                LeanTween.moveY(transitionImage, Screen.height, transitionTime).setEase(LeanTweenType.easeInOutQuad);
                break;
        }
    }

    private void ResetPosition(TransitionDirection direction)
    {
        switch (direction)
        {
            case TransitionDirection.RightToLeft:
                transitionImage.anchoredPosition = new Vector2(Screen.width, 0);
                break;
            case TransitionDirection.LeftToRight:
                transitionImage.anchoredPosition = new Vector2(-Screen.width, 0);
                break;
            case TransitionDirection.BottomToTop:
                transitionImage.anchoredPosition = new Vector2(0, -Screen.height);
                break;
        }
    }
}