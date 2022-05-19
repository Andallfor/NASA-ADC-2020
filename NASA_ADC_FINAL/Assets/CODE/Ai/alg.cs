using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading.Tasks;
using System.Text;

// problem: hashcodes!!!!!
public class alg : MonoBehaviour
{
    // actual data for the AI
    // its a mess yes
    [Header("AI Attributes")]
    public int totalIterationsEach = 10_000; // how many times the ai should run
    public int maxMoves = 1000; // for performance reasons, cap the amount of times the AI can move each iter

    // values for the equation
    [Range(0,1)] public float explorationRate;
    [Range(0,1)] public float learningRate;
    [Range(0,1)] public float discount;


    // x -> order
    // y -> weight
    [Header("Weights")]
    public float distanceWeight;
    public float slopeWeight;
    public float visibilityWeight;
    public float maxSlope;

    // the ai will update these values as it runs
    // just to inform the user what is going on
    [Header("AI Values")]
    public int currentIteration = 0;
    // shows the position of the start and end, in local pos
    // changing it currently doesnt do anything, use generateNewStartEnd
    public Vector2 startPos;
    public Vector2 endPos;

    // outside references
    [Header("Required Info")]
    public mapGenerator nodes;
    // materials to use when showing the path/start and end points
    public Material startMat;
    public Material endMat;

    // hold reference to the parentAI
    [HideInInspector] public ai pAI = new ai();

    
    public void OnValidate() // runs whenever any changes in the inspector are made
    {
        if (startPos != pAI.start || endPos != pAI.end)
        {
            pAI.setStartEnd(startPos, endPos, genMarkers: false);
            pAI.debugUpdate();
        }
    }
    public void genAI()
    {
        //pAI.tryLoadAI();
        pAI.beginNewIter();
    }
    public void genSEEditor() // yeahhhh this code is kinda repeated in 3 areas
    { // really should route everything into one thing
        Vector2[] a = pAI.generateStartAndEnd();
        pAI.setStartEnd(a[0], a[1], genMarkers: false);
        pAI.debugUpdate();
    }
    public void Start()
    {
        pAI.parent = this; // set pAI's parent to this class
        Vector2[] a = pAI.generateStartAndEnd();
        pAI.setStartEnd(a[0], a[1], genMarkers: false);
    }
    public void Update()
    {
        // bad? yes
        // due to some timing issues (when scripts first run and whatnot)
        // this will repeatedly try to generate the start and end cubes
        if (ReferenceEquals(nodes.cubes, null)) return;
        if (nodes.cubes[0,0] != null && pAI.z == null) // if the start and end cubes have not yet been generated
        {
            pAI.generateStartEndMarkers();
        }
    }
}



// ai
public class ai
{
    public Dictionary<State, Dictionary<Vector2Int, float>> Q = new Dictionary<State, Dictionary<Vector2Int, float>>(); 
    private Dictionary<Vector2, State> foundStates = new Dictionary<Vector2, State>();
    public alg parent; // stores reference to its parent

    public Vector2Int start, end, currentPos;
    
    // debugging stuff
    public GameObject z, c;
    public int fails = 0, failDueToOutOfBounds = 0, failDueToSlope = 0, failDueToExceededMaximumMoves = 0, failDueToNoMove = 0, successes = 0;

    // qol stuff
    public readonly Vector2Int[] directions = new Vector2Int[] { // yes this is stupid sue me
        new Vector2Int(0,1), 
        new Vector2Int(1,0), 
        new Vector2Int(0,-1), 
        new Vector2Int(-1,0)};
    

