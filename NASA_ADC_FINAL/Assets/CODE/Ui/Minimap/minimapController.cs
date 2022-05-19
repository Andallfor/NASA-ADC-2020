using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class minimapController : MonoBehaviour
{
    public Camera c;
    public float height, scale;
    public Canvas canvas;
    public GameObject player, playerRep, minimapInfoParent, minimapMesh, gridOverlayMesh, confirm, deny;
    public Material matCircle, matSquare, meshReg, meshMap;
    public Image img, mapKey;
    public bool expandedMap, chunkSelectionIsActive = false, showGridTexture = false, selectedWorldMap = true, waitingForChunkSelectionToEnd = false;
    public Vector2 offset = new Vector2(), currentCameraGridBox;
    public LayerMask defaultCull, worldMapCull;
    public int currentMap = 0;
    private TextMeshProUGUI worldMapName;
    public mapGenerator mg;
    public (XYZ start, XYZ end) currentSelectedPoints;
    public Sprite slopeKey, heightKey, angleKey, boolAzimuthKey; // set in inspector

    private Vector2 initalMousePosition;
    private loadingController lc;

    public void generateMinimapMesh()
    {
        mapGenerator mg = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<mapGenerator>();
        // set up minimap mesh
        Mesh mesh = new Mesh();
        mesh.Clear();

        Vector3[] verts = new Vector3[4]
        {
            new Vector3(0, -10, 0),
            new Vector3(0, -10, mg.yMeshLength),
            new Vector3(mg.xMeshLength, -10, 0),
            new Vector3(mg.xMeshLength, -10, mg.yMeshLength)
        };

        int[] tri = new int[6]
        {
            0, 1, 3,
            0, 3, 2
        };

        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(1, 1)
        };

        mesh.vertices = verts;
        mesh.triangles = tri;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        minimapMesh.GetComponent<MeshFilter>().mesh = mesh;
        gridOverlayMesh.GetComponent<MeshFilter>().mesh = mesh;
        Texture2D texture = new Texture2D(mg.xMeshLength, mg.yMeshLength);
        for (int x = 0; x < mg.xMeshLength; x++)
        {
            for (int y = 0; y < mg.yMeshLength; y++)
            {
                texture.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }

        texture.Apply();
        gridOverlayMesh.GetComponent<MeshRenderer>().material.mainTexture = texture;
    }

    public void Start()
    {
        worldMapName = GameObject.FindGameObjectWithTag("worldmap/mapName").GetComponent<TextMeshProUGUI>();
        minimapInfoParent = GameObject.FindGameObjectWithTag("worldmap/parent");
        gridOverlayMesh = GameObject.FindGameObjectWithTag("worldmap/gridsizeOverlay");
        lc = GameObject.FindGameObjectWithTag("loadingScreen").GetComponent<loadingController>();

        minimapInfoParent.SetActive(false);
        mapKey.gameObject.SetActive(false);
        deny.SetActive(false);
        confirm.SetActive(false);
    }
    public void startMouseRecord()
    {
        initalMousePosition = getMouseWorldPosition();
        chunkSelectionIsActive = true;
    }
    public void generateGridTexture()
    {
        if (gridOverlayMesh.activeInHierarchy == false) 
        {
            gridOverlayMesh.SetActive(true);
        }

        Vector2 currentMousePosition = getMouseWorldPosition();
        // adjust it so that it is a square
        float minDist = Mathf.Min(Mathf.Abs(currentMousePosition.x - initalMousePosition.x), Mathf.Abs(currentMousePosition.y - initalMousePosition.y));
        float yDist = minDist * ((currentMousePosition.y < initalMousePosition.y) ? -1 : 1);
        float xDist = minDist * ((currentMousePosition.x < initalMousePosition.x) ? -1 : 1);
        currentMousePosition.y = initalMousePosition.y + yDist;
        currentMousePosition.x = initalMousePosition.x + xDist;

        Vector2Int maxPos = new Vector2Int((int) Mathf.Max(currentMousePosition.x, initalMousePosition.x), (int) Mathf.Max(currentMousePosition.y, initalMousePosition.y));
        Vector2Int minPos = new Vector2Int((int) Mathf.Min(currentMousePosition.x, initalMousePosition.x), (int) Mathf.Min(currentMousePosition.y, initalMousePosition.y));

        Texture2D texture = new Texture2D(mg.xMeshLength, mg.yMeshLength);

        // probably should switch this out for a line renderer
        for (int x = 0; x < mg.xMeshLength; x++)
        {
            for (int y = 0; y < mg.yMeshLength; y++)
            {
                if ((x >= minPos.x && x <= maxPos.x) && (y >= minPos.y && y <= maxPos.y))
                {
                    if (x == minPos.x || x == maxPos.x || y == minPos.y || y == maxPos.y) texture.SetPixel(x, y, Color.red);
                    else texture.SetPixel(x, y, new Color(0,0,0,0.25f));
                }
                else
                {
                    texture.SetPixel(x, y, new Color(0,0,0,0));
                }
            }
        }

        currentSelectedPoints = (
            new XYZ(Mathf.Min(initalMousePosition.x, currentMousePosition.x), 0, Mathf.Min(initalMousePosition.y, currentMousePosition.y)),
            new XYZ(Mathf.Max(initalMousePosition.x, currentMousePosition.x), 0, Mathf.Max(initalMousePosition.y, currentMousePosition.y))
        );

        texture.Apply();
        texture.filterMode = FilterMode.Point;
        gridOverlayMesh.GetComponent<MeshRenderer>().material.mainTexture = texture;
    }
    private Vector2 getMouseWorldPosition()
    {
        float length = c.orthographicSize * 2f;

        // % distance * length
        float x = (Input.mousePosition.x / Screen.width) * length;
        float y = (Input.mousePosition.y / Screen.height) * length * ((float) Screen.height / (float) Screen.width); // 16:9, scales with x
        // if camera takes up 50% of area, and is in the middle, then the bottom would be 25%, and top 25%
        float yOffset = (1 - ((float) Screen.height / (float) Screen.width))/2f * length;
        y += yOffset;

        // center it relative to camera position
        Vector2 bottomLeft = ToVector2(c.transform.position) - new Vector2(length/2f, length/2f);

        Vector2 returnVector = bottomLeft + new Vector2(x, y);
        returnVector.x = Mathf.Min(Mathf.Max(1, returnVector.x), mg.xMeshLength - 1);
        returnVector.y = Mathf.Min(Mathf.Max(2, returnVector.y), mg.yMeshLength - 2); // walls cover it

        return returnVector;
    }

    public void disableGridTexture()
    {
        gridOverlayMesh.SetActive(false);
        confirm.SetActive(false);
        deny.SetActive(false);
        chunkSelectionIsActive = false;
        waitingForChunkSelectionToEnd = false;
    }
    public void enableGridTexture()
    {
        gridOverlayMesh.SetActive(true);
        confirm.SetActive(true);
        deny.SetActive(true);
        chunkSelectionIsActive = true;
    }
    public void loadNewChunk()
    {
        disableGridTexture();
        XYZ sp = currentSelectedPoints.start;
        XYZ ep = currentSelectedPoints.end;

        double w = mg.parentGrid.gridBounds.width;
        double h = mg.parentGrid.gridBounds.height;

        XYZ scaledStart = new XYZ(
            x : Mathf.Max(Mathf.Min((float) Math.Floor(w * (sp.x / mg.currentGrid.gridBounds.width)), (float) mg.parentGrid.gridBounds.width), 0),
            z : Mathf.Max(Mathf.Min((float) Math.Floor(h * (sp.z / mg.currentGrid.gridBounds.height)), (float) mg.parentGrid.gridBounds.height), 0));

        XYZ scaledEnd = new XYZ(
            x : Mathf.Max(Mathf.Min((float) Math.Floor(w * (ep.x / mg.currentGrid.gridBounds.width)), (float) mg.parentGrid.gridBounds.width), 0),
            z : Mathf.Max(Mathf.Min((float) Math.Floor(h * (ep.z / mg.currentGrid.gridBounds.height)), (float) mg.parentGrid.gridBounds.height), 0));

        lc.reset();
        master.addChunkLoadSteps();

        StartCoroutine(delayFrame(scaledStart, scaledEnd));
    }
    private IEnumerator delayFrame(XYZ scaledStart, XYZ scaledEnd)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        mg.createDisplayGrid(
            mg.parentGrid.getPointsInGridFromGridPos(
                scaledStart, 
                scaledEnd));
    }

    public void Update()
    {
        if (!master.terrainFinishedGenerating) return;

        height = (float) mg.currentGrid.cartHeightBounds.height + 50f;

        c.transform.position = new Vector3(
            player.transform.position.x + offset.x, 
            height, 
            player.transform.position.z + offset.y);
        
        scale = Mathf.Min(Mathf.Max(scale, 10), 350);
        c.orthographicSize = scale;
        playerRep.transform.position = new Vector3(player.transform.position.x, height - 5, player.transform.position.z);

        if (currentMap != 1 && currentMap != 2) showGridTexture = false; // only allow grid on world maps
        if (currentMap <= 4 && currentMap != 0 && !expandedMap) defaultCam(); // only allow local maps to be shown on smaller minimap
    }

    public void defaultCam()
    {
        currentMap = 0;
        c.cullingMask = defaultCull;
        worldMapName.text = "Default Map";
        mapKey.gameObject.SetActive(false);
    }
    private float gridRound(float value, float step) => (Mathf.Floor(value / step) * step) + (step / 2f);
    public void switchToWorldMap(int mapNumber) // texture doesnt seem to be centered correctly? maybe rotate it?
    {
        currentMap = mapNumber;
        c.cullingMask = worldMapCull;
        mapKey.gameObject.SetActive(true);

        Texture2D t = new Texture2D(0,0);

        switch (currentMap)
        {
            case 1:
                worldMapName.text = "Height Map (World)";
                mapKey.overrideSprite = heightKey;
                t = mg.minimapTextureHeight;
                break;
            case 2:
                worldMapName.text = "Height Map (World)";
                mapKey.overrideSprite = slopeKey;
                t = mg.minimapTextureSlope;
                break;
            case 3:
                worldMapName.text = "Elevation Angle Map (World)";
                mapKey.overrideSprite = angleKey;
                t = mg.minimapTextureElevationAngle;
                break;
            case 4:
                worldMapName.text = "Azimuth Map (World)";
                mapKey.overrideSprite = angleKey;
                t = mg.minimapTextureAzimuthAngle;
                break;

            case 5:
                worldMapName.text = "Height Map (Local)";
                mapKey.overrideSprite = heightKey;
                t = mg.currentGrid.heightTexture;
                break;
            case 6:
                worldMapName.text = "Height Map (Local)";
                mapKey.overrideSprite = slopeKey;
                t = mg.currentGrid.slopeTexture;
                break;
            case 7:
                worldMapName.text = "Elevation Angle Map (Local)";
                mapKey.overrideSprite = angleKey;
                t = mg.currentGrid.elevationAngleTexture;
                break;
            case 8:
                worldMapName.text = "Azimuth Map (Local)";
                mapKey.overrideSprite = angleKey;
                t = mg.currentGrid.azimuthAngleTexture;
                break;
            case 9:
                worldMapName.text = "Boolean Azimuth Map (Local)";
                mapKey.overrideSprite = boolAzimuthKey;
                t = mg.currentGrid.booleanAzimuthTexture;
                break;
            default:
                throw new ArgumentException($"Case {currentMap} is not recognized");
        }

        minimapMesh.GetComponent<MeshRenderer>().material.mainTexture = t;
        minimapMesh.GetComponent<MeshRenderer>().material.SetFloat("_Glossiness", 0f);
    }
    private Vector2 ToVector2(Vector3 v) => new Vector2(v.x, v.z);

    // divide to normalize size
    public int totalArea() => (int) ((currentSelectedPoints.end.x - currentSelectedPoints.start.x) * (int) (currentSelectedPoints.end.z - currentSelectedPoints.start.z) * 400f/Mathf.Max(mg.xMeshLength, mg.yMeshLength));
}