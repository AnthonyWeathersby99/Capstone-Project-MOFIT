using UnityEngine;
using OpenCVForUnity.CoreModule;
using System.Collections.Generic;

public class MovementTrackerBridge : MonoBehaviour
{
    [SerializeField] private HammerCurlTracker hammerCurlTracker;
    [SerializeField] private YOLOv8WithOpenCVForUnityExample.YOLOv8PoseEstimationExample poseEstimator;

    private readonly int SHOULDER_INDEX = 5; // Right shoulder index in YOLOv8 pose
    private readonly int ELBOW_INDEX = 7;    // Right elbow index
    private readonly int WRIST_INDEX = 9;    // Right wrist index

    private void Start()
    {
        if (hammerCurlTracker == null)
            hammerCurlTracker = GetComponent<HammerCurlTracker>();

        if (poseEstimator == null)
            poseEstimator = GetComponent<YOLOv8WithOpenCVForUnityExample.YOLOv8PoseEstimationExample>();

        if (hammerCurlTracker == null || poseEstimator == null)
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

        // Get the first detected person's keypoints
        float[] keypointsArray = new float[keypointsMatrix.cols()];
        keypointsMatrix.get(0, 0, keypointsArray); // Get first row of keypoints

        // Extract shoulder, elbow and wrist coordinates
        Vector3 shoulder = new Vector3(
            keypointsArray[SHOULDER_INDEX * 3],     // x
            keypointsArray[SHOULDER_INDEX * 3 + 1], // y
            0                                       // z (we're working in 2D)
        );

        Vector3 elbow = new Vector3(
            keypointsArray[ELBOW_INDEX * 3],
            keypointsArray[ELBOW_INDEX * 3 + 1],
            0
        );

        Vector3 wrist = new Vector3(
            keypointsArray[WRIST_INDEX * 3],
            keypointsArray[WRIST_INDEX * 3 + 1],
            0
        );

        // Check confidence scores
        float shoulderConf = keypointsArray[SHOULDER_INDEX * 3 + 2];
        float elbowConf = keypointsArray[ELBOW_INDEX * 3 + 2];
        float wristConf = keypointsArray[WRIST_INDEX * 3 + 2];

        // Only update tracking if we have high confidence in all key points
        if (shoulderConf > 0.5f && elbowConf > 0.5f && wristConf > 0.5f)
        {
            hammerCurlTracker.UpdateTracking(shoulder, elbow, wrist);
        }
    }

    // Call this when pose estimation results are available
    public void OnPoseEstimationResults(List<Mat> results)
    {
        if (results != null && results.Count >= 2 && !results[1].empty())
        {
            ProcessPoseResults(results[1]); // results[1] contains the keypoints
        }
    }
}