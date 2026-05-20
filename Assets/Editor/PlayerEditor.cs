using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Player player = (Player)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prompt Controls", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        if (GUILayout.Button("Select Prompt 1"))
        {
            player.SelectPrompt1();
        }

        if (GUILayout.Button("Select Prompt 2"))
        {
            player.SelectPrompt2();
        }

        if (GUILayout.Button("Select Prompt 3"))
        {
            player.SelectPrompt3();
        }
        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use the prompt buttons.", MessageType.Info);
        }
    }
}
