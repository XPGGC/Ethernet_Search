using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public int BaudRate1 { get; private set; }
        public int DataBits1 { get; private set; }
        public StopBits StopBits1 { get; private set; }
        public Parity Parity1 { get; private set; }
        public int BaudRate2 { get; private set; }
        public int DataBits2 { get; private set; }
        public StopBits StopBits2 { get; private set; }
        public Parity Parity2 { get; private set; }
        public string jsonData = "";

        private List<int> comBaudRate = new List<int>();
        private List<int> comDataBits = new List<int>();
        private List<StopBits> comStopBits = new List<StopBits>();
        private List<Parity> comParity = new List<Parity>();

        public bool isApply = false;

        //配置指令集
        private Dictionary<string, string> COMMANDS = new Dictionary<string, string>
        {
            //{ "修改4G参数", "{\r\n \"parameterInfo\" : {\r\n \"4G\" : [ {\r\n \"interfaceName\" : \"4G1\",\r\n \"interfacePar\" : {\r\n \"content\" : \"\",\r\n \"username\" : \"\",\r\n \"password\" : \"\",\r\n \"auth\" : 3,\r\n \"ethNetworkSegment\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n说明\r\n在线修改声明，值为1时表示实时生效，\r\n否则修改会失效\r\n25\r\n},\r\n \"parameter\" : \"4G\",\r\n \"messageId\" : \"1718776952659\"\r\n }" },
            { "修改Ethernet参数", "{\r\n \"parameterInfo\" : {\r\n \"Ethernet\" : [ {\r\n \"interfaceName\" : \"Ethernet1\",\r\n \"interfacePar\" : {\r\n \"dhcp\" : 0,\r\n \"ip\" : \"192.168.0.158\",\r\n \"subnetMask\" : \"255.255.255.0\",\r\n \"mac\" : \"02:00:00:32:A1:91\",\r\n \"dns\" : \"114.114.114.114\",\r\n \"dns2\" : \"8.8.8.8\",\r\n \"ntp\" : \"ntp.ntsc.ac.cn\",\r\n \"gateway\" : \"192.168.0.1\"\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"Ethernet\",\r\n \"messageId\" : \"1718780046192\"\r\n }\r\n" },
            //{ "修改WiFi参数", "{\r\n }\r\n \"parameterInfo\" : {\r\n \"onlineModification\" : 1,\r\n \"WiFi\" : [ {\r\n \"interfaceName\" : \"WiFi1\",\r\n \"workMode\" : 0,\r\n \"interfacePar\" : {\r\n \"dhcp\" : 0,\r\n \"ip\" : \"192.168.0.41\",\r\n \"subnetMask\" : \"255.255.255.0\",\r\n \"mac\" : \"FF:FF:FF:FF:FF:FF\",\r\n \"dns\" : \"114.114.114.114\",\r\n \"dns2\" : \"8.8.8.8\",\r\n \"ntp\" : \"ntp.ntsc.ac.cn\",\r\n \"gateway\" : \"192.168.0.1\",\r\n \"username\" : \"yunkenceshi\",\r\n \"password\" : \"yk86557810...\",\r\n \"distributionNetworkEnabled\" : 0\r\n }\r\n } ]\r\n },\r\n \"parameter\" : \"WiFi\",\r\n \"messageId\" : \"1718883684255\"\r\n"},
            { "修改COM口参数", "{\r\n \"parameterInfo\" : {\r\n \"COM\": [ {\r\n \"interfaceName\" : \"COM2\",\r\n \"workMode\" : 0,\r\n \"interfacePar\" : {\r\n \"frameBreakTime\" : 0,\r\n \"baudRate\" : 9600,\r\n \"dataBits\" : 8,\r\n \"stopBits\" : 1,\r\n \"parity\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"COM\",\r\n \"messageId\" : \"1718768583565\"\r\n }"},
        };

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

            uiComboBox1.DataSource = comBaudRate;
            uiComboBox2.DataSource = comDataBits;
            uiComboBox3.DataSource = comStopBits;
            uiComboBox4.DataSource = comParity;

            uiComboBox9.DataSource = comBaudRate;
            uiComboBox8.DataSource = comDataBits;
            uiComboBox7.DataSource = comStopBits;
            uiComboBox6.DataSource = comParity;
        }

        private void uiButton1_Click(object sender, EventArgs e)
        {
            BaudRate1 = (int)uiComboBox1.SelectedItem;
            DataBits1 = (int)uiComboBox2.SelectedItem;
            StopBits1 = (StopBits)uiComboBox3.SelectedItem;
            Parity1 = (Parity)uiComboBox4.SelectedItem;

            //将 JSON 字符串解析为 JObject
            JObject jsonObj = JObject.Parse(COMMANDS["修改COM口参数"]);

            //更新 JSON 对象中的参数值
            jsonObj["parameterInfo"]["COM"][0]["interfaceName"] = "COM1";
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["baudRate"] = BaudRate1;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["dataBits"] = DataBits1;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["stopBits"] = (int)StopBits1;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["parity"] = (int)Parity1;

            //将更新后的 JObject 转回 JSON 字符串
            jsonData = jsonObj.ToString(Formatting.None);

            isApply = true;
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            BaudRate2 = (int)uiComboBox9.SelectedItem;
            DataBits2 = (int)uiComboBox8.SelectedItem;
            StopBits2 = (StopBits)uiComboBox7.SelectedItem;
            Parity2 = (Parity)uiComboBox6.SelectedItem;

            //将 JSON 字符串解析为 JObject
            JObject jsonObj = JObject.Parse(COMMANDS["修改COM口参数"]);

            //更新 JSON 对象中的参数值
            jsonObj["parameterInfo"]["COM"][0]["interfaceName"] = "COM2";
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["baudRate"] = BaudRate2;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["dataBits"] = DataBits2;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["stopBits"] = (int)StopBits2;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["parity"] = (int)Parity2;

            //将更新后的 JObject 转回 JSON 字符串
            jsonData = jsonObj.ToString(Formatting.None);

            isApply = true;
        }
    }
}
