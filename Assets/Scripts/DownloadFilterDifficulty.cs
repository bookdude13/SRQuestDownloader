using UnityEngine;
using TMPro;

public class DownloadFilterDifficulty : MonoBehaviour {
    public TextMeshProUGUI Label;
    public string SiteFilterName;
    public bool IsSelected = true;

    public void Toggle() {
        IsSelected = !IsSelected;

        if (IsSelected) {
            Label.fontStyle = FontStyles.Underline & FontStyles.Bold;
        }
        else {
            Label.fontStyle = FontStyles.Normal;
        }
    }
}