using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitButton : MonoBehaviour
{
    public void ExitApplication()
    {
        Debug.Log("Exiting...");
        Application.Quit(0);
    }
}
