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
    public partial class Form2 : Form
    {
        public int BaudRate { get; private set; }
        public int DataBits { get; private set; }
        public StopBits StopBits { get; private set; }
        public Parity Parity { get; private set; }

        private List<string> comBaudRate = new List<string>();
        private List<string> comDataBits = new List<string>();
        private List<string> comStopBits = new List<string>();
        private List<string> comParity = new List<string>();

        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            comConfig(null, null);
        }

        private void comConfig(object sender, EventArgs e)
        { 
            comBaudRate.Clear();
            comBaudRate.Add("1200");
            comBaudRate.Add("2400");
            comBaudRate.Add("4800");
            comBaudRate.Add("9600");
            comBaudRate.Add("14400");

            comDataBits.Add("7");
            comDataBits.Add("8");

            comStopBits.Add("None");
            comStopBits.Add("One");
            comStopBits.Add("Two");
            comStopBits.Add("OnePointFive");

            comParity.Add("None");
            comParity.Add("Odd");
            comParity.Add("Even");
            comParity.Add("Mark");
            comParity.Add("Space");

            uiComboBox1.DataSource = comBaudRate;
            uiComboBox2.DataSource = comDataBits;
            uiComboBox3.DataSource = comStopBits;
            uiComboBox4.DataSource = comParity;
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            Form2.ActiveForm.Close();
        }

        private void uiButton1_Click(object sender, EventArgs e)
        {
            BaudRate = uiComboBox1.SelectedIndex;
            DataBits = uiComboBox2.SelectedIndex;
            StopBits = (StopBits)uiComboBox3.SelectedIndex;
            Parity = (Parity)uiComboBox4.SelectedIndex;
            
            DialogResult = DialogResult.OK; // 设置对话框结果
            Close();
        }
    }
}
