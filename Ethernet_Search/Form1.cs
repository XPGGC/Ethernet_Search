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
        private string currentComPort = "";   // 当前COM口（串口模式）

        // COM配置参数
        private int comBaudRate = 9600;
        private int comDataBits = 8;
        private StopBits comStopBits = StopBits.One;
        private Parity comParity = Parity.None;
        private int comReadTimeout = 3000;
        private int comWriteTimeout = 3000;

        Form2 form2 = new Form2();

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
                //textBox4.Text = textBox4.Text.ToString() + "\r\n" + sendMessage1;
                
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
                    textBox3.Text = textBox3.Text.ToString() + "\r\n" + sendMessage1;
                    
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
                uiLabel1.Text = "COM搜索失败：";
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
                        textBox3.AppendText("\r\n串口接收错误：" + ex.Message);
                    })));
                    Thread.Sleep(100);
                }
            }
        }

        // 处理接收到的消息（与UDP接收逻辑相同）
        private void ProcessReceivedMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            Invoke((new Action(() =>
            {
                textBox3.AppendText(message + "\r\n");

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

                        textBox3.AppendText(message);

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

        private void uiComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox4.Text = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox3.Text = "";
        }

        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                string jsonData = textBox4.Text.Trim();
                
                if (string.IsNullOrEmpty(jsonData))
                {
                    uiLabel1.Text = "请输入要发送的JSON数据";
                    return;
                }

                // 验证JSON格式
                try
                {
                    JObject.Parse(jsonData);
                }
                catch
                {
                    uiLabel1.Text = "JSON格式错误，请检查数据格式";
                    return;
                }

                // 根据连接模式发送数据
                if (currentConnectionMode == ConnectionMode.Ethernet)
                {
                    // 网卡模式：通过UDP发送
                    SendDataViaEthernet(jsonData);
                }
                else if (currentConnectionMode == ConnectionMode.Serial)
                {
                    // COM口模式：通过串口发送
                    SendDataViaSerial(jsonData);
                }
                else
                {
                    uiLabel1.Text = "请先进行设备搜索（网卡或COM）";
                    return;
                }

                uiLabel1.Text = "指令下发成功";
            }
            catch (Exception ex)
            {
                uiLabel1.Text = "指令下发失败：" + ex.Message;
            }
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
                    // 使用广播发送
                    if (client_ST02 == null || endpoint_ST02 == null)
                    {
                        string SearchIp = uiComboBox1.Text;
                        client_ST02 = new UdpClient(new IPEndPoint(Dns.GetHostAddresses(SearchIp)[0], 0));
                        endpoint_ST02 = new IPEndPoint(IPAddress.Broadcast, 2020);
                    }
                    
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonData);
                    client_ST02.Send(buffer, buffer.Length, endpoint_ST02);
                    uiLabel1.Text = "指令已通过UDP广播发送";
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

        // 通过串口发送数据
        private void SendDataViaSerial(string jsonData)
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
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonData);
                    serialPort_COM.Write(buffer, 0, buffer.Length);
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

        private void uiButton5_Click(object sender, EventArgs e)
        {
            if (uiComboBox3.SelectedItem == null)
            {
                MessageBox.Show("请先选择一个COM口！");
                return; // 如果没有选择，则直接返回，不打开配置窗口
            }

            using (Form2 form2 = new Form2())
            {
                // 显示Form2为模态窗口，等待用户操作
                if (form2.ShowDialog() == DialogResult.OK)
                {
                    // 从Form2的公共属性获取配置并更新Form1的参数
                    comBaudRate = form2.BaudRate;
                    comDataBits = form2.DataBits;
                    comStopBits = form2.StopBits;
                    comParity = form2.Parity;

                    // 重新初始化串口（如果已打开则先关闭）
                    if (serialPort_COM != null && serialPort_COM.IsOpen)
                    {
                        serialPort_COM.Close();
                        serialPort_COM.Dispose();
                    }
                }
            }
        }
    }
}