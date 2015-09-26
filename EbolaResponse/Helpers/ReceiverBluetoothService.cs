// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReceiverBluetoothService.cs" company="saramgsilva">
//   Copyright (c) 2014 saramgsilva. All rights reserved.
// </copyright>
// <summary>
//   The Receiver bluetooth service.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Ionic.Zip;

namespace LastMileHealth.Helpers
{

    public class ListenerStoppedStatus : EventArgs
    {
        public int StatusCode { get; set; }
    }



    /// <summary>
    /// The Receiver bluetooth service.
    /// </summary>
    public class ReceiverBluetoothService : ObservableObject, IDisposable//, IReceiverBluetoothService
    {
        private readonly Guid _serviceClassId;
        private Action<ZipFile> _responseAction;
        private BluetoothListener _listener;
        private CancellationTokenSource _cancelListnerSource;
        private bool _wasStarted;
        public string lastClientName;


        public event EventHandler<ListenerStoppedStatus> ListnerStopped;


        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiverBluetoothService" /> class.
        /// </summary>
        public ReceiverBluetoothService()
        {
            _serviceClassId = new Guid(Constants.ServiceClassId);
        }

        /// <summary>
        /// Gets or sets a value indicating whether was started.
        /// </summary>
        /// <value>
        /// The was started.
        /// </value>
        public bool WasStarted
        {
            get { return _wasStarted; }
            set { Set(() => WasStarted, ref _wasStarted, value); }
        }

        /// <summary>
        /// Starts the listening from Senders.
        /// </summary>
        /// <param name="reportAction">
        /// The report Action.
        /// </param>
        public void Start(Action<ZipFile> reportAction)
        {
            _responseAction = reportAction;

            if (this.WasStarted)
            {
                EventHandler<ListenerStoppedStatus> stopEventHandler = null;

                stopEventHandler = delegate
                {
                    this.ListnerStopped -= stopEventHandler;
                    this.StartListener();
                };

                this.ListnerStopped += stopEventHandler;

                this.Stop();
            }
            else
            {
                this.StartListener();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void StartListener()
        {
            _listener = new BluetoothListener(_serviceClassId)
            {
                ServiceName = "MyService"
            };

            _listener.Start();

            _cancelListnerSource = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                var status = this.Listener(_cancelListnerSource);

                this.Stop();
                this.OnListnerStopped(status);

            });

            WasStarted = true;
        }


