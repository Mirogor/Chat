using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TCP_Server
{
    abstract class Reader
    {
        public static int Port;
        //функция чтения порта из файла 
        public static bool Read_From_File()
        {
            FileStream file;
            StreamReader read;
            string s;
            try
            {
                file = new FileStream("Settings.txt", FileMode.Open);
                read = new StreamReader(file);
                s = read.ReadLine();
                read.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка чтения файла настроек : " + ex.Message);
                return false;
            }
            return int.TryParse(s, out Port);
        }
    }
}
