using UnityEngine;
using TMPro;

public class DownloadFilterDifficulty : MonoBehaviour {
    public TextMeshProUGUI Label;
    public string SiteFilterName;
    public DownloadFilters downloadFilters;
    public bool IsSelected { get; private set; } = true;
    private Color ColorSelected = Color.white;
    private Color ColorUnselected = Color.gray;

    public void Toggle() {
        SetSelected(!IsSelected);
    }

    public void SetSelected(bool isSelected) {
        IsSelected = isSelected;

        if (IsSelected) {
            Label.fontStyle = FontStyles.Underline & FontStyles.Bold;
            Label.color = ColorSelected;
        }
        else {
            Label.fontStyle = FontStyles.Normal;
            Label.color = ColorUnselected;
        }

        downloadFilters.SaveDifficultyFiltersToPrefs();
    }
}