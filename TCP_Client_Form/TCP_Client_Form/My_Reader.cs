using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;


namespace TCP_Client_Form
{
    abstract class My_Reader
    {
        public static string Nick;
        public static IPAddress IP;
        public static int Port;
        public static string S_Port, S_IP;
        //-------------------------------------------------------------------------------------
        //функция чтения настроек для порта из файла 
        public static bool Reader_From_File(out string text_error)
        {
            text_error = null;
            FileStream file2;
            StreamReader read;
            string s, s1, s3;
            try
            {
                file2 = new FileStream("Settings.txt", FileMode.Open);
                read = new StreamReader(file2);
                s = read.ReadLine();
                s1 = read.ReadLine();
                s3 = read.ReadLine();
                read.Close();
                S_Port = s;
                S_IP = s1;
                Nick = s3;
            }
            catch(Exception ex)
            {
                text_error = "Reading fail error: " + ex.Message;
                return false;
            }
            //Проверка порта
            if (!int.TryParse(s, out Port))
            {
                text_error = "Port is wrong";
                return false;
            }
            //Проверка IP
            if (!IPAddress.TryParse(s1, out IP))
            {
                text_error = "IP is wrong";
                return false;
            }
            //Проверка на наличие ника
            if (Nick == "")
            {
                text_error = "Please, write your nick";
                return false;
            }
            return true;
        }
        //-------------------------------------------------------------------------------------
        //Сохранение настроек
        public static bool Reader_W(string s_port, string s_ip, string s_nick, out string text_error)
        {
            text_error = null;
            if (s_nick == "")
            {
                text_error = "Please, write your nick";
                return false;
            }
            for (int i = 0; i < s_nick.Length; i++)
            {
                if (s_nick[i] == ' ')
                {
                    text_error = "Your nick must not have spaces";
                    return false;
                }
            }
            Nick = s_nick;
            if (!int.TryParse(s_port, out Port)) 
            {
                text_error = "Your Port is wrong";
                return false;
            }
            if (!IPAddress.TryParse(s_ip, out IP))
            {
                text_error = "Your IP is wrong";
                return false;
            }
            FileStream file2;
            StreamWriter write;
            try
            {
                file2 = new FileStream("Settings.txt", FileMode.Truncate);
                write = new StreamWriter(file2);
                write.WriteLine(s_port);
                write.WriteLine(s_ip);
                write.WriteLine(s_nick);
                write.Close();
            }
            catch (Exception ex)
            {
                text_error = "Fail error : " + ex.Message;
                return false;
            }
            return true;
        }
    }
}
