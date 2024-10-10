using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using TMPro;

public class AWSIntegrationTest : MonoBehaviour
{
    public TMP_InputField number1Input;
    public TMP_InputField number2Input;
    public Button addButton;
    public TextMeshProUGUI resultText;

    private const string ApiUrl = "https://144ri5poi3.execute-api.us-west-1.amazonaws.com/prod/add";

    private void Start()
    {
        addButton.onClick.AddListener(AddNumbers);
    }

    private void AddNumbers()
    {
        float num1, num2;
        if (float.TryParse(number1Input.text, out num1) && float.TryParse(number2Input.text, out num2))
        {
            StartCoroutine(SendAddRequest(num1, num2));
        }
        else
        {
            resultText.text = "Please enter valid numbers.";
        }
    }

    private IEnumerator SendAddRequest(float num1, float num2)
    {
        var requestData = new { num1 = num1, num2 = num2 };
        string jsonData = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest www = new UnityWebRequest(ApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"Sending request to: {ApiUrl}");
            Debug.Log($"Request Body: {jsonData}");

            yield return www.SendWebRequest();

            Debug.Log($"Response Code: {www.responseCode}");
            Debug.Log($"Response Headers: {www.GetResponseHeaders()}");
            Debug.Log($"Response Body: {www.downloadHandler.text}");

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"Success Response: {responseText}");
                var responseData = JsonConvert.DeserializeObject<AddResponse>(responseText);
                resultText.text = $"Result: {responseData.result}";
            }
            else
            {
                string errorDetails = $"Error: {www.error}\n" +
                                      $"Response Code: {www.responseCode}\n" +
                                      $"Response Headers: {www.GetResponseHeaders()}\n" +
                                      $"Response Body: {www.downloadHandler.text}";
                Debug.LogError(errorDetails);
                resultText.text = $"Error {www.responseCode}: {www.error}. Check console for details.";
            }
        }
    }

    private class AddResponse
    {
        public float result;
    }
}