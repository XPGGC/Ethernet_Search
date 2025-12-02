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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        private ConnectionMode currentConnectionMode = ConnectionMode.None;
        private string targetDeviceIP = "";  // 目标设备IP

        public string jsonData = ""; // 要发送的JSON数据

        private const string readCom = "{\r\n \"messageId\":\"1718711447026\",\r\n \"parameter\":\"COM\"\r\n }";
        private const string readEthernet = "{\r\n  \"messageId\" : \"1718711447026\",\r\n \"parameter\" : \"Ethernet\"\r\n }\r\n";

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

        private string nowFrameBreakTime1;
        private string nowBaudRate1;
        private string nowDataBits1;
        private string nowStopBits1;
        private string nowParity1;
        private string nowFrameBreakTime2;
        private string nowBaudRate2;
        private string nowDataBits2;
        private string nowStopBits2;
        private string nowParity2;

        private string nowIp1;
        private string nowSubnetMask1;
        private string nowGateway1;

        private List<int> frameBreakTimeList = new List<int> { 0, 1, 2, 4, 5};
        private List<int> comBaudRate = new List<int>();
        private List<int> comDataBits = new List<int>();
        private List<StopBits> comStopBits = new List<StopBits>();
        private List<Parity> comParity = new List<Parity>();

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
                tb_type = -1;
                ST02_udp2020();
                this.uiDataGridView1.Rows.Clear();
                string SearchIp = uiComboBox1.Text;

                //再扫描S702网关
                client_ST02 = new UdpClient(new IPEndPoint(Dns.GetHostAddresses(SearchIp)[0], 0));
                endpoint_ST02 = new IPEndPoint(IPAddress.Broadcast, 2020);//IPEndPoint endpoint2
                string ressss2 = GetTimeStamp();
                String sendMessage1 = "{\"messageId\":\"1764663895676\",\"parameter\":\"information\"}";
                byte[] buf2 = Encoding.Default.GetBytes(sendMessage1);
                client_ST02.Send(buf2, buf2.Length, endpoint_ST02);

                // 设置连接模式为网卡模式
                currentConnectionMode = ConnectionMode.Ethernet;
            }
            catch (Exception ex)
            {
                //UIMessageDialog.ShowInfoDialog(this, "请检查选中的网卡IP", UIStyle.Gray);
            }

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
        int tb_type = -1;

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
                    string message = Encoding.UTF8.GetString(bytRecv, 0, bytRecv.Length);
                    
                    Invoke(new Action(() => 
                    {
                        if (string.IsNullOrWhiteSpace(message))
                            return;

                        JObject information = null;

                        try
                        {
                            if (tb_type != -1)
                                return;

                            JObject jo = JObject.Parse(message);

                            // 接收信息的分类判断
                            if (jo["parameterInfo"]?["information"] is JObject parameterInfo)
                            {
                                information = parameterInfo;
                            }
                            else if (jo["information"] is JObject info)
                            {
                                information = info;
                            }

                            nowBaudRate1 = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["baudRate"];
                            nowDataBits1 = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["dataBits"];
                            nowStopBits1 = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["stopBits"];
                            nowParity1 = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["parity"];
                            nowFrameBreakTime1 = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["frameBreakTime"];

                            nowBaudRate2 = (string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["baudRate"];
                            nowDataBits2 = (string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["dataBits"];
                            nowStopBits2 = (string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["stopBits"];
                            nowParity2 = (string)jo["parameterInfo"]?["COM"]?[1]?["interfacePar"]?["parity"];

                            nowIp1 = (string)jo["parameterInfo"]?["Ethernet"]?[0]?["interfacePar"]?["ip"];
                            nowSubnetMask1 = (string)jo["parameterInfo"]?["Ethernet"]?[0]?["interfacePar"]?["subnetMask"];
                            nowGateway1 = (string)jo["parameterInfo"]?["Ethernet"]?[0]?["interfacePar"]?["gateway"];
                            nowFrameBreakTime2 = (string)jo["parameterInfo"]?["COM"]?[0]?["interfacePar"]?["frameBreakTime"];

                            if (information == null)
                                return;

                            // 将读取到的网关信息进行显示
                            int index = uiDataGridView1.Rows.Add();
                            DataGridViewRow row = uiDataGridView1.Rows[index];
                            row.Cells[0].Value = index;
                            row.Cells[1].Value = (string)information["product"]?["name"];
                            row.Cells[2].Value = (string)information["product"]?["model"];
                            row.Cells[3].Value = (string)information["product"]?["sn"];
                            row.Cells[4].Value = (string)(information["Ethernet1"]?["ip"] ?? information["localIp"]?["ip"]);
                            row.Cells[5].Value = (string)information["product"]?["version"];
                            row.Cells[6].Value = (string)information["product"]?["alias"];
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
                    DataGridViewRow selectedRow = uiDataGridView1.SelectedRows[0];
                    if (selectedRow.Cells[4].Value != null)
                    {
                        targetDeviceIP = selectedRow.Cells[4].Value.ToString();
                    }
                }

                // 如果没有选中设备或IP为空
                if (string.IsNullOrEmpty(targetDeviceIP))
                {
                    uiLabel1.Text = "没有选中设备或IP为空";
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
                    uiLabel1.Text = "指令已发送到设备：" + targetDeviceIP; 
                }

            }
            catch (Exception ex)
            {
                throw new Exception("UDP发送失败：" + ex.Message);
            }
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
        }

        private void uiButton3_Click(object sender, EventArgs e)
        {
            ip1 = uiTextBox3.Text;
            subnetMask1 = uiTextBox4.Text;
            gateway1 = uiTextBox9.Text;

            //将 JSON 字符串解析为 JObject
            JObject jsonObj = JObject.Parse(COMMANDS["修改Ethernet参数"]);

            //更新 JSON 对象中的参数值
            jsonObj["parameterInfo"]["Ethernet"][0]["interfaceName"] = "Ethernet1";
            if (uiTextBox3.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["ip"] = ip1;
            if (uiTextBox4.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["subnetMask"] = subnetMask1;
            if (uiTextBox9.Text != "") jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["gateway"] = gateway1;

            //将更新后的 JObject 转回 JSON 字符串
            jsonData = jsonObj.ToString(Formatting.None);

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(jsonData);

                // 根据连接模式发送数据
                if (currentConnectionMode == ConnectionMode.Ethernet)
                {
                    // 通过网卡发送
                    SendDataViaEthernet(payload);
                }
                else
                {
                    uiLabel1.Text = "请先进行设备搜索";
                    return;
                }
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "指令下发失败：" + ex.Message;
            }
        }

        private void uiButton5_Click(object sender, EventArgs e)
        {
            frameBreakTime1 = int.Parse(uiComboBox10.Text);
            BaudRate1 = int.Parse(uiComboBox11.Text);
            DataBits1 = int.Parse(uiComboBox5.Text);
            StopBits1 = (StopBits)Enum.Parse(typeof(StopBits), uiComboBox3.Text);
            Parity1 = (Parity)Enum.Parse(typeof(Parity), uiComboBox2.Text);

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

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(jsonData);

                // 根据连接模式发送数据
                if (currentConnectionMode == ConnectionMode.Ethernet)
                {
                    // 通过网卡发送
                    SendDataViaEthernet(payload);
                }
                else
                {
                    uiLabel1.Text = "请先进行设备搜索";
                    return;
                }
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "指令下发失败：" + ex.Message;
            }
        }

        private void uiButton4_Click(object sender, EventArgs e)
        {
            frameBreakTime2 = int.Parse(uiComboBox12.Text);
            BaudRate2 = int.Parse(uiComboBox9.Text);
            DataBits2 = int.Parse(uiComboBox8.Text);
            StopBits2 = (StopBits)Enum.Parse(typeof(StopBits), uiComboBox7.Text);
            Parity2 = (Parity)Enum.Parse(typeof(Parity), uiComboBox6.Text);

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

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(jsonData);

                // 根据连接模式发送数据
                if (currentConnectionMode == ConnectionMode.Ethernet)
                {
                    // 通过网卡发送
                    SendDataViaEthernet(payload);
                }
                else
                {
                    uiLabel1.Text = "请先进行设备搜索";
                    return;
                }
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "指令下发失败：" + ex.Message;
            }
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            ST02_udp2020();
            string SearchIp = uiComboBox1.Text;
            client_ST02 = new UdpClient(new IPEndPoint(Dns.GetHostAddresses(SearchIp)[0], 0));
            endpoint_ST02 = new IPEndPoint(IPAddress.Broadcast, 2020);//IPEndPoint endpoint2
            byte[] buf2 = Encoding.Default.GetBytes(readCom);
            client_ST02.Send(buf2, buf2.Length, endpoint_ST02);

            uiComboBox10.Text = nowFrameBreakTime1;
            uiComboBox11.Text = nowBaudRate1;
            uiComboBox5.Text = nowDataBits1;
            uiComboBox3.Text = nowStopBits1;
            uiComboBox2.Text = nowParity1;

            uiComboBox12.Text = nowFrameBreakTime2;
            uiComboBox9.Text = nowBaudRate2;
            uiComboBox8.Text = nowDataBits2;
            uiComboBox7.Text = nowStopBits2;
            uiComboBox6.Text = nowParity2;
        }

        private void uiButton6_Click(object sender, EventArgs e)
        {
            ST02_udp2020();
            string SearchIp = uiComboBox1.Text;
            client_ST02 = new UdpClient(new IPEndPoint(Dns.GetHostAddresses(SearchIp)[0], 0));
            endpoint_ST02 = new IPEndPoint(IPAddress.Broadcast, 2020);//IPEndPoint endpoint2
            byte[] buf2 = Encoding.Default.GetBytes(readEthernet);
            client_ST02.Send(buf2, buf2.Length, endpoint_ST02);

            uiTextBox3.Text = nowIp1;
            uiTextBox4.Text = nowSubnetMask1;
            uiTextBox9.Text = nowGateway1;
        }
    }
}
