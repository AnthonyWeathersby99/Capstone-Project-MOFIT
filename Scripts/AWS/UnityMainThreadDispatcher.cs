using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _actions = new Queue<Action>();
    private readonly object _lockObject = new object();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            _instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (_instance == null)
            {
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }
        return _instance;
    }

    public void Enqueue(Action action)
    {
        lock (_lockObject)
        {
            _actions.Enqueue(action);
        }
    }

    private void Update()
    {
        lock (_lockObject)
        {
            while (_actions.Count > 0)
            {
                Action action = _actions.Dequeue();
                action?.Invoke();
            }
        }
    }
}