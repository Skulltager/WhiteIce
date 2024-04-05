using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VolumetricFog))]
public class VolumetricFogEditor : Editor
{
    private const string SELECTED_CHANNEL_KEY = "Worley Noise Key";
    private const string PROPERTY_NOISECHANNEL_RED = "redPositionLayers";
    private const string PROPERTY_NOISECHANNEL_GREEN = "greenPositionLayers";
    private const string PROPERTY_NOISECHANNEL_BLUE = "bluePositionLayers";
    private const string PROPERTY_NOISECHANNEL_ALPHA = "alphaPositionLayers";
    private const string PROPERTY_NOISECHANNEL_SEED = "seed";
    private const string PROPERTY_NOISECHANNEL_TEXTURESIZE = "textureSize";

    private ColorChannel selectedChannel;
    private new VolumetricFog target;

    private void Awake()
    {
        target = (VolumetricFog) base.target;
        selectedChannel = (ColorChannel) EditorPrefs.GetInt(SELECTED_CHANNEL_KEY, 0);
    }

    public override void OnInspectorGUI()
    {
        if (!Application.isPlaying)
        {
            DrawDefaultInspector();
            return;
        }

        selectedChannel = (ColorChannel) EditorGUILayout.EnumPopup(selectedChannel);
        bool changes = false;
        switch(selectedChannel)
        {
            case ColorChannel.Red:
                changes = EditorGUILayout.PropertyField(serializedObject.FindProperty(PROPERTY_NOISECHANNEL_RED));
                break;
            case ColorChannel.Green:
                changes = EditorGUILayout.PropertyField(serializedObject.FindProperty(PROPERTY_NOISECHANNEL_GREEN));
                break;
            case ColorChannel.Blue:
                changes = EditorGUILayout.PropertyField(serializedObject.FindProperty(PROPERTY_NOISECHANNEL_BLUE));
                break;
            case ColorChannel.Alpha:
                changes = EditorGUILayout.PropertyField(serializedObject.FindProperty(PROPERTY_NOISECHANNEL_ALPHA));
                break;
        }
        changes = EditorGUILayout.PropertyField(serializedObject.FindProperty(PROPERTY_NOISECHANNEL_SEED)) || changes;
        changes = EditorGUILayout.PropertyField(serializedObject.FindProperty(PROPERTY_NOISECHANNEL_TEXTURESIZE)) || changes;

        if (changes)
        {
            serializedObject.ApplyModifiedProperties();
            target.RegenerateChannel(selectedChannel);
        }
    }

    private void OnDestroy()
    {
         EditorPrefs.SetInt(SELECTED_CHANNEL_KEY, (int) selectedChannel);
    }
}
