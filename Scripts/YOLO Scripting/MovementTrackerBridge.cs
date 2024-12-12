using UnityEngine;
using OpenCVForUnity.CoreModule;
using System.Collections.Generic;
using YOLOv8WithOpenCVForUnityExample;
using System;

public class MovementTrackerBridge : MonoBehaviour
{
    [SerializeField] private HammerCurlTracker hammerCurlTracker;
    [SerializeField] private YOLOv8WithOpenCVForUnityExample.YOLOv8PoseEstimationExample poseEstimator;
    [SerializeField] private WorkoutSceneUI workoutUI;

    // YOLOv8 keypoint indices
    private readonly int LEFT_SHOULDER_INDEX = 6;  // Left shoulder
    private readonly int LEFT_ELBOW_INDEX = 8;     // Left elbow
    private readonly int LEFT_WRIST_INDEX = 10;    // Left wrist
    private readonly int RIGHT_SHOULDER_INDEX = 5; // Right shoulder
    private readonly int RIGHT_ELBOW_INDEX = 7;    // Right elbow
    private readonly int RIGHT_WRIST_INDEX = 9;    // Right wrist

    private void Start()
    {
        if (hammerCurlTracker == null)
            hammerCurlTracker = GetComponent<HammerCurlTracker>();

        if (poseEstimator == null)
            poseEstimator = GetComponent<YOLOv8PoseEstimationExample>();

        if (workoutUI == null)
            workoutUI = FindObjectOfType<WorkoutSceneUI>();

        if (hammerCurlTracker == null || poseEstimator == null || workoutUI == null)
        {
            Debug.LogError("MovementTrackerBridge: Required components are missing!");
            enabled = false;
            return;
        }
    }

    public void ProcessPoseResults(Mat keypointsMatrix)
    {
        if (keypointsMatrix == null || keypointsMatrix.empty())
            return;

        // Get current workout arm
        string currentArm = workoutUI.currentArm;
        bool isLeftArm = currentArm.Equals("Left", StringComparison.OrdinalIgnoreCase);

        // Select appropriate indices based on current arm
        int shoulderIndex = isLeftArm ? LEFT_SHOULDER_INDEX : RIGHT_SHOULDER_INDEX;
        int elbowIndex = isLeftArm ? LEFT_ELBOW_INDEX : RIGHT_ELBOW_INDEX;
        int wristIndex = isLeftArm ? LEFT_WRIST_INDEX : RIGHT_WRIST_INDEX;

        float[] keypointsArray = new float[keypointsMatrix.cols()];
        keypointsMatrix.get(0, 0, keypointsArray);

        Vector3 shoulder = new Vector3(
            keypointsArray[shoulderIndex * 3],
            keypointsArray[shoulderIndex * 3 + 1],
            0
        );

        Vector3 elbow = new Vector3(
            keypointsArray[elbowIndex * 3],
            keypointsArray[elbowIndex * 3 + 1],
            0
        );

        Vector3 wrist = new Vector3(
            keypointsArray[wristIndex * 3],
            keypointsArray[wristIndex * 3 + 1],
            0
        );

        // Check confidence scores
        float shoulderConf = keypointsArray[shoulderIndex * 3 + 2];
        float elbowConf = keypointsArray[elbowIndex * 3 + 2];
        float wristConf = keypointsArray[wristIndex * 3 + 2];

        // Only update tracking if we have high confidence in all key points
        if (shoulderConf > 0.5f && elbowConf > 0.5f && wristConf > 0.5f)
        {
            hammerCurlTracker.UpdateTracking(shoulder, elbow, wrist);
        }
    }

    public void OnPoseEstimationResults(List<Mat> results)
    {
        if (results != null && results.Count >= 2 && !results[1].empty())
        {
            ProcessPoseResults(results[1]);
        }
    }
}