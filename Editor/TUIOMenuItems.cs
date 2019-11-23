using UnityEditor;
using UnityEngine;

public class TUIOMenuItems 
{
    [MenuItem("GameObject/TUIO/Spawn Plane", false, 10)]
    public static void CreateSpawnPlane(MenuCommand menuCommand)
        => CreateGameObject<TUIOSpawnPlane>("TUIO Spawn Plane", menuCommand.context);

    [MenuItem("GameObject/TUIO/Connection", false, 10)]
    public static void CreateConnection(MenuCommand menuCommand)
        => CreateGameObject<TUIOConnection>("TUIO Connection", menuCommand.context);
    
    private static void CreateGameObject<T>(string name, Object context)
    {
        GameObject go = new GameObject(name, typeof(T));
        GameObjectUtility.SetParentAndAlign(go, context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }
}
