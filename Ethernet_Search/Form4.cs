using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Ethernet_Search
{
    public partial class Form4 : Form
    {
        public string interfaceName = "";
        public string dhcp = "";
        public string ip = "";
        public string subnetMask = "";
        public string mac = "";
        public string dns = "";
        public string dns2 = "";
        public string ntp = "";
        public string gateway = "";


        public Form4()
        {
            InitializeComponent();
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            Form4.ActiveForm.Close();
        }

        private void uiButton1_Click(object sender, EventArgs e)
        {
            interfaceName = textBox1.Text;
            dhcp = textBox2.Text;
            ip = textBox3.Text;
            subnetMask = textBox4.Text;
            mac = textBox5.Text;
            dns = textBox6.Text;
            dns2 = textBox7.Text;
            ntp = textBox8.Text;
            gateway = textBox9.Text;

            DialogResult = DialogResult.OK; // 设置对话框结果
            Close();
        }
    }
}
