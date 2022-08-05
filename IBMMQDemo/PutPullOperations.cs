using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using IBM.XMS;
using IBM.WMQ;

namespace IBMMQDemo
{
    public static class PutPullOperations
    {
        public static bool SendMessage(string queueName, string xml)
        {
            if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(queueName))
                return false;
            JsonMessage json = new JsonMessage(xml);
            bool success = false;
            string exceptionText = null;
            XMSFactoryFactory factoryFactory = null;
            IConnectionFactory cf = null;
            IConnection connectionWMQ = null;
            ISession sessionWMQ = null;
            IDestination destination = null;
            IMessageProducer producer = null;
            ITextMessage textMessage = null;

            try
            {
                // Get an instance of factory.
                factoryFactory = XMSFactoryFactory.GetInstance(XMSC.CT_WMQ);
                if (factoryFactory == null)
                    Console.WriteLine("Failed to load XMSFactoryFactory object");
                else
                {
                    // Create WMQ Connection Factory.
                    cf = factoryFactory.CreateConnectionFactory();
                    if (cf == null)
                        Console.WriteLine("Failed to load IConnectionFactory object");
                    else
                    {
                        // Set the properties
                        ConnectionPropertyBuilder.SetConnectionProperties(ref cf);
                        success = true;
                    }
                }
            }
            catch (Exception e1)
            {
                Console.WriteLine(e1.Message);
                success = false;
            }

            if (!success)
            {
                factoryFactory = null;
                cf = null;
                Console.WriteLine("Factory connection setup FAILED.");
                return false;
            }

            success = false;
            // Create connection.
            try
            {
                using (connectionWMQ = cf.CreateConnection())
                {
                    // Create session
                    using (sessionWMQ = connectionWMQ.CreateSession(false, AcknowledgeMode.AutoAcknowledge))
                    {
                        using (destination = sessionWMQ.CreateQueue(queueName))
                        {
                            using (producer = sessionWMQ.CreateProducer(destination))
                            {
                                try
                                {
                                    connectionWMQ.Start();
                                    textMessage = sessionWMQ.CreateTextMessage();
                                    textMessage.Text = json.toJsonString();
                                    producer.Send(textMessage);
                                    Console.WriteLine("Message for queue " + queueName + " successfully sent.");
                                    success = true;
                                }
                                catch (MessageFormatException fmExc)
                                {
                                    Console.WriteLine(fmExc.Message);
                                    exceptionText = "Type: MessageFormatException; "
                                        + fmExc.Message;
                                    success = false;
                                }
                                catch (InvalidDestinationException destExc)
                                {
                                    Console.WriteLine(destExc.Message);
                                    exceptionText = "Type: InvalidDestinationException; "
                                        + destExc.Message;
                                    success = false;
                                }
                                catch (XMSException xmlExc)
                                {
                                    Console.WriteLine(xmlExc.Message);
                                    exceptionText = "Type: XMSException; "
                                        + xmlExc.Message;
                                    success = false;
                                }
                                catch (Exception e2)
                                {
                                    Console.WriteLine(e2.Message);
                                    success = false;
                                    exceptionText = "Type: Uncat. Exception; "
                                        + e2.Message;
                                    success = false;
                                }
                                finally
                                {
                                    producer.Close();
                                    textMessage = null;
                                }
                            }
                        }
                        sessionWMQ.Close();
                    }
                    connectionWMQ.Close();
                }
            }
            catch (XMSException ex)
            {
                Console.WriteLine("XMS Exception encountered. " + ex.Message);
                success = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught at SendMessage() Method: " + ex.Message);
                success = false;
            }
            finally
            {
                factoryFactory = null;
                cf = null;
            }
            return success;
        }