    // functions related to starting the ai
    public Vector2[] generateStartAndEnd()
    {
        // init empty vector2[]
        Vector2[] returnInfo = new Vector2[2];

        // do twice to generate both a start and an end
        for (int i = 0; i < 2; i++)
        {
            int[] b = new int[2]
            {
                (int) UnityEngine.Random.Range(1f, (float) parent.nodes.xMeshLength - 2),
                (int) UnityEngine.Random.Range(1f, (float) parent.nodes.yMeshLength - 2)
            };
            // save the values
            returnInfo[i] = new Vector2(b[0], b[1]);
        }

        return returnInfo;
    }
    public void beginNewIter()
    {
        generateNewAttempt();
    }
 
    
    // functions related to debugging/data
    public void debugUpdate() // just to update debugging stuff
    {
        c.transform.position = parent.nodes.cubes[(int) end.x, (int) end.y].selfPosition;
        z.transform.position = parent.nodes.cubes[(int) start.x, (int) start.y].selfPosition;
    }
    public void setStartEnd(Vector2 start, Vector2 end, bool genMarkers = true)
    {
        this.start = new Vector2Int((int) start.x, (int) start.y);
        this.end = new Vector2Int((int) end.x, (int) end.y);
        parent.startPos = start;
        parent.endPos = end;
        
        if (genMarkers) generateStartEndMarkers();
    }
    public void generateStartEndMarkers()
    {
        z = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        z.GetComponent<MeshRenderer>().material = parent.startMat;
        z.transform.position = parent.nodes.cubes[(int) start.x, (int) start.y].selfPosition;

        c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        c.GetComponent<MeshRenderer>().material = parent.endMat;
        c.transform.position = parent.nodes.cubes[(int) end.x, (int) end.y].selfPosition;
    }


    // qol functions
    public bool inBounds(Vector2 pos)
    {
        // not efficient
        try
        {
            bool a = parent.nodes.cl.canSee[(int) pos.x, (int) pos.y];
            return true;
        }
        catch {return false;}
    }


    // functions for main ai alg
    public void updateQValue(State s, Vector2Int action, float oldValue, float reward, float future)
    {
        // the meet of the Q learning alg, the actual formula
        float newValue = oldValue + parent.learningRate * (reward + (this.parent.discount * future) - oldValue);

        // init all possible states and action beforehand
        Q[s][action] = newValue;
    }
    public void update(State oldState, Vector2Int action, State newState, float reward)
    {
        float old = this.getQValue(oldState, action);
        float future = this.bestFutureReward(newState);
        this.updateQValue(oldState, action, old, reward, future);
    }
    /// <summary> WARNING: assumes currentPos </summary>
    public Vector2Int chooseAction(State s) // choose an action
    {
        List<Vector2Int> nonOutOfBounds = new List<Vector2Int>();
        
        foreach (Vector2Int v in directions)
        {
            if (inBounds(currentPos + v)) nonOutOfBounds.Add(v);
        }

        // choose random value based on exploration rate
        System.Random r = new System.Random();
        if ((float) r.NextDouble() < this.parent.explorationRate) return nonOutOfBounds[r.Next((int) 0, (int) Mathf.Max(0, nonOutOfBounds.Count - 1))];

        // get best choice from Q values
        (float score, Vector2Int action) a = (Mathf.NegativeInfinity, Vector2Int.zero);
        foreach (Vector2Int v in nonOutOfBounds)
        {
            float score = this.getQValue(s, v);
            if (a.score < score)
            {
                a = (score, v);
            }
        }

        return a.action;
    }
    public float bestFutureReward(State s) // given a state, find the best score possible
    {
        float bestScore = Mathf.NegativeInfinity;
        foreach (KeyValuePair<Vector2Int, float> kvp in s.neighbors)
        {
            float currentScore = this.getQValue(s, kvp.Key);
            bestScore = Mathf.Max(bestScore, currentScore);
        }

        return bestScore;
    }


