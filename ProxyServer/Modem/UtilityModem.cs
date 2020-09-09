using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ProxyServer.Modem
{
    public class UtilityModem
    {
        public static string COMMAND_ENABLE232 = "232SRELBANE\x007F";
        public static string COMMAND_ENABLE485 = "584SRELBANE\x007F";
        public static string GHIMODEM = "PASS12342";
        public static string DOCMODEM = "PASS12341";
        public static string RESETMODEM = "PASS12343";
        public static string encrypt(string text2)
        {
            //doan ma
            string pass = Strings.Mid(text2, 5);
            int num2 = Strings.Len(Strings.Mid(text2, 5));
            int num = 0;
            for (int i = 1; i <= num2; i++)
            {
                num += Strings.Asc(Strings.Mid(pass, i, 1));
            }
            num = (num % 256 & 127);
            text2 += Conversions.ToString(Strings.Chr(num));
            return text2;
        }
        public static byte IntToByte(int i)
        {
            if (i < 0)
                return (byte)0;
            else if (i > 255)
                return (byte)255;
            else
                return System.Convert.ToByte(i);
        }
        public static string resetmodem()
        {
            string text2 = "PASS" + Strings.Trim("1234") + "3";
            string pass = Strings.Mid(text2, 5);
            int num2 = Strings.Len(Strings.Mid(text2, 5));
            int num = 0;
            for (int i = 1; i <= num2; i++)
            {
                num += Strings.Asc(Strings.Mid(pass, i, 1));
            }
            num = (num % 256 & 127);
            text2 += Conversions.ToString(Strings.Chr(num));
            return text2;
        }
        public static string Ascii2Hex(string input)
        {
            string text = "";
            int num = Strings.Len(input);
            int num2 = 1;
            while (true)
            {
                int num3 = num2;
                int num4 = num;
                if (num3 > num4)
                {
                    break;
                }
                string text2 = Strings.Trim(Conversion.Hex(Strings.Asc(Strings.Mid(input, num2, 1))));
                if (Strings.Len(text2) == 1)
                {
                    text2 = "0" + text2;
                }
                text += text2;
                num2 = checked(num2 + 1);
            }
            return text;

        }
        public static long convertIP(string ip)
        {
            return IPAddress.NetworkToHostOrder((int)IPAddress.Parse(ip).Address);
        }
        public string Create_M_BQ(string BQ_In)
        {
            int[] array = new int[8];
            int[] array2 = new int[8];
            if (Strings.Len(BQ_In) != 16)
                return "NOTHING";
            if (Strings.Len(BQ_In) == 16)
            {
                for (int num = 0; num <= 7; num++)
                {
                    array[num] = Conversions.ToInteger("&H" + Strings.Mid(BQ_In, 2 * num + 1, 2));
                }

                for (int num = 0; num <= 7; num++)
                {
                    if (num < 6)
                    {
                        array2[num] = (array[num] ^ array[num + 1] ^ array[num + 2]);
                    }
                    else if (num == 6)
                    {
                        array2[num] = (array[num] ^ array[num + 1] ^ array[0]);
                    }
                    else
                    {
                        array2[num] = (array[num] ^ array[0] ^ array[1]);
                    }
                }

                string text = "";
                for (int num = 0; num <= 7; num++)
                {
                    text += Ascii2Hex(Conversions.ToString(Strings.Chr(array2[num])));
                }
                return text;
            }
            return "NOTHING";

        }
    }
}
