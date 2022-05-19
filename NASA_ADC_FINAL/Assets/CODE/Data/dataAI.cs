using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
desired values:

total exec time
% success
best path length
total squares explored
total unique sqaures explored

*/
public class dataAI : MonoBehaviour
{
    dataHub dHub;
    public Dictionary<State, float> QValues = new Dictionary<State, float>();

    public State[] qKey;
    public float[] qValue;
    public List<sample> AIDATA = new List<sample>();

    public List<float> iter, execTime, successRate, bestPath, squaresExplored = new List<float>();
    private List<float>[] allValues = new List<float>[4];

    void Start()
    {
        dHub = GetComponent<dataHub>();
        allValues = new List<float>[4] {execTime, successRate, bestPath, squaresExplored};
    }
    public void createSample(int iter, float[] values)
    {
        /*
        sample s = new sample(iter, values);
        AIDATA.Add(s);
        this.iter.Add(iter);
        execTime.Add(values[0]);
        successRate.Add(values[1]);
        bestPath.Add(values[2]);
        squaresExplored.Add(values[3]);
        */
    }

    public void orderQ()
    {
        /*
        qKey = new State[QValues.Count];
        qValue = new float[QValues.Count];
        int index = 0;
        foreach(KeyValuePair<State, float> kvp in QValues)
        {
            qKey[index] = kvp.Key;
            qValue[index] = kvp.Value;
            index++;
        }
        */
    }
}

public class sample
{
    public float execTime, successRate, bestPath, squaresExplored;
    public int iter;
    public sample(int iter, float[] values)
    {
        this.iter = iter;
        // idk how to add ref vars to an array
        execTime = values[0];
        successRate = values[1];
        bestPath = values[2];
        squaresExplored = values[3];
    }
}
