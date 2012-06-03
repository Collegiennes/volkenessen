using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

class ConditionalBehaviour : MonoBehaviour
{
    public float SinceAlive;

    public Action Action;
    public Condition Condition;
    public bool TimeScaleInsensitive;

    Stopwatch stopwatch;

    static readonly Pool<Stopwatch> Watches = new Pool<Stopwatch>();

    void Start()
    {
        if (TimeScaleInsensitive)
        {
            stopwatch = Watches.Take();
            stopwatch.Start();
        }
    }

    void Update()
    {
        SinceAlive = TimeScaleInsensitive ? (float)stopwatch.Elapsed.TotalSeconds : SinceAlive + Time.deltaTime;
        if (Condition(SinceAlive))
        {
            if (Action != null) Action();
            Destroy(gameObject);
            if (stopwatch != null)
            {
                Watches.Return(stopwatch);
                stopwatch.Reset();
                stopwatch = null;
            }
            Action = null;
            Condition = null;
        }
    }
}

public delegate bool Condition(float elapsedSeconds);

public static class Wait
{
    public static void Until(Condition condition, Action action, bool timeScaleInsensitive)
    {
        var go = new GameObject("Waiter");
        var w = go.AddComponent<ConditionalBehaviour>();
        w.Condition = condition;
        w.Action = action;
        w.TimeScaleInsensitive = timeScaleInsensitive;
    }
    public static void Until(Condition condition, Action action)
    {
        var go = new GameObject("Waiter");
        var w = go.AddComponent<ConditionalBehaviour>();
        w.Condition = condition;
        w.Action = action;
    }
    public static void Until(Condition condition, bool timeScaleInsensitive)
    {
        var go = new GameObject("Waiter");
        var w = go.AddComponent<ConditionalBehaviour>();
        w.Condition = condition;
        w.TimeScaleInsensitive = timeScaleInsensitive;
    }
    public static void Until(Condition condition)
    {
        var go = new GameObject("Waiter");
        var w = go.AddComponent<ConditionalBehaviour>();
        w.Condition = condition;
    }
}
