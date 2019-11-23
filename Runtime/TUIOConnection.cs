
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TUIO;
using System.Collections;
using System.Threading.Tasks;

public class TrackedObject
{
    public long SymbolId { get; set; }
    public float Angle { get; set; }
    public Vector2 Position { get; set; }
}

public class TUIOConnection : MonoBehaviour
{
    public int Port = 3333;

    public IEnumerable<int> VisibleSymbols => _visibleObjectsDict.Keys.AsEnumerable();
    public IEnumerable<TrackedObject> VisibleObjects => _visibleObjectsDict.Values.AsEnumerable();

    private Dictionary<int, TrackedObject> _visibleObjectsDict = new Dictionary<int, TrackedObject>();

    private Connection _connection;

    void Awake()
    {
        _connection = new Connection(Port);
    }

    async void Start()
    {
        while (gameObject.activeInHierarchy)
        {
            var state = await _connection.Listen();

            await WaitForEndOfFrame();

            _visibleObjectsDict = state.Objects.ToDictionary(x => x.Key, x => new TrackedObject
            {
                SymbolId = x.Value.SymbolID,
                Angle = x.Value.AngleDegrees,
                Position = new Vector2(x.Value.Position.X, x.Value.Position.Y)
            });

        }
    }

    async Task WaitForEndOfFrame()
    {
        var src = new TaskCompletionSource<bool>();
        StartCoroutine(_WaitForEndOfFrame(src));
        await src.Task;
    }

    IEnumerator _WaitForEndOfFrame(TaskCompletionSource<bool> src)
    {
        yield return new WaitForEndOfFrame();
        src.TrySetResult(true);
    }

    void OnApplicationQuit() => _connection.Close();

    void OnDestroy() => _connection.Close();
}
