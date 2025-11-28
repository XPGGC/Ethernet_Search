using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public string jsonData = "";

        public bool isRemove = false;
        public bool isApply = false;

        //配置指令集
        private Dictionary<string, string> COMMANDS = new Dictionary<string, string>
        {
            { "", ""},
            //{ "修改4G参数", "{\r\n \"parameterInfo\" : {\r\n \"4G\" : [ {\r\n \"interfaceName\" : \"4G1\",\r\n \"interfacePar\" : {\r\n \"content\" : \"\",\r\n \"username\" : \"\",\r\n \"password\" : \"\",\r\n \"auth\" : 3,\r\n \"ethNetworkSegment\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n说明\r\n在线修改声明，值为1时表示实时生效，\r\n否则修改会失效\r\n25\r\n},\r\n \"parameter\" : \"4G\",\r\n \"messageId\" : \"1718776952659\"\r\n }" },
            { "修改Ethernet参数", "{\r\n \"parameterInfo\" : {\r\n \"Ethernet\" : [ {\r\n \"interfaceName\" : \"Ethernet1\",\r\n \"interfacePar\" : {\r\n \"dhcp\" : 0,\r\n \"ip\" : \"192.168.0.158\",\r\n \"subnetMask\" : \"255.255.255.0\",\r\n \"mac\" : \"02:00:00:32:A1:91\",\r\n \"dns\" : \"114.114.114.114\",\r\n \"dns2\" : \"8.8.8.8\",\r\n \"ntp\" : \"ntp.ntsc.ac.cn\",\r\n \"gateway\" : \"192.168.0.1\"\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"Ethernet\",\r\n \"messageId\" : \"1718780046192\"\r\n }\r\n" },
            //{ "修改WiFi参数", "{\r\n }\r\n \"parameterInfo\" : {\r\n \"onlineModification\" : 1,\r\n \"WiFi\" : [ {\r\n \"interfaceName\" : \"WiFi1\",\r\n \"workMode\" : 0,\r\n \"interfacePar\" : {\r\n \"dhcp\" : 0,\r\n \"ip\" : \"192.168.0.41\",\r\n \"subnetMask\" : \"255.255.255.0\",\r\n \"mac\" : \"FF:FF:FF:FF:FF:FF\",\r\n \"dns\" : \"114.114.114.114\",\r\n \"dns2\" : \"8.8.8.8\",\r\n \"ntp\" : \"ntp.ntsc.ac.cn\",\r\n \"gateway\" : \"192.168.0.1\",\r\n \"username\" : \"yunkenceshi\",\r\n \"password\" : \"yk86557810...\",\r\n \"distributionNetworkEnabled\" : 0\r\n }\r\n } ]\r\n },\r\n \"parameter\" : \"WiFi\",\r\n \"messageId\" : \"1718883684255\"\r\n"},
            { "修改COM口参数", "{\r\n \"parameterInfo\" : {\r\n \"COM\": [ {\r\n \"interfaceName\" : \"COM2\",\r\n \"workMode\" : 0,\r\n \"interfacePar\" : {\r\n \"frameBreakTime\" : 0,\r\n \"baudRate\" : 9600,\r\n \"dataBits\" : 8,\r\n \"stopBits\" : 1,\r\n \"parity\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"COM\",\r\n \"messageId\" : \"1718768583565\"\r\n }"},
        };

        public Form4()
        {
            InitializeComponent();
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            isRemove = true;
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

            //将 JSON 字符串解析为 JObject
            JObject jsonObj = JObject.Parse(COMMANDS["修改Ethernet参数"]);

            //更新 JSON 对象中的参数值
            jsonObj["parameterInfo"]["Ethernet"][0]["interfaceName"] = interfaceName;
            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["dhcp"] = dhcp;
            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["ip"] = ip;
            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["subnetMask"] = subnetMask;
            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["mac"] = mac;
            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["dns"] = dns;
            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["dns2"] = dns2;
            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["ntp"] = ntp;
            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["gateway"] = gateway;

            //将更新后的 JObject 转回 JSON 字符串
            jsonData = jsonObj.ToString(Formatting.None);

            isApply = true;
            isRemove = true;
        }
    }
}
