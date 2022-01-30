using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class settingsController : MonoBehaviour
{
    private mapGenerator mg;
    private GameObject player;
    private controls c;
    private AStar a;

    public void Start()
    {
        mg = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<mapGenerator>();
        player = GameObject.FindGameObjectWithTag("Player");
        c = GameObject.FindGameObjectWithTag("controlHub").GetComponent<controls>();
        a = GameObject.FindGameObjectWithTag("astar").GetComponent<AStar>();
    }

    public void returnToMenu()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    public void quit()
    {
        Application.Quit();
    }

    public void respawn()
    {
        player.transform.position = mg.cubes[(int) Mathf.Floor(mg.xMeshLength / 4), (int) Mathf.Floor(mg.yMeshLength / 4)].selfPosition;
        player.transform.position += new Vector3(0, 2, 0);
    }

    public void toggleDebug()
    {
        c.toggleDebugging();
    }

    public void clearWayfind()
    {
        a.l.positionCount = 0;
        List<GameObject> previousLinks = GameObject.FindGameObjectsWithTag("cl").ToList();
        foreach (GameObject go in previousLinks) Destroy(go);
    }
}
