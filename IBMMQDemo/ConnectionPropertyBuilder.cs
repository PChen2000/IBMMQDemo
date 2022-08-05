using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IBM.XMS;

namespace IBMMQDemo
{
    public static class ConnectionPropertyBuilder
    {
        private const string CCDT = "MQCCDTURL";
        private const string FILEPREFIX = "file://";

        public static void SetConnectionProperties(ref IConnectionFactory cf)
        {
            if (cf == null) return;
            string ccdtURL = CheckForCCDT();
            string connStr = null;
            // TBD
            //string key_repository = GetKetRepository();
            string key_repository = null;
            int numConnections = 0;
            if (!string.IsNullOrWhiteSpace(ccdtURL))
            {
                //Console.WriteLine("CCDT Environment setting found");
                cf.SetStringProperty(XMSC.WMQ_CCDTURL, ccdtURL);
            }
            else
            {
                //numConnections = env.NumberOfConnections();
                numConnections = 1;
                if (numConnections > 1)
                {
                    //Console.WriteLine("There are "+ numConnections.ToString() + " connections");
                    //connStr = env.BuildConnectionString();
                    // TBD
                    connStr = null;
                    cf.SetStringProperty(XMSC.WMQ_CONNECTION_NAME_LIST, connStr);
                    //Console.WriteLine("Connection string is " + connStr);
                }
                else
                {
                    cf.SetStringProperty(XMSC.WMQ_HOST_NAME,
                        QueueConstants.Host);
                    cf.SetIntProperty(XMSC.WMQ_PORT, QueueConstants.Port);
                }
                cf.SetStringProperty(XMSC.WMQ_CHANNEL, QueueConstants.Channel);
            }
            SetRemConnectionProperties(ref cf, key_repository);
        }

        public static void SetRemConnectionProperties(ref IConnectionFactory cf, string key_repository)
        {
            if (cf == null) return;
            // NOT work in .Net Core
            if (!string.IsNullOrWhiteSpace(key_repository)
            && (key_repository.Contains("*SYSTEM")
            || key_repository.Contains("*USER")))
            {
                cf.SetIntProperty(XMSC.WMQ_CONNECTION_MODE, XMSC.WMQ_CM_CLIENT);
            }
            else
            {
                cf.SetIntProperty(XMSC.WMQ_CONNECTION_MODE, XMSC.WMQ_CM_CLIENT_UNMANAGED);
            }

            cf.SetStringProperty(XMSC.WMQ_QUEUE_MANAGER,
                QueueConstants.QueueManager);
            cf.SetStringProperty(XMSC.USERID,
                QueueConstants.Username);
            cf.SetStringProperty(XMSC.PASSWORD,
                QueueConstants.Password);

            // TBD
            //Console.WriteLine("Connection Cipher is set to " + conn.cipher_suite);
            //Console.WriteLine("Key Repository is set to " + key_repository);

            //if (conn.key_repository != null && conn.cipher_suite != null)
            if (!string.IsNullOrEmpty(key_repository))
            {
                cf.SetStringProperty(XMSC.WMQ_SSL_KEY_REPOSITORY, key_repository);
            }
            /*if (conn.cipher_suite != null)
            {
                cf.SetStringProperty(XMSC.WMQ_SSL_CIPHER_SPEC, conn.cipher_suite);
            }*/
        }

        public static string CheckForCCDT()
        {
            //Console.WriteLine("Checking for CCDT File");
            string ccdt = Environment.GetEnvironmentVariable(CCDT);

            if (!string.IsNullOrWhiteSpace(ccdt))
            {
                //Console.WriteLine(CCDT + " environment variable is set to " + ccdt);
                //Console.WriteLine("Will be checking for " + ccdt.Replace(FILEPREFIX, ""));
                if (File.Exists(ccdt.Replace(FILEPREFIX, "")))
                {
                    //Console.WriteLine("CCDT file found");
                    return ccdt;
                }
            }

            //Console.WriteLine("No CCDT file found or specified");
            return null;
        }
    }
}
