using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace IBMMQDemo
{
    public class JsonMessage
    {
        public string msg;
        public int value;
        public string correlationID;
        private static Random random = new Random();
        private const int CO_ID_LENGTH = 24;
        public JsonMessage(string s)
        {
            msg = s;
            value = random.Next();
            correlationID = correlIdGenerator();
        }
        public string toJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
        private static String correlIdGenerator()
        {
            //JMS/XMS is forcing this to generate a 24byte hext string so we need to
            //generate an ASCII string 1 char per byte of length 24 char and converting that into a hex string
            return Guid.NewGuid().ToString().PadRight(CO_ID_LENGTH);
        }

        public static byte[] ToByteArray(String HexString)
        {
            int NumberChars = HexString.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 16);
            }
            return bytes;
        }

        public static String getCorrFilter(String s)
        {
            return JsonMessage.AsHexString(JsonMessage.AsBytes(s)).Replace("-", "").Substring(0, 2 * CO_ID_LENGTH);
        }

        private static String AsHexString(byte[] ba)
        {
            return BitConverter.ToString(ba);
        }

        private static byte[] AsBytes(String s)
        {
            return Encoding.Default.GetBytes(s);
        }

        public static string Serialize<T>(T obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                return Encoding.Default.GetString(ms.ToArray());
            }
        }

    }
}
