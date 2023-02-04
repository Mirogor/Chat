using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;


namespace TCP_Server
{
    class Program
    {       
        static void Main(string[] args)
        {
            String s;
            Server sr;
            Console.WriteLine("Работаем...");
            //Создаем объект сервера
            sr = new Server();
            Console.ReadKey();
            do
            {
                s = Console.ReadLine();
            }
            while (true); 
        }
    }
}
