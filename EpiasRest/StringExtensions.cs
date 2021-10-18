using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Telemeter.Extensions
{
    public static class StringEx
    {
        public static int HexToInt(this string hexValue)
        {
            return int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
        }

        public static string getWord(ref string command)
        {
            string result = "";
            while ((command.Length > 0) && (command[0] != ' '))
            {
                result += command[0];
                command = command.Remove(0, 1);
            }
            command = command.Trim();
            if (command.Length > 0)
            {
                if (command[0] == '"') command.Remove(0, 1);
                if (command[command.Length - 1] == '"') command.Remove(command.Length - 1, 1);
            }
            return result.Trim();
        }

        public static string getWordtoUpper(ref string command)
        {
            return getWord(ref command).ToUpper().Replace("İ", "I").Trim();
        }

        public static string Right(this string sValue, int length)
        {
            if (string.IsNullOrEmpty(sValue))
            {
                sValue = string.Empty;
            }
            else if (sValue.Length > length)
            {
                sValue = sValue.Substring(sValue.Length - length, length);
            } 
            return sValue;
        }
        public static string Left(this string sValue, int length)
        {
            if (string.IsNullOrEmpty(sValue))
            {
                sValue = string.Empty;
            }
            else if (sValue.Length > length)
            {
                sValue = sValue.Substring(0, length);
            }
            return sValue;
        }

        public static string[] SpecialChars = new string[] { "<NUL>", //Null char
                                                             "<SOH>", //Start of Heading
                                                             "<STX>", //Start of Text
                                                             "<ETX>", //End of Text
                                                             "<EOT>", //End of Transmission
                                                             "<ENQ>", //Enquiry
                                                             "<ACK>", //Acknowledgment
                                                             "<BEL>", //Bell
                                                             "<BS>",  //Back Space
                                                             "<HT>",  //Horizontal Tab
                                                             "<LF>",  //Line Feed
                                                             "<VT>",  //Vertical Tab
                                                             "<FF>",  //Form Feed
                                                             "<CR>",  //Carriage Return
                                                             "<SO>",  //Shift Out / X-On
                                                             "<SI>",  //Shift In / X-Off
                                                             "<DLE>", //Data Line Escape
                                                             "<DC1>", //Device Control 1 (oft. XON)
                                                             "<DC2>", //Device Control 2
                                                             "<DC3>", //Device Control 3 (oft. XOFF)
                                                             "<DC4>", //Device Control 4
                                                             "<NAK>", //Negative Acknowledgement
                                                             "<SYN>", //Synchronous Idle
                                                             "<ETB>", //End of Transmit Block
                                                             "<CAN>", //Cancel
                                                             "<EM>",  //End of Medium
                                                             "<SUB>", //Substitute
                                                             "<ESC>", //Escape
                                                             "<FS>",  //File Separator
                                                             "<GS>",  //Group Separator
                                                             "<RS>",  //Record Separator
                                                             "<US>"  //Unit Separator 
                                                            };
        public static string ConvertToHumanString(this string str)
        {
            string result = str;
            for (int i = 0; i < SpecialChars.Length; i++)
            {
                result = result.Replace(((char)i).ToString(), SpecialChars[i]);
            }
            return result;
        } 
        public static string ConvertToSystemString(this string str)
        {
            string result = str;
            for (int i = 0; i < SpecialChars.Length; i++)
            {
                result = result.Replace(SpecialChars[i], ((char)i).ToString());
            }
            result = ReplaceBcc(result);
            return result;
        }

        private static string ReplaceBcc(string value)
        {
            int bccNdx = value.ToUpper().IndexOf("<BCC>");
            if (bccNdx > 0)
            {
                value = value.Remove(bccNdx, 5);
                value = value.Insert(bccNdx, Convert.ToChar(value.GetBytes().Get_BCC()).ToString());
            } 
            return value;
        }

        public static string LeadStr(this string str, int len, char c = '0', bool isLeft = false)
        { 
            if (isLeft)
                while (str.Length < len) str = str + c;
            else
                while (str.Length < len) str = c + str;
            return str;
        }

        public static object isNull(this object value, object v)
        {
            if (value != null)
            {
                if (value != DBNull.Value)
                    return value;
                else return v;
            }
            else return v;
        }

        public static bool IsNumber(this object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }

        public static object IsNotNumber(this object value, object defaultValue)
        {
            bool number = value.IsNumber();
            if (!number)
            {
                string s = value.ToString();
                decimal val = 0;
                try
                {
                    val = decimal.Parse(s);
                    number = true;
                }
                catch
                {
                    number = false;
                }
            }
            return number ? value : defaultValue;
        }

        public static string SafeSubstring(this string s, int start, int len)
        {
            string result = "";
            try
            {
                if ((len + start) <= s.Length)
                {
                    result = s.Substring(start, len);
                }
                else
                    result = s;
            }
            catch { result = s; }
            return result;
        }

        public static string[] SplitParams(string cmd)
        {
            string[] s = cmd.Split(new char[] { ',', ')', '(', ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < s.Length; i++) s[i] = s[i].Trim();
            return s;
        }

        public static string[] getSplitLikeArgs(string s)
        {
            string[] result = s.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            for(int i=0;i<result.Length; i++)
            {
                result[i] = "/" + result[i];
            }
            return result;
        }
        
        public static string fromBase64(this string value)
        {
            return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(value));
        }

        public static string toBase64(this string value)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
        }
    }
}
