using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class AStar : MonoBehaviour
{
    public mapGenerator mg;
    public alg ai;
    public ComLinks cl;
    public LineRenderer l;
    public int optimization = 0;
    // 0 -> shortest distance
    // 1 -> least change in angle
    // 2 -> time earth is visible
    public int slopeTotalMaxium = 15;

    public bool generate = false;
    public int comLinkSearchRadius = 3;
    public Object comLinkPrefab;
    public float cDist;
    public float cBias;
    public float pDist;
    public float pBias;
    Point[,] points;
    node[,] nodes;
    bool[,] canSee;

    TextMeshProUGUI aiSet;
    TextMeshProUGUI errorMsg;

    public void Start()
    {
        aiSet = GameObject.FindGameObjectWithTag("miscMsgs/aiSet").GetComponent<TextMeshProUGUI>();
        errorMsg = GameObject.FindGameObjectWithTag("miscMsgs/error").GetComponent<TextMeshProUGUI>();
        errorMsg.gameObject.SetActive(false);
        aiSet.gameObject.SetActive(false);
    }

    public void OnValidate()
    {
        if (generate)
        {
            generate = false;
            pathfind();
        }
    }
    public List<Vector3> pathfind(bool onlyGetPath = false, bool customStart = false, Vector2 s = new Vector2(), Vector2 e = new Vector2())
    {
        points = mg.points;
        nodes = mg.cubes;
        canSee = cl.canSee;
        Vector2[] directions = new Vector2[4]
        {
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(0, -1),
            new Vector2(-1, 0)
        };
        if (!customStart)
        {
            s = ai.startPos;
            Debug.Log(s);
            Debug.Log(ai.startPos);
            e = ai.endPos;
        }
        else if (s == new Vector2() && e == new Vector2()) 
        {
            sendError("Please sent start and end for a custom start");
            return new List<Vector3>();
        }

        List<anode> open = new List<anode>();
        List<anode> close = new List<anode>();
        anode start = new anode();
        start.position = s;
        start.f = getLoss(start, e);
        open.Add(start);

        Debug.Log(e);
        Debug.Log(s);

        Debug.Log("-----");

        int index = 0;

        while (open.Count > 0)
        {
            index ++;
            
            anode n = open.OrderBy(x => x.f).First();
            open.Remove(n);
            close.Add(n);

            if (index > 20_000) break;

            if (n.position == e)
            {
                if (!onlyGetPath)
                 //
                {
                    connectComLinks(generateComLinks(n.path));
                    //l.positionCount = 0;
                    //l.positionCount = n.path.Count;
                    //l.SetPositions(n.path.ToArray());
                }
                return n.path;
            }

            foreach (Vector2 v in directions)
            {
                anode p = new anode();
                p.position = n.position + v;
                //p.index = n.index + 1;
                
                if (!inBounds(p.position)) continue; // is out of bounds
                if (close.Contains(p)) continue;
                // somewhere the height actually matters
                // why?
                if (Mathf.Abs(
                    (float) (points[(int) p.position.x, (int) p.position.y].csvMatrix.slope -
                    points[(int) e.x, (int) e.y].csvMatrix.slope))
                    > slopeTotalMaxium) continue;
                if (Mathf.Abs((float) (points[(int) p.position.x, (int) p.position.y].csvMatrix.slope)) > 15f) continue;

                if (!close.Contains(p))
                {
                    if (!open.Contains(p))
                    {
                        p.path = new List<Vector3>(n.path);
                        p.path.Add(nodes[(int) p.position.x, (int) p.position.y].selfPosition + new Vector3(0, 0.25f, 0));

                        p.g = n.g + 1;
                        p.h = getLoss(p, e);
                        p.f = p.g + p.h;

                        open.Add(p);
                    }
                }
            }
        }

        sendError($"No possible path found, checked {index} points");
        return new List<Vector3>();
    }
    public List<Vector3> generateComLinks(List<Vector3> path)
    {
        l.positionCount = 0;
        l.positionCount = path.Count;
        l.SetPositions(path.ToArray());
        return new List<Vector3>();

        List<GameObject> previousLinks = GameObject.FindGameObjectsWithTag("cl").ToList();
        foreach (GameObject go in previousLinks) Destroy(go);

        if (path.Count < 10) sendError("Path is not long enough to generate 10 com. links.");
        else
        {
            List<Vector3> comLinkPos = new List<Vector3>() {ai.startPos, ai.endPos};

            List<Vector3> toCheck = getCheckBounds(ai.startPos, ai.endPos, comLinkSearchRadius);

            for (int i = 1; i <= 8; i++)
            {
                (float s, Vector3 position) bestTile = (float.NegativeInfinity, new Vector3());

                foreach (Vector3 v in toCheck)
                {
                    if (comLinkPos.Contains(v)) continue;
                    Point p = points[(int) v.x, (int) v.z];
                    if (p.slope > slopeTotalMaxium) continue;
                    Vector2 vv = new Vector2(v.x, v.z);

                    float score = (float) p.defaultHeight;
                    score += Mathf.Pow(getClosestDist(vv, comLinkPos), 2) * cDist + cBias;
                    score -= Mathf.Pow(getClosestDist(vv, path), 2) * pDist + pBias;

                    if (score > bestTile.s) bestTile = (score, v);
                }

                if (bestTile.position == new Vector3()) 
                {
                    sendError("Could not find a com. link.");
                }

                comLinkPos.Add(bestTile.position);
            }

            comLinkPos.Remove(ai.endPos);
            comLinkPos.Remove(ai.startPos);

            foreach (Vector3 v in comLinkPos)
            {
                // create the gameobject
                GameObject go = (GameObject) Instantiate(comLinkPrefab, v, Quaternion.Euler(270f, 0f, 0f));
                go.transform.localScale = new Vector3(80, 80f, 80f);

                // add hitbox
                int childCount = go.transform.childCount;
                for (int child = 0; child < childCount; child++)
                {
                    GameObject childGo = go.transform.GetChild(child).gameObject;
                    childGo.AddComponent<MeshCollider>();
                }
            }

            comLinkPos = comLinkPos.OrderByDescending(x => x.x).ToList();

            return comLinkPos;
        }

        return new List<Vector3>();
    }

    private int getManhattanDist(Vector2 a, Vector2 b) => (int) (Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)));
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
                if (!canSee[(int) x, (int) z]) continue;

                Point p = points[(int) x, (int) z];
                node n = nodes[(int) x, (int) z];
                if (p.slope > slopeTotalMaxium) continue;

                returnList.Add(n.selfPosition);
            }
        }

        return returnList;
    }
    
    public void connectComLinks(List<Vector3> comLinks)
    {
        return;
        List<Vector3> fullPath = new List<Vector3>();

        if (comLinks == new List<Vector3>()) return;

        comLinks.Reverse();
        comLinks.Add(nodes[(int) ai.startPos.x, (int) ai.startPos.y].selfPosition);
        Vector3 lastPosition = nodes[(int) ai.endPos.x, (int) ai.endPos.y].selfPosition; // starts from end bc order is reveresed

        foreach (Vector3 v in comLinks)
        {
            List<Vector3> currentPath = pathfind(true, true, new Vector2(lastPosition.x, lastPosition.z), new Vector2(v.x, v.z));
            fullPath.AddRange(currentPath);
            lastPosition = v;
        }

        l.positionCount = 0;
        l.positionCount = fullPath.Count;
        l.SetPositions(fullPath.ToArray());
    }
    public float getLoss(anode a, Vector2 e)
    {
        float mDist = Mathf.Abs(e.x - a.position.x) + Mathf.Abs(e.y - a.position.y);
        float cPosValue = 0;

        switch (optimization)
        {
            case 0:
                break;
            case 1:
                cPosValue = 100 * Mathf.Abs((float) 
                    (points[(int) a.position.x, (int) a.position.y].slope - points[(int) e.x, (int) e.y].slope));
                break;
            case 2:
                cPosValue = 100 * (canSee[(int) a.position.x, (int) a.position.y] ? 1 : -1);
                break;
        }

        return mDist + cPosValue;
    }
    public bool inBounds(Vector2 n) => (n.x >= 1 && n.x <= mg.xMeshLength - 1 && n.y >= 1 && n.y <= mg.yMeshLength - 1);
    public void setOptTo(int opt)
    {
        this.optimization = opt;
        string text = "";
        
        switch (opt)
        {
            case 0:
                text = "AI Set To: Shortest Distance";
                break;
            case 1:
                text = "AI Set To: Least Change in Angle";
                break;
            case 2:
                text = "AI Set To: % Time in View of Earth";
                break;
        }
        
        sendUpdate(text);
    }

    public void setSlopeTo(int desiredSlope)
    {
        this.slopeTotalMaxium = desiredSlope;
        aiSet.gameObject.SetActive(true);

        aiSet.text = $"AI Maxium Slope Set To {desiredSlope}";

        StartCoroutine(fadeOut(aiSet));
    }

    public void setPosToDefault()
    {

        Debug.Log(mg.currentGrid.downFactor);
        Debug.Log(mg.parentGrid.downFactor);
        XYZ sp = mg.currentGrid.estimateLatToGrid(new XYZ(x : 54.794, z : -89.232), mg.currentGrid.downFactor * mg.parentGrid.downFactor);
        XYZ ep = mg.currentGrid.estimateLatToGrid(new XYZ(x : 120.69, z : -89.2), mg.currentGrid.downFactor * mg.parentGrid.downFactor);

        Debug.Log(sp);
        Debug.Log(ep);
        
        if (!inBounds(new Vector2((float) sp.x, (float) sp.z)) || !inBounds(new Vector2((float) ep.x, (float) ep.z)))
        {
            sendError("Default points are out of bounds");
            return;
        }

        Debug.Log("passed");
        ai.pAI.setStartEnd(new Vector2((float) sp.x, (float) sp.z), new Vector2((float) ep.x, (float) ep.z), false);
        ai.pAI.debugUpdate();
    }

    public void sendError(string text)
    {
        StopAllCoroutines();
        errorMsg.gameObject.SetActive(true);
        errorMsg.color = new Color(errorMsg.color.r, errorMsg.color.g, errorMsg.color.b, 1);
        errorMsg.text = text;
        StartCoroutine(fadeOut(errorMsg));
        errorMsg.color = new Color(errorMsg.color.r, errorMsg.color.g, errorMsg.color.b, 1);
    }
    public void sendUpdate(string text)
    {
        aiSet.gameObject.SetActive(true);
        aiSet.color = new Color(aiSet.color.r, aiSet.color.g, aiSet.color.b, 1);
        aiSet.text = text;
        StartCoroutine(fadeOut(aiSet));
        aiSet.color = new Color(aiSet.color.r, aiSet.color.g, aiSet.color.b, 1);
    }
    private IEnumerator fadeOut(TextMeshProUGUI t)
    {
        t.color = new Color(t.color.r, t.color.g, t.color.b, 1);
        for (float i = 0.01f; i < 2f; i += Time.deltaTime)
        {
            t.color = new Color(t.color.r, t.color.g, t.color.b, -1 * ((i/2f) - 1));
            yield return null;
        }
        t.gameObject.SetActive(false);
    }
}

public class anode
{
    public Vector2 position;
    public float g = 0;
    public float h = 0;
    public float f = 0;
    public List<Vector3> path = new List<Vector3>();
    //public int index = 0;


    // only cares about position, nothing else
    public override int GetHashCode() => this.position.GetHashCode();
    public override bool Equals(object obj)
    {
        if (!(obj is anode)) return false;

        anode a = (anode) obj;

        return (a.position == this.position);
    }
}
