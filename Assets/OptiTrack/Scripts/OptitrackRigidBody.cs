/* 
Copyright © 2016 NaturalPoint Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. 

Modified: Won Jang (won DOT jang AT boystown DOT org)
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace Agent { 
/// <summary>
/// Implements live tracking of streamed OptiTrack rigid body data onto an object.
/// </summary>
    public class OptitrackRigidBody : MonoBehaviour
    {
        [Tooltip("The object containing the OptiTrackStreamingClient script.")]
        public OptitrackStreamingClient StreamingClient;

        [Tooltip("The Streaming ID of the rigid body in Motive")]
        public Int32 RigidBodyId;

        [Tooltip("Subscribes to this asset when using Unicast streaming.")]
        public bool NetworkCompensation = true;

        private BTSocket socket;                    // Socket - TCP/IP or UDP (TODO: UDP is not tested. need to review)
        private OptitrackRigidBodyState rbState;    // Optitrack data
        private OptitrackPose userRBstate;          // Custom Rigid Body State. Reset all positions to user
        private Transform offset;                   // Define offset allowance

        private bool isTracking;
        private bool isCalibrated;

                                                    // TODO: Exporting tracking data to files
        private ArrayList trackingData;             // Tracking data will be stored temporary and export to file

        /// <summary>
        ///  Initialize unity environment and set initial values
        /// </summary>
        void Start()
        {
            this.isCalibrated = false;  // custom position from user
            this.isTracking = false;    // tracking mode
            this.trackingData = new ArrayList();
            // If the user didn't explicitly associate a client, find a suitable default.
            if ( this.StreamingClient == null )
            {
                this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();

                // If we still couldn't find one, disable this component.
                if ( this.StreamingClient == null )
                {
                    Debug.LogError( GetType().FullName + ": Streaming client not set, and no " + typeof( OptitrackStreamingClient ).FullName + " components found in scene; disabling this component.", this );
                    this.enabled = false;
                    return;
                }
            }

            this.StreamingClient.RegisterRigidBody( this, RigidBodyId );

            this.socket = new TCPIP(Convert.ToInt16("8888"));
            this.socket.OnDataReceived += new ServerHandlePacketData(server_OnDataReceived);
            this.socket.Start();
            Debug.Log("Listening TCP/IP", this);
        }

        void OnApplicationQuit()
        {
            this.socket.Stop();
            Debug.Log("Closed Socket", this);
        }

        //This method is called when the server has received data from the client
        void server_OnDataReceived(byte[] data, int bytesRead)
        //void server_OnDataReceived(byte[] data, int bytesRead, System.Net.Sockets.TcpClient client)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            string message = encoder.GetString(data, 0, bytesRead);
            //report("Received a message: " + message);

            string[] tokens = message.Split('#');
            string command = tokens[0];
            string contents = tokens[1];
            decodingSocketData(command, contents);
        }

        private void decodingSocketData(string command, string contents)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            string jsonTo;
            switch (command.ToLower())
            {
                case "echosocket":      // simple echo for verification
                    this.socket.SendImmediateToAll(encoder.GetBytes("conneted"));
                    break;
                case "serverdescription":
                    jsonTo = JsonUtility.ToJson(this.StreamingClient.GetServerDesc());
                    this.socket.SendImmediateToAll(encoder.GetBytes(jsonTo));
                    break;
                case "resetorigin":
                    resetOrigin(Int32.Parse(contents));
                    this.socket.SendImmediateToAll(encoder.GetBytes("1"));
                    break;
                case "getposition":
                    string[] tokens = contents.Split('$');
                    Int32 streamingId = Int32.Parse(tokens[0]);
                    string asset = tokens[1];

                    ChangeRegidBody(streamingId);

                    if (String.Equals(asset, "RigidBody", StringComparison.OrdinalIgnoreCase))
                    {
                        jsonTo = JsonUtility.ToJson(getPosition(this.rbState.Pose));
                        this.socket.SendImmediateToAll(encoder.GetBytes(jsonTo));
                    }
                    else if (String.Equals(asset, "Markers", StringComparison.OrdinalIgnoreCase))
                    {
                        OptitrackRigidBodyDefinition rbdef = this.StreamingClient.GetRigidBodyDefinitionById(streamingId);
                        // markers
                        Marker[] SPmarkers = new Marker[rbdef.Markers.Count];
                        //rigid body


                        for (int i = 0; i < rbdef.Markers.Count; i++)
                        {
                            SPmarkers[i] = new Marker();
                            SPmarkers[i].Id = rbdef.Id;
                            SPmarkers[i].Name = rbdef.Name + " - " + i;
                            SPmarkers[i].Position = new Vector3(
                                                                (float)Math.Round((this.rbState.Pose.Position.x - rbdef.Markers[i].Position.x) * 1000),
                                                                (float)Math.Round((this.rbState.Pose.Position.y - rbdef.Markers[i].Position.y) * 1000),
                                                                (float)Math.Round((this.rbState.Pose.Position.z - rbdef.Markers[i].Position.z) * 1000)
                                                                );
                        }

                        jsonTo = JsonHelper.ToJson(SPmarkers);
                        this.socket.SendImmediateToAll(encoder.GetBytes(jsonTo));
                    }
                        break;
                case "setrange":
                    Debug.Log("contents: " + contents, this);
                    Transform pos = JsonUtility.FromJson<Transform>(contents);
                    this.offset = pos;
                    //this.offset.SetQuaternion();
                    break;
                case "checkrange":
                    this.socket.SendImmediateToAll(encoder.GetBytes(isRanged() ? "1" : "0"));
                    break;
                case "getrange":
                    jsonTo = JsonUtility.ToJson(this.offset);
                    this.socket.SendImmediateToAll(encoder.GetBytes(jsonTo));
                    break;
                /* Start Tracking */
                case "starttracking":
                    this.trackingData.Clear();
                    this.socket.SendImmediateToAll(encoder.GetBytes(this.trackingData.Count.ToString()));
                    this.isTracking = true;
                    break;
                case "endtracking":
                    this.isTracking = false;
                    this.socket.SendImmediateToAll(encoder.GetBytes(this.trackingData.Count.ToString()));
                    break;
                case "checkTracking":
                    int outOfRange = 0;
                    for (int i = 0; i < this.trackingData.Count; i++)
                    {
                        if (!isRanged((OptitrackPose)this.trackingData[i]))
                        {
                            outOfRange++;
                        }
                    }
                    this.socket.SendImmediateToAll(encoder.GetBytes(outOfRange.ToString()));
                    break;
                case "getallmarkers":
                    List<OptitrackMarkerState> markers = this.StreamingClient.GetLatestMarkerStates();
                    List<Marker> allMarkers = new List<Marker>();

                    for (int i = 0; i < markers.Count; i++)
                    {
                        if (markers[i].Labeled == false)
                        {
                            Marker mk = new Marker();
                            
                            mk.Id = markers[i].Id;
                            mk.Name = markers[i].Name;
                            mk.Position = new Vector3(
                                                        (float)Math.Round(markers[i].Position.x * 1000),
                                                        (float)Math.Round(markers[i].Position.y * 1000),
                                                        (float)Math.Round(markers[i].Position.z * 1000)
                                                     );
                            allMarkers.Add(mk);
                        }
                    }
                    jsonTo = JsonHelper.ToJson(allMarkers.ToArray());
                    this.socket.SendImmediateToAll(encoder.GetBytes(jsonTo));
                    break;
            }
        }

        private void ChangeRegidBody(Int32 rigidBodyId)
        {
            this.RigidBodyId = rigidBodyId;
            this.StreamingClient.RegisterRigidBody(this, rigidBodyId);
            Thread.Sleep(100);
            //this.rbState = StreamingClient.GetLatestRigidBodyState(RigidBodyId, NetworkCompensation);
        }

        private void resetOrigin(Int32 rigidBodyId)
        {
            ChangeRegidBody(rigidBodyId);
            this.userRBstate = this.rbState.Pose;
            this.isCalibrated = true;
        }

        private Transform getPosition(OptitrackPose currentTransform)
        {
            Transform newTransform = new Transform();
            if (isCalibrated == true)
            { 
                newTransform.X = (float)(Math.Round(currentTransform.Position.x - userRBstate.Position.x, 3) * 1000);
                newTransform.Y = (float)(Math.Round(currentTransform.Position.y - userRBstate.Position.y, 3) * 1000);
                newTransform.Z = (float)(Math.Round(currentTransform.Position.z - userRBstate.Position.z, 3) * 1000);
                
                newTransform.RX = (float)Math.Round(currentTransform.Orientation.x - userRBstate.Orientation.x, 3);
                newTransform.RY = (float)Math.Round(currentTransform.Orientation.y - userRBstate.Orientation.y, 3);
                newTransform.RZ = (float)Math.Round(currentTransform.Orientation.z - userRBstate.Orientation.z, 3);
                newTransform.RW = (float)Math.Round(currentTransform.Orientation.w - userRBstate.Orientation.w, 3);

                newTransform.PITCH = (float)Math.Round(currentTransform.Orientation.eulerAngles.x - userRBstate.Orientation.eulerAngles.x, 3);
                newTransform.YAW = (float)Math.Round(currentTransform.Orientation.eulerAngles.y - userRBstate.Orientation.eulerAngles.y, 3);
                newTransform.ROLL = (float)Math.Round(currentTransform.Orientation.eulerAngles.z - userRBstate.Orientation.eulerAngles.z, 3);
            }
            else
            {
                newTransform.X = (float)(Math.Round(currentTransform.Position.x * 1000));
                newTransform.Y = (float)(Math.Round(currentTransform.Position.y * 1000));
                newTransform.Z = (float)(Math.Round(currentTransform.Position.z * 1000));

                newTransform.RX = (float)Math.Round(currentTransform.Orientation.x, 3);
                newTransform.RY = (float)Math.Round(currentTransform.Orientation.y, 3);
                newTransform.RZ = (float)Math.Round(currentTransform.Orientation.z, 3);
                newTransform.RW = (float)Math.Round(currentTransform.Orientation.w, 3);

                newTransform.PITCH = (float)Math.Round(currentTransform.Orientation.eulerAngles.x, 3);
                newTransform.YAW = (float)Math.Round(currentTransform.Orientation.eulerAngles.y, 3);
                newTransform.ROLL = (float)Math.Round(currentTransform.Orientation.eulerAngles.z, 3);
            }
            
            return newTransform;
        }

        private bool isRanged(OptitrackPose pos)
        {
            if (Math.Abs(pos.Position.x - this.rbState.Pose.Position.x) < this.offset.X &&
                Math.Abs(pos.Position.y - this.rbState.Pose.Position.y) < this.offset.Y &&
                Math.Abs(pos.Position.z - this.rbState.Pose.Position.z) < this.offset.Z &&
                Math.Abs(pos.Orientation.eulerAngles.x - this.rbState.Pose.Orientation.eulerAngles.x) < this.offset.PITCH &&
                Math.Abs(pos.Orientation.eulerAngles.y - this.rbState.Pose.Orientation.eulerAngles.y) < this.offset.YAW &&
                Math.Abs(pos.Orientation.eulerAngles.z - this.rbState.Pose.Orientation.eulerAngles.z) < this.offset.ROLL)
            {
                return true;
            }

            return false;
        }

        private bool isRanged()
        {
            if ( Math.Abs(this.userRBstate.Position.x - this.rbState.Pose.Position.x) < this.offset.X &&
                Math.Abs(this.userRBstate.Position.y - this.rbState.Pose.Position.y) < this.offset.Y &&
                Math.Abs(this.userRBstate.Position.z - this.rbState.Pose.Position.z) < this.offset.Z  &&
                Math.Abs(this.userRBstate.Orientation.eulerAngles.x - this.rbState.Pose.Orientation.eulerAngles.x) < this.offset.PITCH &&
                Math.Abs(this.userRBstate.Orientation.eulerAngles.y - this.rbState.Pose.Orientation.eulerAngles.y) < this.offset.YAW &&
                Math.Abs(this.userRBstate.Orientation.eulerAngles.z - this.rbState.Pose.Orientation.eulerAngles.z) < this.offset.ROLL               ) 
            {
                return true;
            }
            
            return false;
            
        }

        // TODO: Disconnect Socket
        private void disconnect()
        {


        }


