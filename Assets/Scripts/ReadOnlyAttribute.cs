using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Custom attribute
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;                 // disable editing
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;                  // re-enable
    }
}
#endif