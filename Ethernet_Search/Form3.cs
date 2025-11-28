using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ethernet_Search
{
    public partial class Form3 : Form
    {
        public string interfaceName = "";
        public string content = "";
        public string username = "";
        public string password = "";
        public string auth = "";
        public string ethNetworkSegment = "";

        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            Form3.ActiveForm.Close();
        }

        private void uiButton1_Click(object sender, EventArgs e)
        {
            interfaceName = textBox1.Text;
            content = textBox2.Text;
            username = textBox3.Text;
            password = textBox4.Text;
            auth = textBox5.Text;
            ethNetworkSegment = textBox6.Text;

            DialogResult = DialogResult.OK; // 设置对话框结果
            Close();
        }
    }
}
