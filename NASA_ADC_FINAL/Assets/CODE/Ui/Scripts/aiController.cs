using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using System.Linq;

public class aiController : MonoBehaviour
{
    public alg ai;
    public TMP_InputField iterations, maxMoves, dWeight, sWeight, vWeight, maxSlope;
    public Slider epsilon, alpha, discount;
    public GameObject atr, stats, run, end;
    public TextMeshProUGUI totalIters, totalS, totalF, currentInspect;
    public TMP_InputField lineNumber, stepNumber;
    public AIDebugger s;
    public LineRenderer l;
    public GameObject comLinkPrefab;
    public mapGenerator mg;
    public ComLinks cl;

    // 0 -> default (creating ai values)
    // 1 -> inspecting ai run
    // 2 -> inspecting ai line
    private int mode;

    private bool inspectingLine = false;
    private int selectedLine = 0;
    private int lineStep = 0;

    public void startRun()
    {
        ai.totalIterationsEach = (int) float.Parse(iterations.text.Trim());
        ai.maxMoves = (int) float.Parse(maxMoves.text.Trim());
        ai.explorationRate = epsilon.value;
        ai.learningRate = alpha.value;
        ai.discount = discount.value;
        ai.distanceWeight = float.Parse(dWeight.text.Trim());
        ai.slopeWeight = float.Parse(sWeight.text.Trim());
        ai.visibilityWeight = float.Parse(vWeight.text.Trim());
        ai.maxSlope = float.Parse(maxSlope.text.Trim());

        ai.pAI.beginNewIter();
        atr.transform.localPosition = new Vector3(0, -270, 0);
        mode = 1;
        stats.transform.GetChild(0).gameObject.SetActive(true);
        stats.transform.GetChild(1).gameObject.SetActive(false);
        stats.SetActive(true);
        run.SetActive(false);
        end.SetActive(true);
        List<GameObject> previousLinks = GameObject.FindGameObjectsWithTag("cl").ToList();
        foreach (GameObject go in previousLinks) Destroy(go);
    }
    public void endRun()
    {
        end.SetActive(false);
        run.SetActive(true);
        ai.totalIterationsEach = 0;
        mode = 0;
        stats.SetActive(false);
        stats.transform.GetChild(0).gameObject.SetActive(false);
        stats.transform.GetChild(1).gameObject.SetActive(false);
        atr.transform.localPosition = new Vector3(175, 450, 0);
        List<GameObject> previousLinks = GameObject.FindGameObjectsWithTag("cl").ToList();
        foreach (GameObject go in previousLinks) Destroy(go);
    }
    public void Update()
    {
        if (mode != 0)
        {
            totalIters.text = $"Iterations: {ai.currentIteration}/{ai.totalIterationsEach}";
            totalS.text = $"Successes: {ai.pAI.successes}";
            totalF.text = $"Failures: {ai.pAI.fails}";
            currentInspect.text = $"Inspecting Line: {selectedLine}";
            stepNumber.textComponent.text = $"Step: {lineStep}";
        }
    }
    public void genLine()
    {
        inspectingLine = true;
        selectedLine = (int) Mathf.Max(float.Parse(lineNumber.text.Trim()), 0);
        lineStep = 0;
        mode2();
        draw();
    }
    public void genBestLine()
    {
        float closestPosition = Mathf.Infinity;
        float leastSteps = Mathf.Infinity;
        int i = 0;
        foreach (attempt a in s.runs)
        {
            if (i == s.runs.Count - 1) continue;
            if (a.info.closestPosition < closestPosition || (a.succeded && a.moves.Count < leastSteps))
            {
                closestPosition = a.info.closestPosition;
                leastSteps = a.moves.Count;
                selectedLine = i;
            }
            i++;
        }
        lineStep = s.runs[selectedLine].moves.Count - 1;
        inspectingLine = true;
        mode2();
        draw();
    }
    public void nextStep()
    {
        lineStep = Mathf.Min(lineStep + 1, s.runs[selectedLine].moves.Count - 1);
        draw();
    }
    public void lastStep()
    {
        lineStep = Mathf.Max(lineStep - 1, 0);
        draw();
    }
    public void showFullLine()
    {
        lineStep = s.runs[selectedLine].moves.Count - 1;
        draw();
    }
    public void endInspect()
    {
        inspectingLine = false;
        l.positionCount = 0;
        lineNumber.interactable = true;
        stats.transform.GetChild(0).gameObject.SetActive(true);
        stats.transform.GetChild(1).gameObject.SetActive(false);
        List<GameObject> previousLinks = GameObject.FindGameObjectsWithTag("cl").ToList();
        foreach (GameObject go in previousLinks) Destroy(go);
    }
    private void draw()
    {
        mode = 2;
        s.drawLine(s.runs[selectedLine].moves.GetRange(0, Mathf.Min(lineStep, s.runs[selectedLine].moves.Count - 1)));
        lineNumber.interactable = false;
        List<GameObject> previousLinks = GameObject.FindGameObjectsWithTag("cl").ToList();
        foreach (GameObject go in previousLinks) Destroy(go);

        if (lineStep == s.runs[selectedLine].moves.Count - 1 && s.runs[selectedLine].succeded) comLinksGenerator();
    }
    private void mode2()
    {
        stats.transform.GetChild(0).gameObject.SetActive(false);
        stats.transform.GetChild(1).gameObject.SetActive(true);
    }
    public void setStep()
    {
        lineStep = (int) Mathf.Max(float.Parse(stepNumber.text), 0);
        draw();
    }


