#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;

[EditorTool("OBJ Brush")]
public class OBJPlacerEditorTool : EditorTool, IDrawSelectedHandles
{
    public float brushSize = 100f;

    void OnEnable()
    {

    }

    void OnDisable()
    {

    }

    // Called when the active tool is set to this tool instance.
    public override void OnActivated()
    {
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Entering OBJ Brush Tool"), .1f);
    }

    // Called before the active tool is changed, or destroyed.
    public override void OnWillBeDeactivated()
    {
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Exiting OBJ Brush Tool"), .1f);
    }

    // Equivalent to Editor.OnSceneGUI.
    public override void OnToolGUI(EditorWindow window)
    {
        Handles.BeginGUI();
        Event e = Event.current;
        Vector3 mousePosition = e.mousePosition;
        Handles.DrawWireDisc(mousePosition, Vector3.forward, brushSize);
        Handles.EndGUI();
    }

    public void OnDrawHandles()
    {
        
    }
}

#endif