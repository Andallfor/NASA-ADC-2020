using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class dataHub : MonoBehaviour
{
    dataAI dAI;

    public bool generateAIData = false;
    public string interpreter, aiDataPath;
    public string aiIter, aiExecTime, aiSuccessRate, aiBestPath, aiSquaresExplored;

    [Header("Save Data")]
    public bool saveAIData = false;
    public string aiPathKeys;
    public string aiPathValues;

    [Header("Presets")]
    public bool Leo = false;
    public bool Arya = false;
    string leoPreset = "/Users/leo/Desktop/codeAndStuff/NASA_ADC/";
    string aryaPreset = "/Users/Arya/projects/AI-DATA/AI-DATA/bin/Release/";

    void OnValidate()
    {
        if (generateAIData)
        {
            genDataAI();
            generateAIData = false;
        }
        if (Leo && aiDataPath != leoPreset)
        {
            Arya = false;
            setPreset(leoPreset);
        }
        if (Arya && aiDataPath != aryaPreset)
        {
            Leo = false;
            setPreset(aryaPreset);
        }
    }

    void setPreset(string folder)
    {
        aiIter = folder + "aiIter.txt";
        aiExecTime = folder + "aiExecTime.txt";
        aiSuccessRate = folder + "aiSuccessRate.txt";
        aiBestPath = folder + "aiBestPath.txt";
        aiSquaresExplored = folder + "aiSquaresExplored.txt";
        aiPathKeys = folder + "aiKeys.binary";
        aiPathValues = folder + "aiValues.binary";
        aiDataPath = folder;
    }

    void Start()
    {
        dAI = GetComponent<dataAI>();
    }

    public void genDataAI()
    {
        writeAIData();
        ExecuteProcessTerminal($"{interpreter} {aiDataPath}");
    }
    public void writeAIData()
    {
        string[] dataFiles = new string[5] {aiIter, aiExecTime, aiSuccessRate, aiBestPath, aiSquaresExplored};
        List<float>[] a = new List<float>[5] {dAI.iter, dAI.execTime, dAI.successRate, dAI.bestPath, dAI.squaresExplored};

        for (int i = 0; i < 5; i++)
        {
            using (StreamWriter sw = File.CreateText(dataFiles[i]))
            {
                foreach (float j in a[i])
                {
                    sw.WriteLine(j);
                }
            }
        }

        // save AI data
        if (saveAIData)
        {
            dAI.orderQ();
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream saveFile = File.Create(aiPathKeys);
            FileStream saveFile2 = File.Create(aiPathValues);

            formatter.Serialize(saveFile, dAI.qKey);
            formatter.Serialize(saveFile2, dAI.qValue);
            saveFile.Close();
            saveFile2.Close();
        }
    }

    /*
    code from: https://forum.unity.com/threads/run-terminal-command-and-get-output-within-unity-application-osx.683164/#post-4574398 
    (mac is stupid why did i get a mac)
    */
    private string ExecuteProcessTerminal(string argument)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "/bin/bash",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                Arguments = " -c \"" + argument + " \""
            };
            Process myProcess = new Process
            {
                StartInfo = startInfo
            };
            myProcess.Start();
            string output = myProcess.StandardOutput.ReadToEnd();
            myProcess.WaitForExit();
 
            return output;
        }
        catch (Exception e)
        {
            print(e);
            return null;
        }
    }
}
