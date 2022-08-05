using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using IBM.XMS;

namespace IBMMQDemo
{
    public static class PubSubOperations
    {
        public const int TIMEOUTTIME = 10000;
        public static string GetClientID(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic) || topic == "/")
                return "UNKNOWN";
            topic = topic.Replace("/", null);
            return topic;
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

        public static bool SendMessage(string topic, string text)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(topic))
                return false;
            JsonMessage xmsJson = new JsonMessage(text);
            bool success = false;
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
                {
                    Console.WriteLine("Failed to load XMSFactoryFactory object");
                    success = false;
                }
                else
                {
                    // Create WMQ Connection Factory.
                    cf = factoryFactory.CreateConnectionFactory();
                    if (cf == null)
                    {
                        Console.WriteLine("Failed to load IConnectionFactory object");
                        success = false;
                    }
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
            using (connectionWMQ = cf.CreateConnection())
            {
                // Create session
                using (sessionWMQ = connectionWMQ.CreateSession(false, AcknowledgeMode.AutoAcknowledge))
                {
                    // Create destination
                    using (destination = sessionWMQ.CreateTopic(topic))
                    {
                        destination.SetIntProperty(XMSC.WMQ_TARGET_CLIENT, XMSC.WMQ_TARGET_DEST_MQ);
                        // Create producer
                        using (producer = sessionWMQ.CreateProducer(destination))
                        {
                            producer.DeliveryMode = DeliveryMode.Persistent;
                            try
                            {
                                // Start the connection to receive messages.
                                connectionWMQ.Start();

                                // Create a text message and send it.
                                textMessage = sessionWMQ.CreateTextMessage();
                                textMessage.Text = xmsJson.toJsonString();
                                producer.Send(textMessage);
                                success = true;
                                textMessage = null;
                                Console.WriteLine("Message for topic " + topic + " successfully sent.");
                                success = true;
                            }
                            catch (Exception e2)
                            {
                                Console.WriteLine(e2.Message);
                                success = false;
                            }
                            finally
                            {
                                producer.Close();
                            }
                        }
                    }
                }
                connectionWMQ.Close();
            }
            if (!success)
                Console.WriteLine("SendMessage() FAILED!");
            factoryFactory = null;
            cf = null;
            return success;
        }

        public static string GetMessageText(string topic, string subscription)
        {
            if (string.IsNullOrWhiteSpace(subscription) || string.IsNullOrWhiteSpace(topic))
                return null;
            bool success = false;
            string clientID = null;
            string xml = null;
            XMSFactoryFactory factoryFactory = null;
            IConnectionFactory cf = null;
            IConnection connectionWMQ = null;
            ISession sessionWMQ = null;
            IDestination destination = null;
            IMessageConsumer subscriber = null;
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
                        // Create connection.
                        using (connectionWMQ = cf.CreateConnection())
                        {
                            clientID = GetClientID(topic);
                            connectionWMQ.ClientID = clientID;
                            using (sessionWMQ = connectionWMQ.CreateSession(false, AcknowledgeMode.AutoAcknowledge))
                            {
                                using (destination = sessionWMQ.CreateTopic(topic))
                                {
                                    using (subscriber = sessionWMQ.CreateDurableSubscriber(destination, subscription, null, true))
                                    {
                                        connectionWMQ.Start();
                                        textMessage = (ITextMessage)subscriber.Receive(TIMEOUTTIME);
                                        if (textMessage != null && !string.IsNullOrWhiteSpace(textMessage.Text))
                                            xml = GetXMLFromJSONString(textMessage.Text);
                                        textMessage = null;
                                        subscriber.Close();
                                    }
                                }
                                //sessionWMQ.Unsubscribe(subscription);
                                sessionWMQ.Close();
                            }
                            connectionWMQ.Close();
                        }
                    }
                }
                success = true;
            }
            catch (Exception e1)
            {
                Console.WriteLine(e1.Message);
                success = false;
            }

            factoryFactory = null;
            cf = null;
            connectionWMQ = null;
            sessionWMQ = null;
            destination = null;
            subscriber = null;
            textMessage = null;

            if (!success)
                Console.WriteLine("Subscriber GetMessageTexts() FAILED.");

            return xml;
        }

    }
}
