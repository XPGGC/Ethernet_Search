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
    public partial class Form2 : Form
    {   
        public String usedComBaudRate = null;
        public String usedComDataBit = null;
        public String usedComStopBit = null;
        public String usedComParity = null;

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

            comStopBits.Add("0.5");
            comStopBits.Add("1");
            comStopBits.Add("1.5");
            comStopBits.Add("2");

            comParity.Add("None");
            comParity.Add("Odd");
            comParity.Add("Even");

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
            usedComBaudRate = uiComboBox1.Text;
            usedComDataBit = uiComboBox2.Text;
            usedComStopBit = uiComboBox3.Text;
            usedComParity = uiComboBox4.Text;

            Form2.ActiveForm.Close();
        }
    }
}