        /// <summary>
        /// Stops the listening from Senders.
        /// </summary>
        public void Stop()
        {
            if (WasStarted)
            {
                WasStarted = false;
                _listener.Stop();
                _cancelListnerSource.Cancel();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnListnerStopped(int status)
        {
            var handler = this.ListnerStopped;
            if (handler != null)
            {
                handler.Invoke(this, new ListenerStoppedStatus() { StatusCode = status });
            }
        }


        /// <summary>
        /// Listeners the accept bluetooth client.
        /// </summary>
        /// <param name="token">
        /// The token.
        /// </param>
        private int Listener(CancellationTokenSource token)
        {
            Logger.LogEvent(true, "ReceiverBluetoothService.Listener Start");

            int status = 0;
            lastClientName = null;
            try
            {
                while (true)
                {
                    using (var client = _listener.AcceptBluetoothClient())
                    {
                        Logger.LogEvent(true, "ReceiverBluetoothService.Listener 100");

                        if (token.IsCancellationRequested)
                        {
                            Logger.LogEvent(true, "ReceiverBluetoothService.Listener 101");

                            _cancelListnerSource.Dispose();
                            _cancelListnerSource = null;

                            status = 0;

                            break;
                        }

                        Logger.LogEvent(true, "ReceiverBluetoothService.Listener 102");

                        var clientStream = client.GetStream();

                        Logger.LogEvent(true, "ReceiverBluetoothService.Listener 103");

                        if (clientStream.CanRead)
                        {
                            Logger.LogEvent(true, "ReceiverBluetoothService.Listener 104");

                            lastClientName = client.RemoteMachineName;
                            byte[] myReadBuffer = new byte[1024]; 
                            int numberOfBytesRead = 0;

                            Logger.LogEvent(true, "ReceiverBluetoothService.Listener 105");

                            bool firstRead = true;
                            byte[] fileBytes = null;
                            // Incoming message may be larger than the buffer size. 
                            int readedFileBytes = 0;

                            Logger.LogEvent(true, "ReceiverBluetoothService.Listener 106");

                            do
                            {
                                numberOfBytesRead = clientStream.Read(myReadBuffer, 0, myReadBuffer.Length);

                                Logger.LogEvent(true, "ReceiverBluetoothService.Listener 107 numberOfBytesRead: {0}, \n myReadBuffer.Length: {1}", numberOfBytesRead, myReadBuffer.Length);

                                if (firstRead)
                                {
                                    firstRead = false;
                                    byte[] sizeBytes = new byte[4];
                                    Buffer.BlockCopy(myReadBuffer, 0, sizeBytes, 0, 4);
                                    var fileLength = Utils.ByteArrayToInt(sizeBytes);
                                    fileBytes = new byte[fileLength];
                                    int bytesToCopy = numberOfBytesRead - 4;
                                    Buffer.BlockCopy(myReadBuffer, 4, fileBytes, 0, bytesToCopy);
                                    readedFileBytes += bytesToCopy;

                                    Logger.LogEvent(true, "ReceiverBluetoothService.Listener 108 numberOfBytesRead: {0}, \n myReadBuffer.Length: {1}, \n fileBytes.Length: {2}, \n readedFileBytes: {3}",
                                        numberOfBytesRead,
                                        myReadBuffer.Length,
                                        fileBytes.Length,
                                        readedFileBytes);
                                }
                                else
                                {
                                    Logger.LogEvent(true, "ReceiverBluetoothService.Listener 109 numberOfBytesRead: {0}, \n myReadBuffer.Length: {1}, \n fileBytes.Length: {2}, \n readedFileBytes: {3}", 
                                        numberOfBytesRead, 
                                        myReadBuffer.Length,
                                        fileBytes.Length,
                                        readedFileBytes);

                                    //if (!clientStream.DataAvailable && fileBytes.Length != numberOfBytesRead + readedFileBytes)
                                    //{
                                    //    Logger.LogEvent(true, "Error!. File size specified and actual file size does not match! \n fileBytes.Length: {0}, \n numberOfBytesRead: {1}, \n readedFileBytes: {2}",
                                    //        fileBytes.Length,
                                    //        numberOfBytesRead,
                                    //        readedFileBytes);
                                    //    throw new Exception("File size specified and actual file size does not match!");
                                    //}

                                    Buffer.BlockCopy(myReadBuffer, 0, fileBytes, readedFileBytes, numberOfBytesRead);
                                    readedFileBytes += numberOfBytesRead;
                                }

                                Logger.LogEvent(true, "ReceiverBluetoothService.Listener 110 numberOfBytesRead: {0}, \n myReadBuffer.Length: {1}, \n fileBytes.Length: {2}, \n readedFileBytes: {3}",
                                    numberOfBytesRead,
                                    myReadBuffer.Length,
                                    fileBytes.Length,
                                    readedFileBytes);

                                //try to wait for some time, bevause conection is not stable!!!
                                if (readedFileBytes < fileBytes.Length && !clientStream.DataAvailable)
                                {
                                    Logger.LogEvent(true, "ReceiverBluetoothService.Listener 111");

                                    for (int i = 0; i < 600; i++)
                                    {
                                        //await Task.Delay(50);
                                        //await Utils.Delay(50);
                                        //await TaskEx.Delay(50);
                                        //Utils.Wait(50);

                                        Thread.Sleep(100);

                                        Logger.LogEvent(true, "ReceiverBluetoothService.Listener 111.5");

                                        if (clientStream.DataAvailable)
                                        {
                                            Logger.LogEvent(true, "ReceiverBluetoothService.Listener 112");

                                            break;
                                        }
                                    }
                                }

                                Logger.LogEvent(true, "ReceiverBluetoothService.Listener 113.5 DataAvailable: {0}\n, CanRead: {1}\n", clientStream.DataAvailable, clientStream.CanRead);
                            }
                            while (clientStream.DataAvailable);

                            Logger.LogEvent(true, "ReceiverBluetoothService.Listener 113");

                            string filePath = Path.Combine(Constants.ODKPath, "RowData");
                            if (!File.Exists(filePath))
                            {
                                var stream = File.Create(filePath);
                                stream.Dispose();
                            }

                            File.WriteAllBytes(filePath, fileBytes);
                            
                            var zip = ZipFile.Read(new MemoryStream(fileBytes));

                            Logger.LogEvent(true, "ReceiverBluetoothService.Listener 114");

                            if (!zip.ContainsEntry("forms.txt"))
                            {
                                Logger.LogEvent(true, "ReceiverBluetoothService.Listener 115");

                                clientStream.Write(Utils.IntToByteArray(readedFileBytes), 0, 4);
                                clientStream.Flush();

                                Logger.LogEvent(true, "ReceiverBluetoothService.Listener 116");
                            }
                            
                            _responseAction(zip);

                            Logger.LogEvent(true, "ReceiverBluetoothService.Listener 117");
                        }
                        else
                        {
                            Debug.WriteLine("Sorry. You cannot read from this NetworkStream.");
                            Logger.LogEvent(true, "ReceiverBluetoothService.Listener 118");
                        }
                        //}
                        //catch (IOException ioEx)
                        //{
                        //    client.Close();
                        //    break;
                        //}
                        //catch (Exception ex)
                        //{

                        // MessageBox.Show(ex.Message + "\n\nStackTrace: \n" + ex.StackTrace);
                        //client.Close();
                        //break;
                        //}
                        //finally
                        //{
                        //if (clientStream != null)
                        //{
                        //    clientStream.Flush();
                        //    clientStream.Close();
                        //}
                        //client.Close();
                        //}

                        //using (var streamReader = new StreamReader(client.GetStream()))
                        //{
                        //    try
                        //    {
                        //        var content = streamReader.ReadToEnd();
                        //        if (!string.IsNullOrEmpty(content))
                        //        {
                        //            _responseAction(content);
                        //        }
                        //    }
                        //    catch (IOException)
                        //    {
                        //        client.Close();
                        //        break;
                        //    }
                        //}
                    }
                }
            }
            catch (SocketException e)
            {
                status = e.ErrorCode;
                Logger.LogEvent(false, "AppException Error= {0} , \nStackTrace= {1}", e.Message, e.StackTrace);
            }
            catch (Exception e)
            {
                status = 200;
                Logger.LogEvent(false, "AppException Error= {0} , \nStackTrace= {1}", e.Message, e.StackTrace);

                //MessageBox.Show(exception.Message);
                // todo handle the exception
                // for the sample it will be ignored
            }

            Logger.LogEvent(false, "ReceiverBluetoothService.Listener Finish");

            return status;
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="disposing">
        /// The disposing.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cancelListnerSource != null)
                {
                    _listener.Stop();
                    _listener = null;
                    _cancelListnerSource.Cancel();
                }
            }
        }
    }
}
