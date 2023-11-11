using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SRQDPanel : MonoBehaviour
{
    [SerializeField] private string _panelName;
    [SerializeField] private CanvasGroup canvasGroup;
    public string PanelName => _panelName;

    public void Hide() => SetVisible(false);
    public void Show() => SetVisible(true);
    
    public void Close() => ToggleActive(false);
    public void Open() => ToggleActive(true);

    private void SetVisible(bool isVisible)
    {
        canvasGroup.alpha = isVisible ? 1f : 0f;
    }
    
    private void ToggleActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
}
