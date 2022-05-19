using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class helpController : MonoBehaviour
{
    public GameObject infoController;
    public GameObject controlsController;
    public GameObject creditsController;
    public GameObject quitController;
    public GameObject aiController;
    public List<optionDisplayController> controllers;
    public bool isOpen = false;

    public void Start()
    {
        controllers = new List<optionDisplayController>()
        {
            infoController.transform.GetChild(1).GetChild(0).GetComponent<optionDisplayController>(),
            controlsController.transform.GetChild(1).GetChild(0).GetComponent<optionDisplayController>(),
            creditsController.transform.GetChild(1).GetChild(0).GetComponent<optionDisplayController>(),
            quitController.transform.GetChild(1).GetChild(0).GetComponent<optionDisplayController>(),
            aiController.transform.GetChild(1).GetChild(0).GetComponent<optionDisplayController>()
        };
    }

    public void toggleHelp(bool instant = false)
    {
        // could cause a problem? bc i call this elsewhere too
        StopAllCoroutines();
        if (isOpen)
        {
            // close menu
            isOpen = false;
            StartCoroutine(moveToLocal(new Vector3(0,85,0), Vector3.zero, infoController, instant, true));
            StartCoroutine(moveToLocal(new Vector3(0,160,0), Vector3.zero, controlsController, instant, true));
            StartCoroutine(moveToLocal(new Vector3(0,235,0), Vector3.zero, creditsController, instant, true));
            StartCoroutine(moveToLocal(new Vector3(0,310,0), Vector3.zero, quitController, instant, true));
            StartCoroutine(moveToLocal(new Vector3(0,385,0), Vector3.zero, aiController, instant, true));
        }
        else
        {
            // open menu
            isOpen = true;
            StartCoroutine(moveToLocal(Vector3.zero, new Vector3(0,85,0), infoController, instant));
            StartCoroutine(moveToLocal(Vector3.zero, new Vector3(0,160,0), controlsController, instant));
            StartCoroutine(moveToLocal(Vector3.zero, new Vector3(0,235,0), creditsController, instant));
            StartCoroutine(moveToLocal(Vector3.zero, new Vector3(0,310,0), quitController, instant));
            StartCoroutine(moveToLocal(Vector3.zero, new Vector3(0,385,0), aiController, instant));
        }
    }

    private IEnumerator moveToLocal(Vector3 start, Vector3 end, GameObject go, bool instant, bool disable = false)
    {
        go.SetActive(true);
        go.transform.localPosition = start;
        Vector3 fullStep = end - start;

        int stepCount = (instant) ? 1 : 20;
        for (int i = 0; i < stepCount; i++)
        {
            go.transform.localPosition += fullStep / stepCount;
            if (!instant) yield return new WaitForSeconds(0.01f);
        }

        if (disable) go.SetActive(false);
    }
}
