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
        public  string Com { get; private set; }
        public int BaudRate { get; private set; }
        public int DataBits { get; private set; }
        public StopBits StopBits { get; private set; }
        public Parity Parity { get; private set; }

        private List<int> comBaudRate = new List<int>();
        private List<int> comDataBits = new List<int>();
        private List<StopBits> comStopBits = new List<StopBits>();
        private List<Parity> comParity = new List<Parity>();
        private List<string> coms = new List<string>();

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
            coms.Add("COM1");
            coms.Add("COM2");

            comBaudRate.Add(1200);
            comBaudRate.Add(2400);
            comBaudRate.Add(4800);
            comBaudRate.Add(9600);
            comBaudRate.Add(19200);
            comBaudRate.Add(38400);
            comBaudRate.Add(57600);

            comDataBits.Add(7);
            comDataBits.Add(8);

            comStopBits.Add(StopBits.One);
            comStopBits.Add(StopBits.Two);

            comParity.Add(Parity.None);
            comParity.Add(Parity.Odd);
            comParity.Add(Parity.Even);

            uiComboBox5.DataSource = coms;
            uiComboBox1.DataSource = comBaudRate;
            uiComboBox2.DataSource = comDataBits;
            uiComboBox3.DataSource = comStopBits;
            uiComboBox4.DataSource = comParity;

            Com = "COM1";
            BaudRate = 1200;
            DataBits = 7;
            StopBits = StopBits.None;
            Parity = Parity.None;
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            Form2.ActiveForm.Close();
        }

        private void uiButton1_Click(object sender, EventArgs e)
        {
            Com = (string)uiComboBox5.SelectedItem;
            BaudRate = (int)uiComboBox1.SelectedItem;
            DataBits = (int)uiComboBox2.SelectedItem;
            StopBits = (StopBits)uiComboBox3.SelectedItem;
            Parity = (Parity)uiComboBox4.SelectedItem;

            DialogResult = DialogResult.OK; // 设置对话框结果
            Close();
        }
    }
}
