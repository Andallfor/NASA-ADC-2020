using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class waypointInfo : MonoBehaviour
{
    public Color color;
    public GameObject go;
    public Material m;

    public void Start()
    {
        go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = this.gameObject.transform.position + new Vector3(0, 50, 0);
        go.transform.parent = this.gameObject.transform;
        go.transform.localScale = new Vector3(2f/transform.lossyScale.x, 2f/transform.lossyScale.y, 2f/transform.lossyScale.z);
        go.GetComponent<MeshRenderer>().material = m;
        go.GetComponent<MeshRenderer>().material.color = color;
        go.layer = 9;
    }
}
