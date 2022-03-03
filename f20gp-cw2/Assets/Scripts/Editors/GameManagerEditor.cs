using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GameManager t = (GameManager)target;
        DrawDefaultInspector();

        if (Application.isPlaying && GUILayout.Button("Restart"))
        {
            t.Clear();
            t.StartGame();
        }

        if (Application.isPlaying && GUILayout.Button("Clear"))
        {
            t.Clear();
        }
    }
}