        public static string GetXMLFromJSONString(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return null;
            JsonTextReader reader = new JsonTextReader(new StringReader(jsonString));
            string xml = null;
            string propertyName = null;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.PropertyName:
                            propertyName = reader.Value.ToString();
                            break;
                        case JsonToken.String:
                            if (propertyName == "msg")
                                xml = reader.Value.ToString();
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.StartArray:
                            propertyName = null;
                            break;
                        case JsonToken.StartObject:
                            propertyName = null;
                            break;
                        case JsonToken.EndArray:
                            propertyName = null;
                            break;
                        case JsonToken.EndObject:
                            break;
                        default:
                            break;
                    }
                }
            }
            return xml;
        }

        public static string GetXMLMessage(MQMessage mess)
        {
            if (mess == null || mess.MessageLength == 0)
                return null;
            string json = null;
            try
            {
                json = mess.ReadString(mess.MessageLength);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                json = null;
            }
            if (string.IsNullOrWhiteSpace(json)) return null;
            return GetXMLFromJSONString(json);
        }
        public static MQQueueManager CreateQueueManager()
        {
            MQQueueManager queueManager = null;
            Hashtable connectionProperties = new Hashtable();
            connectionProperties.Add(IBM.WMQ.MQC.TRANSPORT_PROPERTY, IBM.WMQ.MQC.TRANSPORT_MQSERIES_MANAGED);
            connectionProperties.Add(IBM.WMQ.MQC.HOST_NAME_PROPERTY, QueueConstants.Host);
            connectionProperties.Add(IBM.WMQ.MQC.CHANNEL_PROPERTY, QueueConstants.Channel);
            connectionProperties.Add(IBM.WMQ.MQC.PORT_PROPERTY, QueueConstants.Port);
            connectionProperties.Add(IBM.WMQ.MQC.USER_ID_PROPERTY, QueueConstants.Username);
            connectionProperties.Add(IBM.WMQ.MQC.PASSWORD_PROPERTY, QueueConstants.Password);
            queueManager = new MQQueueManager(QueueConstants.QueueManager, connectionProperties);

            return queueManager;
        }

        public static string GetMessage(string queueName)
        {
            bool success = false;
            MQQueueManager qm = CreateQueueManager();
            MQQueue queue = null;
            string xml = null;
            if (qm == null) return null;
            try
            {
                queue
                    = qm.AccessQueue(queueName,
                    IBM.WMQ.MQC.MQOO_INPUT_SHARED
                    + IBM.WMQ.MQC.MQOO_FAIL_IF_QUIESCING
                    + IBM.WMQ.MQC.MQOO_INQUIRE);
                success = true;
            }
            catch (Exception e1)
            {
                success = false;
                Console.WriteLine(e1.Message);
            }
            if (!success)
            {
                qm = null;
                return null;
            }
            MQMessage mess = null;
            IBM.WMQ.MQGetMessageOptions options = null;
            try
            {
                try
                {
                    success = false;
                    options = new MQGetMessageOptions();
                    options.Options
                        = IBM.WMQ.MQC.MQGMO_SYNCPOINT + IBM.WMQ.MQC.MQGMO_WAIT
                        + IBM.WMQ.MQC.MQGMO_FAIL_IF_QUIESCING;
                    mess = new MQMessage();
                    queue.Get(mess, options);
                    xml = GetXMLMessage(mess);
                    if (!string.IsNullOrWhiteSpace(xml))
                    {
                        Console.WriteLine(xml);
                        Console.WriteLine();
                    }
                    mess.MessageId = IBM.WMQ.MQC.MQMI_NONE;
                    mess.CorrelationId = IBM.WMQ.MQC.MQMI_NONE;
                    mess.ClearMessage();
                    success = true;
                }
                catch (MQException mqex)
                {
                    success = false;
                    queue.Close();
                }
                catch (Exception e2)
                {
                    success = false;
                    Console.WriteLine(e2.Message);
                }
                finally
                {
                    options = null;
                    mess = null;
                }

                if (queue.IsOpen) queue.Close();
                qm.Disconnect();
            }
            catch (MQException mqex)
            {
                Console.WriteLine(mqex.Message);
            }
            finally
            {
                qm = null;
                queue = null;
            }
            return xml;
        }

    }
}
