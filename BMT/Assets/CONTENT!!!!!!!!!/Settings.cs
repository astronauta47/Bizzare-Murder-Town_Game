using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public Text sensivityText;
    public ConnectManager connectManager;

    public static float sensivity;

    public void SetSensivity()
    {
        if (sensivityText.text == "")
            sensivity = 2;
        else
            sensivity = float.Parse(sensivityText.text);
    }

    public void StartGame()
    {
        SetSensivity();
        connectManager.Connect();
    }
}
