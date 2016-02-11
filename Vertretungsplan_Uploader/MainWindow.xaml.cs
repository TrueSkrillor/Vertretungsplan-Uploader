using System;
using System.Windows;
using MahApps.Metro.Controls;
using Vertretungsplan_Uploader.DataClasses;
using Vertretungsplan_Uploader;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Diagnostics;

namespace VertretungsplanUploader
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private Settings _todaySettings;
        private Settings _tomorrowSettings;
        private VertretungsplanManager _managerToday;
        private VertretungsplanManager _managerTomorrow;
        private System.Timers.Timer updateChecker;

        private NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Vertretungsplan_Uploader.Properties.Resources.notifyIcon;
            notifyIcon.Text = "Vertretungsplan";
            notifyIcon.Visible = false;
            notifyIcon.Click += NotifyIcon_Click;

            new Action(() => _todaySettings = LoadSettings(Types.Heute)).BeginInvoke(null, this);
            new Action(() => _tomorrowSettings = LoadSettings(Types.Morgen)).BeginInvoke(null, this);

            updateChecker = new System.Timers.Timer(3600000);
            updateChecker.AutoReset = true;
            updateChecker.Elapsed += UpdateChecker_Elapsed;
            updateChecker.Start();
        }

        private void UpdateChecker_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "VUUpdate.exe"))
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "VUUpdate.exe");
            else 
                AppendMessageToLog("Es wurde kein Updater gefunden!");
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
            if (_todaySettings != null && _tomorrowSettings != null && _managerToday != null && _managerTomorrow != null)
            {
                try
                {
                    _managerToday.DeleteAllOnlineFilesAsync();
                    _managerTomorrow.DeleteAllOnlineFilesAsync();
                    if (File.Exists(_todaySettings.FilePath))
                        _managerToday.FileLastEdited = DateTime.Now;
                    if (File.Exists(_tomorrowSettings.FilePath))
                        _managerTomorrow.FileLastEdited = DateTime.Now;
                }
                catch (IOException ex) { AppendMessageToLog("Es ist ein Fehler beim Ändern der letzten Bearbeitungszeit aufgetreten: " + ex.Message); }
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
            _todaySettings = new Settings(tbLocalToday.Text, tbFtpFolder.Text, tbFtpUser.Text, tbFtpPassword.Password, Types.Heute);
            _tomorrowSettings = new Settings(tbLocalTomorrow.Text, tbFtpFolder.Text, tbFtpUser.Text, tbFtpPassword.Password, Types.Morgen);

            AppendMessageToLog("Einstellungen gespeichert!");

            if (_managerToday != null)
                _managerToday.changeSettings(_todaySettings);
            else
                _managerToday = new VertretungsplanManager(_todaySettings, this);
            if (_managerTomorrow != null)
                _managerTomorrow.changeSettings(_tomorrowSettings);
            else
                _managerTomorrow = new VertretungsplanManager(_tomorrowSettings, this);

            AppendMessageToLog("Einstellungen an den Uploadmanager übergeben.");
            flyoutSettings.IsOpen = false;

        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_todaySettings != null)
                SaveSettingsToFile(Types.Heute);
            if (_tomorrowSettings != null)
                SaveSettingsToFile(Types.Morgen);
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            if(WindowState == WindowState.Minimized) {
                notifyIcon.Visible = true;
                ShowInTaskbar = false;
            }
        }

        public void AppendMessageToLog(string pMessage) => Dispatcher.BeginInvoke(new Action(() => tbStatus.Text = tbStatus.Text + String.Format("[{0}][{1}]\t{2}\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), pMessage)));

        private Settings LoadSettings(Types pExtension)
        {
            if (File.Exists("./settings" + pExtension.ToString().ToLower() + ".bin"))
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("./settings" + pExtension.ToString().ToLower() + ".bin", FileMode.Open, FileAccess.Read, FileShare.Read);
                Settings result = (Settings)formatter.Deserialize(stream);
                stream.Close();

                if (result != null)
                {
                    if (pExtension == Types.Heute)
                    {
                        Dispatcher.BeginInvoke(new Action(() => tbLocalToday.Text = result.LocalFolder));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpFolder.Text = result.RemotePath));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpUser.Text = result.Username));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpPassword.Password = result.Password));
                        _managerToday = new VertretungsplanManager(result, this);
                    }
                    else {
                        Dispatcher.BeginInvoke(new Action(() => tbLocalTomorrow.Text = result.LocalFolder));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpFolder.Text = result.RemotePath));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpUser.Text = result.Username));
                        Dispatcher.BeginInvoke(new Action(() => tbFtpPassword.Password = result.Password));
                        _managerTomorrow = new VertretungsplanManager(result, this);
                    }

                }

                AppendMessageToLog("Einstellungen erfolgreich aus settings" + pExtension.ToString().ToLower() + ".bin geladen");
                return result;
            }
            AppendMessageToLog("Keine vorherigen Einstellungen für " + pExtension.ToString().ToLower() + " gefunden, bitte konfigurieren!");
            return null;
        }

        private void SaveSettingsToFile(Types pExtension)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("./settings" + pExtension.ToString().ToLower() + ".bin", FileMode.Create, FileAccess.Write, FileShare.None);
            if (pExtension == Types.Heute)
                formatter.Serialize(stream, _todaySettings);
            else
                formatter.Serialize(stream, _tomorrowSettings);
            stream.Close();
        }
    }
}
