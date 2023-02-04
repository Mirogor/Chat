using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TCP_Client_Form
{
    class Friend
    {
        //Имя клиента для общения
        string name_friend = null;
        public string Name_Friend { get { return name_friend; } }
        //Элемент для вывода сообщений
        RichTextBox viewer = null;
        public RichTextBox Viewer { get { return viewer; } }
        //Список клиентов
        List<Friend> clients = new List<Friend>();
        //Конструктор
        public Friend(Panel panel, string name)
        {
            this.name_friend = name;
            viewer = new RichTextBox();
            viewer.Parent = panel;
            viewer.Dock = DockStyle.Fill;
            viewer.Visible = false;
            viewer.Font = new System.Drawing.Font("Courier New", 11);
        }
    }
}
