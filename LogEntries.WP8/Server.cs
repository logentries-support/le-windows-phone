using SocketEx;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LogEntries
{
    internal class Server
    {
        // Logentries API server address
        private const string hostName = "data.logentries.com";
        // Logentries secure API server address
        private const string hostSecureName = "api.logentries.com";
        // Port number to connect to
        public static int portNumber { get; set; }
        // User token
        public static string userToken { get; set; }
        // True if use secure connection, false - otherwise
        public static bool useSSL { get; set; }
        // Socket object that will be used to send data
        private static Socket socket = null;
        // Secure Socket object that will be used to send data
        private static SecureTcpClient secureSocket = null;
        
        static Server()
        {
            useSSL = true;
        }

        /// <summary>
        /// Return socket status
        /// </summary>
        public static bool IsConnected
        {
            get
            {
                return socket != null;
            }
        }

        /// <summary>
        /// Attempt a TCP socket connection to logentries
        /// </summary>
        /// <param name="callback">Success action</param>
        /// <param name="error">Failure action</param>
        public static void Connect(Action callback, Action error)
        {
            if (useSSL)
            {
                try
                {
                    secureSocket = new SecureTcpClient(hostSecureName, portNumber);

                    callback();
                }
                catch
                {
                    error();
                }
            }
            else
            {
                try
                {
                    // Create DnsEndPoint. The hostName and port are passed in to this method.
                    DnsEndPoint hostEntry = new DnsEndPoint(hostName, portNumber);

                    // Create a stream-based, TCP socket using the InterNetwork Address Family. 
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Create a SocketAsyncEventArgs object to be used in the connection request
                    SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                    socketEventArg.RemoteEndPoint = hostEntry;

                    socketEventArg.Completed += (s, e) =>
                    {
                        // Retrieve the result of this request
                        if (e.SocketError == SocketError.Success)
                        {
                            callback();
                        }
                        else
                        {
                            error();
                        }
                    };

                    // Make an asynchronous Connect request over the socket
                    socket.ConnectAsync(socketEventArg);
                }
                catch
                {
                    error();
                }
            }
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        public static void Disconnect()
        {
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }

            if (secureSocket != null)
            {
                secureSocket.Dispose();
                secureSocket = null;
            }
        }

        /// <summary>
        /// Send the given data to the server using the established connection
        /// </summary>
        /// <param name="data">The data to send to the server</param>
        /// <param name="callback">Success action</param>
        /// <param name="error">Failure action</param>
        public static void Send(string data, Action callback, Action error)
        {
            if (useSSL)
            {
                try
                {
                    Stream stream = secureSocket.GetStream();

                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        Debug.WriteLine(data);

                        writer.WriteLine(data);
                        writer.Flush();

                        writer.Close();
                    }

                    callback();
                }
                catch
                {
                    error();
                }
            }
            else
            {                
                try
                {
                    // Create SocketAsyncEventArgs context object
                    SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

                    // Set properties on context object
                    socketEventArg.RemoteEndPoint = socket.RemoteEndPoint;
                    socketEventArg.UserToken = null;

                    socketEventArg.Completed += (s, e) =>
                    {
                        if (e.SocketError == SocketError.Success)
                        {
                            Debug.WriteLine(data);

                            callback();
                        }
                        else
                        {
                            error();
                        }
                    };

                    // Add the data to be sent into the buffer
                    byte[] payload = Encoding.UTF8.GetBytes(data);
                    socketEventArg.SetBuffer(payload, 0, payload.Length);

                    // Make an asynchronous Send request over the socket
                    socket.SendAsync(socketEventArg);
                }
                catch
                {
                    error();
                }
            }
        }

        public static void UploadLogMessage(Message message, Action callback, Action error)
        {
            string data = userToken + " " + message.ToString() + "\n";

            if (IsConnected)
            {
                Send(data, callback, error);
            }
            else
            {
                Connect(() =>
                {
                    Send(data, callback, error);
                }, () =>
                {
                    error();
                });
            }

            // Mock server:

            //Random random = new Random();

            //Deployment.Current.Dispatcher.BeginInvoke(() =>
            //{
            //    DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(random.NextDouble() * 2 + 1) };
            //    timer.Tick += (s, e) =>
            //    {
            //        timer.Stop();

            //        if (random.Next(0, 5) != 0)
            //        {
            //            ThreadPool.QueueUserWorkItem((wokr) =>
            //            {
            //                System.Diagnostics.Debug.WriteLine(message.ToString());
            //                callback();
            //            });
            //        }
            //        else
            //        {
            //            ThreadPool.QueueUserWorkItem((wokr) =>
            //            {
            //                error();
            //            });
            //        }
            //    };
            //    timer.Start();
            //});
        }
    }
}
