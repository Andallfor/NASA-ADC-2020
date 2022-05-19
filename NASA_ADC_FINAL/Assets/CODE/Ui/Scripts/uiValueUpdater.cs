using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// updates on screen values by putting them in master.sharedInfo
public class uiValueUpdater : MonoBehaviour
{
    private Vector2Int playerLastPosition;
    private GameObject player;
    private mapGenerator mg;
    private minimapController mmc;
    public bool passedStage1 = false;
    public bool passedStage2 = false;
    public bool passedStage3 = false;
    public Vector2 mgPoints;
    public Vector2 currentPos;



    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        mg = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<mapGenerator>();
        mmc = GameObject.FindGameObjectWithTag("minimapController").GetComponent<minimapController>();

        master.sharedInfo["playerHeight"] = "\0";
        master.sharedInfo["playerSlope"] = "\0";
        master.sharedInfo["playerAzimuth"] = "\0";
        master.sharedInfo["playerElevation"] = "\0";
        master.sharedInfo["pointCart"] = "\0";
        master.sharedInfo["pointGeo"] = "\0";
    }

    void Update()
    {
        if (!master.terrainFinishedGenerating) return;

        // default info
        updateInfo();
    }

    public void updateInfo()
    {
        Vector2Int pos;
        passedStage1 = true;
        if (mmc.expandedMap) 
        {
            if (new Vector2(mmc.c.transform.position.x, mmc.c.transform.position.z) == playerLastPosition) return;
            pos = new Vector2Int((int) mmc.c.transform.position.x, (int) mmc.c.transform.position.z);
        }
        else
        {
            if (new Vector2(player.transform.position.x, player.transform.position.z) == playerLastPosition) return;
            pos = new Vector2Int((int) player.transform.position.x, (int) player.transform.position.z);
        }
        playerLastPosition = pos;

        passedStage2 = true;
        mgPoints = new Vector2(mg.points.GetLength(0), mg.points.GetLength(1));
        currentPos = pos;
        if (!(mg.points.GetLength(0) > pos.x && pos.x > 0 &&
            mg.points.GetLength(1) > pos.y && pos.y > 0)) return;

        passedStage3 = true;
        Point p = mg.points[pos.x, pos.y];
        if (p == null)
        {
            master.sharedInfo["playerHeight"] = "\0";
            master.sharedInfo["playerSlope"] = "\0";
            master.sharedInfo["playerAzimuth"] = "\0";
            master.sharedInfo["playerElevation"] = "\0";
            master.sharedInfo["pointCart"] = "\0";
            master.sharedInfo["pointGeo"] = "\0";
        }
        else
        {
            master.sharedInfo["playerHeight"] = $"{p.defaultHeight}";
            master.sharedInfo["playerSlope"] = $"{p.slope}";
            master.sharedInfo["playerAzimuth"] = $"{Math.Round(p.azimuth * Mathf.Rad2Deg, 2)}";
            master.sharedInfo["playerElevation"] = $"{Math.Round(p.elevationAngle * Mathf.Rad2Deg, 2)}";
            master.sharedInfo["pointCart"] = $"{p.defaultCartPos}";
            master.sharedInfo["pointGeo"] = $"{p.geoPos}";
        }
    }
}
