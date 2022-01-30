using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class playgame : MonoBehaviour
{
    public TextMeshProUGUI t;
    public bool canStart = false;
    public void PlayGame()
    {
        t.text = "PREPARING";
        canStart = true;
    }

    public void Update()
    {
        if (canStart)
        {
            canStart = false;
            master.selectedMap = selectedMap;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
        }
    }

    public int selectedMap = 0; // 0 -> regional, 1 -> full
    public TextMeshProUGUI mapText;
    public void nextSelectedMap()
    {
        if (selectedMap == 0)
        {
            selectedMap = 1;
            mapText.text = "Full";
        }
        else
        {
            selectedMap = 0;
            mapText.text = "Regional";
        }
    }

    public void quit()
    {
        Application.Quit();
    }
}
