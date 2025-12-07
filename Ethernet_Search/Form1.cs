using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sunny.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Ethernet_Search
{
    public partial class Form1 : Form
    {
        // 连接模式枚举
        private enum ConnectionMode
        {
            None,      // 未连接
            Ethernet,  // 网卡连接
        }

        private string targetDeviceIP = "";  // 目标设备IP

        public string jsonData = ""; // 要发送的JSON数据

        private const string readCom = "{\r\n \"messageId\":\"1718711447026\",\r\n \"parameter\":\"COM\"\r\n }";
        private const string readEthernet = "{\r\n  \"messageId\" : \"1718711447026\",\r\n \"parameter\" : \"Ethernet\"\r\n }\r\n";
        private const string enterConfig = "{\r\n \"messageId\" : \"1718794950506\",\r\n \"commandPar\" : {\r\n \"remoteConfigMode\" : 1\r\n }\r\n }";
        private const string exitConfig = "{\r\n \"messageId\" : \"1718794950506\",\r\n \"commandPar\" : {\r\n \"remoteConfigMode\" : 0\r\n }\r\n }";

        private string ip1 = "";
        private string subnetMask1 = "";
        private string gateway1 = "";

        private int frameBreakTime1;
        private int BaudRate1;
        private int DataBits1;
        private StopBits StopBits1;
        private Parity Parity1;
        private int frameBreakTime2;
        private int BaudRate2;
        private int DataBits2;
        private StopBits StopBits2;
        private Parity Parity2;

        private List<string> frameBreakTimeList = new List<string>();
        private List<int> comBaudRate = new List<int>();
        private List<int> comDataBits = new List<int>();
        private List<int> comStopBits = new List<int>();
        private List<Parity> comParity = new List<Parity>();

        //COM参数映射
        private Dictionary<string, string> frameBreakTimeMap = new Dictionary<string, string>
        {
            { "自动", "0" },
            { "20", "1" },
            { "30", "2" },
            { "50", "4" },
            { "100", "5" },
        };

        private Dictionary<string, string> parityMap = new Dictionary<string, string>
        {
            { "None", "0" },
            { "Odd", "1" },
            { "Even", "2" },
        };

        //查询命令计时器
        private System.Windows.Forms.Timer commandTimer = null;
        private List<string> commandSequence = null;
        private int commandSendIndex = 0;
        private int expectedReplies = 0;
        private int receivedReplies = 0;
        private bool sequenceActive = false;
        private readonly object seqLock = new object();

        private List<string> nowIps = new List<string>();
        private List<string> nowSubnetMasks = new List<string>();
        private List<string> nowGateways = new List<string>();

        //选择网关
        private int selectDevice = -1;

        //目标设备IP与本地网卡IP不在同一网段标志
        private bool noSameNet = false;

        //选择计时器
        private System.Windows.Forms.Timer selectTimer = null;

        //配置指令集
        private Dictionary<string, string> COMMANDS = new Dictionary<string, string>
        {
            { "修改Ethernet参数", "{\r\n \"parameterInfo\" : {\r\n \"Ethernet\" : [ {\r\n \"interfaceName\" : \"Ethernet1\",\r\n \"interfacePar\" : {\r\n \"dhcp\" : 0,\r\n \"ip\" : \"192.168.0.158\",\r\n \"subnetMask\" : \"255.255.255.0\",\r\n \"mac\" : \"02:00:00:32:A1:91\",\r\n \"dns\" : \"114.114.114.114\",\r\n \"dns2\" : \"8.8.8.8\",\r\n \"ntp\" : \"ntp.ntsc.ac.cn\",\r\n \"gateway\" : \"192.168.0.1\"\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"Ethernet\",\r\n \"messageId\" : \"1718780046192\"\r\n }\r\n" },
            { "修改COM口参数", "{\r\n \"parameterInfo\" : {\r\n \"COM\": [ {\r\n \"interfaceName\" : \"COM2\",\r\n \"workMode\" : 0,\r\n \"interfacePar\" : {\r\n \"frameBreakTime\" : 0,\r\n \"baudRate\" : 9600,\r\n \"dataBits\" : 8,\r\n \"stopBits\" : 1,\r\n \"parity\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"COM\",\r\n \"messageId\" : \"1718768583565\"\r\n }"},
        };

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Form1Search_Load();
        }
        
        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static string GetTimeStamp() 
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }

        private void uiButton1_Click(object sender, EventArgs e)
        {
            try
            {
                uiTextBox3.Text = "";
                uiTextBox4.Text = "";
                uiTextBox9.Text = "";

                uiComboBox10.Text = "";
                uiComboBox11.Text = "";
                uiComboBox5.Text = "";
                uiComboBox3.Text = "";
                uiComboBox2.Text = "";

                uiComboBox12.Text = "";
                uiComboBox9.Text = "";
                uiComboBox8.Text = "";
                uiComboBox7.Text = "";
                uiComboBox6.Text = "";

                uiDataGridView1.ClearRows();

                selectDevice = -1;
                // 按序发送指令
                commandSequence = new List<string>
                {
                    "{\"messageId\":\"1764663895676\",\"parameter\":\"information\"}"
                };
                // 启动按序发送指令的序列
                StartSendSequence();

                if (selectTimer == null)
                {
                    selectTimer = new System.Windows.Forms.Timer();
                    selectTimer.Interval = 100;
                    selectTimer.Tick += needSelectDevice;
                }
                selectTimer.Start();
            }
            catch (Exception ex)
            {
                //UIMessageDialog.ShowInfoDialog(this, "请检查选中的网卡IP", UIStyle.Gray);
            }
        }

        private void needSelectDevice(object sender, EventArgs e)
        {
            try
            {
                if (selectDevice == -1)
                {
                    uiTextBox3.ReadOnly = true;
                    uiTextBox4.ReadOnly = true;
                    uiTextBox9.ReadOnly = true;
                    uiComboBox11.Enabled = false;
                    uiComboBox5.Enabled = false;
                    uiComboBox3.Enabled = false;
                    uiComboBox2.Enabled = false;
                    uiComboBox10.Enabled = false;
                    uiComboBox9.Enabled = false;
                    uiComboBox8.Enabled = false;
                    uiComboBox7.Enabled = false;
                    uiComboBox6.Enabled = false;
                    uiComboBox12.Enabled = false;
                    uiButton2.Enabled = false;
                    uiButton3.Enabled = false;
                    uiButton4.Enabled = false;
                    uiButton5.Enabled = false;
                    uiButton6.Enabled = false;
                    uiButton7.Enabled = false;
                }
                else
                {
                    if (noSameNet)
                    {
                        uiTextBox3.ReadOnly = false;
                        uiTextBox4.ReadOnly = false;
                        uiTextBox9.ReadOnly = false;
                        uiButton2.Enabled = true;
                    }
                    else if (!noSameNet)
                    {
                        uiTextBox3.ReadOnly = false;
                        uiTextBox4.ReadOnly = false;
                        uiTextBox9.ReadOnly = false;
                        uiComboBox11.Enabled = true;
                        uiComboBox5.Enabled = true;
                        uiComboBox3.Enabled = true;
                        uiComboBox2.Enabled = true;
                        uiComboBox10.Enabled = true;
                        uiComboBox9.Enabled = true;
                        uiComboBox8.Enabled = true;
                        uiComboBox7.Enabled = true;
                        uiComboBox6.Enabled = true;
                        uiComboBox12.Enabled = true;
                        uiButton2.Enabled = true;
                        uiButton3.Enabled = true;
                        uiButton4.Enabled = true;
                        uiButton5.Enabled = true;
                        uiButton6.Enabled = true;
                        uiButton7.Enabled = true;

                        selectTimer.Stop();
                    }
                }
            }
            catch (Exception ex)
            { }
        }

        #region//网口搜索
        UdpClient client_ST02 = null;
        IPEndPoint endpoint_ST02 = null;
        Dictionary<string, string> IPinfo = new Dictionary<string, string>();
        //搜索电脑网卡
        private void Form1Search_Load()
        {
            comConfig(null, null);

            IPinfo.Clear();
            List<String> typeList = new List<string>();
            List<String> typeListIP = new List<string>();
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {                
                string sdf = adapter.ToString();
                string name = adapter.Name;
                //获取IPInterfaceProperties实例  
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                typeList.Add(name);
                string ipxinxi = "";
                // 获取该接口上的所有IP地址
                foreach (UnicastIPAddressInformation ipAddressInfo in adapterProperties.UnicastAddresses)
                {
                    // 过滤掉IPv6地址和非本地链接地址
                    if (ipAddressInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ipAddressInfo.Address))
                    {
                        string ip = ipAddressInfo.Address.ToString();
                        if (ipxinxi == "")
                        {
                            ipxinxi = ip;
                        }
                        else
                        {
                            ipxinxi = ipxinxi + "|" + ip;
                        }
                    }
                }
                if (ipxinxi == "")
                {
                    ipxinxi = "127.0.0.1";
                }
                IPinfo.Add(name, ipxinxi);
            }

            uiComboBox4.DataSource = typeList;
            string moren = typeList[0].ToString();
            string morenips1 = IPinfo[moren].ToString();
            string[] morenips = morenips1.Split('|');
            for (int i = 0; i < morenips.Count(); i++)
            {
                typeListIP.Add(morenips[i]);
            }
            
            uiComboBox1.DataSource = typeListIP;
        }

        static bool IsUdpcRecvStart_ST02 = false;

        static UdpClient udpcRecv_ST02 = null;

        static IPEndPoint localIpep_ST02 = null;

        static Thread thrRecv_ST02;

        private void ST02_udp2020()
        {

            try
            {
                if (!IsUdpcRecvStart_ST02) // 未监听的情况，开始监听
                {
                    //string SearchIp = uiComboBox1.Text;
                    // 1,创建socket
                    //udpServer_ST02 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    ////2,绑定ip跟端口号
                    //udpServer_ST02.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2020));

                    ////3，接收数据
                    //new Thread(ReceiveMessage_ST02) { IsBackground = true }.Start();
                    //IsUdpcRecvStart_ST02 = true;

                    localIpep_ST02 = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2020); // 本机IP和监听端口号
                    udpcRecv_ST02 = new UdpClient(localIpep_ST02);
                    thrRecv_ST02 = new Thread(ReceiveMessage_ST021);
                    thrRecv_ST02.Start();
                    IsUdpcRecvStart_ST02 = true;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("端口被占用");
            }
        }

        // 接收端口信息
        private void ReceiveMessage_ST021(object obj)
        {
            while (IsUdpcRecvStart_ST02)
            {
                try
                {
                    byte[] bytRecv = udpcRecv_ST02.Receive(ref localIpep_ST02);
                    string receiveMessage = Encoding.UTF8.GetString(bytRecv, 0, bytRecv.Length);

                    Invoke(new Action(() =>
                    {
                        if (string.IsNullOrWhiteSpace(receiveMessage))
                           return;

                        JObject information = null;

                        try
                        {
                            //if (!sequenceActive)
                            //    return;

                            JObject jo = JObject.Parse(receiveMessage);


                            // 接收信息的分类判断
                            if (jo["parameterInfo"]?["information"] is JObject parameterInfo)
                            {
                                information = parameterInfo;
                            }
                            else if (jo["information"] is JObject info)
                            {
                                information = info;
                            }

                            if ((string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["baudRate"] != null) uiComboBox11.Text = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["baudRate"];
                            if ((string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["dataBits"] != null) uiComboBox5.Text = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["dataBits"];
                            if ((string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["stopBits"] != null) uiComboBox3.Text = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["stopBits"];
                            if ((string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["parity"] != null) uiComboBox2.Text = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["parity"];
                            if ((string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["frameBreakTime"] != null) uiComboBox10.Text = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["frameBreakTime"];

                            if ((string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["baudRate"] != null) uiComboBox9.Text = (string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["baudRate"];
                            if ((string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["dataBits"] != null) uiComboBox8.Text = (string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["dataBits"];
                            if ((string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["stopBits"] != null) uiComboBox7.Text = (string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["stopBits"];
                            if ((string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["parity"] != null) uiComboBox6.Text = (string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["parity"];
                            if ((string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["frameBreakTime"] != null) uiComboBox12.Text = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["frameBreakTime"];

                            if ((string)jo["parameterInfo"]?["Ethernet"]?[0]?["interfacePar"]?["ip"] != null) uiTextBox3.Text = (string)jo["parameterInfo"]?["Ethernet"]?[0]?["interfacePar"]?["ip"];
                            if ((string)jo["parameterInfo"]?["Ethernet"]?[0]?["interfacePar"]?["subnetMask"] != null) uiTextBox4.Text = (string)jo["parameterInfo"]?["Ethernet"]?[0]?["interfacePar"]?["subnetMask"];
                            if ((string)jo["parameterInfo"]?["Ethernet"]?[0]?["interfacePar"]?["gateway"] != null) uiTextBox9.Text = (string)jo["parameterInfo"]?["Ethernet"]?[0]?["interfacePar"]?["gateway"];

                            if (information == null)
                                return;
                          
                            // 将读取到的网关信息进行显示
                            if (uiDataGridView1.Rows.GetRowCount(DataGridViewElementStates.None) == 0)
                            {
                                int index = uiDataGridView1.Rows.Add();
                                DataGridViewRow row = uiDataGridView1.Rows[index];
                                row.Cells[0].Value = index;
                                row.Cells[1].Value = (string)information["product"]?["name"];
                                row.Cells[2].Value = (string)information["product"]?["model"];
                                row.Cells[3].Value = (string)information["product"]?["sn"];
                                row.Cells[4].Value = (string)(information["Ethernet1"]?["ip"] ?? information["localIp"]?["ip"]);
                                row.Cells[5].Value = (string)information["product"]?["version"];
                                row.Cells[6].Value = (string)information["product"]?["alias"];

                                string nowIp = (string)(information["Ethernet1"]?["ip"] ?? information["localIp"]?["ip"]);
                                string nowSubnetMask = (string)(information["Ethernet1"]?["subnetMask"] ?? information["localIp"]?["subnetMask"]);
                                string nowGateway = (string)(information["Ethernet1"]?["gateway"] ?? information["localIp"]?["gateway"]);

                                nowIps.Add(nowIp);
                                nowSubnetMasks.Add(nowSubnetMask);
                                nowGateways.Add(nowGateway);
                            }

                            foreach (DataGridViewRow existedRow in uiDataGridView1.Rows)
                            {
                                if (existedRow.IsNewRow)
                                    continue;

                                if (existedRow.Cells[4].Value.ToString() == (string)(information["Ethernet1"]?["ip"] ?? information["localIp"]?["ip"]))
                                {
                                    continue;
                                }
                                else
                                {
                                    int newindex = uiDataGridView1.Rows.Add();
                                    DataGridViewRow newrow = uiDataGridView1.Rows[newindex];
                                    newrow.Cells[0].Value = newindex;
                                    newrow.Cells[1].Value = (string)information["product"]?["name"];
                                    newrow.Cells[2].Value = (string)information["product"]?["model"];
                                    newrow.Cells[3].Value = (string)information["product"]?["sn"];
                                    newrow.Cells[4].Value = (string)(information["Ethernet1"]?["ip"] ?? information["localIp"]?["ip"]);
                                    newrow.Cells[5].Value = (string)information["product"]?["version"];
                                    newrow.Cells[6].Value = (string)information["product"]?["alias"];

                                    string nowIp = (string)(information["Ethernet1"]?["ip"] ?? information["localIp"]?["ip"]);
                                    string nowSubnetMask = (string)(information["Ethernet1"]?["subnetMask"] ?? information["localIp"]?["subnetMask"]);
                                    string nowGateway = (string)(information["Ethernet1"]?["gateway"] ?? information["localIp"]?["gateway"]);

                                    nowIps.Add(nowIp);
                                    nowSubnetMasks.Add(nowSubnetMask);
                                    nowGateways.Add(nowGateway);
                                }
                            }
                        }
                        catch
                        {
                            // JSON解析失败，忽略
                        }
                    }));
                 }
                catch (Exception ex)
                {
                }
            }
        }
        #endregion

        private void uiComboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            string name = uiComboBox4.Text;
            List<String> typeListIP = new List<string>();
            string morenips1 = IPinfo[name].ToString();
            string[] morenips = morenips1.Split('|');
            for (int i = 0; i < morenips.Count(); i++)
            {
                typeListIP.Add(morenips[i]);
            }
            uiComboBox1.DataSource = typeListIP;
        }

        // 通过网卡发送数据
        private void SendDataViaEthernet(byte[] payload)
        {
            try
            {
                // 检查是否有选中的设备
                if (uiDataGridView1.SelectedRows.Count > 0)
                {
                    // 获取选中设备的IP地址
                    DataGridViewRow selectedRow = uiDataGridView1.SelectedRows[selectDevice];
                    if (selectedRow.Cells[4].Value != null)
                    {
                        targetDeviceIP = selectedRow.Cells[4].Value.ToString();
                    }
                }

                // 如果没有选中设备或IP为空
                if (string.IsNullOrEmpty(targetDeviceIP))
                {
                    MessageBox.Show("没有选中设备或IP为空");
                }
                else
                {
                    // 发送到指定设备IP
                    if (client_ST02 == null)
                    {
                        string SearchIp = uiComboBox1.Text;
                        client_ST02 = new UdpClient(new IPEndPoint(Dns.GetHostAddresses(SearchIp)[0], 0));
                    }

                    IPEndPoint targetEndpoint = new IPEndPoint(IPAddress.Parse(targetDeviceIP), 2020);
                    client_ST02.Send(payload, payload.Length, targetEndpoint);
                }

            }
            catch (Exception ex)
            {
                throw new Exception("UDP发送失败：" + ex.Message);
            }
        }

        private void comConfig(object sender, EventArgs e)
        {
            frameBreakTimeList.Add("自动");
            frameBreakTimeList.Add("20");
            frameBreakTimeList.Add("30");
            frameBreakTimeList.Add("50");
            frameBreakTimeList.Add("100");

            comBaudRate.Add(1200);
            comBaudRate.Add(2400);
            comBaudRate.Add(4800);
            comBaudRate.Add(9600);
            comBaudRate.Add(19200);
            comBaudRate.Add(38400);
            comBaudRate.Add(57600);

            comDataBits.Add(7);
            comDataBits.Add(8);

            comStopBits.Add(1);
            comStopBits.Add(2);

            comParity.Add(Parity.None);
            comParity.Add(Parity.Odd);
            comParity.Add(Parity.Even);

            uiComboBox10.DataSource = frameBreakTimeList;
            uiComboBox11.DataSource = comBaudRate;
            uiComboBox5.DataSource = comDataBits;
            uiComboBox3.DataSource = comStopBits;
            uiComboBox2.DataSource = comParity;

            uiComboBox12.DataSource = frameBreakTimeList;
            uiComboBox9.DataSource = comBaudRate;
            uiComboBox8.DataSource = comDataBits;
            uiComboBox7.DataSource = comStopBits;
            uiComboBox6.DataSource = comParity;

            uiComboBox10.Text = "";
            uiComboBox11.Text = "";
            uiComboBox5.Text = "";
            uiComboBox3.Text = "";
            uiComboBox2.Text = "";

            uiComboBox12.Text = "";
            uiComboBox9.Text = "";
            uiComboBox8.Text = "";
            uiComboBox7.Text = "";
            uiComboBox6.Text = "";
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            ip1 = uiTextBox3.Text;
            subnetMask1 = uiTextBox4.Text;
            gateway1 = uiTextBox9.Text;
            int com1 = int.Parse(uiTextBox1.Text);
            int com2 = int.Parse(uiTextBox2.Text);

            if (com1 < 0 | com1 > 65535) { MessageBox.Show("COM1端口需要设置在0~65535之间"); return; }
            if (com2 < 0 | com2 > 65535) { MessageBox.Show("COM2端口需要设置在0~65535之间"); return; }

            System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex(@"\b((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))\b");
            if (rx.IsMatch(ip1) == false) { MessageBox.Show("IP地址不合法，请重新输入"); return; }
            if (rx.IsMatch(subnetMask1) == false) { MessageBox.Show("子网掩码地址不合法，请重新输入"); return; }
            if (rx.IsMatch(gateway1) == false) { MessageBox.Show("默认地址不合法，请重新输入"); return; }

            //将 JSON 字符串解析为 JObject
            JObject jsonObj = JObject.Parse(COMMANDS["修改Ethernet参数"]);

            //更新 JSON 对象中的参数值
            jsonObj["parameterInfo"]["Ethernet"][0]["interfaceName"] = "Ethernet1";
            if (uiTextBox3.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["ip"] = ip1;
            if (uiTextBox4.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["subnetMask"] = subnetMask1;
            if (uiTextBox9.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["gateway"] = gateway1;

            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["COM1"] = com1;
            jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["COM2"] = com2;

            //将更新后的 JObject 转回 JSON 字符串
            jsonData = jsonObj.ToString(Formatting.None);

            try
            {
                if (selectDevice < 0 || selectDevice >= uiDataGridView1.Rows.Count)
                {
                    MessageBox.Show("未选择设备");
                    return;
                }

                // 获取目标设备当前 IP
                var selectedRow = uiDataGridView1.Rows[selectDevice];
                if (selectedRow.Cells[4].Value == null)
                {
                    MessageBox.Show("目标设备 IP 为空");
                    return;
                }
                string deviceIp = selectedRow.Cells[4].Value.ToString();

                targetDeviceIP = deviceIp;

                byte[] payload = Encoding.UTF8.GetBytes(jsonData);

                // 发送前：检查本地选中网卡与目标设备是否在同一网段
                string localIpStr = uiComboBox1.Text;
                if (string.IsNullOrWhiteSpace(localIpStr))
                {
                    MessageBox.Show("未选择本地网卡 IP");
                    return;
                }

                IPAddress localIp = Dns.GetHostAddresses(localIpStr).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                if (localIp == null)
                {
                    MessageBox.Show("无法解析本地网卡 IP");
                    return;
                }

                // 查找本地网卡、掩码和网关信息
                var adapter = GetNetworkInterfaceByLocalIp(localIp.ToString());
                if (adapter == null)
                {
                    MessageBox.Show("找不到对应网卡信息");
                    return;
                }

                var uni = adapter.GetIPProperties().UnicastAddresses.FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork && u.Address.ToString() == localIp.ToString());
                if (uni == null)
                {
                    MessageBox.Show("网卡未配置 IPv4 地址");
                    return;
                }

                IPAddress localMask = uni.IPv4Mask;
                IPAddress deviceIPAddress = IPAddress.Parse(deviceIp);

                bool sameSubnet = IsSameSubnet(localIp, localMask, deviceIPAddress);

                bool routeAdded = false;
                string routeTarget = deviceIp;
                try
                {

                    DialogResult result = MessageBox.Show("是否确认执行重启操作", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                    if (sameSubnet)
                    {
                        // 如果修改后 IP 与当前设备 IP 不一致，提示重启并从列表移除
                        if (uiTextBox3.Text != deviceIp)
                        {
                            MessageBox.Show("已修改网关IP地址，等待网关重启");
                            uiDataGridView1.Rows.RemoveAt(selectDevice);
                        }

                        // 直接使用当前本地 IP 发送
                        RebindUdpClient(localIp.ToString());

                        if (result == DialogResult.OK)
                        {
                            SendDataViaEthernet(payload);
                            return;
                        }
                        else if (result == DialogResult.Cancel)
                        {
                            return;
                        }
                    }

                    // 不在同一网段：读取并保存当前网卡配置，临时修改本机 IP 到目标网段，发送后还原
                    var original = GetWmiAdapterConfig(adapter);
                    if (original == null)
                    {
                        MessageBox.Show("无法读取网卡原始配置，取消操作");
                        return;
                    }

                    // 计算临时 IP（使用目标前三段 + 本机末段，避免与目标冲突）
                    string[] devOctets = deviceIp.Split('.');
                    string[] localOctets = localIp.ToString().Split('.');
                    int localLast;
                    if (!int.TryParse(localOctets[3], out localLast)) localLast = 200;
                    string tempIp = $"{devOctets[0]}.{devOctets[1]}.{devOctets[2]}.{localLast}";

                    // 若临时 IP 与设备冲突则尝试 +1..+10
                    if (tempIp == deviceIp)
                    {
                        for (int d = 1; d <= 10; d++)
                        {
                            int cand = (localLast + d) % 254;
                            if (cand == 0) cand = 1;
                            var c = $"{devOctets[0]}.{devOctets[1]}.{devOctets[2]}.{cand}";
                            if (c != deviceIp) { tempIp = c; break; }
                        }
                    }

                    // 使用用户输入网关
                    string useGateway = !string.IsNullOrWhiteSpace(gateway1) ? gateway1 :
                        (original.Gateways != null && original.Gateways.Length > 0 ? original.Gateways[0] : "0.0.0.0");

                    bool setOk = SetInterfaceStaticAddress(adapter.Name, tempIp, subnetMask1, useGateway);
                    if (!setOk)
                    {
                        MessageBox.Show("设置临时 IP 失败（需要管理员权限或命令执行失败）");
                        return;
                    }

                    // 等待系统应用地址
                    Thread.Sleep(2000);

                    try
                    {
                        // 如果修改后 IP 与当前设备 IP 不一致，提示重启并从列表移除
                        if (uiTextBox3.Text != deviceIp)
                        {
                            MessageBox.Show("已修改网关IP地址，等待网关重启");
                            uiDataGridView1.Rows.RemoveAt(selectDevice);
                        }

                        RebindUdpClient(tempIp);

                        if (result == DialogResult.OK)
                        {
                            SendDataViaEthernet(payload);
                        }
                        else if (result == DialogResult.Cancel)
                        {
                            return;
                        }
                    }
                    finally
                    {
                        // 还原原始配置
                        bool restoreOk = RestoreAdapterConfig(adapter.Name, original);
                        if (!restoreOk)
                        {
                            MessageBox.Show("已发送指令，但还原原始网卡配置失败，请手动检查网络设置（可能需要管理员权限）。");
                        }
                        else
                        {
                            if (original.IPs != null && original.IPs.Length > 0)
                                RebindUdpClient(original.IPs[0]);
                        }
                    }
                }
                catch { };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"指令下发失败：{ex.Message}");
            }

            //uiDataGridView1.Rows[0].Cells[4].Value = uiTextBox3.Text;
        }

        #region 帮助函数（用于跨网段传指令）
        private NetworkInterface GetNetworkInterfaceByLocalIp(string localIp)
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var props = ni.GetIPProperties();
                foreach (var uni in props.UnicastAddresses)
                {
                    if (uni.Address.AddressFamily == AddressFamily.InterNetwork && uni.Address.ToString() == localIp)
                        return ni;
                }
            }
            return null;
        }

        private bool IsSameSubnet(IPAddress ip1, IPAddress mask, IPAddress ip2)
        {
            byte[] a = ip1.GetAddressBytes();
            byte[] b = ip2.GetAddressBytes();
            byte[] m = mask.GetAddressBytes();
            if (a.Length != b.Length || a.Length != m.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if ((a[i] & m[i]) != (b[i] & m[i])) return false;
            }
            return true;
        }

        private class AdapterConfig
        {
            public bool DhcpEnabled;
            public string[] IPs;
            public string[] Masks;
            public string[] Gateways;
        }

        private AdapterConfig GetWmiAdapterConfig(NetworkInterface ni)
        {
            try
            {
                string mac = ni.GetPhysicalAddress().ToString();
                if (string.IsNullOrEmpty(mac)) return null;
                // WMI 中 MAC 用冒号分隔
                var macFormatted = Enumerable.Range(0, mac.Length / 2)
                    .Select(i => mac.Substring(i * 2, 2))
                    .ToArray();
                string macStr = string.Join(":", macFormatted);

                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
                foreach (ManagementObject mo in searcher.Get())
                {
                    var macAddr = (mo["MACAddress"] ?? "").ToString();
                    if (string.IsNullOrEmpty(macAddr)) continue;
                    if (!string.Equals(macAddr.Replace(":", "").Replace("-", ""), mac, StringComparison.OrdinalIgnoreCase)) continue;

                    var cfg = new AdapterConfig();
                    cfg.DhcpEnabled = mo["DHCPEnabled"] != null && (bool)mo["DHCPEnabled"];
                    cfg.IPs = mo["IPAddress"] as string[] ?? new string[0];
                    cfg.Masks = mo["IPSubnet"] as string[] ?? new string[0];
                    cfg.Gateways = mo["DefaultIPGateway"] as string[] ?? new string[0];
                    return cfg;
                }
            }
            catch
            {
                // ignore
            }
            return null;
        }

        private bool SetInterfaceStaticAddress(string interfaceName, string ip, string mask, string gateway)
        {
            try
            {
                // netsh 需管理员权限；Verb="runas" 会触发 UAC
                string args = $"interface ip set address \"{interfaceName}\" static {ip} {mask} {gateway} 1";
                var psi = new System.Diagnostics.ProcessStartInfo("netsh", args)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };
                var p = System.Diagnostics.Process.Start(psi);
                if (p == null) return false;
                p.WaitForExit(10000);
                return p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool RestoreAdapterConfig(string interfaceName, AdapterConfig original)
        {
            try
            {
                if (original == null) return false;
                if (original.DhcpEnabled)
                {
                    string args = $"interface ip set address \"{interfaceName}\" dhcp";
                    var psi = new System.Diagnostics.ProcessStartInfo("netsh", args)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = true
                    };
                    var p = System.Diagnostics.Process.Start(psi);
                    if (p == null) return false;
                    p.WaitForExit(8000);

                    // 恢复 DNS 为 DHCP
                    try
                    {
                        var psiDns = new System.Diagnostics.ProcessStartInfo("netsh", $"interface ip set dns \"{interfaceName}\" dhcp")
                        {
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = true
                        };
                        var pd = System.Diagnostics.Process.Start(psiDns);
                        if (pd != null) pd.WaitForExit(3000);
                    }
                    catch { }

                    return p.ExitCode == 0;
                }
                else
                {
                    string ip = (original.IPs != null && original.IPs.Length > 0) ? original.IPs[0] : null;
                    string mask = (original.Masks != null && original.Masks.Length > 0) ? original.Masks[0] : "255.255.255.0";
                    string gw = (original.Gateways != null && original.Gateways.Length > 0) ? original.Gateways[0] : "0.0.0.0";
                    if (ip == null) return false;

                    string args = $"interface ip set address \"{interfaceName}\" static {ip} {mask} {gw} 1";
                    var psi = new System.Diagnostics.ProcessStartInfo("netsh", args)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = true
                    };
                    var p = System.Diagnostics.Process.Start(psi);
                    if (p == null) return false;
                    p.WaitForExit(8000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void RebindUdpClient(string bindIp)
        {
            try
            {
                if (client_ST02 != null)
                {
                    try { client_ST02.Close(); } catch { }
                    client_ST02 = null;
                }
                IPAddress bindAddr = IPAddress.Parse(bindIp);
                client_ST02 = new UdpClient(new IPEndPoint(bindAddr, 0));
            }
            catch
            {
                // ignore
            }
        }
        #endregion

        private void uiButton5_Click(object sender, EventArgs e)
        {
            if (uiComboBox10.Text == "" | uiComboBox11.Text == "" | uiComboBox5.Text == "" | uiComboBox3.Text == "" | uiComboBox2.Text == "")
            {
                MessageBox.Show("COM1配置不可为空");
                return;
            }

            frameBreakTime1 = int.Parse(frameBreakTimeMap[uiComboBox10.Text]);
            BaudRate1 = int.Parse(uiComboBox11.Text);
            DataBits1 = int.Parse(uiComboBox5.Text);
            StopBits1 = (StopBits)Enum.Parse(typeof(StopBits), uiComboBox3.Text);
            Parity1 = (Parity)Enum.Parse(typeof(Parity), parityMap[uiComboBox2.Text]);

            //将 JSON 字符串解析为 JObject
            JObject jsonObj = JObject.Parse(COMMANDS["修改COM口参数"]);

            //更新 JSON 对象中的参数值
            jsonObj["parameterInfo"]["COM"][0]["interfaceName"] = "COM1";
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["frameBreakTime"] = frameBreakTime1;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["baudRate"] = BaudRate1;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["dataBits"] = DataBits1;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["stopBits"] = (int)StopBits1;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["parity"] = (int)Parity1;

            //将更新后的 JObject 转回 JSON 字符串
            jsonData = jsonObj.ToString(Formatting.None);

            MessageBox.Show(jsonData);

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(jsonData);

                // 通过网卡发送
                SendDataViaEthernet(payload);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"指令下发失败：{ex.Message}");
            }
        }

        private void uiButton3_Click(object sender, EventArgs e)
        {
            if (uiComboBox12.Text == "" | uiComboBox9.Text == "" | uiComboBox8.Text == "" | uiComboBox7.Text == "" | uiComboBox6.Text == "")
            {
                MessageBox.Show("COM2配置不可为空");
                return;
            }

            frameBreakTime2 = int.Parse(frameBreakTimeMap[uiComboBox12.Text]);
            BaudRate2 = int.Parse(uiComboBox9.Text);
            DataBits2 = int.Parse(uiComboBox8.Text);
            StopBits2 = (StopBits)Enum.Parse(typeof(StopBits), uiComboBox7.Text);
            Parity2 = (Parity)Enum.Parse(typeof(Parity), parityMap[uiComboBox6.Text]);

            //将 JSON 字符串解析为 JObject
            JObject jsonObj = JObject.Parse(COMMANDS["修改COM口参数"]);

            //更新 JSON 对象中的参数值
            jsonObj["parameterInfo"]["COM"][0]["interfaceName"] = "COM2";
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["frameBreakTime"] = frameBreakTime2;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["baudRate"] = BaudRate2;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["dataBits"] = DataBits2;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["stopBits"] = (int)StopBits2;
            jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["parity"] = (int)Parity2;

            //将更新后的 JObject 转回 JSON 字符串
            jsonData = jsonObj.ToString(Formatting.None);

            MessageBox.Show(jsonData);

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(jsonData);

                // 通过网卡发送
                SendDataViaEthernet(payload);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"指令下发失败：{ex.Message}");
            }
        }

        private void StartSendSequence()
        {
            // 确保监听已启动
            ST02_udp2020();

            // 初始化 UDP client
            string SearchIp = uiComboBox1.Text;
            client_ST02 = new UdpClient(new IPEndPoint(Dns.GetHostAddresses(SearchIp)[0], 0));
            endpoint_ST02 = new IPEndPoint(IPAddress.Broadcast, 2020);

            // 初始化或重用计时器
            if (commandTimer == null)
            {
                commandTimer = new System.Windows.Forms.Timer();
                commandTimer.Interval = 500; // 每条指令间隔 500ms
                commandTimer.Tick += CommandTimer_Tick;
            }
            commandTimer.Start();
        }

        private void CommandTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (commandSendIndex < commandSequence.Count)
                {
                    byte[] buf = Encoding.Default.GetBytes(commandSequence[commandSendIndex]);
                    client_ST02.Send(buf, buf.Length, endpoint_ST02);
                    commandSendIndex++;
                }
                else
                {
                    // 已发送完所有指令，停止发送定时器，等待接收
                    commandTimer.Stop();

                    // 启动一个超时任务：若在指定时间内未收到全部回复，则结束序列并提示超时
                    Task.Run(async () =>
                    {
                        int waited = 0;
                        int timeout = 5000; // 等待 5s
                        while (true)
                        {
                            lock (seqLock)
                            {
                                if (!sequenceActive) return;
                                if (receivedReplies >= expectedReplies)
                                {
                                    sequenceActive = false;
                                    return;
                                }
                            }
                            if (waited >= timeout)
                            {
                                MessageBox.Show("接收超时");
                                return;
                            }
                            await Task.Delay(100);
                            waited += 100;
                        }
                    });

                    commandSendIndex = 0;
                }
            }
            catch (Exception ex)
            {
                commandTimer.Stop();
                Invoke(new Action(() =>
                {
                    MessageBox.Show($"发送序列异常：{ex.Message}");
                }));
            }
        }

        private void uiButton6_Click(object sender, EventArgs e)
        {
            commandSequence.Clear();
            // 按序发送二条指令
            commandSequence = new List<string>
            {
                "",
                readCom,
                readEthernet,
            };
            // 启动按序发送二条指令的序列
            StartSendSequence();
        }

        private void uiDataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            selectDevice = e.RowIndex;
            string name = uiComboBox4.Text;
            string morenips1 = IPinfo[name].ToString();

            string[] devOctets = morenips1.Split('.');
            string[] localOctets = uiDataGridView1.SelectedRows[selectDevice].Cells[4].Value.ToString().Split('.');

            if (devOctets[0] != localOctets[0] | devOctets[1] != localOctets[1] | devOctets[2] != localOctets[2])
            {
                noSameNet = true;
                MessageBox.Show("请设备设置为与网卡在同一网段下");

                uiTextBox3.Text = nowIps[selectDevice];
                uiTextBox4.Text = nowSubnetMasks[selectDevice];
                uiTextBox9.Text = nowGateways[selectDevice];
            }
            else
            {
                noSameNet = false;
            }
        }

        private void uiButton4_Click(object sender, EventArgs e)
        {
            uiButton5_Click(null, null);
            uiButton3_Click(null, null);
            uiButton2_Click(null, null);
        }

        private void uiButton7_Click(object sender, EventArgs e)
        {
            commandSequence.Clear();
            // 按序发送指令
            commandSequence = new List<string>
            {
                "",
                enterConfig,
            };
            // 启动按序发送指令的序列
            StartSendSequence();

            uiButton4_Click(null, null);

            commandSequence.Clear();
            // 按序发送指令
            commandSequence = new List<string>
            {
                "",
                exitConfig,
            };
            // 启动按序发送指令的序列
            StartSendSequence();


            uiDataGridView1.ClearRows();
        }
    }
}
