/* 
Copyright © 2023 Boystown Research Hospital

Modified By: Won Jang (won DOT jang AT boystown DOT org)
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Agent
{
    class UDP : BTSocket
    {
        public event ServerHandlePacketData OnDataReceived;
        private int port = 8888;
        bool done;

        UdpClient listener;
        IPEndPoint groupEP;
        /// <summary>
        /// Constructs a new TCP server which will listen on a given port
        /// </summary>
        /// <param name="port"></param>
        public UDP(int port)
        {
            this.port = port;
        }

        public UDP()
        {
        }

        /// <summary>
        /// Begins listening on the port provided to the constructor
        /// </summary>
        public void Start()
        {
            listener = new UdpClient(this.port);
            groupEP = new IPEndPoint(IPAddress.Any, port);
            Console.WriteLine("Started UDP on port" + this.port);

            Thread thread = new Thread(new ThreadStart(ListenForClients));
            thread.Start();
            //started = true;
            done = false;
            //listener = new UdpClient(port);
            //IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, port);

            //try
            //{
            //    while (!done)
            //    {
            //        Console.WriteLine("Waiting for broadcast");
            //        byte[] bytes = listener.Receive(ref groupEP);

            //        Console.WriteLine("Received broadcast from {0} :\n {1}\n",
            //            groupEP.ToString(),
            //            Encoding.ASCII.GetString(bytes, 0, bytes.Length));
            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.ToString());
            //}
            //finally
            //{
            //    listener.Close();
            //}
        }
        
        private void ListenForClients()
        {
            while (!done)
            { 
                Console.WriteLine("Waiting for broadcast");
                try
                {
                    byte[] bytes = listener.Receive(ref groupEP);

                    if (OnDataReceived != null)
                    {
                        Console.WriteLine("Received a new message");
                        //Send off the data for other classes to handle
                        OnDataReceived(bytes, bytes.Length);
                        //OnDataReceived(clientBuffers[tcpClient].ReadBuffer, bytesRead, tcpClient);
                    }
                }
                catch { Console.WriteLine("listener stopped"); }
                

                //Console.WriteLine("Received broadcast from {0} :\n {1}\n",
                //        groupEP.ToString(),
                //        Encoding.ASCII.GetString(bytes, 0, bytes.Length));
            }
        }

        /// <summary>
        /// Stops the server from accepting new clients
        /// </summary>
        public void Stop()
        {
            done = true;
            listener.Close();
        }

        public void SendImmediateToAll(byte[] data)
        {

        }
    }
}
