using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TCP_Server
{
    class Client
    {
        static Encoding enc = Encoding.UTF8;
        //Ник клиента
        string client_name;
        public string Client_Name { get { return client_name; } }
        //Сокет клиента
        Socket client = null;
        //Полный текст для сообщений
        String full_name_client = "";
        //Выделение памяти под буффер
        byte[] buff = new byte[4096];
        //Буффер для остатка сообщения 
        string buff_s = null;
        //--------------------
        //Делегат для функции сервера
        my_del function_search = null;
        //Делегат для функции сервера
        delete_del function_del = null;
        //Делегат для функции сервера
        list_del function_list = null;
        //Делегат для функции сервера
        same_cl_del function_same = null;
        //--------------------
        //Время последнего прихода сообщения от клиента
        DateTime stamp_time = DateTime.Now;
        public DateTime Stamp_Time { get { return stamp_time; } }
        //------------------------------------------------------------------------
        //Конструктор
        public Client(Socket Soket_Client, my_del function_search, delete_del function_del, list_del function_list, same_cl_del function_same)
        {
            this.client = Soket_Client;
            this.function_search = function_search;
            this.function_del = function_del;
            this.function_list = function_list;
            this.function_same = function_same;
            //Получение полного имени
            Set_Full_Client_Name();
            //безопасное принятие данных от клиента
            if (!Undangerous_BeginReceive())
                return;
        }
        //------------------------------------------------------------------------
        //Функция обратного при приходе данных от клиента
        void RCallBack(IAsyncResult ar)
        {
            SocketError s_err = SocketError.Success;
            int size_data = 0;
            string s, data_s;
            //----------------------------------------
            //Получаем приходящие данные
            lock (this)
                if (client != null)
                    try
                    {
                        size_data = client.EndReceive(ar, out s_err); //завершение чтения данных
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка при получении данных от "  +full_name_client + ex.Message);
                        return;
                    }
            //----------------------------------------
            //Проверка прихода нулевых данных
            //Если пришли - отключаем клиента
            if (size_data == 0)
            {
                //Удаление клиента из списка
                function_del(this);
                //Отключение клиента от сервера
                Disconnected();
                return;
            }
            //----------------------------------------
            //перевод байтов в строку
            s = enc.GetString(buff, 0, size_data);
            //Добавляем приходящие сообщения к остатку предыдущих сообщений
            if (buff_s == null)
                buff_s = s;
            else
                buff_s = buff_s + s;
            //----------------------------------------
            //Обрабатываем приходящие данные
            do
            {
                //Получаем данные из телеграммы 
                data_s = Get_Message_From_Telegramm(ref buff_s);
                
                //Если данные получены 
                if (data_s != null)
                {
                    Console.WriteLine("Получено сообщение от " + full_name_client + data_s);
                    Using_Message(data_s);
                }
            }
            while (data_s != null);
            
            //безопасное принятие данных от клиента
            if (!Undangerous_BeginReceive())
                return;
        }
        //------------------------------------------------------------------------
        //безопасное принятие данных от клиента
        bool Undangerous_BeginReceive()
        {
            bool b = false;
            lock(this)
                if (client != null)
                    try
                    {
                        // ожидание приема данных заново
                        client.BeginReceive(buff, 0, buff.Length, SocketFlags.None, new AsyncCallback(RCallBack), null); 
                        b = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка старта приёма данных от " + full_name_client + ex.Message);
                        //Удаление клиента из списка
                        function_del(this);
                        //Отключение клиента от сервера - без блокировки
                        Disconnect_Without_Lock();
                    }
            return b;
        }
        //------------------------------------------------------------------------
        //безопасная отправка данных клиенту
        public void Send(string s)
        {
            //Добавляем терминальные символы 
            s = char.ConvertFromUtf32(0) + s + char.ConvertFromUtf32(1);
            lock (this)
                if (client != null)
                {
                    //Отправляем сообщение
                    Console.WriteLine("Отправляем для " + full_name_client + s);
                    try
                    {
                        client.Send(enc.GetBytes(s)); //кодировка в байты
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка отправления сообщения для " + full_name_client + ex.Message);
                    }
                }
        }
        //------------------------------------------------------------------------
        string Get_Message_From_Telegramm(ref string s)
        {
            int start, end;
            string data_s;
            //Находим начало сообщения 
            start = s.IndexOf(char.ConvertFromUtf32(0));
            if (start < 0)
                return null;
            s = s.Substring(start);
            //Находим конец сообщения 
            end = s.IndexOf(char.ConvertFromUtf32(1));
            if (end < 0)
                return null;
            //Получаем данные из сообщения
            data_s = s.Substring(start + 1, end - 1);
            //Получение оставшейся строки
            if (s.Length > end + 1)
                s = s.Substring(end + 1);
            else
                s = "";
            //Передача сообщения
            return data_s;
        }
        //------------------------------------------------------------------------
        //вырезание имени из сообщения
        string Get_Name(string s)
        {
            int i = 3;
            string nm = "";
            while(s[i] != ' ')
            {
                nm = nm + s[i];
                i++;
            }
            return nm;
        }
        //------------------------------------------------------------------------
        //вырезание сообщения без имени
        string Get_Message(string s)
        {
            int i; string mess = "";
            if (s.Length <= 3)
                return "";
            for (i = 4; i < s.Length; i++)
            {
                if (s[i] == ' ')
                {
                    for (int n = i + 1; n < s.Length; n++)
                        mess = mess + s[n];
                    break;
                }
            }
            mess = " " + mess;
            return  mess;
        }
        //------------------------------------------------------------------------
        //Отключение клиента от сервера
        public void Disconnected()
        {
            lock (this)
            {
                //Отключение клиента от сервера (без блокировки)
                Disconnect_Without_Lock();
            }
        }
        //------------------------------------------------------------------------
        //Отключение клиента от сервера - без блокировки
        void Disconnect_Without_Lock()
        {
            if (client != null)
            {
                try
                {
                    client.Disconnect(true);
                    Console.WriteLine("Отключили клиента " + full_name_client);
                    client = null;
                }
                catch (Exception ex1)
                {
                    Console.WriteLine("Ошибка отключения клиента " + full_name_client + ex1.Message);
                }
            }
        }
        //------------------------------------------------------------------------
        //Обработка сообщения
        void Using_Message(string s)
        { 
            string send_name, s1, mess;
            Client cl_send;
            switch (s[1])
            {
                //Сообщение от одного клиента к другому клиенту
                case '0':
                    //вырезание имени из сообщения
                    send_name = Get_Name(s);
                    //Вызов делегата
                    cl_send = function_search(send_name);
                    //Проверка найден ли клиент
                    if (cl_send != null)
                    {
                        mess = Get_Message(s);
                        s1 = " 0 " + client_name + " " + Get_Message(s);
                        //безопасная отправка данных клиенту
                        cl_send.Send(s1);
                    }
                    break;
                //------------------------------------
                //Сообщение с именем клиента
                case '1':
                    for (int i = 3; i < s.Length; i++)
                        client_name = client_name + s[i];
                    //Функция создания сообщения со списком клиентов
                    if (function_same(this) == false)
                    {
                        //Получение полного имени
                        Set_Full_Client_Name();
                        function_list();
                    }
                    else
                        this.Send(" 4 ");
                    break;
                //------------------------------------
                //Сообщение-таймер о жизни клинта 
                case '3':
                    stamp_time = DateTime.Now;
                    break;
            }               
        }
        //------------------------------------------------------------------------
        //Получение полного имени для сообщений
        void Set_Full_Client_Name()
        {
            full_name_client = "'" + client_name + "'";

            lock (this)
                if (client != null)
                    full_name_client = full_name_client + " - " + client.RemoteEndPoint.ToString();
            full_name_client = full_name_client + " : ";
        }
    }
}
