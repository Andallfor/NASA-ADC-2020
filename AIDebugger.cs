using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class AIDebugger : MonoBehaviour
{
    public bool enableDebugging = false;
    public int iterCurrentStep = 0;

    public List<attempt> runs = new List<attempt>();

    Mesh m;
    mapGenerator mg;
    LineRenderer l;
    GameObject t;
    GameObject wc;
    public alg a;

    public void Start()
    {
        m = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<MeshFilter>().mesh;
        mg = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<mapGenerator>();
        l = GetComponent<LineRenderer>();
        t = (GameObject) Resources.Load("AI/AIQValue", typeof(GameObject));
        wc = GameObject.FindGameObjectWithTag("worldCanvas");
        a = this.gameObject.GetComponent<alg>();
    }

    public void drawLine(List<Vector2Int> previousMoves)
    {
        l.positionCount = 0;
        l.positionCount = previousMoves.Count;
        Vector2Int currentPosition = new Vector2Int((int) a.startPos.x, (int) a.startPos.y);
        List<Vector3> positions = new List<Vector3>();

        foreach (Vector2Int v in previousMoves)
        {
            currentPosition += v;
            positions.Add(mg.cubes[currentPosition.x, currentPosition.y].selfPosition + new Vector3(0, 0.125f, 0));

        }
        l.SetPositions(positions.ToArray());

        Vector2Int[] directions = new Vector2Int[] {
            new Vector2Int(0,1), 
            new Vector2Int(1,0), 
            new Vector2Int(0,-1),  
            new Vector2Int(-1,0)};
    }
}

public class attempt
{
    public List<State> states = new List<State>();
    public List<State> qStates = new List<State>(); // instead of angles, it has the q value
    public List<Vector2Int> moves = new List<Vector2Int>();
    public bool succeded; 
    public attemptInfo info;

    public attempt(List<State> states, List<State> qStates, List<Vector2Int> moves, bool succeded, attemptInfo aInfo)
    {
        this.states = states;
        this.qStates = qStates;
        this.moves = moves;
        this.succeded = succeded;
        this.info = aInfo;
    }

    public attempt(attempt other)
    {
        this.states = other.states;
        this.qStates = other.qStates;
        this.moves = other.moves;
        this.succeded = other.succeded;
        this.info = other.info;
    }
}

public class attemptInfo
{
    public int closestPosition;
    public int exitCode;
    public int closestPosIter;
    public int loopNumber;

    public attemptInfo(int closestPosition, int exitCode, int closestPosIter, int loopNumber)
    {
        this.closestPosition = closestPosition;
        this.exitCode = exitCode;
        this.closestPosIter = closestPosIter;
        this.loopNumber = loopNumber;
    }
}