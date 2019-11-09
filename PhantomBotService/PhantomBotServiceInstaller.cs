using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace PhantomBotService
{
    [RunInstaller(true)]
    public partial class PhantomBotServiceInstaller : System.Configuration.Install.Installer
    {
        private bool processInstalled = false;
        private bool serviceInstalled = false;
        private string appFolder = "";

        public PhantomBotServiceInstaller()
        {
            this.InitializeComponent();

            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            processInstaller.AfterInstall += this.ProcessInstaller_AfterInstall;

            serviceInstaller.DisplayName = "PhantomBotService";
            serviceInstaller.DelayedAutoStart = false;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = "PhantomBotService";
            serviceInstaller.AfterInstall += this.ServiceInstaller_AfterInstall;

            this.Installers.Add(processInstaller);
            this.Installers.Add(serviceInstaller);
        }

        private void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            this.serviceInstalled = true;

            this.appFolder = this.Context.Parameters["TargetDir"];

            this.UpdateConfig();
        }

        private void ProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            this.processInstalled = true;

            this.appFolder = this.Context.Parameters["TargetDir"];

            this.UpdateConfig();
        }

        private void UpdateConfig()
        {
            if (!this.processInstalled || !this.serviceInstalled)
            {
                return;
            }

            FileStream f = File.Open(this.appFolder, FileMode.OpenOrCreate, FileAccess.Write);

            string data = "[Bot Install Directory]" + Environment.NewLine +
                this.appFolder.Replace("\\\\", "") + Environment.NewLine + Environment.NewLine +
                "[Bot launch Script]" + Environment.NewLine + "launch-service.bat" + Environment.NewLine + Environment.NewLine +
                "[Logging Enabled]" + Environment.NewLine + "false";

            f.Write(Encoding.UTF8.GetBytes(data), 0, data.Length);
            f.Close();
        }
    }
}
