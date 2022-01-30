using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;

public static class mapGeneratorTimer
{
    private static Dictionary<string, System.Diagnostics.Stopwatch> timers = new Dictionary<string, System.Diagnostics.Stopwatch>();
    private static Dictionary<string, double> unloadedTime = new Dictionary<string, double>();
    public static void startCounter(string name)
    {
        System.Diagnostics.Stopwatch a = Stopwatch.StartNew();
        timers.Add(name, a);
    }
    public static void endCounter(string name)
    {
        double timeElapsed = timers[name].ElapsedMilliseconds;
        timers[name].Stop();
        unloadedTime.Add(name, timeElapsed);
    }
    public static void quickEndStart(string end, string start)
    {
        startCounter(start);
        endCounter(end);
    }
    public static void loadCounters()
    {
        using (StreamWriter sw = File.CreateText($"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/TGenData6.txt"))
        {
            foreach (KeyValuePair<string, double> kvp in unloadedTime)
            {
                sw.WriteLine($"{kvp.Key}: {kvp.Value}ms");
            }
        }
    }
}
