using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

//test case:-44000000f
//normal: -42100000f
public class ComLinks : MonoBehaviour
{
    //pulls up mesh points
    public bool[,] canSee;

    public int distance;
    public LayerMask lMask;
    private mapGenerator mg;
    public void Start()
    {
        mg = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<mapGenerator>();
    }
    private int reverseGridRound(float value, float step) => (int) Mathf.Floor(value / step);
    private float gridRound(float value, float step) => (Mathf.Floor(value / step) * step) + (step / 2f);

    public Texture2D code()
    {
        Mesh mesh = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<MeshFilter>().mesh;
        int downFactor = mg.currentGrid.downFactor * mg.parentGrid.downFactor;
        Vector3 earth = new Vector3(
            reverseGridRound(gridRound(361000000f + (float) mg.currentGrid.offset.x, mg.currentGrid.gridSize), mg.currentGrid.gridSize) / downFactor, 
            reverseGridRound(gridRound((-42100000f + (float) mg.currentGrid.offset.y), mg.currentGrid.gridSize), mg.currentGrid.gridSize) / downFactor, 
            reverseGridRound(gridRound((float) mg.currentGrid.offset.z, mg.currentGrid.gridSize), mg.currentGrid.gridSize) / downFactor);
        Texture2D boolMap = new Texture2D(mg.xMeshLength, mg.yMeshLength);
        canSee = new bool[mg.xMeshLength, mg.yMeshLength]; // this is used later in other algs so i need to actually save it

        distance = Mathf.Max(mg.xMeshLength, mg.yMeshLength);

        // 2d array with all points currently in the mesh
        node[,] cubes = mg.cubes;

        foreach (node c in cubes)
        {
            int x = (int) c.selfPosition.x;
            int y = (int) c.selfPosition.y;
            int z = (int) c.selfPosition.z;

            Vector3 input = new Vector3(x, y, z);
            RaycastHit hit;
            if (Physics.Raycast(input, earth, out hit, (float) Mathf.Infinity, lMask))
            {
                // cannot see the earth
                boolMap.SetPixel(x, z, Color.blue);
                canSee[x, z] = false;   
            }
            else
            {
                // can see the earth
                //Debug.DrawLine(new Vector3(x, y + 1, z), earth / 1_000, Color.red, 10000000f);
                boolMap.SetPixel(x, z, Color.green);
                canSee[x, z] = true;
            }
        }
        boolMap.Apply();
        boolMap.filterMode = FilterMode.Point;

        return boolMap;
    }

    private bool containsPoint(Vector3 point)
    {
        XYZ pos = new XYZ(Mathf.Round(point.x), 0, Mathf.Round(point.z));

        if (mg.currentGrid.grid.ContainsKey(pos)) return true;
        return false;
    }
}
