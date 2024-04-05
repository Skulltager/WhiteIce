using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (!Application.isPlaying)
        {
            DrawDefaultInspector();
            return;
        }

        if (DrawDefaultInspector())
        {
            serializedObject.ApplyModifiedProperties();

            //if(serializedObject.FindProperty("autoRegenerate").boolValue)
            //    (target as WorldGenerator).RegenerateNoiseMaps();
        }
    }
}
