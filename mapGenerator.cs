using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class mapGenerator : MonoBehaviour
{
    public bool generateNewMap = false; // when true, generate a new map
    // please just keep these the same it just makes everything so much easier

    public int xMeshLength, yMeshLength;

    public bool cubesStart = false;

    // image processing stuff
    public Gradient grad, angleGrad;

    // lists used
    private float[,] surface; // doesnt actually need to be defined here, since im only using it once
    public node[,] cubes; // holds all nodes
    public Point[,] points; // stop judging | holds all points (woah)
    public Texture2D minimapTextureHeight, minimapTextureSlope, minimapTextureAzimuthAngle, minimapTextureElevationAngle;
    public GRID currentGrid, parentGrid;

    [HideInInspector] public ComLinks cl;

    
    void Start()
    {
        // initalize cubes array, with empty values
        cubes = new node[xMeshLength, yMeshLength];
        cl = GameObject.FindGameObjectWithTag("comLink").GetComponent<ComLinks>();
        master.init();
    }

    public void initalTerrainGeneration()
    {
        resetData();
        internalStart();
    }

    private async void internalStart()
    {
        // mapGeneratorTimer.startCounter("Total loading time");
        // mapGeneratorTimer.startCounter("Total data loading time");

        List<CSVFILESTORAGE> csv = loadCSV((master.selectedMap == 0) ? "griddedCSV" : "CSVFORADC");
        Task t = Task.Run(() => loadData(ref surface, csv));
        await t;

        parentGrid.generatePictures(20, (int) parentGrid.cartHeightBounds.y, (int) parentGrid.cartHeightBounds.height, grad, angleGrad);
        minimapTextureHeight = parentGrid.heightTexture;
        minimapTextureSlope = parentGrid.slopeTexture;
        minimapTextureAzimuthAngle = parentGrid.azimuthAngleTexture;
        minimapTextureElevationAngle = parentGrid.elevationAngleTexture;

        currentGrid.generatePictures(20, (int) currentGrid.cartHeightBounds.y, (int) currentGrid.cartHeightBounds.height, grad, angleGrad);
        master.nextStep();

        // mapGeneratorTimer.endCounter("Generating child float map");
        // mapGeneratorTimer.quickEndStart("Total data loading time", "Total mesh loading time");
        genMap(surface, ref currentGrid);
        // mapGeneratorTimer.endCounter("Total mesh loading time");
        // mapGeneratorTimer.endCounter("Total loading time");

        //mapGeneratorTimer.loadCounters();
        master.terrainFinishedGenerating = true;
    }

    void genMap(float[,] surface, ref GRID g)
    {
        drawMap(surface);
        setNodeNeighbors();
        generateMesh();
        g.booleanAzimuthTexture = cl.code();
    }

    public async void createDisplayGrid(GRID g)
    {
        GameObject.FindGameObjectWithTag("loadingScreen").GetComponent<loadingController>().playText.text = "LOADING";
        Task t = Task.Run(() => asyncCreateDisplayGrid(ref g));
        await t;
        master.nextStep();
        g.generatePictures(15, (int) g.cartHeightBounds.y, (int) g.cartHeightBounds.height, grad, angleGrad);

        currentGrid = g;

        master.nextStep();
        master.updatePercent(-1);
        this.genMap(g.generateFloatMap(), ref g);
        master.terrainFinishedGenerating = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.transform.position = cubes[(int) Mathf.Floor(xMeshLength / 4), (int) Mathf.Floor(yMeshLength / 4)].selfPosition;
        player.transform.position += new Vector3(0, 2, 0);

        AStar a = GameObject.FindGameObjectWithTag("astar").GetComponent<AStar>();
        a.l.positionCount = 0;
        List<GameObject> previousLinks = GameObject.FindGameObjectsWithTag("cl").ToList();
        foreach (GameObject go in previousLinks) Destroy(go);
    }
    private void asyncCreateDisplayGrid(ref GRID g)
    {
        kernel gBlur3 = new kernel(new int[3,3]{
            {1, 2, 1},
            {2, 4, 2},
            {1, 2, 1}
        }, (1f/16f));
        g.centerGrid();
        master.nextStep();
        g.generateGrid(1, false);
        master.nextStep();
        g.scaleTo(400, 1, gBlur3, false);;
        g.downFactor = g.downFactor / this.parentGrid.downFactor;
        master.nextStep();
        points = g.generatePointMap();
        master.nextStep();
    }

    public void resetData()
    {
        cubesStart = false;
        points = new Point[0,0];
        cubes = new node[0,0];
    }

    public void loadData(ref float[,] r, List<CSVFILESTORAGE> csv) // each operation on all 7mil points takes ~15-25 seconds
    {
        kernel gBlur3 = new kernel(new int[3,3]{
            {1, 2, 1},
            {2, 4, 2},
            {1, 2, 1}
        }, (1f/16f));
        
        GRID baseGrid = new GRID();

        baseGrid.unloadCSV(csv);
        master.nextStep();

        baseGrid.centerGrid();
        master.nextStep();

        baseGrid.generateGrid(1, false);
        master.nextStep();

        baseGrid.scaleTo(1250, 1, gBlur3, false);
        master.nextStep();
        master.updatePercent(-1);

        baseGrid.key = 1;

        parentGrid = new GRID(baseGrid);

        GRID displayGrid = new GRID(baseGrid);
        master.nextStep();

        displayGrid.scaleTo(400, 1, gBlur3, false);
        master.nextStep();
        
        master.updatePercent(-1);
        displayGrid.key = 2;
        currentGrid = new GRID(displayGrid);
        master.nextStep();

        points = displayGrid.generatePointMap();

        r = displayGrid.generateFloatMap();
        master.nextStep();
        master.updatePercent(-1);
    }

    List<CSVFILESTORAGE> loadCSV(string file = "smallerCSV")
    {
        List<CSVFILESTORAGE> returnList = new List<CSVFILESTORAGE>();

        TextAsset data = (TextAsset) Resources.Load($"MoonData/{file}");

        CSVFILESTORAGE currentStorage = new CSVFILESTORAGE();
        int varIndex = 0;
        StringBuilder sb = new StringBuilder();

        foreach (char c in data.text.ToCharArray())
        {
            switch (c)
            {
                case '\0':
                    break;
                case '\n':
                    // new line
                    currentStorage.slope = Convert.ToDouble(sb.ToString());
                    returnList.Add(currentStorage);
                    currentStorage = new CSVFILESTORAGE();
                    varIndex = 0;
                    sb.Clear();
                    break;
                case ',':
                    // add new value
                    switch (varIndex)
                    {
                        case 0:
                            currentStorage.lat = Convert.ToDouble(sb.ToString());
                            break;
                        case 1:
                            currentStorage.lon = Convert.ToDouble(sb.ToString());
                            break;
                        case 2:
                            currentStorage.height = Convert.ToDouble(sb.ToString());
                            break;
                    }
                    sb.Clear();
                    varIndex++;
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return returnList;
    }


    // regular stuff
    void drawMap(float[,] map)
    {
        xMeshLength = map.GetLength(0) - 1;
        yMeshLength = map.GetLength(1) - 1;
        cubes = new node[xMeshLength, yMeshLength]; // redunant

        // loop through x and y to get every single point that would be used
        for (int x = 0; x < xMeshLength; x++)
        {
            for (int y = 0; y < yMeshLength; y++)
            {
                // convert from local units (1,2,3,4, etc) to global units
                float posX = (float) x;
                float posY = (float) y;

                // get the desired height for the point (set in createMap())
                float height = map[x,y];

                // generate a real world position (for visualization purposes)
                Vector3 pos = new Vector3(posX, height, posY);

                // assign a node with the new values
                cubes[x, y] = new node(pos, new int[] {x, y}, height);
            }
        }
    }

    void generateMesh() // generate a mesh
    {
        // create a new empty mesh
        Mesh mesh = new Mesh();
        mesh = GetComponent<MeshFilter>().mesh;
        MeshCollider meshCol = GetComponent<MeshCollider>();
        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        // create two lists, once with all vertices, and one with each triangle
        Vector3[] allVertices = findVertices();
        List<int> tri = new List<int>();

        Dictionary<Vector2, int> vertexKey = new Dictionary<Vector2, int>();
        int index = 0;
        foreach (Vector3 p in allVertices)
        {
            vertexKey.Add(new Vector2(p.x, p.z), index);
            index++;
        }

        // iterate through all points (minus the points that dont have a neighbor)
        for (int y = 0; y < yMeshLength - 1; y++)
        {
            for (int x = 0; x < xMeshLength - 1; x++)
            {
                Vector2 pos = new Vector2(x, y);
                tri.Add(vertexKey[pos]);
                tri.Add(vertexKey[pos + new Vector2(0, 1)]);
                tri.Add(vertexKey[pos + new Vector2(1, 1)]);
                tri.Add(vertexKey[pos]);
                tri.Add(vertexKey[pos + new Vector2(1, 1)]);
                tri.Add(vertexKey[pos + new Vector2(1, 0)]);
            }
        }

        List<Vector2> uvs = new List<Vector2>();
        foreach (Vector3 v in allVertices)
        {
            uvs.Add(new Vector2(v.x / (float) xMeshLength, v.z / (float) yMeshLength));
        }

        // give the mesh the vertices and triangles
        mesh.vertices = allVertices;
        mesh.triangles = tri.ToArray();
        mesh.uv = uvs.ToArray();

        // reload the mesh
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // finsih setting up by calling some other functions
        meshCol.sharedMesh = mesh;
        GameObject.FindGameObjectWithTag("wallParent").GetComponent<wallGenerator>().generateWalls();
        GameObject.FindGameObjectWithTag("minimapController").GetComponent<minimapController>().generateMinimapMesh();
    }

    Vector3[] findVertices() // get the real world position of each node
    {
        // this code is kinda stupid, would be much easier just to save each real world pos in the creation phase
        // but it works so im not changing it

        // init an empty list
        List<Vector3> returnList = new List<Vector3>();

        // iterate through all nodes
        int index = 0;
        foreach (node n in cubes)
        {
            // assign its position
            returnList.Add(n.selfPosition);
            index++;
        }

        return returnList.ToArray();
    }

    void setNodeNeighbors() // each node has a neighbor value, so set it here
    {
        foreach (node n in cubes) // iterate through all nodes, which also is all points
        {
            // create a list of all directions to be checked, for ease
            int[,] dir = new int[8,2] {{-1,1}, {0,1}, {1,1}, {1,0}, {1,-1}, {-1,0}, {-1,-1}, {0,-1}};

            // iterate through the directions list
            for (int i = 0; i < 8; i++)
            {
                // assign newPos as the current node position + the direction (to get the node in that direction)
                int[] newPos = new int[] {n.lPos[0] + dir[i,0], n.lPos[1] + dir[i,1]};

                // init the neighbor as null
                node neighbor = null;

                // check if the newPos is in bounds or not
                if (newPos[0] >= 0 && newPos[1] >= 0 && newPos[0] < xMeshLength && newPos[1] < yMeshLength)
                {
                    // is in bounds

                    // assign the neighbor with the info of the node at newPos
                    neighbor = cubes[newPos[0], newPos[1]];

                    // give the neighbors info to the node
                    n.nearbyNodes.Add(new int[] {dir[i,0], dir[i,1]}, neighbor);

                    // do some math to get the angle between the node and the neighbor
                    // (check this, it might not be right - I suck at math)
                    float dHeight = neighbor.height - n.height; // pos if neighbor is above, neg if below
                    // adj is always 1
                    float angle = Mathf.Atan(dHeight); // in radians

                    // give the angle info to the node
                    n.angleToNode.Add(new Vector2(dir[i,0], dir[i,1]), angle);
                }
            }
        }
    }
}
