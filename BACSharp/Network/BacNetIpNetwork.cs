﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BACSharp.Types;

namespace BACSharp.Network
{
    public class BacNetIpNetwork : IBacNetNetwork
    {
        private UdpClient _udpSendClient;
        private NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public BacNetIpNetwork(IPAddress adress, IPAddress mask,  int udpport = 47808)
        {
            byte[] addressBytes = adress.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();
            byte[] broadcastBytes = new byte[4];
            for (int i = 0; i < addressBytes.Length; i++)
            {
                broadcastBytes[i] = (byte)(addressBytes[i] | (255 - maskBytes[i]));
            }
            Address = adress;
            UdpPort = udpport;
            Broadcast = new IPAddress(broadcastBytes);

            if (BacNetDevice.Instance.Listener != null)
            {
                BacNetDevice.Instance.Remote.Clear();
                BacNetDevice.Instance.Listen = false;
                Send(new byte[1], new IPEndPoint(Broadcast, UdpPort));
                Thread.Sleep(100);
            }
            BacNetDevice.Instance.Listener = new BacNetListener(udpport);
        }

        public IPAddress Address { get; set; }
        public IPAddress Broadcast { get; set; }

        public int UdpPort { get; set; }

        public void Send(byte[] message, IPEndPoint endPoint = null)
        {
            _udpSendClient = new UdpClient() { EnableBroadcast = true };
            _udpSendClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            try
            {
                _udpSendClient.Client.Bind(new IPEndPoint(Address, UdpPort));
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return;
            }
            

            if (endPoint == null)
                _udpSendClient.Connect(Broadcast, UdpPort);
            else 
                _udpSendClient.Connect(endPoint.Address, endPoint.Port);

            _udpSendClient.Send(message, message.Length);
            _udpSendClient.Close();
        }
    }
}