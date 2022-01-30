using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class optionDisplayController : MonoBehaviour
{
    public GameObject[] pushAside;
    public GameObject[] removeBehind;
    public bool open = false;
    private RectTransform rt;
    private minimapController mmc;

    public void Start()
    {
        rt = this.GetComponent<RectTransform>();
        mmc = GameObject.FindGameObjectWithTag("controlHub").GetComponent<controls>().mc.GetComponent<minimapController>();
    }
    public void toggle(bool instant = false)
    {
        // figure out how to scale this
        this.transform.position = new Vector3(0,0,0);
        if (open)
        {
            // close
            open = false;
            StartCoroutine(moveToLocal(new Vector3(-80, -234, 0), new Vector3(-460, -234, 0), this.gameObject, instant));
            this.transform.parent.parent.GetChild(0).gameObject.SetActive(true);
            foreach (GameObject go in removeBehind) go.SetActive(true);
        }
        else
        {
            // open
            open = true;
            StartCoroutine(moveToLocal(new Vector3(-460, -234, 0), new Vector3(-80, -234, 0), this.gameObject, instant));
            this.transform.parent.parent.GetChild(0).gameObject.SetActive(false);
            foreach (GameObject go in removeBehind) go.SetActive(false);
        }
    }
    private IEnumerator moveToLocal(Vector3 start, Vector3 end, GameObject go, bool instant, bool disable = false)
    {
        go.SetActive(true);
        go.transform.localPosition = start;
        go.transform.position = new Vector3(go.transform.position.x, 0, go.transform.position.z);
        Vector3 fullStep = end - start;

        int stepCount = (instant) ? 1 : 15;
        for (int i = 0; i < stepCount; i++)
        {
            float xChange = go.transform.position.x;
            go.transform.localPosition += fullStep / stepCount;
            xChange -= go.transform.position.x;

            go.transform.position = new Vector3(go.transform.position.x, 0, go.transform.position.z);
            foreach (GameObject push in pushAside)
            {
                push.transform.position -= new Vector3(xChange, 0, 0);
            }
            if (!instant) yield return new WaitForSeconds(0.01f);
        }

        if (disable) go.SetActive(false);
    }
}
