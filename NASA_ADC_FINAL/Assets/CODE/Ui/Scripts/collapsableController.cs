using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class collapsableController : MonoBehaviour
{
    // 0 : Height
    // 1 : Slope
    // 2 : Elevation
    // 3 : Azimuth
    // 4: Cart
    // 5: Geo
    public bool[] options = new bool[6];
    public bool open = false;
    public GameObject stayBelow;

    private int height = 5;
    private RectTransform panel;
    private GameObject textPrefab;
    private GameObject textParent;
    private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
    
    private void Start()
    {
        foreach (bool b in options) if (b) height += 20;
        panel = this.transform.GetChild(1).GetComponent<RectTransform>();
        textPrefab = (GameObject) Resources.Load("UI/collapsableText");
        textParent = this.transform.GetChild(2).gameObject;
    }
    public void clickEvent()
    {
        if (open)
        {
            open = false;
            panel.offsetMin = new Vector2(panel.offsetMin.x, 0);
            
            int childCount = textParent.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Destroy(textParent.transform.GetChild(i).gameObject);
            }
            texts = new List<TextMeshProUGUI>();
        }
        else
        {
            open = true;
            panel.offsetMin = new Vector2(panel.offsetMin.x, -height);

            int j = 0;
            for (int i = 0; i < 6; i++)
            {
                if (options[i])
                {
                    GameObject go = Instantiate(textPrefab, new Vector3(), Quaternion.identity, textParent.transform);
                    go.transform.localPosition = new Vector3(-20f, -20 * j - 40, 0);
                    go.GetComponent<TextMeshProUGUI>().text = loadOption(i);
                    texts.Add(go.GetComponent<TextMeshProUGUI>());
                    j++;
                }
            }
        }
    }
    private void Update()
    {
        if (!master.terrainFinishedGenerating) return;
        if (open)
        {
            int i = 0;
            for (int j = 0; j < 6; j++)
            {
                if (options[j])
                {
                    texts[i].text = loadOption(j);
                    i++;
                }
            }
        }

        if (stayBelow != null)
        {
            float height = stayBelow.transform.GetChild(1).GetComponent<RectTransform>().offsetMin.y;
            this.transform.position = new Vector3(
                stayBelow.transform.position.x, 
                0,
                this.transform.position.z);
            this.transform.localPosition = new Vector3(this.transform.localPosition.x, height - 47, this.transform.localPosition.z);
        }
    }
    private string loadOption(int i)
    {
        switch (i)
        {
            case 0:
                return $"Height: {master.sharedInfo["playerHeight"]}";
            case 1:
                return $"Slope: {master.sharedInfo["playerSlope"]}";
            case 2:
                return $"Elevation: {master.sharedInfo["playerElevation"]}";
            case 3:
                return $"Azimuth: {master.sharedInfo["playerAzimuth"]}";
            case 4:
                string pointCart = master.sharedInfo["pointCart"];
                return $"Cartesian: {((pointCart == "\0") ? "" : fancyRound(pointCart, true, false, true))}";
            case 5:
                string pointGeo = master.sharedInfo["pointGeo"];
                return $"Geographic: {((pointGeo == "\0") ? "" : fancyRound(pointGeo, true, false, true))}";
        }
        return "";
    }

    public string fancyRound(string p, bool useX, bool useY, bool useZ)
    {
        string returnString = "";
        XYZ point = new XYZ(
            Convert.ToDouble(p.Substring(0, p.IndexOf(','))), 
            Convert.ToDouble(p.Substring(p.IndexOf(',') + 1, p.LastIndexOf(',') - p.IndexOf(',') - 1)),
            Convert.ToDouble(p.Substring(p.LastIndexOf(',') + 1)));

        if (useX)
        {
            returnString += $"{Math.Round(Convert.ToDouble(point.x))}";
        }
        if (useY)
        {
            if (useX) returnString += ", ";
            returnString += $"{Math.Round(Convert.ToDouble(point.y))}";
        }
        if (useZ)
        {
            if (useX || useY) returnString += ", ";
            returnString += $"{Math.Round(Convert.ToDouble(point.z))}";
        }
        return returnString;
    }
}
