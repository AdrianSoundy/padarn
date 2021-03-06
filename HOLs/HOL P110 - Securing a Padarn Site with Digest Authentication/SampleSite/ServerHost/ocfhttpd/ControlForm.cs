using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using OpenNETCF.Diagnostics;
using System.IO;
using OpenNETCF.Threading;
using OpenNETCF.Web.Server;
using OpenNETCF.Windows.Forms;
using System.Net.Sockets;
using System.Diagnostics;
using Padarn;
using System.Security.Cryptography;
using OpenNETCF.Web.Security.Cryptography;

namespace SampleSite
{
    public partial class ControlForm : Form
    {
        private static WebServer m_ws;
        private NotifyIcon m_notifyIcon;

        public ControlForm()
        {
            // don't show in the taskbar
            // for some reason this worked in CF 1.0, but fails in CF 2.0
            OpenNETCF.Win32.Win32Window w = new OpenNETCF.Win32.Win32Window(this.Handle);
            w.ExtendedStyle |= OpenNETCF.Win32.WS_EX.NOANIMATION;

            try
            {
                m_ws = new WebServer();

                m_ws.Configuration.Authentication.AuthenticationCallback = VerifyUsername;

                m_ws.Configuration.Authentication.Users.Add(new OpenNETCF.Web.Configuration.User { Name = "username", Password = "password" });

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            InitializeComponent();

            m_notifyIcon = new NotifyIcon();
            m_notifyIcon.Icon = this.Icon;
            m_notifyIcon.Visible = true;
            m_notifyIcon.Click += new EventHandler(m_notifyIcon_Click);
            m_notifyIcon.DoubleClick += new EventHandler(m_notifyIcon_DoubleClick);
            this.Visible = true;
        }

        bool VerifyUsername(IAuthenticationCallbackInfo info)
        {
            // no username is invalid
            if (string.IsNullOrEmpty(info.UserName)) return false;

            // first do a lookup of the password - this might come from a database, file, etc
            string password = GetPasswordForUser(info.UserName);
            if (password == null) return false;

            // determine the type
            BasicAuthInfo basic = info as BasicAuthInfo;
            if (basic != null)
            {
                // we're using basic auth
                return (basic.Password == password);
            }

            // it wasn't basic, so it must be digest
            DigestAuthInfo digest = info as DigestAuthInfo;
            return digest.MatchCredentials(password);

        }

        private string GetPasswordForUser(string username)
        {
            if (username == "admin")
            {
                return "passw0rd";
            }

            return null;
        }

        private string MD5Hash(string str)
        {
            MD5 hash = MD5.Create();
            byte[] h = hash.ComputeHash(Encoding.ASCII.GetBytes(str));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in h)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        void m_notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Visible = true;
        }

        void m_notifyIcon_Click(object sender, EventArgs e)
        {
            try
            {
                this.contextMenu1.Show(this, new Point(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
            }
            catch
            {
                m_notifyIcon_DoubleClick(sender, e);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            if (base.Width < 491)
                base.Width = 491;
            if (base.Height < 192)
                base.Height = 192;
            base.OnResize(e);

        }

        private void UpdateIP()
        {
            if (m_ws.Configuration.LocalIP.Equals(IPAddress.Any))
            {
                this.lblIP.Text = "Padarn IP: {All Unassigned}";
            }
            else
            {
                this.lblIP.Text = "Padarn IP:" + m_ws.Configuration.LocalIP;
            }

            foreach (var address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                Debug.WriteLine(address.ToString());
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            btnStart.Enabled = true;

            UpdateIP();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (m_ws.Running)
            {
                btnStop_Click(null, null);
            }

            m_notifyIcon.Visible = false;
            m_notifyIcon.Dispose();
        }

        private void HandleException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0} at {1}\r\n", ex.GetType().FullName, DateTime.Now.ToString("MM/dd/yy hh:mm:ss")));
            sb.Append(string.Format("Message: {0}\r\n", ex.Message));
            sb.Append(string.Format("Stack trace: {0}\r\n", ex.StackTrace));

            Trace2.WriteLine(sb.ToString());

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            else
            {
                string exceptionPath = Path.Combine(Program.AppPath, "exceptionlog.txt");
                StreamWriter writer = File.Exists(exceptionPath) ? File.AppendText(exceptionPath) : File.CreateText(exceptionPath);

                writer.Write(sb.ToString());
                writer.Close();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                this.miStartPadarn.Enabled = this.btnStart.Enabled = true;
                this.miStop.Enabled = this.btnStop.Enabled = false;
                this.lblStatus.Text = "Padarn is currently stopped.";
                m_ws.Stop();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateIP();
                this.miStartPadarn.Enabled = this.btnStart.Enabled = false;
                this.miStop.Enabled = this.btnStop.Enabled = true;
                this.lblStatus.Text = "Padarn is currently running.";
                m_ws.Start();
            }
            /*
        catch (CertificateNotFoundException ex)
        {
            MessageBox.Show(ex.Message, "Certificate Error");
        }
        catch (InvalidCertificatePasswordException ex)
        {
            MessageBox.Show(ex.Message, "Certificate Error");
        }
        catch (PlatformNotSupportedException ex)
        {
            MessageBox.Show(ex.Message, "Certificate Error");
        */
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void miOpen_Click(object sender, EventArgs e)
        {
            this.Visible = true;
            this.BringToFront();
        }
    }
}