    private void comLinksGenerator()
    {
        List<GameObject> previousLinks = GameObject.FindGameObjectsWithTag("cl").ToList();
        foreach (GameObject go in previousLinks) Destroy(go);

        List<Vector3> comLinkPos = new List<Vector3>() {ai.startPos, ai.endPos};

        List<Vector3> toCheck = movesToPositions(s.runs[selectedLine].moves);

        // 8 times for each link
        for (int i = 1; i <= 8; i++)
        {
            (float s, Vector3 position) bestTile = (float.NegativeInfinity, new Vector3());

            foreach (Vector3 v in toCheck)
            {
                if (comLinkPos.Contains(v)) continue;
                Point p = mg.points[(int) v.x, (int) v.z];
                if (p.slope > ai.maxSlope) continue;
                Vector2 vv = new Vector2(v.x, v.z);

                // assign each position a score
                float score = (float) p.defaultHeight;
                score += Mathf.Pow(getClosestDist(vv, comLinkPos), 2) * 3;

                if (score > bestTile.s) bestTile = (score, v);
            }

            comLinkPos.Add(bestTile.position);
        }

        comLinkPos.Remove(ai.endPos);
        comLinkPos.Remove(ai.startPos);

        foreach (Vector3 v in comLinkPos)
        {
            // create the gameobject
            GameObject go = (GameObject) Instantiate(comLinkPrefab, v, Quaternion.identity);

            // add hitbox
            int childCount = go.transform.childCount;
            for (int child = 0; child < childCount; child++)
            {
                GameObject childGo = go.transform.GetChild(child).gameObject;
                childGo.AddComponent<MeshCollider>();
            }
        }
    }

    private List<Vector3> movesToPositions(List<Vector2Int> moves)
    {
        List<Vector3> returnList = new List<Vector3>();
        Vector3 current = new Vector3(ai.startPos.x, 0, ai.startPos.y);

        foreach (Vector2Int v in moves)
        {
            current += new Vector3(v.x, 0, v.y);
            returnList.Add(mg.cubes[(int) current.x, (int) current.z].selfPosition);
        }
        return returnList;
    }

    private int getClosestDist(Vector2 currentPos, List<Vector3> toCheck)
    {
        int returnInt = int.MaxValue;

        foreach (Vector3 v in toCheck)
        {
            if (currentPos == new Vector2(v.x, v.z)) continue;
            int currentScore = getManhattanDist(currentPos, new Vector2(v.x, v.z));
            returnInt = Mathf.Min(currentScore, returnInt);
        }

        return returnInt;
    }
    private List<Vector3> getCheckBounds(Vector2 s, Vector2 e, int buffer)
    {
        float minX = Mathf.Min(s.x, e.x) - buffer;
        float maxX = Mathf.Max(s.x, e.x) + buffer;
        float minZ = Mathf.Min(s.y, e.y) - buffer;
        float maxZ = Mathf.Max(s.y, e.y) + buffer;

        List<Vector3> returnList = new List<Vector3>();

        for (float x = minX; x < maxX; x++)
        {
            for (float z = minZ; z < maxZ; z++)
            {
                if (!inBounds(new Vector2(x, z))) continue;
                if (!cl.canSee[(int) x, (int) z]) continue;

                Point p = mg.points[(int) x, (int) z];
                node n = mg.cubes[(int) x, (int) z];
                if (p.slope > ai.maxSlope) continue;

                returnList.Add(n.selfPosition);
            }
        }

        return returnList;
    }
    private int getManhattanDist(Vector2 a, Vector2 b) => (int) (Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)));
    private bool inBounds(Vector2 n) => (n.x >= 1 && n.x <= mg.xMeshLength - 1 && n.y >= 1 && n.y <= mg.yMeshLength - 1);

    private float getLoss(anode a, Vector2 e) => Mathf.Abs(e.x - a.position.x) + Mathf.Abs(e.y - a.position.y);
}