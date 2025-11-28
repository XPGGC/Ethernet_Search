using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sunny.UI;
using System;
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
            Serial     // COM口连接
        }

        private ConnectionMode currentConnectionMode = ConnectionMode.None;
        private string targetDeviceIP = "";  // 目标设备IP（网卡模式）
        private string currentComPort = "";   // 当前COM口（串口模式）

        private string jsonData = ""; // 要发送的JSON数据

        private int comBaudRate = 9600;
        private int comDataBits = 8;
        private StopBits comStopBits = StopBits.One;
        private Parity comParity = Parity.None;
        private int comReadTimeout = 3000;
        private int comWriteTimeout = 3000;

        //配置指令集
        private Dictionary<string, string> COMMANDS = new Dictionary<string, string>
        {
            { "实时修改网络接入方式", "{\r\n \"parameterInfo\" : {\r\n \"networkWay\" : 0,\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"networkWay\",\r\n \"messageId\" : \"1718776504184\"\r\n }" },
            { "修改4G参数", "{\r\n \"parameterInfo\" : {\r\n \"4G\" : [ {\r\n \"interfaceName\" : \"4G1\",\r\n \"interfacePar\" : {\r\n \"content\" : \"\",\r\n \"username\" : \"\",\r\n \"password\" : \"\",\r\n \"auth\" : 3,\r\n \"ethNetworkSegment\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n说明\r\n在线修改声明，值为1时表示实时生效，\r\n否则修改会失效\r\n25\r\n},\r\n \"parameter\" : \"4G\",\r\n \"messageId\" : \"1718776952659\"\r\n }" },
            { "修改COM口参数", "{\r\n \"parameterInfo\" : {\r\n \"COM\": [ {\r\n \"interfaceName\" : \"COM2\",\r\n \"workMode\" : 0,\r\n \"interfacePar\" : {\r\n \"frameBreakTime\" : 0,\r\n \"baudRate\" : 9600,\r\n \"dataBits\" : 8,\r\n \"stopBits\" : 1,\r\n \"parity\" : 0\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"COM\",\r\n \"messageId\" : \"1718768583565\"\r\n }"},
            { "修改Ethernet参数", "{\r\n }\r\n \"parameterInfo\" : {\r\n \"Ethernet\" : [ {\r\n \"interfaceName\" : \"Ethernet1\",\r\n \"interfacePar\" : {\r\n \"dhcp\" : 0,\r\n \"ip\" : \"192.168.0.158\",\r\n \"subnetMask\" : \"255.255.255.0\",\r\n \"mac\" : \"02:00:00:32:A1:91\",\r\n \"dns\" : \"114.114.114.114\",\r\n \"dns2\" : \"8.8.8.8\",\r\n \"ntp\" : \"ntp.ntsc.ac.cn\",\r\n \"gateway\" : \"192.168.0.1\"\r\n }\r\n } ],\r\n \"onlineModification\" : 1\r\n },\r\n \"parameter\" : \"Ethernet\",\r\n \"messageId\" : \"1718780046192\"\r\n" },
            {"", ""}
        };


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Form1Search_Load();
            GetPort(null, null);
        }
        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 关闭串口
            CloseSerialPort();
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
        private void uiButton2_Click(object sender, EventArgs e)
        {
            try
            {
                tb_type = -1;
                this.uiDataGridView1.Rows.Clear();
                string comPort = uiComboBox3.Text;

                if (string.IsNullOrEmpty(comPort))
                {
                    uiLabel1.Text = "请选择COM口";
                    return;
                }

                // 启动串口监听
                StartSerialPortReceive(comPort);

                // 通过COM口发送查询命令
                string ressss2 = GetTimeStamp();
                String sendMessage1 = "{\"messageId\":\"" + ressss2 + "\",\"parameter\":\"information\"}";

                if (serialPort_COM != null && serialPort_COM.IsOpen)
                {
                    byte[] buf2 = Encoding.UTF8.GetBytes(sendMessage1);
                    serialPort_COM.Write(buf2, 0, buf2.Length);

                    // 设置连接模式为串口模式
                    currentConnectionMode = ConnectionMode.Serial;
                    currentComPort = comPort;
                }
                else
                {
                    uiLabel1.Text = "COM口未打开，请检查COM口设置";
                }
            }
            catch (Exception ex)
            {
                uiLabel1.Text = $"COM搜索失败：{ex}";
            }
        }
        #region//COM搜索
        //得到COM口
        private void GetPort(object sender, EventArgs e)
        {
            try
            {
                List<string> list = new List<string>();
                RegistryKey hklm = Registry.LocalMachine;

                RegistryKey software11 = hklm.OpenSubKey("HARDWARE");

                //打开"HARDWARE"子健
                RegistryKey software = software11?.OpenSubKey("DEVICEMAP");

                RegistryKey sitekey = software?.OpenSubKey("SERIALCOMM");

                if (sitekey != null)
                {
                    //获取当前子健
                    string[] Str2 = sitekey.GetValueNames();

                    //获得当前子健下面所有健组成的字符串数组
                    int ValueCount = sitekey.ValueCount;
                    //获得当前子健存在的健值
                    for (int i = 0; i < ValueCount; i++)
                    {
                        list.Add(sitekey.GetValue(Str2[i]).ToString());
                    }
                    sitekey.Close();
                }
                software?.Close();
                software11?.Close();
                hklm.Close();

                if (list.Count > 0)
                {
                    uiComboBox3.DataSource = list.ToArray();
                    uiLabel1.Text = "已查询到" + list.Count + "个COM接口";
                }
                else
                {
                    uiLabel1.Text = "无法查询到COM接口";
                }
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "查询COM接口失败：" + ex.Message;
            }
        }

        // 串口相关变量
        private SerialPort serialPort_COM = null;
        private bool IsSerialPortRecvStart = false;
        private Thread thrSerialRecv = null;

        // 启动串口接收
        private void StartSerialPortReceive(string comPort)
        {
            try
            {
                // 如果串口已打开，先关闭
                if (serialPort_COM != null && serialPort_COM.IsOpen)
                {
                    serialPort_COM.Close();
                    serialPort_COM.Dispose();
                }

                // 创建并配置串口（使用保存的配置参数）

                serialPort_COM = new SerialPort(comPort);

                serialPort_COM.BaudRate = comBaudRate;
                serialPort_COM.DataBits = comDataBits;
                serialPort_COM.StopBits = comStopBits;
                serialPort_COM.Parity = comParity;
                serialPort_COM.ReadTimeout = comReadTimeout;
                serialPort_COM.WriteTimeout = comWriteTimeout;
                serialPort_COM.Encoding = Encoding.UTF8;

                // 打开串口
                serialPort_COM.Open();

                // 启动接收线程
                if (!IsSerialPortRecvStart)
                {
                    IsSerialPortRecvStart = true;
                    thrSerialRecv = new Thread(ReceiveSerialMessage);
                    thrSerialRecv.IsBackground = true;
                    thrSerialRecv.Start();
                }
            }
            catch (Exception ex)
            {
                IsSerialPortRecvStart = false;
                throw new Exception("打开COM口失败：" + ex.Message);
            }
        }

        // 串口数据接收线程
        private void ReceiveSerialMessage()
        {
            StringBuilder buffer = new StringBuilder();
            while (IsSerialPortRecvStart && serialPort_COM != null && serialPort_COM.IsOpen)
            {
                try
                {
                    // 读取可用数据
                    if (serialPort_COM.BytesToRead > 0)
                    {
                        byte[] bufferBytes = new byte[serialPort_COM.BytesToRead];
                        int bytesRead = serialPort_COM.Read(bufferBytes, 0, bufferBytes.Length);
                        string receivedData = Encoding.UTF8.GetString(bufferBytes, 0, bytesRead);
                        buffer.Append(receivedData);

                        // 尝试解析完整的JSON消息（假设以换行符或完整JSON对象结束）
                        string fullMessage = buffer.ToString();

                        // 尝试查找完整的JSON对象
                        int jsonStart = fullMessage.IndexOf('{');
                        if (jsonStart >= 0)
                        {
                            int braceCount = 0;
                            int jsonEnd = -1;
                            for (int i = jsonStart; i < fullMessage.Length; i++)
                            {
                                if (fullMessage[i] == '{') braceCount++;
                                if (fullMessage[i] == '}') braceCount--;
                                if (braceCount == 0)
                                {
                                    jsonEnd = i;
                                    break;
                                }
                            }

                            if (jsonEnd > jsonStart)
                            {
                                string jsonMessage = fullMessage.Substring(jsonStart, jsonEnd - jsonStart + 1);
                                buffer.Clear();
                                if (jsonStart > 0)
                                {
                                    buffer.Append(fullMessage.Substring(0, jsonStart));
                                }
                                if (jsonEnd < fullMessage.Length - 1)
                                {
                                    buffer.Append(fullMessage.Substring(jsonEnd + 1));
                                }

                                // 处理接收到的JSON消息
                                ProcessReceivedMessage(jsonMessage);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(10); // 避免CPU占用过高
                    }
                }
                catch (TimeoutException)
                {
                    // 超时是正常的，继续等待
                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    // 发生错误，记录但不中断
                    Invoke((new Action(() =>
                    {
                    })));
                    Thread.Sleep(100);
                }
            }
        }

        // 处理接收到的消息                 
        private void ProcessReceivedMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            Invoke((new Action(() =>
            {
                try
                {
                    JObject jo = (JObject)JsonConvert.DeserializeObject(message);
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
                            //this.uiDataGridView1.Rows[index].Cells[4].Value = (string)information["Ethernet1"]["ip"];
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
                            //this.uiDataGridView1.Rows[index].Cells[4].Value = (string)information["localIp"]["ip"];
                            this.uiDataGridView1.Rows[index].Cells[5].Value = (string)information["product"]["version"];
                            this.uiDataGridView1.Rows[index].Cells[6].Value = (string)information["product"]["alias"];
                        }
                    }
                }
                catch (Exception ex)
                    {
                        // JSON解析失败，忽略
                    }
            })));
        }

        // 关闭串口
        private void CloseSerialPort()
        {
            try
            {
                IsSerialPortRecvStart = false;
                if (thrSerialRecv != null && thrSerialRecv.IsAlive)
                {
                    thrSerialRecv.Join(1000);
                }
                if (serialPort_COM != null && serialPort_COM.IsOpen)
                {
                    serialPort_COM.Close();
                }
                if (serialPort_COM != null)
                {
                    serialPort_COM.Dispose();
                    serialPort_COM = null;
                }
            }
            catch (Exception ex)
            {
                // 忽略关闭时的错误
            }
        }

        #endregion

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

        // 通过串口发送数据
        private void SendDataViaSerial(byte[] payload)
        {
            try
            {
                if (serialPort_COM == null || !serialPort_COM.IsOpen)
                {
                    // 如果串口未打开，尝试重新打开
                    if (!string.IsNullOrEmpty(currentComPort))
                    {
                        StartSerialPortReceive(currentComPort);
                    }
                    else
                    {
                        throw new Exception("COM口未打开，请先进行COM搜索");
                    }
                }

                if (serialPort_COM != null && serialPort_COM.IsOpen)
                {
                    serialPort_COM.Write(payload, 0, payload.Length);
                    uiLabel1.Text = "指令已通过COM口发送：" + currentComPort;
                }
                else
                {
                    throw new Exception("COM口打开失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("串口发送失败：" + ex.Message);
            }
        }

        private void configCom(string value)
        {
            using (Form2 form2 = new Form2())
            {
                // 显示Form2为模态窗口，等待用户操作
                if (form2.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        //将 JSON 字符串解析为 JObject
                        JObject jsonObj = JObject.Parse(value);

                        //更新 JSON 对象中的参数值
                        jsonObj["parameterInfo"]["COM"][0]["interfaceName"] = form2.Com;
                        jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["baudRate"] = form2.BaudRate;
                        jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["dataBits"] = form2.DataBits;
                        jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["stopBits"] = (int)form2.StopBits;
                        jsonObj["parameterInfo"]["COM"][0]["interfacePar"]["parity"] = (int)form2.Parity;

                        //将更新后的 JObject 转回 JSON 字符串
                        jsonData = jsonObj.ToString(Formatting.None);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }


        private void config4G(string value)
        {
            using (Form3 form3 = new Form3())
            {
                // 显示Form3为模态窗口，等待用户操作
                if (form3.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        //将 JSON 字符串解析为 JObject
                        JObject jsonObj = JObject.Parse(value);

                        //更新 JSON 对象中的参数值
                        jsonObj["parameterInfo"]["4G"][0]["interfaceName"] = form3.interfaceName;
                        jsonObj["parameterInfo"]["4G"][0]["interfacePar"]["content"] = form3.content;
                        jsonObj["parameterInfo"]["4G"][0]["interfacePar"]["username"] = form3.username;
                        jsonObj["parameterInfo"]["4G"][0]["interfacePar"]["password"] = form3.password;
                        jsonObj["parameterInfo"]["4G"][0]["interfacePar"]["auth"] = form3.auth;
                        jsonObj["parameterInfo"]["4G"][0]["interfacePar"]["ethNetworkSegment"] = form3.ethNetworkSegment;

                        //将更新后的 JObject 转回 JSON 字符串
                        jsonData = jsonObj.ToString(Formatting.None);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }


        private void configEthernet(string value)
        {
            using (Form4 form4 = new Form4())
            {
                // 显示Form4为模态窗口，等待用户操作
                if (form4.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        //将 JSON 字符串解析为 JObject
                        JObject jsonObj = JObject.Parse(value);

                        //更新 JSON 对象中的参数值
                        jsonObj["parameterInfo"]["Ethernet"][0]["interfaceName"] = form4.interfaceName;
                        jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["dhcp"] = form4.dhcp;
                        jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["ip"] = form4.ip;
                        jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["subnetMask"] = form4.subnetMask;
                        jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["mac"] = form4.mac;
                        jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["dns"] = form4.dns;
                        jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["dns2"] = form4.dns2;
                        jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["ntp"] = form4.ntp;
                        jsonObj["parameterInfo"]["Ethernet"][0]["interfacePar"]["gateway"] = form4.gateway;

                        //将更新后的 JObject 转回 JSON 字符串
                        jsonData = jsonObj.ToString(Formatting.None);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }


        private void button11_Click(object sender, EventArgs e)
        {
            string key = COMMANDS.ElementAt(uiComboBox2.SelectedIndex).Key;
            string value = COMMANDS.ElementAt(uiComboBox2.SelectedIndex).Value;

            if (key == "修改COM口参数") configCom(value);
            if (key == "修改Ethernet参数") configEthernet(value);
            if (key == "修改4G参数") config4G(value);

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(jsonData);

                // 根据连接模式发送数据
                if (currentConnectionMode == ConnectionMode.Ethernet)
                {
                    // 网卡模式：通过UDP发送
                    SendDataViaEthernet(payload);
                }
                else if (currentConnectionMode == ConnectionMode.Serial)
                {
                    // COM口模式：通过串口发送
                    SendDataViaSerial(payload);
                }
                else
                {
                    uiLabel1.Text = "请先进行设备搜索（网卡或COM）";
                    return;
                }
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "指令下发失败：" + ex.Message;
            }
        }
    }
}