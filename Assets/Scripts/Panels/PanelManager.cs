using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    [SerializeField] private List<SRQDPanel> panels = new();
    [SerializeField] private SRQDPanel startingPanel;

    private SRQDPanel currentPanel;
    private Dictionary<string, SRQDPanel> panelNameLookup = new();

    private void Awake()
    {
        foreach (var panel in panels)
        {
            panelNameLookup.Add(panel.PanelName, panel);
            panel.Close();
        }

        ChangeToPanel(startingPanel);
    }

    public void ChangeToPanel(SRQDPanel panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("Cannot change to null panel!");
            return;
        }

        if (currentPanel != null)
        {
            currentPanel.Close();
        }

        currentPanel = panel;
        currentPanel.Open();
    }
    
    public void ChangeToPanel(string panelName)
    {
        if (!panelNameLookup.ContainsKey(panelName))
        {
            Debug.LogWarning($"Panel '{panelName}' not found!");
            return;
        }
        
        ChangeToPanel(panelNameLookup[panelName]);
    }
}
