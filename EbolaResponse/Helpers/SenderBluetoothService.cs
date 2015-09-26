// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SenderBluetoothService.cs" company="saramgsilva">
//   Copyright (c) 2014 saramgsilva. All rights reserved.
// </copyright>
// <summary>
//   The Sender bluetooth service.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EbolaResponse.Models;
using InTheHand.Net;
using InTheHand.Net.Sockets;

namespace EbolaResponse.Helpers
{
    /// <summary>
    /// The Sender bluetooth service.
    /// </summary>
    public sealed class SenderBluetoothService //: ISenderBluetoothService
    {
         private readonly Guid _serviceClassId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SenderBluetoothService"/> class. 
        /// </summary>
        public SenderBluetoothService()
        {
            // this guid is random, only need to match in Sender & Receiver
            // this is like a "key" for the connection!
            _serviceClassId = new Guid(Constants.ServiceClassId);
        }

        /// <summary>
        /// Gets the devices.
        /// </summary>
        /// <returns>The list of the devices.</returns>
        public IList<Device> GetDevices()
        {
            // for not block the UI it will run in a different threat
            var task = Task.Factory.StartNew(() =>
            {
                var devices = new List<Device>();
                using (var bluetoothClient = new BluetoothClient())
                {
                    var array = bluetoothClient.DiscoverDevices();
                    for (var i = 0; i < array.Length; i++)
                    {
                        devices.Add(new Device() { Name = array[i].DeviceName, DeviceAddressSap = array[i].DeviceAddress.Sap });
                    }
                }
                return devices;
            });
            return task.Result;
        }

        /// <summary>
        /// Sends the data to the Receiver.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="content">The content.</param>
        /// <returns>If was sent or not.</returns>
        public bool Send(BluetoothAddress deviceAddress, string content)
        {
            if (deviceAddress == null)
            {
                throw new ArgumentNullException("deviceAddress");
            }

            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException("content");
            }

            // for not block the UI it will run in a different threat
            var task = Task.Factory.StartNew(() =>
            {
                using (var bluetoothClient = new BluetoothClient())
                {
                    try
                    {
                        var ep = new BluetoothEndPoint(deviceAddress, _serviceClassId);
                       
                        // connecting
                        bluetoothClient.Connect(ep);

                        // get stream for send the data
                        var bluetoothStream = bluetoothClient.GetStream();

                        // if all is ok to send
                        if (bluetoothClient.Connected && bluetoothStream != null)
                        {
                            // write the data in the stream
                            var buffer = System.Text.Encoding.UTF8.GetBytes(content);
                            bluetoothStream.Write(buffer, 0, buffer.Length);
                            bluetoothStream.Flush();
                            bluetoothStream.Close();
                            return true;
                        }
                        return false;
                    }
                    catch
                    {
                        // the error will be ignored and the send data will report as not sent
                        // for understood the type of the error, handle the exception
                    }
                }
                return false;
            });
            return task.Result;
        }
    }
}
