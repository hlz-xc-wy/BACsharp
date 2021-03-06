﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BACSharp.NPDU;
using BACSharp.Services.Acknowledgement;
using BACSharp.Services.Confirmed;
using BACSharp.Services.Unconfirmed;
using BACSharp.Types;

namespace BACSharp
{
    public class BacNetResponse
    {
        private NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public BacNetResponse()
        {    
        }

        #region Unconfirmed

        public void ReceivedIAm(BacNetRawMessage msg, IPEndPoint endPoint)
        {
            BacNetRemoteDevice newDevice = new BacNetRemoteDevice();
            newDevice.EndPoint = endPoint;

            BacNetIpNpdu npdu;
            IAm apdu;
            try
            {
                npdu = new BacNetIpNpdu(msg.Npdu);
                apdu = new IAm(msg.Apdu);
            }
            catch (Exception ex)
            {
                _logger.WarnException("Received malformed I-am", ex);
                return;
            }
            
            if (npdu.Source != null)
                newDevice.BacAddress = npdu.Source;
            newDevice.MaxApduLength = apdu.MaxApduLength;
            newDevice.InstanceNumber = apdu.deviceObject.ObjectId;            
            newDevice.Segmentation = apdu.SegmentationSupported;
            newDevice.VendorId = apdu.VendorId;

            if (newDevice.InstanceNumber == BacNetDevice.Instance.DeviceId)
                return;

            BacNetRemoteDevice rem =
                BacNetDevice.Instance.Remote.FirstOrDefault(s => s.InstanceNumber == newDevice.InstanceNumber);
            if (rem != null)
                BacNetDevice.Instance.Remote.Remove(rem);

            BacNetDevice.Instance.Remote.Add(newDevice);
        }

        public void ReceivedIHave(BacNetRawMessage msg)
        {
            //todo: implement method
        }

        public void ReceivedCovNotification(BacNetRawMessage msg)
        {
            UnconfirmedCOVnotification apdu;
            try
            {
                apdu = new UnconfirmedCOVnotification(msg.Apdu);
            }
            catch { throw; }

            if (BacNetDevice.Instance.Waiter is int && Convert.ToInt32(BacNetDevice.Instance.Waiter) == apdu.InvokeId)
            {
                if (apdu.Object == null)
                    _logger.Warn("Received empty object");
                BacNetDevice.Instance.Waiter = apdu;
            }
        }

        public void ReceivedEventNotification(BacNetRawMessage msg)
        {
            UnconfirmedEventNotification apdu;
            try
            {
                apdu = new UnconfirmedEventNotification(msg.Apdu);
            }
            catch { return; }
            BacNetDevice.Instance.OnNotificationEvent(apdu);
        }

        public void ReceivedPrivateTransfer(BacNetRawMessage msg)
        {
            //todo: implement method
        }

        public void ReceivedTextMessage(BacNetRawMessage msg)
        {
            //todo: implement method
        }

        public void ReceivedTimeSynchronization(BacNetRawMessage msg)
        {
            //todo: implement method
        }

        public void ReceivedWhoHas(BacNetRawMessage msg)
        {
            //todo: implement method
        }

        public void ReceivedWhoIs(BacNetRawMessage msg)
        {
            WhoIs apdu = new WhoIs(msg.Apdu);
            uint devId = BacNetDevice.Instance.DeviceId;
            if ((apdu.LowLimit != null && apdu.HighLimit != null && apdu.LowLimit.Value < devId && apdu.HighLimit.Value > devId) || (apdu.LowLimit == null || apdu.HighLimit == null))
                BacNetDevice.Instance.Services.Unconfirmed.IAm();
        }

        public void ReceivedUtcTimeSynchronization(BacNetRawMessage msg)
        {
            //todo: implement method
        }

        #endregion

        #region Confirmed

        public void ReceivedReadProperty(BacNetRawMessage msg)
        {
            try
            {
                ReadProperty apdu = new ReadProperty(msg.Apdu);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Reject.Missing_required_paramter")
                {
                    //Отправляем сообщение об ошибке
                }
                throw;
            }            
        }

        #endregion

        #region Acknowledgement

        public void ReceivedReadPropertyAck(BacNetRawMessage msg)
        {
            ReadPropertyAck apdu;
            try
            {
                apdu = new ReadPropertyAck(msg.Apdu);
            }
            catch(Exception ex)
            {
                _logger.WarnException("Malformed ReadPropertyAck: ", ex);
                BacNetDevice.Instance.Waiter = new ArrayList();
                return;
            }
            if (BacNetDevice.Instance.Waiter is int && Convert.ToInt32(BacNetDevice.Instance.Waiter) == apdu.InvokeId)
                BacNetDevice.Instance.Waiter = apdu.Obj.Properties[0].Values;
        }

        public void ReceivedReadPropertyMultipleAck(BacNetRawMessage msg)
        {
            ReadPropertyMultipleAck apdu;
            try
            {
                apdu = new ReadPropertyMultipleAck(msg.Apdu);
            }
            catch (Exception ex)
            {
                _logger.WarnException("Malformed ReadPropertyMultipleAck: ", ex);
                BacNetDevice.Instance.Waiter = new List<BacNetObject>();
                return;
            }
            if (BacNetDevice.Instance.Waiter is int && Convert.ToInt32(BacNetDevice.Instance.Waiter) == apdu.InvokeId)
            {
                if (apdu.ObjectList == null)
                    _logger.Warn("Received empty object list");
                BacNetDevice.Instance.Waiter = apdu.ObjectList;
            }

            //RpmE
            BacNetDevice.Instance.Services.Confirmed.RpmCallBack(apdu.InvokeId, apdu.ObjectList);
        }

        public void ReceivedErrorAck(BacNetRawMessage msg)
        {
            ErrorAck apdu = new ErrorAck(msg.Apdu);
            if (apdu.ServiceChoise == 12)
            {
                ArrayList res = new ArrayList();
                res.Add(apdu.ErrorCode);
                if (BacNetDevice.Instance.Waiter is int &&
                    Convert.ToInt32(BacNetDevice.Instance.Waiter) == apdu.InvokeId)
                    BacNetDevice.Instance.Waiter = res;
            }
            if (apdu.ServiceChoise == 15)
            {
                BacNetDevice.Instance.Services.Confirmed.WritePropertyCallBack(apdu.InvokeId, BacNetEnums.GetErrorMessage((byte)apdu.ErrorCode));
            }
        }

        public void ReceivedSimpleAck(BacNetRawMessage msg)
        {
            var npdu = new BacNetIpNpdu(msg.Npdu);
            var apdu = new SimpleAck(msg.Apdu);
            //WritePropertyOk
            if (apdu.ServiceChoise == 15)
            {
                BacNetDevice.Instance.Services.Confirmed.WritePropertyCallBack(apdu.InvokeId, "Ok");
            }
        }

        #endregion
    }
}