    // sub functions for alg
    public float getQValue(State s, Vector2Int a)
    {
        // get q value, return 0 if value does not exist
        if (Q.ContainsKey(s) && Q[s].ContainsKey(a)) return Q[s][a];
        return 0;
    }
    public State getState(Vector2 pos) // generate a stateAction
    {
        if (!foundStates.ContainsKey(pos))
        {
            Dictionary<Vector2Int, float> neighbors = new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, bool> visiblityMap = new Dictionary<Vector2Int, bool>();
            float currentAngle = 90f;
            bool currentVisibility = false;
            if (inBounds(pos))
            {
                currentAngle = (float) parent.nodes.points[(int) pos.x, (int) pos.y].csvMatrix.slope;
                currentVisibility = parent.nodes.cl.canSee[(int) pos.x, (int) pos.y];
            }
            // get the angle to all its neighboring tiles
            foreach (Vector2Int dir in directions)
            {
                Vector2 newPos = dir + pos;
                if (inBounds(newPos))
                {
                    currentAngle -= (float) parent.nodes.points[(int) newPos.x, (int) newPos.y].csvMatrix.slope;
                    neighbors.Add(dir, currentAngle);

                    visiblityMap.Add(dir, parent.nodes.cl.canSee[(int) newPos.x, (int) newPos.y]);
                }
                else neighbors.Add(dir, 90f); // out of bounds
            }

            Vector2Int mDist = new Vector2Int(
                (int) (this.end.x - pos.x), 
                (int) (this.end.y - pos.y));
            State returnState = new State(neighbors, visiblityMap, currentAngle, currentVisibility, mDist);
            foundStates.Add(pos, returnState);
            return returnState;
        }
        else return foundStates[pos];
    }

    private int totalDistToEnd() => Mathf.Abs(end.x - currentPos.x) + Mathf.Abs(end.y - currentPos.y);

    private float cost(int loop)
    {
        // get best path (straight line between start and end)
        float m = ((float) end.y - (float) start.y) / ((float) end.x - (float) start.x);
        float b = (float) end.y - (m * (float) end.x);

        // get two positions on main axises (x and y)
        Vector2 pos = new Vector2(
            Mathf.Clamp(currentPos.x, Mathf.Min(start.x, end.x), Mathf.Max(start.x, end.x)),
            Mathf.Clamp(currentPos.y, Mathf.Min(start.y, end.y), Mathf.Max(start.y, end.y)));
        
        (float x, float y) yInter = (pos.x, (m * pos.x) + b);
        (float x, float y) xInter = ((pos.y - b)/m, pos.y);

        // get mid point
        (float x, float y) mid = ((yInter.x + xInter.x)/2f, (yInter.y + xInter.y)/2f);

        // get distance from currentPos to mid (dont use constained pos)
        float dist = Mathf.Sqrt(
            Mathf.Pow(mid.x - currentPos.x, 2) + 
            Mathf.Pow(mid.y - currentPos.y, 2));

        return -(dist * loop/100f);
    }
    private float votingCost(State s, int loop)
    {
        // 0 <-> 1050
        float distanceScore = this.parent.distanceWeight * -s.absoluteMDist * 1.5f;
        // 0 <-> 700
        float angleScore = this.parent.slopeWeight * -(Mathf.Abs(s.selfAngle) * 5f + ((Mathf.Abs(s.selfAngle) > this.parent.maxSlope) ? 250 : 0));
        // 0 <-> 500
        float visibilityScore = this.parent.visibilityWeight * -((s.selfVisibility) ? 0 : 500);

        float totalScore = distanceScore + angleScore + visibilityScore;
        
        return totalScore - 100 - loop/100f;
    }

    // main ai loop
    async void generateNewAttempt()
    {
        // references for AIDebugger
        AIDebugger deb = parent.GetComponent<AIDebugger>();
        // +1 ofc (i dont think is actually needed btw)
        this.Q = new Dictionary<State, Dictionary<Vector2Int, float>>(parent.nodes.xMeshLength * parent.nodes.yMeshLength * 4 + 1);
        // pre-generate all possible states and actions
        
        for (int x = 0; x < parent.nodes.xMeshLength; x++)
        {
            for (int y = 0; y < parent.nodes.yMeshLength; y++)
            {
                Vector2 pos = new Vector2(x, y);
                State s = this.getState(pos);

                if (this.Q.ContainsKey(s)) continue;

                this.Q.Add(s, new Dictionary<Vector2Int, float>(4));

                foreach (Vector2Int v in this.directions) this.Q[s].Add(v, 10);
            }
        }
        

        // the main loop
        for (int i = 0; i < parent.totalIterationsEach; i++)
        {
            Task t = Task.Run(() => this.internalRun(i, deb));
            await t;
        }
    }

