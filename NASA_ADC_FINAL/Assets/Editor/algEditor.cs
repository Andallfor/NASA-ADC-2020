using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(alg))]
public class algEditor : Editor
{
    public override void OnInspectorGUI()
    {
        alg s = (alg) target;
        if(GUILayout.Button("Generate AI"))
        {
            s.genAI();
        }
        if(GUILayout.Button("Randomize Start and End"))
        {
            s.genSEEditor();
        }

        DrawDefaultInspector();
    }
}
