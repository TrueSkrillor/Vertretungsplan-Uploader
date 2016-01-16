using System;
using System.Windows;
using MahApps.Metro.Controls;
using Vertretungsplan_Uploader.DataClasses;
using Vertretungsplan_Uploader;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace VertretungsplanUploader
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private Settings todaySettings;
        private Settings tomorrowSettings;
        private VertretungsplanManager managerToday;
        private VertretungsplanManager managerTomorrow;

        private NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Vertretungsplan_Uploader.Properties.Resources.notifyIcon;
            notifyIcon.Text = "Vertretungsplan";
            notifyIcon.Visible = false;
            notifyIcon.Click += NotifyIcon_Click;

            new Action(() => todaySettings = loadSettings("heute")).BeginInvoke(null, this);
            new Action(() => tomorrowSettings = loadSettings("morgen")).BeginInvoke(null, this);
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            ShowInTaskbar = true;
            notifyIcon.Visible = false;
            WindowState = WindowState.Normal;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e) => flyoutSettings.IsOpen = !flyoutSettings.IsOpen;

        private void btnManualSync_Click(object sender, RoutedEventArgs e)
        {
            if (todaySettings != null && tomorrowSettings != null && managerToday != null && managerTomorrow != null)
            {
                try
                {
                    managerToday.DeleteAllOnlineFilesAsync();
                    managerTomorrow.DeleteAllOnlineFilesAsync();
                    if (File.Exists(todaySettings.LocalPath + "/schuelerplan.html"))
                        new FileInfo(todaySettings.LocalPath + "/schuelerplan.html").LastWriteTime = DateTime.Now;
                    if (File.Exists(tomorrowSettings.LocalPath + "/schuelerplan.html"))
                        new FileInfo(tomorrowSettings.LocalPath + "/schuelerplan.html").LastWriteTime = DateTime.Now;
                }
                catch (IOException ex)
                {
                    AppendMessageToLog("Es ist ein Fehler beim Ändern der letzten Bearbeitungszeit aufgetreten: " + ex.Message);
                }
            }
            else
                AppendMessageToLog("Vor einem manuellen Sync müssen alle Einstellungen korrekt eingestellt sein!");
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e) => new Action(() => SaveSettings()).Invoke();

        private void SaveSettings()
        {
            if(tbLocalToday.Text.Equals("") || tbLocalTomorrow.Text.Equals("") || tbFtpFolder.Text.Equals("") || tbFtpPassword.Password.Equals("") || tbFtpUser.Text.Equals(""))
            {
                AppendMessageToLog("Es fehlen einige Einstellungen. Die Einstellungen konnten nicht gespeichert werden!");
                return;
            }
            todaySettings = new Settings(tbLocalToday.Text, tbFtpFolder.Text, tbFtpUser.Text, tbFtpPassword.Password, "heute");
            tomorrowSettings = new Settings(tbLocalTomorrow.Text, tbFtpFolder.Text, tbFtpUser.Text, tbFtpPassword.Password, "morgen");

            AppendMessageToLog("Einstellungen gespeichert!");

            if (managerToday != null)
                managerToday.changeSettings(todaySettings);
            else
                managerToday = new VertretungsplanManager(todaySettings, this);
            if (managerTomorrow != null)
                managerTomorrow.changeSettings(tomorrowSettings);
            else
                managerTomorrow = new VertretungsplanManager(tomorrowSettings, this);

            AppendMessageToLog("Einstellungen an den Uploadmanager übergeben.");
            flyoutSettings.IsOpen = false;

        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (todaySettings != null)
                SaveSettingsToFile("heute");
            if (tomorrowSettings != null)
                SaveSettingsToFile("morgen");
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            if(WindowState == WindowState.Minimized) {
                notifyIcon.Visible = true;
                ShowInTaskbar = false;
            }
        }

        public void AppendMessageToLog(string pMessage) => Dispatcher.BeginInvoke(new Action(() => tbStatus.Text = tbStatus.Text + String.Format("[{0}][{1}]\t{2}\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), pMessage)));

        private Settings loadSettings(string pExtension)
        {
            if (File.Exists("./settings" + pExtension + ".bin"))
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("./settings" + pExtension + ".bin", FileMode.Open, FileAccess.Read, FileShare.Read);
                Settings result = (Settings)formatter.Deserialize(stream);
                stream.Close();

                if (result != null)
                {
                    if (pExtension.Equals("heute"))
                    {
                        Dispatcher.BeginInvoke(new Action(() => tbLocalToday.Text = result.LocalPath));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpFolder.Text = result.RemotePath));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpUser.Text = result.Username));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpPassword.Password = result.Password));
                        managerToday = new VertretungsplanManager(result, this);
                    }
                    else {
                        Dispatcher.BeginInvoke(new Action(() => tbLocalTomorrow.Text = result.LocalPath));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpFolder.Text = result.RemotePath));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpUser.Text = result.Username));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpPassword.Password = result.Password));
                        managerTomorrow = new VertretungsplanManager(result, this);
                    }

                }

                AppendMessageToLog("Einstellungen erfolgreich aus settings" + pExtension + ".bin geladen");
                return result;
            }
            AppendMessageToLog("Keine vorherigen Einstellungen für " + pExtension + " gefunden, bitte konfigurieren!");
            return null;
        }

        private void SaveSettingsToFile(string pExtension)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("./settings" + pExtension + ".bin", FileMode.Create, FileAccess.Write, FileShare.None);
            if (pExtension.Equals("heute"))
                formatter.Serialize(stream, todaySettings);
            else
                formatter.Serialize(stream, tomorrowSettings);
            stream.Close();
        }
    }
}