    private void internalRun(int i, AIDebugger deb)
    {
        // store starting info for ai
        currentPos = start;
        State lastState;

        List<State> qStates = new List<State>();
        List<State> states = new List<State>();
        List<Vector2Int> pathTaken = new List<Vector2Int>();
        float closestPosition = Mathf.Infinity;
        int closestPosIter = 0;

        int loops = 0;
        int exitCode = 0; // 0 is normal, 1 is success, 2+ is different types of failure
        while (true)
        {
            if (loops > parent.maxMoves) 
            {
                exitCode = 2;
            }
            
            // get new state
            lastState = this.getState(currentPos);
            Vector2Int action = this.chooseAction(lastState);

            // move
            currentPos += action;
            State newState = this.getState(currentPos);

            // update counters
            loops++;

            // update debugger counters
            if (deb.enableDebugging)
            {
                // saves the last states
                pathTaken.Add(action);
                states.Add(lastState);

                int absDistToEnd = Mathf.Abs(-this.totalDistToEnd());
                if (absDistToEnd < closestPosition) 
                {
                    closestPosition = absDistToEnd;
                    closestPosIter = loops;
                }
            }

            // reward player
            if (currentPos == this.end)
            {
                // gotten to the end
                this.update(lastState, action, newState, 10000000000);
                exitCode = 1;
            }
            else
            {
                //this.update(lastState, action, newState, this.cost(loops));
                //this.update(lastState, action, newState, -newState.absoluteMDist);
                this.update(lastState, action, newState, this.votingCost(newState, loops));
            }
            
            /*
            if (action == Vector2Int.zero)
            {
                // stuck
                // not likely to actually run
                this.update(lastState, action, newState, -20000);
                //exitCode = 5;
            }
            else if (!this.inBounds(currentPos))
            {
                // ran out of bounds
                this.update(lastState, action, newState, -20000);
                //exitCode = 3;
            }
            else if (Mathf.Abs(lastState.neighbors[action]) >= 15f || 
                     Mathf.Abs((float) parent.nodes.points[(int) currentPos.x, (int) currentPos.y].csvMatrix.slope) >= 15f)
            {
                // attempted to move too high/low
                this.update(lastState, action, newState, -20000);
                //exitCode = 4;
            }
            else if (currentPos == this.end)
            {
                // gotten to the end
                this.update(lastState, action, newState, 25000);
                exitCode = 1;
            }
            else
            {
                // default
                
            }
            */
            
            //rewardTime += (DateTime.Now - rs).Milliseconds;

            // termination
            if (exitCode > 0) break;
        }

        this.parent.currentIteration = i + 1;

        // save info to debugger
        bool success = (exitCode == 1);
        if (success)
        {
            this.successes++;
        }
        else
        {
            this.fails++;
            if (exitCode == 2) this.failDueToExceededMaximumMoves++;
            if (exitCode == 3) this.failDueToOutOfBounds++;
            if (exitCode == 4) this.failDueToSlope++;
            if (exitCode == 5) this.failDueToNoMove++;
            
        }
        if (deb.enableDebugging) 
        {
            attemptInfo info = new attemptInfo((int) closestPosition, exitCode, closestPosIter, i);
            deb.runs.Add(new attempt(states, qStates, pathTaken, success, info));
        }
    }
}

// actions are just a Vector2Int
public struct State
{
    public readonly Dictionary<Vector2Int, float> neighbors;
    public readonly Vector2Int manhattanDistToEnd;
    public int absoluteMDist
    {
        get {return Mathf.Abs(this.manhattanDistToEnd.x) + Mathf.Abs(this.manhattanDistToEnd.y);}
    }
    public readonly float selfAngle;
    public readonly Dictionary<Vector2Int, bool> visibility;
    public readonly bool selfVisibility;
    private int internalHash;
    private bool didAlreadyGenerateHash;

