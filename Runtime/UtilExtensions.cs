using UnityEngine;

public static class UtilExtensions
{
    public static TUIOConnection GetConnection(this Component behaviour)
    {
        var tuioConnection = behaviour.GetComponent<TUIOConnection>();
        if (tuioConnection != null) return tuioConnection;

        tuioConnection = Object.FindObjectOfType<TUIOConnection>();
        if (tuioConnection != null) return tuioConnection;

        return new GameObject("TUIO Connection", typeof(TUIOConnection)).GetComponent<TUIOConnection>();
    }
}

