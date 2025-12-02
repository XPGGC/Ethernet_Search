using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Ethernet_Search
{   
    public partial class Form4 : Form
    {
        public string ip1 = "";
        public string subnetMask1 = "";
        public string dns1 = "";
        public string dns21 = "";
        public string gateway1 = "";

        public string ip2 = "";
        public string subnetMask2 = "";
        public string dns2 = "";
        public string dns22 = "";
        public string gateway2 = "";

        public string jsonData = "";

        public bool isApply = false;

        //配置指令集
        private Dictionary<string, string> COMMANDS = new Dictionary<string, string>
        {
            //{ "修改4G参数", "{\r\n \"parameterInfo\" : {\r\n \"4G\" : [ {\r\n \"interfaceName\" : \"4G1\",\r\n \"interfacePar\" : {\r\n \"content\" : \"\",\r\n \"username\" : \"\",\r\n \"password\" : \"\",\r\n \"auth\" : 3,\r\n \"ethNetworkSegment\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n说明\r\n在线修改声明，值为1时表示实时生效，\r\n否则修改会失效\r\n25\r\n},\r\n \"parameter\" : \"4G\",\r\n \"messageId\" : \"1718776952659\"\r\n }" },
            { "修改Ethernet参数", "{\r\n \"parameterInfo\" : {\r\n \"Ethernet\" : [ {\r\n \"interfaceName\" : \"Ethernet1\",\r\n \"interfacePar\" : {\r\n \"dhcp\" : 0,\r\n \"ip\" : \"192.168.0.158\",\r\n \"subnetMask\" : \"255.255.255.0\",\r\n \"mac\" : \"02:00:00:32:A1:91\",\r\n \"dns\" : \"114.114.114.114\",\r\n \"dns2\" : \"8.8.8.8\",\r\n \"ntp\" : \"ntp.ntsc.ac.cn\",\r\n \"gateway\" : \"192.168.0.1\"\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"Ethernet\",\r\n \"messageId\" : \"1718780046192\"\r\n }\r\n" },
            //{ "修改WiFi参数", "{\r\n }\r\n \"parameterInfo\" : {\r\n \"onlineModification\" : 1,\r\n \"WiFi\" : [ {\r\n \"interfaceName\" : \"WiFi1\",\r\n \"workMode\" : 0,\r\n \"interfacePar\" : {\r\n \"dhcp\" : 0,\r\n \"ip\" : \"192.168.0.41\",\r\n \"subnetMask\" : \"255.255.255.0\",\r\n \"mac\" : \"FF:FF:FF:FF:FF:FF\",\r\n \"dns\" : \"114.114.114.114\",\r\n \"dns2\" : \"8.8.8.8\",\r\n \"ntp\" : \"ntp.ntsc.ac.cn\",\r\n \"gateway\" : \"192.168.0.1\",\r\n \"username\" : \"yunkenceshi\",\r\n \"password\" : \"yk86557810...\",\r\n \"distributionNetworkEnabled\" : 0\r\n }\r\n } ]\r\n },\r\n \"parameter\" : \"WiFi\",\r\n \"messageId\" : \"1718883684255\"\r\n"},
            { "修改COM口参数", "{\r\n \"parameterInfo\" : {\r\n \"COM\": [ {\r\n \"interfaceName\" : \"COM2\",\r\n \"workMode\" : 0,\r\n \"interfacePar\" : {\r\n \"frameBreakTime\" : 0,\r\n \"baudRate\" : 9600,\r\n \"dataBits\" : 8,\r\n \"stopBits\" : 1,\r\n \"parity\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"COM\",\r\n \"messageId\" : \"1718768583565\"\r\n }"},
        };

        public Form4()
        {
            InitializeComponent();
        }

        private void uiButton1_Click(object sender, EventArgs e)
        {
            ip1 = uiTextBox3.Text;
            subnetMask1 = uiTextBox4.Text;
            dns1 = uiTextBox6.Text;
            dns21 = uiTextBox7.Text;
            gateway1 = uiTextBox9.Text;

            //将 JSON 字符串解析为 JObject
            JObject jsonObj = JObject.Parse(COMMANDS["修改Ethernet参数"]);

            //更新 JSON 对象中的参数值
            jsonObj["parameterInfo"]["Ethernet"][0]["interfaceName"] = "Ethernet1";
            if (uiTextBox3.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["ip"] = ip1;
            if (uiTextBox4.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["subnetMask"] = subnetMask1;
            if (uiTextBox6.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["dns"] = dns1;
            if (uiTextBox7.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["dns2"] = dns21;
            if (uiTextBox9.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["gateway"] = gateway1;

            //将更新后的 JObject 转回 JSON 字符串
            jsonData = jsonObj.ToString(Formatting.None);

            isApply = true;
        }
    }
}
