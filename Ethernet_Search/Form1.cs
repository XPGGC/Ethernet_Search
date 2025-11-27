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

    }
}