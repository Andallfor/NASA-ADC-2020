using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class loadingController : MonoBehaviour
{
    public mapGenerator mg;
    public playerMovement pm;

    public TextMeshProUGUI currentTask;
    public TextMeshProUGUI stepAmount;
    public TextMeshProUGUI percentDone;
    public TextMeshProUGUI playText;

    private bool done = false;

    public void Start()
    {
        pm.start = false;
        mg.initalTerrainGeneration();
    }

    private int lastStep = -1;

    private void Update()
    {
        if (done) return;
        if (master.terrainFinishedGenerating)
        {
            done = true;
            gameObject.SetActive(false);
            pm.start = true;
        }

        if (lastStep != master.step)
        {
            lastStep = master.step;
            currentTask.text = master.stepDescription(master.step);
            stepAmount.text = $"{master.step}/{master.maxStep}";
        }

        int p = (int) master.currentPercent(lastStep);
        
        if (p < 0) percentDone.text = "";
        else percentDone.text = $"{master.currentPercent(lastStep)}%";
    }
    public void reset()
    {
        done = false;
        currentTask.text = "\0";
        percentDone.text = "\0";
        stepAmount.text = "\0";
        playText.text = "Preparing";
        this.gameObject.SetActive(true);
    }
}
