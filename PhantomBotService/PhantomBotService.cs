using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace PhantomBotService
{
    public partial class PhantomBotService : ServiceBase
    {
        private Process phantomBotProcess;
        private FileStream f;
        private bool log = false;
        private bool allowExit = false;
        private DateTime nextAttempt;
        private System.Timers.Timer t;

        public PhantomBotService()
        {
            this.InitializeComponent();
            string cmdLine = Environment.CommandLine.Remove(Environment.CommandLine.Length - 2, 2).Remove(0, 1);
            string appFolder = Path.GetDirectoryName(cmdLine);
            string path = appFolder + "\\PhantomBotService.config";
            this.nextAttempt = DateTime.Now;

            try
            {
                this.f = File.Open(path, FileMode.Open, FileAccess.Read);
            }
            catch (IOException)
            {
            }

            byte[] b = new byte[1024];
            byte[] data = new byte[this.f.Length];
            int k = 0;

            while (this.f.Read(b, 0, b.Length) > 0)
            {
                for (int i = 0; i < b.Length; i++)
                {
                    if (k < data.Length)
                    {
                        data[k] = b[i];
                        k++;
                    }
                }
            }

            this.f.Close();

            string configData = Encoding.UTF8.GetString(data);

            string[] sdata = configData.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            string workingDir = sdata[1];
            string exec = sdata[4];
            this.log = sdata[7].Equals("true", StringComparison.OrdinalIgnoreCase);

            this.phantomBotProcess = new Process();
            this.phantomBotProcess.StartInfo.WorkingDirectory = workingDir;
            this.phantomBotProcess.StartInfo.FileName = exec;
            this.phantomBotProcess.StartInfo.CreateNoWindow = true;
            this.phantomBotProcess.StartInfo.UseShellExecute = true;
            this.phantomBotProcess.EnableRaisingEvents = true;

            this.phantomBotProcess.Exited += this.PhantomBotProcess_Exited;

            if (this.log)
            {
                this.phantomBotProcess.OutputDataReceived += this.PhantomBotProcess_OutputDataReceived;
                this.phantomBotProcess.ErrorDataReceived += this.PhantomBotProcess_ErrorDataReceived;
            }
        }

        private void PhantomBotProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e) => this.logLine("!! " + e.Data + Environment.NewLine);
        private void PhantomBotProcess_OutputDataReceived(object sender, DataReceivedEventArgs e) => this.logLine(">> " + e.Data + Environment.NewLine);

        private void PhantomBotProcess_Exited(object sender, EventArgs e)
        {
            if (this.allowExit)
            {
                return;
            }

            if (this.t != null)
            {
                this.t.Dispose();

                this.t = null;
            }

            if (this.phantomBotProcess.HasExited)
            {
                if (DateTime.Compare(this.nextAttempt, DateTime.Now) >= 0)
                {
                    this.nextAttempt = DateTime.Now.AddSeconds(5);
                    this.OnStart(null);
                }
                else
                {
                    this.t = new System.Timers.Timer
                    {
                        Interval = 5000,
                        AutoReset = false
                    };

                    this.t.Elapsed += this.PhantomBotProcess_Exited;

                    this.t.Start();
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                if (this.log)
                {
                    if (this.f.CanRead || this.f.CanWrite)
                    {
                        this.f.Close();
                    }

                    string path = this.phantomBotProcess.StartInfo.WorkingDirectory + "\\PhantomBotService." + DateTime.Now.ToFileTime() + ".log";
                    this.f = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
                    this.logLine(DateTime.Now.ToShortDateString() + " @ " + DateTime.Now.ToShortTimeString() + " >> Starting PhantomBot" + Environment.NewLine);
                }

                this.phantomBotProcess.Start();
            }
            catch (Exception)
            {
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (this.log)
                {
                    this.logLine(DateTime.Now.ToShortDateString() + " @ " + DateTime.Now.ToShortTimeString() + " >> Stopping PhantomBot" + Environment.NewLine);
                    this.f.Close();
                }

                this.allowExit = true;
                this.phantomBotProcess.CloseMainWindow();
                Thread.Sleep(15000);

                if (this.phantomBotProcess.HasExited)
                {
                    this.phantomBotProcess.Close();
                }
                else
                {
                    this.phantomBotProcess.Kill();
                }
            }
            catch (Exception)
            {
            }
        }

        private void logLine(string logline) => this.f.Write(Encoding.UTF8.GetBytes(logline), 0, logline.Length);
    }
}
