using System;
using System.ComponentModel;
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

            ((ISupportInitialize)(this.EventLog)).BeginInit();
            if (!EventLog.SourceExists(this.EventLog.Source))
            {
                EventLog.CreateEventSource(this.EventLog.Source, this.EventLog.Log);
            }
            ((ISupportInitialize)(this.EventLog)).EndInit();

            this.EventLog.Source = this.ServiceName;
            this.EventLog.Log = "Application";

            string cmdLine = Environment.CommandLine.Remove(Environment.CommandLine.Length - 2, 2).Remove(0, 1);
            string appFolder = Path.GetDirectoryName(cmdLine);
            string path = appFolder + "\\PhantomBotService.config";
            this.nextAttempt = DateTime.Now;

            try
            {
                this.f = File.Open(path, FileMode.Open, FileAccess.Read);
            }
            catch (IOException e)
            {
                this.EventLog.WriteEntry("Failed to open config file: " + path + Environment.NewLine + e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace, EventLogEntryType.Error);
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
            this.log = sdata[4].Equals("true", StringComparison.OrdinalIgnoreCase);

            this.phantomBotProcess = new Process();
            this.phantomBotProcess.StartInfo.WorkingDirectory = workingDir;
            this.phantomBotProcess.StartInfo.FileName = "java-runtime\\bin\\java.exe";
            this.phantomBotProcess.StartInfo.Arguments = "--add-opens java.base/java.lang=ALL-UNNAMED -Djava.security.policy=config/security -Dinteractive -Xms1m -Dfile.encoding=UTF-8 -jar \"PhantomBot.jar\"";
            this.phantomBotProcess.StartInfo.CreateNoWindow = true;
            this.phantomBotProcess.StartInfo.UseShellExecute = false;
            this.phantomBotProcess.EnableRaisingEvents = true;

            this.phantomBotProcess.Exited += this.PhantomBotProcess_Exited;

            if (this.log)
            {
                this.phantomBotProcess.StartInfo.RedirectStandardOutput = true;
                this.phantomBotProcess.StartInfo.RedirectStandardError = true;
                this.phantomBotProcess.StartInfo.RedirectStandardInput = true;
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
            string path = this.phantomBotProcess.StartInfo.WorkingDirectory + "\\PhantomBotService." + DateTime.Now.ToFileTime() + ".log";

            try
            {
                if (this.log)
                {
                    if (this.f.CanRead || this.f.CanWrite)
                    {
                        this.f.Close();
                    }
                    this.f = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                    this.logLine(DateTime.Now.ToShortDateString() + " @ " + DateTime.Now.ToShortTimeString() + " >> Starting PhantomBot" + Environment.NewLine);
                }

            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry("Failed to open log file: " + path + Environment.NewLine + e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace, EventLogEntryType.Error);
            }

            try
            {
                this.phantomBotProcess.Start();
                this.phantomBotProcess.BeginErrorReadLine();
                this.phantomBotProcess.BeginOutputReadLine();
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry("Failed to start process: " + this.phantomBotProcess.StartInfo.WorkingDirectory + "\\" + this.phantomBotProcess.StartInfo.FileName + Environment.NewLine + e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace, EventLogEntryType.Error);
            }

        }

        protected override void OnStop()
        {
            try
            {
                this.allowExit = true;

                using (StreamWriter writer = this.phantomBotProcess.StandardInput)
                {
                    writer.WriteLine("exit");
                }

                for (int i = 0; i < 30; i++)
                {
                    if (this.phantomBotProcess.HasExited)
                    {
                        this.phantomBotProcess.Close();
                        break;
                    }

                    Thread.Sleep(500);
                }
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry("Failed to end process" + Environment.NewLine + e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace, EventLogEntryType.Error);
            }

            try
            {
                if (!this.phantomBotProcess.HasExited)
                {
                    this.phantomBotProcess.Kill();
                }
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry("Failed to kill process" + Environment.NewLine + e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace, EventLogEntryType.Error);
            }

            try
            {
                if (this.log)
                {
                    this.logLine(DateTime.Now.ToShortDateString() + " @ " + DateTime.Now.ToShortTimeString() + " >> Stopping PhantomBot" + Environment.NewLine);
                    this.f.Close();
                }
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry("Failed to close log file" + Environment.NewLine + e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace, EventLogEntryType.Error);
            }
        }

        private void logLine(string logline)
        {
            try
            {
                this.f.Write(Encoding.UTF8.GetBytes(logline), 0, logline.Length);
                this.f.Flush();
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry("Failed to write to log file: " + logline + Environment.NewLine + Environment.NewLine + e.GetType().FullName + ": " + e.Message + Environment.NewLine + e.StackTrace, EventLogEntryType.Error);
            }

        }
    }
}
