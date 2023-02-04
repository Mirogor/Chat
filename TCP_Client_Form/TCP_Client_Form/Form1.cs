using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TCP_Client_Form
{
    //Делегат для вызова функции формы из потока приема сообщений(для класса Client)
    delegate void view_mess(object o);

    public partial class Form1 : Form
    {
        //Объект клиента
        Client cl = null;
        //Список друзей для общения
        List<Friend> friends = new List<Friend>();
        //Текущий собеседник
        Friend current_friend = null;
        //Обычный шрифт
        Font font_normal;
        //Жирный шрифт(при непрочитанном сообщении)
        Font font_bold;
        //---------------------------------------------------------------
        //Конструктор
        public Form1()
        {
            String text_error;
            InitializeComponent();
            cl = new Client(this);
            if (!My_Reader.Reader_From_File(out text_error))
                Errors(text_error);
            else 
            {
                textBox3.Text = My_Reader.S_Port; 
                textBox4.Text = My_Reader.S_IP;
                textBox1.Text = My_Reader.Nick;
                label3.Text = My_Reader.Nick;
                Good_Mess("Options were created");
                cl.Creating_Point(My_Reader.IP, My_Reader.Port);
            }
            //Получаем обычный шрифт
            font_normal = new System.Drawing.Font(dataGridView1.Font, dataGridView1.Font.Style);
            //Получаем жирный шрифт
            font_bold = new System.Drawing.Font(dataGridView1.Font, FontStyle.Bold);
        }
        //---------------------------------------------------------------
        //Функция для вывода ошибок для главного потока 
        void Errors(string text_error)
        {
            toolStripStatusLabel1.Text = text_error;
            toolStripStatusLabel1.ForeColor = Color.White;
            toolStripStatusLabel1.BackColor = Color.Red;
        }
        //---------------------------------------------------------------
        //Функция для вывода хороших сообщений в главном потоке
        void Good_Mess(string text_error)
        {
            toolStripStatusLabel1.Text = text_error;
            toolStripStatusLabel1.ForeColor = Color.White;
            toolStripStatusLabel1.BackColor = Color.Green;
        }
        //---------------------------------------------------------------
        //Вывод ошибок для потока клиента
        public void View_Error(object o)
        {
            Errors(o.ToString());
        }
        //-------------------------------------------------------------
        //Вывод хорошего сообщения для потока клиента
        public void View_Good(object o)
        {
            string s = o.ToString();
            Good_Mess(s);
        }
        //---------------------------------------------------------------
        //Кнопка для сохранения настроек 
        private void button2_Click(object sender, EventArgs e)
        {
            String text_error;
            if (!My_Reader.Reader_W(textBox3.Text, textBox4.Text, textBox1.Text, out text_error))
                Errors(text_error);
            else
            {
                //Отключение клиента
                cl.Disconnected();
                //Создание новой точки подключения
                cl.Creating_Point(My_Reader.IP, My_Reader.Port);
                label3.Text = textBox1.Text;
                if (current_friend != null)
                    this.current_friend.Viewer.Text = "";
            }
        }
        //---------------------------------------------------------------
        //Кнопка для отправления сообщения 
        private void button1_Click(object sender, EventArgs e)
        {
            string getter;
            if (dataGridView1.SelectedRows.Count == 0)
            {
                Errors("Please, choose sender");
                return;
            }
            if (textBox2.Text == "" || textBox2.Text == " ")
                return;
            getter = dataGridView1.SelectedRows[0].Cells[0].Value.ToString();
            if (My_Reader.Nick == "")
                Errors("Nick is error");
            else
            {
                toolStripStatusLabel1.Text = null;
                string s;
                s = " 0 " + getter + ' ' + textBox2.Text;
                //Отображения посылаемого сообщения
                View_My_Message(s);
                cl.Send(s);
                textBox2.Text = "";
            }
        }
        //-------------------------------------------------------------
        //Отображение сообщений от других пользователей 
        public void View_Message(object o)
        {
            int n = 0;
            string s = o.ToString();
            string name_sender = Get_Name(s);
            for (int i = 0; i < friends.Count; i++)
                if (name_sender == friends[i].Name_Friend)
                {
                    int f, pos;
                    pos = s.Length;
                    s = s.Substring(3, pos - 3);
                    n = friends[i].Viewer.Text.Length;
                    friends[i].Viewer.AppendText(s);
                    f = name_sender.Length;
                    friends[i].Viewer.Select(n, f);
                    friends[i].Viewer.SelectionColor = Color.Red;
                    friends[i].Viewer.SelectionFont = new System.Drawing.Font("Courier New", 13);
                    friends[i].Viewer.AppendText("\r\n");
                    break;
                }
            if (current_friend.Name_Friend != name_sender)
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    if (dataGridView1.Rows[i].Cells[0].Value.ToString() == name_sender)
                        dataGridView1.Rows[i].Cells[0].Style.Font = font_bold;
        }
        //-------------------------------------------------------------
        //Отображение сообщений от меня 
        public void View_My_Message(object o)
        {
            int n = 0;
            if (current_friend != null)
            {
                int pos1, l_name;
                string s = o.ToString();
                pos1 = s.Length;
                l_name = Get_Name(s).Length;
                s = "              I" + s.Substring(3 + l_name, pos1 - 3 - l_name);
                n = current_friend.Viewer.Text.Length + 14;
                current_friend.Viewer.AppendText(s);
                current_friend.Viewer.Select(n, 1);
                current_friend.Viewer.SelectionColor = Color.Red;
                current_friend.Viewer.SelectionFont = new System.Drawing.Font("Courier New", 13);
                current_friend.Viewer.AppendText("\r\n");
            }
        }
        //-------------------------------------------------------------
        //Заполнение клиентов в список
        public void View_List(object o)
        {
            string[] ss;
            string s = o.ToString();
            List<Friend> fr_new = new List<Friend>();
            Friend temp_friend = null;
            ss = s.Split(' ');
            //Проходим по элементам старого списка
            foreach (string name in ss)
            {
                //Очищаем временную переменную 
                temp_friend = null;
                //Проходим по элементам старого списка
                foreach (Friend friend in friends)
                    //Находим друга в старом списке
                    if (name == friend.Name_Friend)
                    {
                        temp_friend = friend;
                        break;
                    }
                //Если в старом списке есть друг - переносим в новый
                if (temp_friend != null)
                    friends.Remove(temp_friend);
                else
                    temp_friend = new Friend(panel8, name);
                fr_new.Add(temp_friend);
            }
            friends = fr_new;
            if (dataGridView1.SelectedRows.Count > 0)
            {
                foreach (Friend friend in friends)
                    if (dataGridView1.SelectedRows[0].ToString() == friend.Name_Friend)
                        current_friend = friend;
            }
            //Обнуляем список перед заполнением
            dataGridView1.Rows.Clear();
            //Заполнение списка
            for (int i = 0; i < friends.Count; i++)
                dataGridView1.Rows.Add(friends[i].Name_Friend);
            //Выбираем прошлый элемент списка
            if (current_friend != null)
            {
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    if (current_friend.Name_Friend == dataGridView1.Rows[i].Cells[0].Value.ToString())
                    {
                        dataGridView1.Rows[i].Selected = true;
                        break;
                    }
            }
            else
                if (dataGridView1.Rows.Count > 0)
                    dataGridView1.Rows[0].Selected = true;
        }
        //-------------------------------------------------------------
        //вырезание имени из сообщения
        string Get_Name(string s)
        {
            int i = 3; string nm = "";
            while (s[i] != ' ')
            {
                nm = nm + s[i];
                i++;
            }
            return nm;
        }
        //-------------------------------------------------------------
        //Вырезание имени из сообщения
        string Get_Name1(string s)
        {
            String ss = s.Trim();
            return ss.Substring(0, ss.IndexOf(' ')); 
        }
        //-------------------------------------------------------------
        //Отлавливание события смены собеседника
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
                return;
            if (dataGridView1.SelectedRows.Count == 0)
                return;
            //Скрываем сообщения
            if (current_friend != null)
                current_friend.Viewer.Visible = false;
            foreach (Friend fr in friends)
                if (fr.Name_Friend == dataGridView1.SelectedRows[0].Cells[0].Value.ToString())
                {
                    current_friend = fr;
                    current_friend.Viewer.Visible = true;
                    dataGridView1.SelectedRows[0].Cells[0].Style.Font = font_normal;
                    break;
                }
        }
    }
}
