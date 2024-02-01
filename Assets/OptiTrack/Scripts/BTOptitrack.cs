using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace Agent
{
    /// <summary>
    /// Implements live tracking of streamed OptiTrack rigid body data onto an object.
    /// </summary>
    public class BTOptitrack : MonoBehaviour
    {
        // Socket - TCP/IP or UDP (TODO: UDP is not tested. need to review)
        private BTSocket _socket;

    }
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }

        public class Log : MonoBehaviour
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

                Application.logMessageReceivedThreaded += HandleLog;
            }

            void OnDisable()
            {
                Application.logMessageReceivedThreaded -= HandleLog;
            }

            void HandleLog(string logString, string stackTrace, LogType type)
            {
                Debug.Log("############ DEBUG HERE ############");
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
    }
}