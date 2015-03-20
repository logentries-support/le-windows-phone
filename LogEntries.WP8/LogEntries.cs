using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace LogEntries
{
    public static class LogEntriesService
    {
        private const int TRACE_LENGTH = 8; // Message trace default length.
        /// <summary>
        /// Wait for n seconds to upload log message again
        /// </summary>
        private const int waitIfFailedInterval = 15;
        /// <summary>
        /// True if log message is uploading now, false otherwise
        /// </summary>
        private static bool uploadInProgress;
        /// <summary>
        /// Class contains user token, connection type and port number
        /// </summary>
        private static ConnectionParameters connectionParameters;
        /// <summary>
        /// Config file name
        /// </summary>
        private const string configFilename = "config.xml";
        /// <summary>
        /// Logs file name
        /// </summary>
        private const string logsFilename = "logs.xml";
        /// <summary>
        /// object for synchronization threads
        /// </summary>
        private static object sync = new object();

        private static string msgTrace = GenerateMsgTraceString(); // Unique trace message for the current lib. instance.

        private static List<Message> messagesQueue;
        /// <summary>
        /// Collection of log messages waiting for upload
        /// </summary>
        private static List<Message> MessagesQueue
        {
            get
            {
                lock (sync)
                {
                    if (messagesQueue == null)
                    {
                        messagesQueue = Storage.LoadFromFile<List<Message>>(logsFilename);

                        if (messagesQueue == null)
                        {
                            messagesQueue = new List<Message>();
                        }
                    }
                }

                return messagesQueue;
            }
        }

        private static DispatcherTimer syncTimer;
        /// <summary>
        /// Timer used for waiting some time after current log message failed to upload
        /// </summary>
        private static DispatcherTimer SyncTimer
        {
            get
            {
                if (syncTimer == null)
                {
                    syncTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(waitIfFailedInterval) };
                    syncTimer.Tick += (s, e) =>
                    {
                        ThreadPool.QueueUserWorkItem((work) =>
                        {
                            // begin upload log messages waiting in queue
                            UploadLogMessagesQueue();
                        });
                    };
                }

                return syncTimer;
            }
        }

        /// <summary>
        /// Begin waiting before next upload attempt
        /// </summary>
        private static void StartSync()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (!SyncTimer.IsEnabled)
                {
                    ThreadPool.QueueUserWorkItem((work) =>
                    {
                        // begin upload log messages waiting in queue
                        UploadLogMessagesQueue();
                    });

                    SyncTimer.Start();
                }
            });
        }

        /// <summary>
        /// Stop waiting before next upload attempt
        /// </summary>
        private static void StopSync()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (SyncTimer.IsEnabled)
                {
                    SyncTimer.Stop();
                }
            });
        }

        /// <summary>
        /// Last upload failed timestamp
        /// </summary>
        private static DateTime LastUploadFailed = new DateTime(0);

        /// <summary>
        /// Saves log messages queue to isolated storage
        /// </summary>
        private static void SaveQueue()
        {
            Storage.SaveToFile(logsFilename, MessagesQueue);
        }

        /// <summary>
        /// Adds log message to upload queue
        /// </summary>
        /// <param name="message">log message</param>
        private static void QueueLogMessage(Message message)
        {
            if (connectionParameters == null)
            {
                throw new Exception("call Initialize or Activate before use this method");
            }

            // go to background
            ThreadPool.QueueUserWorkItem((work) =>
            {
                lock (sync)
                {
                    MessagesQueue.Add(message);

                    SaveQueue();
                }

                // last upload failed so we need to wait time specified in `waitIfFailedInterval` const
                if (syncTimer != null && syncTimer.IsEnabled) return;

                // begin upload log messages waiting in queue
                UploadLogMessagesQueue();
            });
        }

        /// <summary>
        /// Begins to upload messages one by one
        /// </summary>
        private static void UploadLogMessagesQueue()
        {
            lock (sync)
            {
                // perform upload only in one thread
                if (uploadInProgress) return;

                uploadInProgress = true;
            }

            int messagesCount;
            Message message = null;

            lock (sync)
            {
                messagesCount = MessagesQueue.Count;

                if (messagesCount > 0)
                {
                    // get first message in queue to upload
                    message = MessagesQueue[0];
                }
            }

            if (messagesCount > 0)
            {
                // preserve server spaming when no internet available
                if (DateTime.Now >= LastUploadFailed.AddSeconds(waitIfFailedInterval))
                {
                    // stop timer. turn it on only if upload will fail
                    StopSync();

                    // uploads log message while queue not empty
                    UploadLogMessage(message);
                }
                else
                {
                    uploadInProgress = false;
                }
            }
            else
            {
                uploadInProgress = false;
            }
        }

        /// <summary>
        /// Uploads log message
        /// </summary>
        private static void UploadLogMessage(Message message)
        {
            Server.UploadLogMessage(message, () =>
            {
                lock (sync)
                {
                    // success upload. remove it from queue
                    MessagesQueue.Remove(message);

                    SaveQueue();
                }

                int messagesCount;

                lock (sync)
                {
                    messagesCount = MessagesQueue.Count;
                }

                // keep going upload messages one by one
                if (messagesCount > 0)
                {
                    uploadInProgress = false;

                    UploadLogMessagesQueue();
                }
                else
                {
                    uploadInProgress = false;

                    Server.Disconnect();
                }
            }, () =>
            {
                UploadMessageFailed();
            });
        }

        /// <summary>
        /// Call when message upload fails
        /// </summary>
        private static void UploadMessageFailed()
        {
            // failed to upload. start timer to upload it again later

            LastUploadFailed = DateTime.Now;

            uploadInProgress = false;

            StartSync();
        }

        private static string GenerateMsgTraceString()
        {
            const string ALPHANUM_SRC = "A0B0CD1E1F2G2H3I3J4K4L5MN56O6P7Q7R8S8T9U9VWXYZ";
            Random rnd = new Random((int) DateTime.Now.Ticks & 0x0000FFFF);
            return new string(Enumerable.Repeat(ALPHANUM_SRC, TRACE_LENGTH).
                Select(sel => sel[rnd.Next(ALPHANUM_SRC.Length)]).ToArray());
        }

        public static string GetMsgTrace()
        {
            return msgTrace;
        }

        /// <summary>
        /// Perform logentries initialization. call this method in Application_Launching event
        /// </summary>
        /// <param name="userToken">User token</param>
        public static void Initialize(Application application, string userToken)
        {
            Initialize(application, userToken, true);
        }

        /// <summary>
        /// Perform logentries initialization. call this method in Application_Launching event
        /// </summary>
        /// <param name="userToken">User token</param>
        /// <param name="useSSL">True if use SSL connection. default value is true</param>
        public static void Initialize(Application application, string userToken, bool useSSL)
        {
            if (useSSL)
            {
                Initialize(application, userToken, true, 443);
            }
            else
            {
                Initialize(application, userToken, false, 80);
            }
        }

        /// <summary>
        /// Perform logentries initialization. call this method in Application_Launching event
        /// </summary>
        /// <param name="userToken">User token</param>
        /// <param name="useSSL">True if use SSL connection. default value is true</param>
        /// <param name="port">Port number to connect to. default port number is 443 for SSL and 80 otherwise</param>
        public static void Initialize(Application application, string userToken, bool useSSL, int port)
        {
            if (String.IsNullOrWhiteSpace(userToken))
            {
                throw new ArgumentException("invalid user token");
            }

            if (port < 0 || port > 65536)
            {
                throw new ArgumentException("parameter port is not a valid port number");
            }

            application.UnhandledException += application_UnhandledException;

            connectionParameters = new ConnectionParameters()
            {
                userToken = userToken,
                useSSL = useSSL,
                port = port
            };

            Server.portNumber = port;
            Server.userToken = userToken;
            Server.useSSL = useSSL;

            Storage.SaveToFile(configFilename, connectionParameters);

            Sync();
        }

        static void application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Crash(e.ExceptionObject);
        }

        /// <summary>
        /// Perform logentries restoring. call this method in Application_Activated event
        /// </summary>
        public static void Activate(Application application)
        {
            application.UnhandledException -= application_UnhandledException;
            application.UnhandledException += application_UnhandledException;

            connectionParameters = Storage.LoadFromFile<ConnectionParameters>(configFilename);

            if (connectionParameters != null)
            {
                Server.portNumber = connectionParameters.port;
                Server.userToken = connectionParameters.userToken;
                Server.useSSL = connectionParameters.useSSL;

                Sync();
            }
        }

        /// <summary>
        /// Start uploading log messages it queue is not empty
        /// </summary>
        private static void Sync()
        {
            int messagesCount;

            lock (sync)
            {
                messagesCount = MessagesQueue.Count;
            }

            if (messagesCount > 0 && !SyncTimer.IsEnabled)
            {
                StartSync();
            }
        }

        /// <summary>
        /// Send log message
        /// </summary>
        /// <param name="message">Log entry</param>
        public static void Log(string message)
        {
            QueueLogMessage(new Message(message, Severity.Log));
        }

        /// <summary>
        /// Send log message
        /// </summary>
        /// <param name="messages">Log entries</param>
        public static void Log(Dictionary<object, object> messages)
        {
            QueueLogMessage(new Message(messages, Severity.Log));
        }

        /// <summary>
        /// Send emergency message
        /// </summary>
        /// <param name="message">Emergency entry</param>
        public static void Emergency(string message)
        {
            QueueLogMessage(new Message(message, Severity.Emergency));
        }

        /// <summary>
        /// Send emergency messages
        /// </summary>
        /// <param name="messages">Emergency entries</param>
        public static void Emergency(Dictionary<object, object> messages)
        {
            QueueLogMessage(new Message(messages, Severity.Emergency));
        }

        /// <summary>
        /// Send alert message
        /// </summary>
        /// <param name="message">Alert entry</param>
        public static void Alert(string message)
        {
            QueueLogMessage(new Message(message, Severity.Alert));
        }

        /// <summary>
        /// Send alert messages
        /// </summary>
        /// <param name="messages">Alert entries</param>
        public static void Alert(Dictionary<object, object> messages)
        {
            QueueLogMessage(new Message(messages, Severity.Alert));
        }

        /// <summary>
        /// Send critical message
        /// </summary>
        /// <param name="message">Critical entry</param>
        public static void Critical(string message)
        {
            QueueLogMessage(new Message(message, Severity.Critical));
        }

        /// <summary>
        /// Send critical messages
        /// </summary>
        /// <param name="messages">Critical entries</param>
        public static void Critical(Dictionary<object, object> messages)
        {
            QueueLogMessage(new Message(messages, Severity.Critical));
        }

        /// <summary>
        /// Send error message
        /// </summary>
        /// <param name="message">Error entry</param>
        public static void Error(string message)
        {
            QueueLogMessage(new Message(message, Severity.Error));
        }

        /// <summary>
        /// Send error messages
        /// </summary>
        /// <param name="messages">Error entries</param>
        public static void Error(Dictionary<object, object> messages)
        {
            QueueLogMessage(new Message(messages, Severity.Error));
        }

        /// <summary>
        /// Send warning message
        /// </summary>
        /// <param name="message">Warning entry</param>
        public static void Warning(string message)
        {
            QueueLogMessage(new Message(message, Severity.Warning));
        }

        /// <summary>
        /// Send warning messages
        /// </summary>
        /// <param name="message">Warning entries</param>
        public static void Warning(Dictionary<object, object> messages)
        {
            QueueLogMessage(new Message(messages, Severity.Warning));
        }

        /// <summary>
        /// Send notice message
        /// </summary>
        /// <param name="message">Notice entry</param>
        public static void Notice(string message)
        {
            QueueLogMessage(new Message(message, Severity.Notice));
        }

        /// <summary>
        /// Send notice messages
        /// </summary>
        /// <param name="message">Notice entries</param>
        public static void Notice(Dictionary<object, object> messages)
        {
            QueueLogMessage(new Message(messages, Severity.Notice));
        }

        /// <summary>
        /// Send info message
        /// </summary>
        /// <param name="message">Info entry</param>
        public static void Info(string message)
        {
            QueueLogMessage(new Message(message, Severity.Info));
        }

        /// <summary>
        /// Send info messages
        /// </summary>
        /// <param name="message">Info entries</param>
        public static void Info(Dictionary<object, object> messages)
        {
            QueueLogMessage(new Message(messages, Severity.Info));
        }

        /// <summary>
        /// Send debug message
        /// </summary>
        /// <param name="message">Debug entry</param>
        public static void Debug(string message)
        {
            QueueLogMessage(new Message(message, Severity.Debug));
        }

        /// <summary>
        /// Send debug messages
        /// </summary>
        /// <param name="message">Debug entries</param>
        public static void Debug(Dictionary<object, object> messages)
        {
            QueueLogMessage(new Message(messages, Severity.Debug));
        }

        /// <summary>
        /// Send crash details
        /// </summary>
        /// <param name="message">Exception entry</param>
        private static void Crash(Exception exception)
        {
            Message message = new Message(exception.Message + " . Stacktrace: " + exception.StackTrace.Replace('\n', ' '), Severity.Crash);

            lock (sync)
            {
                MessagesQueue.Add(message);

                SaveQueue();
            }
        }
    }
}
