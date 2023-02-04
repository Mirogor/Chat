using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace TCP_Client_Form
{   
    class Client
    {
        static byte[] buff = new byte[4096];
        static Encoding enc = Encoding.UTF8;
        //Объект для связи с сервером
        public static TcpClient client;
        //Контекст главного потока 
        SynchronizationContext sc = null;
        Form1 ref_form;
        //Точка подключения к серверу
        IPEndPoint point = null;
        //Буфер для случая нарушения целостности пакета 
        string buff_s;
        //Таймер о том что мы живы
        Timer timer_for_life = null;
        //------------------------------------------------------------------------
        //конструктор
        public Client(Form1 ref_form) 
        {
            this.ref_form = ref_form;
            //Контекст главного потока
            sc = System.Threading.SynchronizationContext.Current;
            //Создание таймера
            timer_for_life = new Timer(Timer_Life, null, 1000, 3000);
        }
        //------------------------------------------------------------------------
        //Функция для отссылки сообщения к серверу о том что мы живы
        private void Timer_Life(object o)
        {
            //Проверка подключение к серверу
            if (Connect_To_Server())
            {
                Send(" 3 ");
            }
        }
        //------------------------------------------------------------------------
        //Подключение к серверу
        public bool Connect_To_Server()
        {
            bool b = true;
            bool is_con = false;
            lock (this)
            {
                if ((client == null) && (point != null))
                {
                    //Поключение к серверу
                    try
                    {
                        //Создаём клиента
                        client = new TcpClient();
                        //попытка подключения к серверу
                        client.Connect(point);
                        //Выводим текст сообщения
                        sc.Post(ref_form.View_Good, "Connected");
                        //Признак о том что мы подключились
                        is_con = true;
                    }
                    catch (Exception ex)
                    {
                        sc.Post(ref_form.View_Error, "Connecting error : " + ex.Message);
                        client.Client = null;
                        b = false;
                    }
                }
            }
            //------------------------------
            //Если подключились
            if (is_con)
            {
                //безопасное принятие данных от сервера
                if (!Undangerous_BeginReceive())
                    return false;
                Send(" 1 " + My_Reader.Nick);
            }
            return b;
        }
        //------------------------------------------------------------------------
        //безопасное принятие данных от сервера
        bool Undangerous_BeginReceive()
        {
            bool b = true;
            lock (this)
                if (client != null)
                    try
                    {
                        client.Client.BeginReceive(buff, 0, buff.Length, SocketFlags.None, new AsyncCallback(RCallBack), null);
                    }
                    catch (Exception ex)
                    {
                        sc.Post(ref_form.View_Error, "Waiting message error : " + ex.Message);
                        b = false;
                    }
            return b;
        }
        //------------------------------------------------------------------------
        //безопасная отправка данных серверу
        public void Send(string s)
        {
            //Добавляем терминальные символы 
            s = char.ConvertFromUtf32(0) + s + char.ConvertFromUtf32(1);

            //Отправляем сообщение
            lock (this)
                if (client != null)
                    try
                    {
                        client.Client.Send(enc.GetBytes(s)); //кодировка в байты
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Sending error : " + ex.Message);
                        //Отключение клиента от сервера - без блокировки
                        Disconnect_Without_Lock();
                        return;
                    }
        }
        //------------------------------------------------------------------------
        //прием данных от сервера
        void RCallBack(IAsyncResult ar)
        {
             SocketError s_err = SocketError.Success;
            int size_data = 0;
            string s, data_s;
            bool b = true;
            //----------------------------------------
            //Получаем приходящие данные
            lock(this)
                if (client != null)
                    try
                    {
                        size_data = client.Client.EndReceive(ar, out s_err); //завершение чтения данных
                    }
                    catch (Exception ex)
                    {
                        sc.Post(ref_form.View_Error, "Getting message error : " + ex.Message);
                        //Отключение клиента от сервера (без блокировки)
                        Disconnect_Without_Lock();
                        b = false;
                    }
            //----------------------------------------
            //Ошибка при получении данных
            if (!b)
                return;
            //----------------------------------------
            //Проверка прихода нулевых данных
            //Если пришли - отключаем клиента
            if (size_data == 0)
            {
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
            //Обрабатывем приходящие данные
            do
            {
                //Получаем данные из телеграммы 
                data_s = Get_Message_From_Telegramm(ref buff_s);
                //Если данные получены 
                if (data_s != null)
                    Using_Message(data_s);
            }
            while (data_s != null);

            //Безопасное принятие данных от сервера
            Undangerous_BeginReceive();
        }
        //------------------------------------------------------------------------
        //Функция нахождения сообщения из пакета данных
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
            //Выпиливаем данные из сообщения
            data_s = s.Substring(start + 1, end - 1);
            //Получение оставшейся строки
            s = s.Substring(end + 1);
            //Передача сообщения
            return data_s;
        }
        //------------------------------------------------------------------------
        //Отключение клиента от сервера
        public void Disconnected()
        {
            lock (this)
            {
                //Отключение клиента от сервера - без блокировки
                Disconnect_Without_Lock();
            }
        }
        //------------------------------------------------------------------------
        //Отключение клиента от сервера без блокировки
        void Disconnect_Without_Lock()
        {
            if ((client != null) && (client.Client != null))
            {
                try
                {
                    //Отключения клиента от сервера
                    client.Client.Disconnect(true);
                    client = null;
                    sc.Post(ref_form.View_Error, "No connection");
                }
                catch (Exception ex)
                {
                    sc.Post(ref_form.View_Error, "Disconnetcion error : " + ex.Message);
                }
            }
        }
        //------------------------------------------------------------------------
        //Функция определения типа сообщения 
        void Using_Message(string s)
        {
            string s1;
            switch (s[1])
            {
                case '0':
                    sc.Post(ref_form.View_Message, s);
                    break;
                case '2':
                    if (s.Length <= 3)
                        return;
                    else
                    {
                        s1 = s; s = "";
                        s = s1.Substring(3);
                        sc.Post(ref_form.View_List, s);
                    }
                    break;
                case '4':
                    Disconnected();
                    sc.Post(ref_form.View_Error, "Your nick is being used, please, rename and save");
                    break;
            }
        }
        //------------------------------------------------------------------------
        //Функция создания точки подключения 
        public void Creating_Point(IPAddress ip, int port)
        {
            lock(this)
                point = new IPEndPoint(ip, port); //определение порта
        }
    }
}
