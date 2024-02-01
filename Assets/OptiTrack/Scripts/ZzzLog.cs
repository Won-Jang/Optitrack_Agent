using UnityEngine;
using System.Collections;

public class ZzzLog : MonoBehaviour
{
    uint qsize = 30;  // number of messages to keep
    Queue myLogQueue = new Queue();

    void Start()
    {
        Debug.Log("Started up logging.");
    }

    /// <summary>
    /// Write logs to a file
    /// </summary>
    private void OnApplicationQuit()
    {
        //
    }

    void OnEnable()
    {
        
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        
        myLogQueue.Enqueue("<color=black>[" + type + "] : " + logString + "</color>");
        if (type == LogType.Exception)
            myLogQueue.Enqueue(stackTrace);
        while (myLogQueue.Count > qsize)
            myLogQueue.Dequeue();
        
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 400, 0, 400, Screen.height));
        GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()));
        GUILayout.Button("Click me");
        GUILayout.EndArea();
    }
}
