using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Telemeter.Extensions;

namespace Telemeter.Extensions
{
    public static class ArrayHelper
    {
        public static string MainFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static readonly byte[] _CRLF = new byte[] { 13, 10 };
        public static byte[] GetBytes(this string str)
        {
            return ASCIIEncoding.ASCII.GetBytes(str);
        }

        public static string GetString(this byte[] bytes)
        {
            return ASCIIEncoding.ASCII.GetString(bytes);
        }

        public static T[] Append<T>(this T[] First, T[] Second)
        {
            int FirstLen = First.Length;
            Array.Resize(ref First, First.Length + Second.Length);
            System.Buffer.BlockCopy(Second, 0, First, FirstLen, Second.Length);
            return First;
        }

        public static byte[] getCRC(byte[] buf)
        {
            ushort one = 0x0001;
            ushort crc = 0xFFFF;
            ushort lsb = 0;
            for (int j = 0; j < buf.Length; j++)
            {
                byte b = buf[j];
                crc ^= b;
                for (int i = 8; i >= 1; i--)
                {
                    lsb = (ushort)(crc & one);
                    crc >>= 1;
                    if (lsb == 1) crc ^= 0xA001;
                }
            }
            return BitConverter.GetBytes(crc);
        }

        public static byte[] CopyBytes(this byte[] buffer, int Offset, int Size)
        {
            byte[] bytes = new byte[Size];
            System.Buffer.BlockCopy(buffer, Offset, bytes, 0, Size);
            return bytes;
        }


        public static byte Get_BCC(this string Data)
        {
            byte Bcc = 0;
            Data = Data.ConvertToSystemString();
            string Invalid = "<SOH><STX>".ConvertToSystemString();
            if ((Data.Length > 0) && (Invalid.IndexOf(Data[0]) >= 0)) Data = Data.Skip(1).ToString();
            for (int i = 0; i < Data.Length; i++)
            {
                byte xbyte = Convert.ToByte(Data[i]);
                Bcc = (byte)((int)Bcc ^ (int)xbyte);
            }
            return Bcc;
        }

        public static byte Get_BCC(this byte[] Data)
        {
            byte Bcc = 0;
            string Invalid = "<SOH><STX>".ConvertToSystemString();
            while ((Data.Length > 0) && (Invalid.IndexOf(Convert.ToChar(Data[0])) >= 0)) Data = Data.Skip(1).ToArray();
            for (int i = 0; i < Data.Length; i++)
            {
                byte xbyte = Data[i];
                Bcc = (byte)((int)Bcc ^ (int)xbyte);
            }
            return Bcc;
        }

        public static IEnumerable<int> PatternAt(this byte[] source, byte[] pattern)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    yield return i;
                }
            }
        }

        public static bool EndsWith(this byte[] buffer, string EndString)
        {
            if (EndString.IndexOf("<BCC>") >= 0)
            {
                while ((buffer.Length > 0) && (buffer[0] != 2) && (buffer[0] != 1)) buffer = buffer.Skip(1).ToArray();
                if (buffer.Length > 0)
                {
                    byte Bcc = buffer.CopyBytes(0, buffer.Length - 1).Get_BCC();
                    EndString = EndString.Replace("<BCC>", Convert.ToChar(Bcc).ToString());
                }
                else return false;
            }
            var EndBytes = EndString.ConvertToSystemString().GetBytes();
            bool result = false;
            if (buffer.Length >= EndBytes.Length) result = buffer.CopyBytes(buffer.Length - EndBytes.Length, EndBytes.Length).SequenceEqual(EndBytes);
            return result;
        }

        public static string EndsData(this byte[] buffer, string EndString)
        {
            if (EndString.IndexOf("<BCC>") >= 0)
            {
                while ((buffer.Length > 0) && (buffer[0] != 2)) buffer = buffer.Skip(1).ToArray();
                if (buffer.Length > 0)
                {
                    byte Bcc = buffer.CopyBytes(0, buffer.Length - 1).Get_BCC();
                    EndString = EndString.Replace("<BCC>", Convert.ToChar(Bcc).ToString());
                }
                else return "";
            }
            var EndBytes = EndString.ConvertToSystemString().GetBytes();
            bool result = false;
            if (buffer.Length >= EndBytes.Length) result = buffer.CopyBytes(buffer.Length - EndBytes.Length, EndBytes.Length).SequenceEqual(EndBytes);
            if (result) return EndBytes.GetString();
            else return "";
        }

        public static byte[] appendCRC(byte[] buf)
        {
            byte[] crc = getCRC(buf);
            return buf.Append(crc);
        }

        public static bool checkCRC(byte[] buf)
        {
            bool result = false;
            try
            {
                result = buf.Length >= 3;
                if (result)
                {
                    byte[] IncomingCrc = new byte[2] { buf[buf.Length - 2], buf[buf.Length - 1] };
                    Array.Resize(ref buf, buf.Length - 2);
                    byte[] CalculatedCrc = getCRC(buf);
                    result = Enumerable.SequenceEqual(CalculatedCrc, IncomingCrc);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in buffer \r\n" + e.Message, e);
            }
            return result;
        }

        internal static byte[] AddConsereth(byte[] bytes)
        {
            byte[] arr = new byte[3] { 0x03, 0x00, (byte)bytes.Length };
            return arr.Append(bytes);
        }

        internal static int RemoveConsereth(ref byte[] buffer, int Len)
        { 
            var data = buffer.CopyBytes(0, Len);

            int posConsereth = 0;
            while ((posConsereth < Len) && (!((buffer[posConsereth] == 3) && (buffer[posConsereth + 1] == 0)))) posConsereth++;
            Array.Copy(data, 0, buffer, 0, posConsereth);
            data = data.Skip(posConsereth).ToArray();

            Len = posConsereth;
            while (data.Length > 0)
            {
                int PartLen = data[2];
                if (PartLen > data.Length) PartLen = data.Length - 3;
                Array.Copy(data, 3, buffer, Len, PartLen);
                Len += PartLen;
                data = data.Skip(3 + PartLen).ToArray(); 
            } 
            return Len;
        }

        public static bool IsList(this object o)
        {
            if (o == null) return false;
            return o is IList &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        public static bool IsDictionary(this object o)
        {
            if (o == null) return false;
            return o is IDictionary &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }

        public static bool IsArray(this object o)
        {
            return o is Array;
        }

        public static byte[] BinarySerialize(this object data)
        {
            using (var stream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, data);
                return stream.ToArray();
            }
        }

        public static object BinaryDeserialize(this byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                IFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(stream);
            }
        }
    }
}