using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.EventSystems;

public class controls : MonoBehaviour
{
    public GameObject mc;
    private minimapController mmc;
    private RectTransform mcrt;
    public string expandMap;
    public string zoomIn;
    public string zoomOut;
    public string mTeleport;

    [Header("-----")]
    public GameObject pl;
    private playerMovement plm;

    [Header("-----")]
    public GameObject tm;
    private mapGenerator tmg;
    private Mesh mesh;
    [Header("-----")]
    public GameObject ch;
    private crosshairController chc;
    [Header("-----")]
    public string setStart;
    public string setEnd;
    public string toggleMapType;
    public string setMapToDefault;
    public string setMapToHeight;
    public string setMapToSlope;
    public string setMapToElevationAngle;
    public string setMapToAzimuthAngle;
    public string setLocalMapToBooleanAzimuth;
    [Header("------")]
    public GameObject ai;
    private alg aia;
    private AIDebugger aid;
    [Header("------")]
    public gameDebugger gd;
    [Header("------")]
    public GameObject aStar;
    private AStar astar;
    public string setPositionsToDefault;
    [Header("------")]
    public GameObject it;
    [Header("------")]
    public helpController hc;


    void Start()
    {
        mmc = mc.GetComponent<minimapController>();
        mcrt = mc.GetComponent<RectTransform>();
        plm = pl.GetComponent<playerMovement>();
        tmg = tm.GetComponent<mapGenerator>();
        mesh = tm.GetComponent<MeshFilter>().mesh;
        chc = ch.GetComponent<crosshairController>();
        aia = ai.GetComponent<alg>();
        aid = ai.GetComponent<AIDebugger>();
        astar = aStar.GetComponent<AStar>();
    }