    public State(Dictionary<Vector2Int, float> neighbors, Dictionary<Vector2Int, bool> visibility, float selfAngle, bool selfVisibility, Vector2Int manhattanDistToEnd)
    {
        this.neighbors = neighbors;
        this.manhattanDistToEnd = manhattanDistToEnd;
        this.didAlreadyGenerateHash = false;
        this.internalHash = 0;
        this.visibility = visibility;
        this.selfAngle = selfAngle;
        this.selfVisibility = selfVisibility;
    }
    public State(State other)
    {
        this.neighbors = new Dictionary<Vector2Int, float>(other.neighbors);
        this.manhattanDistToEnd = other.manhattanDistToEnd;
        this.didAlreadyGenerateHash = other.didAlreadyGenerateHash;
        this.internalHash = other.internalHash;
        this.visibility = other.visibility;
        this.selfVisibility = other.selfVisibility;
        this.selfAngle = other.selfAngle;
    }

    public override int GetHashCode()
    {
        // FNV - 1a Hash
        // https://gist.github.com/StephenCleary/4f6568e5ab5bee7845943fdaef8426d2
        if (this.didAlreadyGenerateHash) return internalHash;

        unchecked
        {
            int hash = unchecked((int) 2166136261);
            this.addIntToHash(manhattanDistToEnd.x, ref hash);
            this.addIntToHash(manhattanDistToEnd.y, ref hash);
            foreach (float value in this.neighbors.Values)
            {
                this.addIntToHash((int) value, ref hash);
            }
            foreach (bool value in this.visibility.Values)
            {
                this.addIntToHash(Convert.ToInt16(value), ref hash);
            }
            this.addIntToHash((int) selfAngle, ref hash);
            this.addIntToHash(Convert.ToInt16(selfVisibility), ref hash);

            this.internalHash = hash;
            this.didAlreadyGenerateHash = true;
            return (int) hash;
        }
    }
    private void addIntToHash(int value, ref int hash)
    {
        this.addByteToHash((byte) value, ref hash);
        this.addByteToHash((byte) (value >> 8), ref hash);
        this.addByteToHash((byte) (value >> 16), ref hash);
        this.addByteToHash((byte) (value >> 24), ref hash);
    }

    private void addByteToHash(byte data, ref int hash)
    {
        unchecked
        {
            hash ^= data;
            hash *= 16777619;
        }
    }

    public override bool Equals(object obj) // needed to be defined allow with GetHasCode
    {
        if (obj.GetType() != this.GetType()) return false;
        if (this.GetHashCode() == obj.GetHashCode()) return true;
        return false;
    }

    public static bool operator==(State a, State b) => (a.GetHashCode() == b.GetHashCode());
    public static bool operator!=(State a, State b) => (a.GetHashCode() != b.GetHashCode());
}




[Serializable]
public class Vector2Self // just because unity doesnt let me serialize Vector2s, so i had to create my own version of it
{
    public float x;
    public float y;

    public Vector2Self(float x = 0, float y = 0)
    {
        this.x = x;
        this.y = y;
    }

    // redefine some important functions
    public static Vector2Self operator+(Vector2Self a, Vector2Self b)
    {
        return new Vector2Self(a.x + b.x, a.y + b.y);
    }
    public static Vector2Self operator-(Vector2Self a, Vector2Self b)
    {
        return new Vector2Self(a.x - b.x, a.y - b.y);
    }    
    public static implicit operator Vector2(Vector2Self v)
    {
        return new Vector2(v.x, v.y);
    }
    public static explicit operator Vector2Self(Vector2 v)
    {
        return new Vector2Self(v.x, v.y);
    }
    public override string ToString()
    {
        return $"({this.x}, {this.y})";
    }
}
