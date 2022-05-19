using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

public static class master
{
    public static bool enableControls = true;
    public static bool terrainFinishedGenerating = false;
    private static bool hasRan = false;
    public static int selectedMap = 0;
    public static Dictionary<string, string> sharedInfo = new Dictionary<string, string>(); // save game info to here UI pulls it from here too
    private static Dictionary<int, string> orders = new Dictionary<int, string>();
    private static Dictionary<int, float> percentDone = new Dictionary<int, float>();
    private static int _step, index;
    public static int step
    {
        get 
        {
            if (!hasRan) init();
            return _step;
        }
    }
    public static int maxStep
    {
        get {return orders.Count;}
    }
    public static float currentPercent(int step) => (percentDone.ContainsKey(step)) ? percentDone[step] : 0;
    public static string stepDescription(int step) => (orders.ContainsKey(step)) ? orders[step] : "";


    public static void reset()
    {
        orders = new Dictionary<int, string>();
        percentDone = new Dictionary<int, float>();
        _step = 0;
        index = 0;
        terrainFinishedGenerating = false;
    }
    public static void addChunkLoadSteps()
    {
        reset();
        addStep("Centering Grid");
        addStep("Generating Grid");
        addStep("Downscaling Grid");
        addStep("Generating Mesh Points");
        addStep("Drawing Maps");
        addStep("Finalizing");
    }
    public static void updatePercent(float newPercent)
    {
        percentDone[step] = Mathf.RoundToInt(newPercent * 100);
    }
    public static void nextStep()
    {
        updatePercent(1);
        _step++;
    }
    public static void init()
    {
        if (hasRan) reset();
        hasRan = true;

        addStep("Unloading CSV Data");
        addStep("Centering Points");
        addStep("Generating Grid");
        addStep("Downscaling Master Grid");
        addStep("Saving Master Grid Points");
        addStep("Downscaling Child Grid");
        addStep("Saving Child Grid Points");
        addStep("Generating Mesh Points");
        addStep("Drawing Maps");
        addStep("Finalizing");
    }
    private static void addStep(string desc)
    {
        orders.Add(index, desc);
        percentDone.Add(index, 0);

        index++;
    }
}
