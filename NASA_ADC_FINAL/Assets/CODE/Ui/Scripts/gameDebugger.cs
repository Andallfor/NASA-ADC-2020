using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class gameDebugger : MonoBehaviour
{
    public bool enableDebugging = false;
    private TextMeshProUGUI cartX;
    private TextMeshProUGUI cartZ;
    private TextMeshProUGUI geoLat;
    private TextMeshProUGUI geoLon;
    private TextMeshProUGUI gridX;
    private TextMeshProUGUI gridY;
    private TextMeshProUGUI defaultH;
    private TextMeshProUGUI displayH;
    private TextMeshProUGUI slope;
    private TextMeshProUGUI isFake;
    private TextMeshProUGUI index;
    public LayerMask lMask;
    private List<TextMeshProUGUI> allDebugText = new List<TextMeshProUGUI>();
    private mapGenerator mg;
    private MeshRenderer mgr;
    private GameObject dbParent;
    public minimapController mmc;
    private bool textIsHidden = true;
    public void Start() // I REGRET NOTHINGGGGGGGGGGG
    {
        cartX = GameObject.FindGameObjectWithTag("debugUI/cartPos/x").GetComponent<TextMeshProUGUI>();
        cartZ = GameObject.FindGameObjectWithTag("debugUI/cartPos/z").GetComponent<TextMeshProUGUI>();
        geoLat = GameObject.FindGameObjectWithTag("debugUI/geoPos/lat").GetComponent<TextMeshProUGUI>();
        geoLon = GameObject.FindGameObjectWithTag("debugUI/geoPos/lon").GetComponent<TextMeshProUGUI>();
        gridX = GameObject.FindGameObjectWithTag("debugUI/gridPos/x").GetComponent<TextMeshProUGUI>();
        gridY = GameObject.FindGameObjectWithTag("debugUI/gridPos/y").GetComponent<TextMeshProUGUI>();
        defaultH = GameObject.FindGameObjectWithTag("debugUI/general/defaultH").GetComponent<TextMeshProUGUI>();
        displayH = GameObject.FindGameObjectWithTag("debugUI/general/displayH").GetComponent<TextMeshProUGUI>();
        slope = GameObject.FindGameObjectWithTag("debugUI/general/slope").GetComponent<TextMeshProUGUI>();
        isFake = GameObject.FindGameObjectWithTag("debugUI/general/fakePoint").GetComponent<TextMeshProUGUI>();
        index = GameObject.FindGameObjectWithTag("debugUI/general/index").GetComponent<TextMeshProUGUI>();
        dbParent = GameObject.FindGameObjectWithTag("debugUI/parent");

        allDebugText.AddRange(new List<TextMeshProUGUI>
        {
            cartX, cartZ, geoLat, geoLon, gridX, gridY, defaultH, displayH, slope, isFake, index
        });

        mg = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<mapGenerator>();
        mgr = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<MeshRenderer>();
        dbParent.SetActive(false);
    }

    public void Update()
    {
        if (enableDebugging)
        {
            if (textIsHidden)
            {
                textIsHidden = false;
                showText();
            }

            if (!mmc.expandedMap)
            {
                // not in world map
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit, 100, lMask))
                {
                    updateText(mg.points[(int) hit.point.x, (int) hit.point.z]);

                    Texture2D texture = new Texture2D(mg.xMeshLength, mg.yMeshLength);
                    texture.filterMode = FilterMode.Point;
                    texture.SetPixel((int) hit.point.x, (int) hit.point.z, Color.red);
                    texture.Apply();

                    mgr.material.mainTexture = texture;
                }
                else clearText();
            }
            else
            {
                if (mmc.currentMap == 0)
                {
                    int x = (int) mmc.c.transform.position.x;
                    int z = (int) mmc.c.transform.position.z;

                    if (!(x < 0 || x > mg.xMeshLength - 1|| z < 0 || z > mg.yMeshLength - 1))
                    {
                        Point p = mg.points[(int) mmc.c.transform.position.x, (int) mmc.c.transform.position.z];
                        updateText(p);
                    }
                    else clearText();
                }
                else clearText();
            }
        }
        else
        {
            if (!textIsHidden)
            {
                textIsHidden = true;
                hideText();
            }
        }
    }

    public void updateText(Point p)
    {
        cartX.text = $"x: {p.cartPos.x}";
        cartZ.text = $"z: {p.cartPos.z}";
        geoLat.text = $"lat: {p.geoPos.z}";
        geoLon.text = $"lon: {p.geoPos.x}";
        gridX.text = $"x: {p.gridPos.x}";
        gridY.text = $"y: {p.gridPos.z}";
        defaultH.text = $"DefaultH: {p.defaultHeight}";
        displayH.text = $"DisplayH: {p.displayHeight}";
        slope.text = $"Slope: {p.slope}";
        isFake.text = $"Faked: {p.fakePoint}";
        index.text = $"Index: {p.index}";
    }

    public void hideText()
    {
        dbParent.SetActive(false);
        Texture2D texture = new Texture2D(mg.xMeshLength, mg.yMeshLength);
        texture.Apply();
        mgr.material.mainTexture = texture;
    }

    public void showText()
    {
        dbParent.SetActive(true);
    }
    public void clearText()
    {
        foreach (TextMeshProUGUI t in allDebugText) t.text = "NA";
    }
}
