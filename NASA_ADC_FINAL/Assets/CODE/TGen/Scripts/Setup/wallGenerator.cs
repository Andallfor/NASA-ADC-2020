using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class wallGenerator : MonoBehaviour
{
    mapGenerator mg;
    public GameObject wallPrefab;
    void Start()
    {
        mg = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<mapGenerator>();
    }
    public void generateWalls()
    {
        List<GameObject> previousWalls = GameObject.FindGameObjectsWithTag("wall").ToList();
        foreach (GameObject w in previousWalls) Destroy(w);

        float height = ((float) mg.currentGrid.cartHeightBounds.height + 1f) * 0.75f;
        Transform parent = GameObject.FindGameObjectWithTag("wallParent").transform;

        Vector3[] positions = new Vector3[4] {
            new Vector3(-0.5f, height, mg.yMeshLength/2  - 0.5f),
            new Vector3(mg.xMeshLength/2 - 0.5f, height, -0.5f),
            new Vector3(mg.xMeshLength/2 - 0.5f, height, mg.yMeshLength - 0.5f),
            new Vector3(mg.xMeshLength - 0.5f, height, mg.yMeshLength/2 - 0.5f)
        };

        Vector3[] scales = new Vector3[4] {
            new Vector3(1, height * 2, mg.yMeshLength),
            new Vector3(mg.xMeshLength, height * 2, 1),
            new Vector3(mg.xMeshLength, height * 2, 1),
            new Vector3(1, height * 2, mg.yMeshLength)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject wall = Instantiate(wallPrefab, positions[i], Quaternion.identity, parent);
            wall.transform.localScale = scales[i];
        }
    }
}
