using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace TCP_Server
{
    //------------------------------------------------------------------------
    //Делегат для поиска клиента - кому передать сообщение
    delegate Client my_del(string name);
    //Делегат для удаления клиента из списка
    delegate void delete_del(Client c);
    //Делегат для отправки списка клиентов
    delegate void list_del();
    //Делегат для проверки одинаковых клиентов
    delegate bool same_cl_del(Client c);
    //------------------------------------------------------------------------
    //------------------------------------------------------------------------
    //------------------------------------------------------------------------
    class Server
    {
        static byte[] buff = new byte[4096];
        Encoding enc = Encoding.ASCII;       
        //Ссылка на прослушивателя (Отрываем порт и ждём клиента)
        TcpListener tcp_lis;
        //Список подключенных клиентов
        List<Client> clients = new List<Client>();
        //Таймер проверки подключенных клиентов
        Timer timer_for_life = null;
        //------------------------------------------------------------------------
        public Server() 
        {
            //функция чтения настроек для порта из файла 
            if (!Reader.Read_From_File())
                return;
            Console.WriteLine("Прочтен файл с настройками");
            //Открытие порта для прослушивания
            if (!OpenPort())
                return;
            Console.WriteLine("Порт открыт, ожидание клиентов");
            //Создание таймера
            timer_for_life = new Timer(Timer_Life, null, 1000, 6000);
        }
        //------------------------------------------------------------------------
        //Открытие порта для прослушивания
        bool OpenPort()
        {
            try
            {
                //Создаем точку подключения для прослушивателя на любой сетевой карте
                IPEndPoint point = new IPEndPoint(IPAddress.Any, TCP_Server.Reader.Port); //определение порта  
                //Создание точки прослушивания
                tcp_lis = new TcpListener(point);
                //ожидание запросов
                tcp_lis.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка открытия порта : " + ex.Message);
                return false;
            }
            //попытка принятия входящего запроса
            return Undangerous_BeginAcceptSocket();
        }
        //------------------------------------------------------------------------
        //Функция обратного вызова при подключении клиента
        void RCallBackClient(IAsyncResult ar)
        {
            Socket sock_cl;  //Ссылка на сокет подключившегося клиента
            Client cl;  //Ссылка на клиента при сохранении в список
            //Подключение клиента
           try
            {
                sock_cl = tcp_lis.EndAcceptSocket(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка подключения клиента : " + ex.Message);
                //Попытка принятия входящего запроса
                Undangerous_BeginAcceptSocket();
                return;
            }
            Console.WriteLine("Подключился клиент - " + sock_cl.RemoteEndPoint.ToString());
            //Создаем объект клиента и заносим его в список
            cl = new Client(sock_cl, Search_Client, Delete_Client, Send_List_All_Client, Finding_Same_Client);
            lock (cl)
                clients.Add(cl);
            //Попытка принятия входящего запроса
            if (!Undangerous_BeginAcceptSocket())
                return;
        }
        //------------------------------------------------------------------------
        //Безопасное принятие входящего запроса
        bool Undangerous_BeginAcceptSocket()
        {
            try
            {
                tcp_lis.BeginAcceptSocket(new AsyncCallback(RCallBackClient), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка вызова функции : " + ex.Message);
                return false;
            }
            return true;
        }
        //------------------------------------------------------------------------
        //Поиск клиента в списке
        Client Search_Client(string name)
        {
            Client cl = null;
            lock(this)
                foreach(Client c in clients)
                    if (c.Client_Name == name)
                    {
                        cl = c;
                        break;
                    }
            return cl;
        }
        //------------------------------------------------------------------------
        //Удаление клиента из списка
        void Delete_Client(Client c)
        {
            lock (this)
                clients.Remove(c);
            Send_List_All_Client();
        }
        //------------------------------------------------------------------------
        //Поиск на наличие одинковых клиентов
        bool Finding_Same_Client(Client cl)
        {
            bool b = false;
            String client_name = cl.Client_Name;
            lock (this)
            {
                foreach (Client c in clients)
                    if ((c != cl) && (c.Client_Name == client_name))
                    {
                        clients.Remove(cl);
                        b = true;
                        break;
                    }
            }
            return b;
        }
        //------------------------------------------------------------------------
        //Функция формирования списка и отправки данному клиенту
        void Send_List_Of_Clients(Client cl)
        {
            string s = " 2 ";
            //Проходим по клиентам
            foreach (Client c in clients)
            {
                if (c != cl)
                    s = s + c.Client_Name + " ";
            }
            s = s.Substring(0, s.Length - 1);
            cl.Send(s);
        }
        //------------------------------------------------------------------------
        //Функция рассылки всем клиентам списка
        void Send_List_All_Client()
        {
            lock (this)
                foreach (Client c in clients)
                    Send_List_Of_Clients(c);
        }
        //------------------------------------------------------------------------
        //Функция для отссылки сообщения к серверу о том что мы живы
        private void Timer_Life(object o)
        {
            DateTime dt = DateTime.Now;
            TimeSpan ts;
            bool List = false; 
            //Проход по клиентам
            lock(this)
                foreach (Client c in clients)
                {
                    ts = dt - c.Stamp_Time;
                    if (ts.Milliseconds > 6000)
                    {
                        //Удаляем клиента из списка
                        clients.Remove(c);
                        //Отключение клиента от сервера
                        c.Disconnected();
                        List = true;
                    }
                }
            if (List == true)
                Send_List_All_Client();
        }
    }
}
