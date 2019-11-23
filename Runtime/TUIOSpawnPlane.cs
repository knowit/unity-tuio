using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TUIOSpawnPlane : MonoBehaviour 
{

    public Vector2 Size = Vector2.one;

    [Serializable]
    public class SymbolMapEntry
    {
        public int SymbolID;
        public GameObject Prefab;
    }
    public SymbolMapEntry[] SymbolMap = new SymbolMapEntry[] { };

    private TUIOConnection _connection;
    private Dictionary<long, GameObject> _trackedObjects = new Dictionary<long, GameObject>();

    void Awake()
    {
        _connection = this.GetConnection();
    }

    void Update()
    {
        var visibleObjects = _connection.VisibleObjects.ToList();

        foreach(var deletableObject in _trackedObjects.Where(x => visibleObjects.FirstOrDefault(y => y.SymbolId == x.Key) == null).ToList())
        {
            Debug.Log($"Destory object with symbol ID: {deletableObject.Key}");
            Destroy(deletableObject.Value);
            _trackedObjects.Remove(deletableObject.Key);
        }

        foreach(var visibleObject in _connection.VisibleObjects)
        {
            GameObject sceneObject = null;
            if(!_trackedObjects.TryGetValue(visibleObject.SymbolId, out sceneObject))
            {
                var prefab = SymbolMap.FirstOrDefault(x => x.SymbolID == visibleObject.SymbolId)?.Prefab;
                if (prefab == null)
                {
                    Debug.Log($"No prefab for symbol ID: {visibleObject.SymbolId}");
                    continue;
                }
                Debug.Log($"Instantiate object with symbol ID: {visibleObject.SymbolId}");
                sceneObject = Instantiate(prefab, transform);
                _trackedObjects.Add(visibleObject.SymbolId, sceneObject);
            }

            var normal = transform.rotation * Vector3.up;
            var position = new Vector3(
                Mathf.LerpUnclamped(transform.position.x - (Size.x/2), transform.position.x + (Size.x / 2), visibleObject.Position.x), 
                transform.position.y,
                Mathf.LerpUnclamped(transform.position.y - (Size.y / 2), transform.position.y + (Size.y / 2), visibleObject.Position.y));

            sceneObject.transform.SetPositionAndRotation(
                RotateAroundPoint(position, transform.position, transform.rotation),
                Quaternion.AngleAxis(visibleObject.Angle, normal));
        }
    }

    private Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle) 
        => angle * (point-pivot)+pivot;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, 0.1f);
        Gizmos.DrawCube(transform.position, new Vector3(Size.x, 0.0f, Size.y));

        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(transform.position, new Vector3(Size.x, 0.0f, Size.y));
    }
}
