using UnityEngine;
using TMPro;

public class DownloadFilterDifficulty : MonoBehaviour {
    public TextMeshProUGUI Label;
    public string SiteFilterName;
    public bool IsSelected = true;
    private Color ColorSelected = Color.white;
    private Color ColorUnselected = Color.gray;

    public void Toggle() {
        IsSelected = !IsSelected;

        if (IsSelected) {
            Label.fontStyle = FontStyles.Underline & FontStyles.Bold;
            Label.color = ColorSelected;
        }
        else {
            Label.fontStyle = FontStyles.Normal;
            Label.color = ColorUnselected;
        }
    }
}