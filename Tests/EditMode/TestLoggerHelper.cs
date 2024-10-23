using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class TestLoggerHelper
{
    private static TestLogger logger;
    private static int totalTests = 0;
    private static int passedTests = 0;
    private static int failedTests = 0;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        if (logger == null)
        {
            GameObject loggerObject = new GameObject("TestLogger");
            logger = loggerObject.AddComponent<TestLogger>();
            yield return null; // Wait a frame for MonoBehaviour to initialize
        }
    }

    public static void LogTestComplete(string testName, bool passed, string message = "", string stackTrace = "")
    {
        if (logger == null) return;

        totalTests++;
        if (passed)
            passedTests++;
        else
            failedTests++;

        logger.LogTestResult(
            testName,
            passed,
            message,
            stackTrace,
            Time.time // Using Time.time as a simple duration metric
        );
    }

    public static void LogSummary()
    {
        if (logger == null) return;

        logger.LogTestSummary(totalTests, passedTests, failedTests);
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        if (logger != null)
        {
            LogSummary();
            Object.Destroy(logger.gameObject);
            logger = null;
        }
        yield return null;
    }
}