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

        private BTSocket socket;                    // Socket - TCP/IP or UDP (TODO: UDP is not tested. need to review)
    }
}