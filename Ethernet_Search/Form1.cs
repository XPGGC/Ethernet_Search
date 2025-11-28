using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
            Serial     // COM口连接
        }

        private ConnectionMode currentConnectionMode = ConnectionMode.None;
        private string targetDeviceIP = "";  // 目标设备IP（网卡模式）
        private SerialPort serialPort = null;  // 串口对象
        private string selectedComPort = "";  // 选中的COM口
        private StringBuilder serialDataBuffer = new StringBuilder();  // 串口数据缓冲区
        private AutoResetEvent serialDataReceived = new AutoResetEvent(false);  // 串口数据接收事件
        private string receivedJsonData = "";  // 接收到的JSON数据

        private Dictionary<string, string> COMMANDS = new Dictionary<string, string>       
        {
            { "实时修改网络接入方式", "{\r\n \"parameterInfo\" : {\r\n \"networkWay\" : 0,\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"networkWay\",\r\n \"messageId\" : \"1718776504184\"\r\n }" },
            { "修改4G参数", "{\r\n \"parameterInfo\" : {\r\n \"4G\" : [ {\r\n \"interfaceName\" : \"4G1\",\r\n \"interfacePar\" : {\r\n \"content\" : \"\",\r\n \"username\" : \"\",\r\n \"password\" : \"\",\r\n \"auth\" : 3,\r\n \"ethNetworkSegment\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n说明\r\n在线修改声明，值为1时表示实时生效，\r\n否则修改会失效\r\n25\r\n},\r\n \"parameter\" : \"4G\",\r\n \"messageId\" : \"1718776952659\"\r\n }" },
            
            { "修改Ethernet 参数", "{\r\n }\r\n \"parameterInfo\" : {\r\n \"Ethernet\" : [ {\r\n \"interfaceName\" : \"Ethernet1\",\r\n \"interfacePar\" : {\r\n \"dhcp\" : 0,\r\n \"ip\" : \"192.168.0.158\",\r\n \"subnetMask\" : \"255.255.255.0\",\r\n \"mac\" : \"02:00:00:32:A1:91\",\r\n \"dns\" : \"114.114.114.114\",\r\n \"dns2\" : \"8.8.8.8\",\r\n \"ntp\" : \"ntp.ntsc.ac.cn\",\r\n \"gateway\" : \"192.168.0.1\"\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"Ethernet\",\r\n \"messageId\" : \"1718780046192\"\r\n" }
        };

        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            Form1Search_Load();
            LoadAvailableComPorts();  // 加载可用的COM口列表
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
                String sendMessage1 = "{\"messageId\":\"" + ressss2 + "\",\"parameter\":\"information\"}";
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
                //IPinfo.Add(name, "127.0.0.1");
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
                        //break;
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

            uiComboBox2.DataSource = COMMANDS.Keys.ToList();
        }


        static bool IsUdpcRecvStart_ST02 = false;
        private static Socket udpServer_ST02;
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
                    //MessageBox.Show("UDP监听器已成功启动");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("端口被占用");
            }
        }


        private void ReceiveMessage_ST021(object obj)
        {
            while (IsUdpcRecvStart_ST02)
            {
                try
                {
                    byte[] bytRecv = udpcRecv_ST02.Receive(ref localIpep_ST02);
                    string message = Encoding.UTF8.GetString(bytRecv, 0, bytRecv.Length);
                    Invoke((new Action(() =>
                    {

                        JObject jo = (JObject)JsonConvert.DeserializeObject(message);
                        try
                        {
                            if (tb_type == -1)
                            {
                                //扫描网关信息
                                if (jo.Property("parameterInfo") != null)
                                {
                                    JObject information = (JObject)jo["parameterInfo"]["information"];
                                    int index = this.uiDataGridView1.Rows.Add();
                                    this.uiDataGridView1.Rows[index].Cells[0].Value = index;
                                    this.uiDataGridView1.Rows[index].Cells[1].Value = (string)information["product"]["name"];
                                    this.uiDataGridView1.Rows[index].Cells[2].Value = (string)information["product"]["model"];
                                    this.uiDataGridView1.Rows[index].Cells[3].Value = (string)information["product"]["sn"];
                                    this.uiDataGridView1.Rows[index].Cells[4].Value = (string)information["Ethernet1"]["ip"];
                                    this.uiDataGridView1.Rows[index].Cells[5].Value = (string)information["product"]["version"];
                                    this.uiDataGridView1.Rows[index].Cells[6].Value = (string)information["product"]["alias"];

                                }
                                else if (jo.Property("information") != null)
                                {
                                    JObject information = (JObject)jo["information"];
                                    int index = this.uiDataGridView1.Rows.Add();
                                    this.uiDataGridView1.Rows[index].Cells[0].Value = index;
                                    this.uiDataGridView1.Rows[index].Cells[1].Value = (string)information["product"]["name"];
                                    this.uiDataGridView1.Rows[index].Cells[2].Value = (string)information["product"]["model"];
                                    this.uiDataGridView1.Rows[index].Cells[3].Value = (string)information["product"]["sn"];
                                    this.uiDataGridView1.Rows[index].Cells[4].Value = (string)information["localIp"]["ip"];
                                    this.uiDataGridView1.Rows[index].Cells[5].Value = (string)information["product"]["version"];
                                    this.uiDataGridView1.Rows[index].Cells[6].Value = (string)information["product"]["alias"];

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            int a = 0;
                        }
                    })));
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
            //textBox1.Text = IPinfo[name];
            string morenips1 = IPinfo[name].ToString();
            string[] morenips = morenips1.Split('|');
            for (int i = 0; i < morenips.Count(); i++)
            {
                typeListIP.Add(morenips[i]);
            }
            uiComboBox1.DataSource = typeListIP;
        }


        // 通过网卡（UDP）发送数据
        private void SendDataViaEthernet(string jsonData)
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

                // 如果没有选中设备或IP为空，使用广播
                if (string.IsNullOrEmpty(targetDeviceIP))
                {
                    //// 使用广播发送
                    //if (client_ST02 == null || endpoint_ST02 == null)
                    //{
                    //    string SearchIp = uiComboBox1.Text;
                    //    client_ST02 = new UdpClient(new IPEndPoint(Dns.GetHostAddresses(SearchIp)[0], 0));
                    //    endpoint_ST02 = new IPEndPoint(IPAddress.Broadcast, 2020);
                    //}

                    //byte[] buffer = Encoding.UTF8.GetBytes(jsonData);
                    //client_ST02.Send(buffer, buffer.Length, endpoint_ST02);
                    //uiLabel1.Text = "指令已通过UDP广播发送";

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
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonData);
                    client_ST02.Send(buffer, buffer.Length, targetEndpoint);
                    uiLabel1.Text = "指令已发送到设备：" + targetDeviceIP;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("UDP发送失败：" + ex.Message);
            }
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            string key = COMMANDS.ElementAt(uiComboBox2.SelectedIndex).Value;
            SendDataViaEthernet(key);
        }

        // 通过串口发送数据
        private void SendDataViaSerial(byte[] data)
        {
            try
            {
                if (serialPort == null || !serialPort.IsOpen)
                {
                    if (string.IsNullOrEmpty(selectedComPort))
                    {
                        uiLabel1.Text = "请先选择COM口";
                        return;
                    }
                    
                    // 打开串口
                    serialPort = new SerialPort(selectedComPort);
                    serialPort.BaudRate = 9600;  // 默认波特率，可根据需要调整
                    serialPort.DataBits = 8;
                    serialPort.StopBits = StopBits.One;
                    serialPort.Parity = Parity.None;
                    serialPort.ReadTimeout = 3000;  // 增加读取超时时间
                    serialPort.WriteTimeout = 1000;
                    
                    // 注册数据接收事件
                    serialPort.DataReceived += SerialPort_DataReceived;
                    
                    serialPort.Open();
                    
                    // 设置连接模式为串口模式
                    currentConnectionMode = ConnectionMode.Serial;
                }
                
                // 清空缓冲区
                serialDataBuffer.Clear();
                receivedJsonData = "";
                
                // 发送数据
                serialPort.Write(data, 0, data.Length);
                uiLabel1.Text = "指令已通过串口发送到：" + selectedComPort;
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "串口发送失败：" + ex.Message;
                throw new Exception("串口发送失败：" + ex.Message);
            }
        }

        // 串口数据接收事件处理
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (serialPort == null || !serialPort.IsOpen)
                    return;

                // 读取可用数据
                int bytesToRead = serialPort.BytesToRead;
                byte[] buffer = new byte[bytesToRead];
                serialPort.Read(buffer, 0, bytesToRead);

                // 将接收到的数据追加到缓冲区
                string receivedData = Encoding.UTF8.GetString(buffer);
                serialDataBuffer.Append(receivedData);

                // 检查是否接收到完整的JSON数据（通常以}结尾）
                string currentData = serialDataBuffer.ToString();
                if (currentData.Contains("}") && IsValidJson(currentData))
                {
                    receivedJsonData = currentData;
                    serialDataBuffer.Clear();
                    
                    // 通知主线程处理数据
                    serialDataReceived.Set();
                    
                    // 在UI线程中处理接收到的数据
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => ProcessReceivedComPortData(receivedJsonData)));
                    }
                    else
                    {
                        ProcessReceivedComPortData(receivedJsonData);
                    }
                }
            }
            catch (Exception ex)
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => uiLabel1.Text = "接收数据错误：" + ex.Message));
                }
                else
                {
                    uiLabel1.Text = "接收数据错误：" + ex.Message;
                }
            }
        }

        // 检查字符串是否为有效的JSON
        private bool IsValidJson(string jsonString)
        {
            try
            {
                JObject.Parse(jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 处理接收到的COM口数据
        private void ProcessReceivedComPortData(string jsonData)
        {
            try
            {
                JObject comjs = JObject.Parse(jsonData);

                // 从JSON中获取COM口信息
                string comPort = "";

                // 尝试多种可能的JSON结构来获取COM口
                if (comjs["parameterInfo"] != null)
                {
                    // 如果parameterInfo中有COM相关信息
                    if (comjs["parameterInfo"]["com"] != null)
                    {
                        if (comjs["parameterInfo"]["com"]["port"] != null)
                        {
                            comPort = (string)comjs["parameterInfo"]["com"]["port"];
                        }
                        else if (comjs["parameterInfo"]["com"]["name"] != null)
                        {
                            comPort = (string)comjs["parameterInfo"]["com"]["name"];
                        }
                    }
                    else if (comjs["parameterInfo"]["serial"] != null)
                    {
                        if (comjs["parameterInfo"]["serial"]["port"] != null)
                        {
                            comPort = (string)comjs["parameterInfo"]["serial"]["port"];
                        }
                    }
                    else if (comjs["parameterInfo"]["comPort"] != null)
                    {
                        comPort = (string)comjs["parameterInfo"]["comPort"];
                    }
                }
                else if (comjs["com"] != null)
                {
                    if (comjs["com"]["port"] != null)
                    {
                        comPort = (string)comjs["com"]["port"];
                    }
                    else if (comjs["com"]["name"] != null)
                    {
                        comPort = (string)comjs["com"]["name"];
                    }
                }
                else if (comjs["serial"] != null)
                {
                    if (comjs["serial"]["port"] != null)
                    {
                        comPort = (string)comjs["serial"]["port"];
                    }
                }
                else if (comjs["comPort"] != null)
                {
                    comPort = (string)comjs["comPort"];
                }
                else if (comjs["port"] != null)
                {
                    comPort = (string)comjs["port"];
                }
                else if (comjs["data"] != null && comjs["data"]["comPort"] != null)
                {
                    comPort = (string)comjs["data"]["comPort"];
                }

                // 如果找到了COM口信息
                if (!string.IsNullOrEmpty(comPort))
                {
                    selectedComPort = comPort;

                    // 更新UI显示COM口（如果uiComboBox3存在）
                    if (uiComboBox3 != null)
                    {
                        // 检查COM口是否已在列表中
                        bool exists = false;
                        for (int i = 0; i < uiComboBox3.Items.Count; i++)
                        {
                            if (uiComboBox3.Items[i].ToString() == comPort)
                            {
                                exists = true;
                                uiComboBox3.SelectedIndex = i;
                                break;
                            }
                        }

                        // 如果不存在，添加到列表并选中
                        if (!exists)
                        {
                            uiComboBox3.Items.Add(comPort);
                            uiComboBox3.SelectedItem = comPort;
                        }
                    }

                    uiLabel1.Text = "已获取COM口：" + comPort;
                }
                else
                {
                    uiLabel1.Text = "未找到COM口信息，接收到的JSON：" + jsonData;
                }
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "解析COM口信息失败：" + ex.Message + "，数据：" + jsonData;
            }
        }

        // 获取COM口信息并处理（发送指令并等待从机返回）
        private void ProcessComPortInfo(string readCom)
        {
            try
            {
                //获取com口信息
                byte[] comInfo = Encoding.UTF8.GetBytes(readCom);
                SendDataViaSerial(comInfo);

                // 等待从机返回数据（最多等待3秒）
                bool dataReceived = serialDataReceived.WaitOne(3000);
                
                if (!dataReceived)
                {
                    uiLabel1.Text = "等待从机响应超时";
                }
                // 数据接收后会在SerialPort_DataReceived事件中自动处理
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "获取COM口信息失败：" + ex.Message;
            }
        }

        // 获取系统中所有可用的COM口
        private void LoadAvailableComPorts()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                if (uiComboBox3 != null)
                {
                    uiComboBox3.DataSource = ports;
                    if (ports.Length > 0)
                    {
                        selectedComPort = ports[0];
                    }
                }
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "获取COM口列表失败：" + ex.Message;
            }
        }

        // 关闭串口连接
        private void CloseSerialPort()
        {
            try
            {
                if (serialPort != null)
                {
                    // 取消事件订阅
                    serialPort.DataReceived -= SerialPort_DataReceived;
                    
                    if (serialPort.IsOpen)
                    {
                        serialPort.Close();
                    }
                    serialPort.Dispose();
                    serialPort = null;
                }
            }
            catch (Exception ex)
            {
                // 忽略关闭时的错误
            }
        }

        // 窗体关闭时清理资源
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CloseSerialPort();
            base.OnFormClosing(e);
        }
    }
}