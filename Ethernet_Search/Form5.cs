using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ethernet_Search
{
    public partial class Form5 : Form
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
        public string username = "";
        public string password = "";
        public string distributionNetworkEnabled = "";

        public Form5()
        {
            InitializeComponent();
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
            username = textBox10.Text;
            password = textBox11.Text;
            distributionNetworkEnabled = textBox12.Text;

            DialogResult = DialogResult.OK; // 设置对话框结果
            Close();
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            Form5.ActiveForm.Close();
        }
    }
}
