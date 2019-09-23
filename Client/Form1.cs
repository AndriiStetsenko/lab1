using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.IO;

namespace Client
{
    public partial class Form1 : Form
    {
        static string localPath = AppDomain.CurrentDomain.BaseDirectory;
        static ushort port = 45000;
        
        List<Process> procs = new List<Process>();

        
        public static int CompareProcess(Process x, Process y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                else
                {
                    return x.KernelModeTime.CompareTo(y.KernelModeTime);
                }
            }
        }

        
        private static XDocument TalkOutIn(string requestPath, IPAddress serverIP)
        {
            XDocument resXml = null;

            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IPEndPoint epThere = new IPEndPoint(serverIP, port);
                try
                {
                    IAsyncResult result = s.BeginConnect(epThere, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(3000, true);

                    if (s.Connected)
                    {
                        s.SendFile(requestPath);
                        resXml = UR.ReceiveXML(s);
                        s.Shutdown(SocketShutdown.Both);
                        s.EndConnect(result);
                    }
                    else
                    {
                        throw new ApplicationException("Failed to connect server.");
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Can`t connect.\n" + e.Message);
                    if (s.Connected)
                        s.Shutdown(SocketShutdown.Both);
                }
                
            }
            return resXml;
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (IPAddress.TryParse(textBox1.Text, out IPAddress serverIP))
            {
                comboBox1.DataSource = null;

                string XmlPath = localPath + UR.Req1;
                
                XDocument reqXml = new XDocument(new XElement("path", textBox2.Text))
                {
                    Declaration = new XDeclaration("1.0", "utf-8", null)
                };
                reqXml.Save(XmlPath);

                XDocument resXml = TalkOutIn(XmlPath, serverIP);

                if (resXml != null && resXml.Descendants("error").Count() == 0)
                {
                    procs.Clear();
                    button2.Enabled = false;
                    
                    resXml.Save(localPath + UR.Res1);
                    
                    foreach (XElement node in resXml.Descendants("process"))
                    {
                        procs.Add(new Process(node));
                    }

                    if (procs.Count > 0)
                    {
                        button2.Enabled = true;
                        procs.Sort(CompareProcess);
                        comboBox1.DataSource = procs;
                        comboBox1.DisplayMember = "Description";
                    }
                    else
                    {
                        label1.Text = "No results";
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (IPAddress.TryParse(textBox1.Text, out IPAddress serverIP) && comboBox1.Items.Count > 0)
            {
                string XmlPath = localPath + UR.Req2;
                
                XDocument reqXml = new XDocument(procs[comboBox1.SelectedIndex].ToXml())
                {
                    Declaration = new XDeclaration("1.0", "utf-8", null)
                };
                reqXml.Save(XmlPath);

                XDocument resXml = TalkOutIn(XmlPath, serverIP);
                
                if (resXml != null && resXml.Descendants("error").Count() == 0)
                {
                    resXml.Save(localPath + UR.Res2);
                    label1.Text = resXml.Descendants("mesg").Single().Value;
                }

                button1_Click(sender, e);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
