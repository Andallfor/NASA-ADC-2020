using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(AIDebugger))]
public class debuggerEditor : Editor
{
    AIDebugger s;
    mapGenerator mg;
    alg a;
    int runsShown;

    bool inspectingLine = false;
    int selectedLine = 0;
    int lineStep = 0;
    int lastLineStep = 0;
    int lastLineDrawn = 0;
    public override void OnInspectorGUI()
    {
        s = (AIDebugger) target;
        a = s.a;
        mg = GameObject.FindGameObjectWithTag("tMapGenerator").GetComponent<mapGenerator>();
        LineRenderer l = s.gameObject.GetComponent<LineRenderer>();

        s.enableDebugging = GUILayout.Toggle(s.enableDebugging, "Enable Debugging");

        if (s.enableDebugging && Application.IsPlaying(this) && s.runs.Count != 0)
        {
            // show best run under certain timeframe
            runsShown = (int) GUILayout.HorizontalSlider((int) runsShown, 0, 10);
            GUILayout.Label("");
            GUILayout.Label($"Showing the best of every {runsShown} attempts");
            GUILayout.Label($"# of successess: {a.pAI.successes}");
            GUILayout.Label($"# of failures: {a.pAI.fails}");
            GUILayout.Label($"Fails due to max moves: {a.pAI.failDueToExceededMaximumMoves}");
            GUILayout.Label($"Fails due to angle: {a.pAI.failDueToSlope}");
            GUILayout.Label($"Fails due to out of bounds: {a.pAI.failDueToOutOfBounds}");
            GUILayout.Label($"Fails due to no move: {a.pAI.failDueToNoMove}");
            //GUILayout.Label($"Length of states: {a.pAI.Q.Count}");
            if (runsShown != 0)
            {
                if (runsShown == 1)
                {
                    Vector2Int currentPos = new Vector2Int((int) a.startPos.x, (int) a.startPos.y);
                    List<Vector3> positions = new List<Vector3>(){mg.cubes[(int) a.startPos.x, (int) a.startPos.y].selfPosition + new Vector3(0, 1, 0)};
                    foreach (Vector2Int v in s.runs[s.runs.Count - 2].moves)
                    {
                        currentPos += v;
                        if (currentPos.x < 0 || currentPos.x > mg.xMeshLength - 1 || currentPos.y < 0 || currentPos.y > mg.yMeshLength - 1)
                        {
                            break;
                        }
                        positions.Add(mg.cubes[currentPos.x, currentPos.y].selfPosition + new Vector3(0, 1, 0));
                    }

                    l.positionCount = positions.Count;
                    l.SetPositions(positions.ToArray());
                }
                else if (s.runs.Count % runsShown == 0 && s.runs.Count != lastLineDrawn && s.runs.Count != 0)
                {
                    lastLineDrawn = s.runs.Count;
    
                    l.positionCount = 0;
                    List<Vector3> positions = new List<Vector3>()
                    {
                        mg.cubes[(int) a.startPos.x, (int) a.startPos.y].selfPosition + new Vector3(0, 1, 0)
                    };

                    float bestScore = Mathf.Infinity;
                    attempt bestAttempt = s.runs[s.runs.Count - runsShown];
                    for (int i = Mathf.Max(0, s.runs.Count - runsShown); i < s.runs.Count; i++)
                    {
                        attempt currentAttempt = s.runs[i];

                        if (currentAttempt.succeded && bestScore < currentAttempt.moves.Count)
                        {
                            bestScore = currentAttempt.moves.Count;
                            bestAttempt = new attempt(currentAttempt);
                        }
                    }

                    Vector2Int currentPos = new Vector2Int((int) a.startPos.x, (int) a.startPos.y);
                    foreach (Vector2Int v in bestAttempt.moves)
                    {
                        currentPos += v;
                        if (currentPos.x < 0 || currentPos.x > mg.xMeshLength - 1 || currentPos.y < 0 || currentPos.y > mg.yMeshLength - 1)
                        {
                            break;
                        }

                        positions.Add(mg.cubes[currentPos.x, currentPos.y].selfPosition + new Vector3(0, 1, 0));
                        
                    }

                    l.positionCount = positions.Count;
                    l.SetPositions(positions.ToArray());
                }
                else if (runsShown == 0) l.positionCount = 0;
            }

            GUILayout.Space(20);

            if (!inspectingLine)
            {
                selectedLine = Mathf.Max(Mathf.Min(EditorGUILayout.IntField("Line #:", selectedLine), s.runs.Count - 1), 0);

                if (GUILayout.Button("Generate Line"))
                {
                    inspectingLine = true;
                }
                if (GUILayout.Button("Generate Best Line"))
                {
                    float closestPosition = Mathf.Infinity;
                    float leastSteps = Mathf.Infinity;
                    int i = 0;
                    foreach (attempt a in s.runs)
                    {
                        if (i == s.runs.Count - 1) continue;
                        if (a.info.closestPosition < closestPosition || (a.succeded && a.moves.Count < leastSteps))
                        {
                            closestPosition = a.info.closestPosition;
                            leastSteps = a.moves.Count;
                            selectedLine = i;
                        }
                        i++;
                    }
                    lineStep = s.runs[selectedLine].moves.Count - 1;
                    inspectingLine = true;
                }
            }
            else
            {
                GUILayout.Label($"Inspecting line #: {selectedLine}");
                lineStep = EditorGUILayout.IntField("Step #:", lineStep);

                GUILayout.Space(20);

                GUILayout.Label($"Closest Distance of {s.runs[selectedLine].info.closestPosition} at Iteration {s.runs[selectedLine].info.closestPosIter}");
                GUILayout.Label($"Finished With Exit Code {s.runs[selectedLine].info.exitCode}");

                GUILayout.Space(20);

                if (GUILayout.Button("Next Step"))
                {
                    lineStep = Mathf.Min(lineStep + 1, s.runs[selectedLine].moves.Count - 1);
                }
                if (GUILayout.Button("Last Step"))
                {
                    lineStep = Mathf.Max(lineStep - 1, 0);
                }

                GUILayout.Space(20);
                if (GUILayout.Button("Show Full Line"))
                {
                    lineStep = s.runs[selectedLine].moves.Count - 1;
                }
                if (GUILayout.Button("End Inspection"))
                {
                    inspectingLine = false;
                    l.positionCount = 0;
                }

                if (lastLineStep != lineStep)
                {
                    lastLineStep = lineStep;
                    s.drawLine(s.runs[selectedLine].moves.GetRange(0, lineStep));
                }
            }
        }
    }
}
