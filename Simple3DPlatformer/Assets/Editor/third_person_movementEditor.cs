using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(third_person_movement))]
public class third_person_movementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        third_person_movement tpmvmnt = (third_person_movement) target;

        GUILayout.BeginHorizontal();
        
        GUILayout.EndHorizontal();
    }
}