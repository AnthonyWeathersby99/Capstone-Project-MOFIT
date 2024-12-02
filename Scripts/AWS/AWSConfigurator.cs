using UnityEngine;
using Amazon;

public class AWSConfigurator : MonoBehaviour
{
    public string AwsRegion = "us-west-1"; // AWS region

    void Awake()
    {
       // AWSConfigs.AWSRegion = AwsRegion;
      //  AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
    }
}