#if UNITY_2017_1_OR_NEWER
        void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }


        void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }


        void OnBeforeRender()
        {
            UpdatePose();
        }
#endif

        public struct OptiData
        {
            public OptiData(int frameID, OptitrackPose pos)
            {
                FrameID = frameID;
                Pos = pos;
            }

            public int FrameID { get; set; }
            public OptitrackPose Pos { get; set; }
        }

        void Update()
        {
            UpdatePose();
        }
        

        void UpdatePose()
        {
            rbState = StreamingClient.GetLatestRigidBodyState( RigidBodyId, NetworkCompensation);
            
            if (this.isTracking == true)
            {
                this.trackingData.Add(rbState.Pose);
            }

            if ( rbState != null )
            {
                this.transform.localPosition = rbState.Pose.Position;
                this.transform.localRotation = rbState.Pose.Orientation;
            }


            //Debug.Log(rbState.Pose.Position, this);
        }
    }

    [System.Serializable]
    public class Transform
    {
        public float X, Y, Z;
        public float RX, RY, RZ, RW;
        public float PITCH, YAW, ROLL;

        public static Transform CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<Transform>(jsonString);
        }
        
        public Quaternion GetQuaternion() 
        {
            return new Quaternion(this.RX, this.RY, this.RZ, this.RW);
        }

        public void SetQuaternion()
        {
            Quaternion rotation = Quaternion.Euler(this.PITCH, this.YAW, this.ROLL);
            this.RX = rotation.x;
            this.RY = rotation.y;
            this.RZ = rotation.z;
            this.RW = rotation.w;
        }
    }

    [System.Serializable]
    public class Marker
    {
        public Int32 Id;
        public bool IsActive;
        public bool Labeled;
        public string Name;
        public Vector3 Position;
        public float Size;
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
    }
}