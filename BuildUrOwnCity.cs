using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildUrOwnCity : MonoBehaviour
{
    // Start is called before the first frame update
    public LayerMask lMask;
    RaycastHit hit;

    public void spawnObj(GameObject go)
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, lMask))
        {
            Vector3 ObjDropPos = new Vector3(hit.point.x, hit.point.y + 0.1f, hit.point.z);
            // 270, 0, 0, | 8, 8, 8 | COMS
            // 0, 0, 0, | 20, 20, 20 | BASE
            // 270, 0, 0, | 0, 0, 0 | ROVER
            // 270, 0, 0, | 0, 0, 0 | MOBILE
            // 270, 0, 0, | 0, 0, 0 | POWER
           GameObject obj = Instantiate(go, ObjDropPos + go.transform.localPosition, go.transform.rotation, this.transform);
        }
    }
}