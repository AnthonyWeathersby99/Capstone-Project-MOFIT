using UnityEngine;
using TMPro;
using System.Linq;

public class SetDetailsPanel : MonoBehaviour
{
    private TextMeshProUGUI setNumberText;
    private TextMeshProUGUI timestampText;
    private TextMeshProUGUI repsValueText;
    private TextMeshProUGUI formScoreValueText;
    private TextMeshProUGUI durationValueText;
    private TextMeshProUGUI formIssuesListText;

    private void Awake()
    {
        // Updated paths to match your exact hierarchy
        setNumberText = transform.Find("Header/SetNumber (Text)")?.GetComponent<TextMeshProUGUI>();
        timestampText = transform.Find("Header/Timestamp (Text)")?.GetComponent<TextMeshProUGUI>();
        repsValueText = transform.Find("Details/Reps/Value (Text)")?.GetComponent<TextMeshProUGUI>();
        formScoreValueText = transform.Find("Details/FormScore/Value (Text)")?.GetComponent<TextMeshProUGUI>();
        durationValueText = transform.Find("Details/Duration/Value (Text)")?.GetComponent<TextMeshProUGUI>();
        formIssuesListText = transform.Find("FormIssues/Lists (Text)")?.GetComponent<TextMeshProUGUI>();

        // Add debug logging with more detail
        Debug.Log($"SetDetailsPanel Initialization on object {gameObject.name}:" +
                  $"\nsetNumberText path: Header/SetNumber (Text) - Found: {setNumberText != null}" +
                  $"\ntimestampText path: Header/Timestamp (Text) - Found: {timestampText != null}" +
                  $"\nrepsValueText path: Details/Reps/Value (Text) - Found: {repsValueText != null}" +
                  $"\nformScoreValueText path: Details/FormScore/Value (Text) - Found: {formScoreValueText != null}" +
                  $"\ndurationValueText path: Details/Duration/Value (Text) - Found: {durationValueText != null}" +
                  $"\nformIssuesListText path: FormIssues/Lists (Text) - Found: {formIssuesListText != null}");
    }

    public void PopulateSetDetails(WorkoutSet set)
    {
        if (set == null)
        {
            Debug.LogError("Attempting to populate with null WorkoutSet");
            return;
        }

        Debug.Log($"Attempting to populate set details for Set {set.setNumber}");

        if (setNumberText != null)
            setNumberText.text = $"Set {set.setNumber} - {set.arm} Arm";
        else
            Debug.LogError("setNumberText component not found");

        if (timestampText != null)
            timestampText.text = set.GetFormattedTimestamp();
        else
            Debug.LogError("timestampText component not found");

        if (repsValueText != null)
            repsValueText.text = set.reps.ToString();
        else
            Debug.LogError("repsValueText component not found");

        if (formScoreValueText != null)
            formScoreValueText.text = $"{set.averageFormScore:F1}%";
        else
            Debug.LogError("formScoreValueText component not found");

        if (durationValueText != null)
            durationValueText.text = $"{set.duration:F1}s";
        else
            Debug.LogError("durationValueText component not found");

        if (formIssuesListText != null && set.formIssues != null)
        {
            var uniqueIssues = set.formIssues.Distinct().Take(4).ToList();
            formIssuesListText.text = uniqueIssues.Any() ?
                $"• {string.Join("\n• ", uniqueIssues)}" :
                "No form issues";
        }
        else
            Debug.LogError("formIssuesListText component not found or formIssues is null");
    }
}