    void Update()
    {
        if (!master.enableControls) return;

        {// minimap control
        if (Input.GetKeyDown(expandMap))
        {
            if (mmc.expandedMap)
            {
                // close map
                tmg.GetComponent<Renderer>().material = mmc.meshReg;
                chc.setTexture(chc.crosshairRegular);
                mmc.expandedMap = false;
                mmc.img.material = mmc.matCircle;
                plm.start = true;
                
                mcrt.anchoredPosition = new Vector3(-80, -80, 0);
                mcrt.sizeDelta = new Vector2(150, 150);
                mmc.offset = new Vector2();
                mmc.scale = 20;
                Cursor.lockState = CursorLockMode.Locked;
                mmc.minimapInfoParent.SetActive(false);
                mmc.showGridTexture = false;
                it.SetActive(true);
                hc.gameObject.SetActive(true);
            }
            else if (!mmc.expandedMap)
            {
                // open map
                tmg.GetComponent<Renderer>().material = mmc.meshMap;
                chc.setTexture(chc.crosshairMap);
                mmc.expandedMap = true;
                mmc.img.material = mmc.matSquare;
                plm.start = false;
                Cursor.lockState = CursorLockMode.None;

                mcrt.localPosition = new Vector3(0, 0, 0);

                Vector3[] corners = new Vector3[4];
                mmc.canvas.GetComponent<RectTransform>().GetLocalCorners(corners);

                float height = Mathf.Abs(corners[0].y - corners[1].y);
                float width = Mathf.Abs(corners[1].x - corners[2].x);

                mcrt.sizeDelta = new Vector2(Mathf.Max(width, height), Mathf.Max(width, height));
                mmc.scale = 75;
                mmc.minimapInfoParent.SetActive(true);
                it.SetActive(true);

                foreach (optionDisplayController odc in hc.controllers)
                {
                    if (odc.open) odc.toggle(true);
                }

                if (hc.isOpen) hc.toggleHelp(true);
                hc.gameObject.SetActive(false);
            }
        }
        if (mmc.expandedMap)
        {
            if (Input.GetKey("d")) mmc.offset += new Vector2(1,0);
            if (Input.GetKey("a")) mmc.offset += new Vector2(-1,0);
            if (Input.GetKey("w")) mmc.offset += new Vector2(0,1);
            if (Input.GetKey("s")) mmc.offset += new Vector2(0,-1);
            
            // chunk loading
            if (mmc.currentMap >= 1 && mmc.currentMap <= 4)
            {
                // is a world map
                // dont allow anything to happen if theyre trying to click on a button or something
                if (Input.GetMouseButtonDown(0))
                {
                    if (!mmc.chunkSelectionIsActive && !mmc.waitingForChunkSelectionToEnd) 
                    {
                        mmc.startMouseRecord();
                        mmc.chunkSelectionIsActive = true;
                        mmc.waitingForChunkSelectionToEnd = false;
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    // show confirm and deny button if mmc.chunkSelectionIsActive is true
                    if (mmc.chunkSelectionIsActive) 
                    {
                        // the selected area must be a certain size
                        if (mmc.totalArea() < 1250) mmc.disableGridTexture();
                        else 
                        {
                            mmc.enableGridTexture();
                            mmc.chunkSelectionIsActive = false;
                            mmc.waitingForChunkSelectionToEnd = true;
                        }
                    }
                }
                else if (Input.GetMouseButton(0))
                {
                    // again, allow draging if mmc.chunkSelectionIsActive
                    if (mmc.chunkSelectionIsActive && !mmc.waitingForChunkSelectionToEnd) mmc.generateGridTexture();
                }
            }

            if (Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                mmc.scale -= Input.GetAxis("Mouse ScrollWheel") * 10;
            }
            if (Input.GetKeyDown(mTeleport))
            {
                if (mmc.currentMap != 1 && mmc.currentMap != 2)
                {
                    mmc.offset = new Vector2();
                    Vector3 telPos = mmc.c.transform.position;
                    
                    Vector2Int nodePos = new Vector2Int((int) telPos.x, (int) telPos.z);
                    try // i mean its like used so rarely, so it doesnt reallyyyyy impact performance
                    {
                        telPos.y = tmg.cubes[nodePos.x, nodePos.y].selfPosition.y + 5f;
                        pl.transform.position = telPos;
                    }
                    catch{}
                }
            }
        }
        else
        {
            if (Input.GetKey(zoomIn))
            {
                mmc.scale -= 0.5f;
            }
            else if (Input.GetKey(zoomOut))
            {
                mmc.scale += 0.5f;
            }
        }
        
        int offset = (mmc.selectedWorldMap) ? 0 : 4;
        if (Input.GetKeyDown(setMapToHeight)) mmc.switchToWorldMap(1 + offset);
        if (Input.GetKeyDown(setMapToSlope)) mmc.switchToWorldMap(2 + offset);
        if (Input.GetKeyDown(setMapToElevationAngle)) mmc.switchToWorldMap(3 + offset);
        if (Input.GetKeyDown(setMapToAzimuthAngle)) mmc.switchToWorldMap(4 + offset);
        if (Input.GetKeyDown(setLocalMapToBooleanAzimuth)) mmc.switchToWorldMap(9);
        if (Input.GetKeyDown(setMapToDefault)) mmc.defaultCam();
        if (Input.GetKeyDown(toggleMapType)) 
        {
            mmc.selectedWorldMap = !mmc.selectedWorldMap;
            astar.sendUpdate((mmc.selectedWorldMap) ? "Rendering set to world maps" : "Rendering set to local maps");
        }
        }

        {// player control
        if (Input.GetMouseButtonDown(1))
        {
            plm.start = !plm.start;
            if (!plm.start) Cursor.lockState = CursorLockMode.None;
            else Cursor.lockState = CursorLockMode.Locked;
        }
        if (Input.GetKeyDown(setStart))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Vector2Int pos = new Vector2Int((int) hit.point.x, (int) hit.point.z);
                aia.pAI.setStartEnd(pos, aia.endPos, genMarkers: false);
                aia.pAI.debugUpdate();
            }
        }
        if (Input.GetKeyDown(setEnd))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Vector2Int pos = new Vector2Int((int) hit.point.x, (int) hit.point.z);
                aia.pAI.setStartEnd(aia.startPos, pos, genMarkers: false);
                aia.pAI.debugUpdate();
            }
        }
        }

        {// ui control

        }
    
        {// a* control

            if (Input.GetKeyDown(setPositionsToDefault)) astar.setPosToDefault();
        }
    }

    public void toggleDebugging()
    {
        gd.enableDebugging = !gd.enableDebugging;
    }
}
