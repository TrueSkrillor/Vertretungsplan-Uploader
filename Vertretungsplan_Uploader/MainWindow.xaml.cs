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
        private Settings _settings;
        private VertretungsplanManager _manager;
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

            new Action(() => _settings = LoadSettings()).BeginInvoke(null, this);

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
            if (_settings != null && _manager != null)
            {
                try
                {
                    _manager.DeleteAllOnlineFilesAsync();
                    if (File.Exists(_settings.FilePathToday))
                        _manager.TodayLastEdited = DateTime.Now;
                    if (File.Exists(_settings.FilePathTomorrow))
                        _manager.TomorrowLastEdited = DateTime.Now;
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
            _settings = new Settings(tbLocalToday.Text, tbLocalTomorrow.Text, tbFtpFolder.Text, tbFtpUser.Text, tbFtpPassword.Password);

            AppendMessageToLog("Einstellungen gespeichert!");

            if (_manager != null)
                _manager.changeSettings(_settings);
            else
                _manager = new VertretungsplanManager(_settings, this);

            AppendMessageToLog("Einstellungen an den Uploadmanager übergeben.");
            flyoutSettings.IsOpen = false;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) => SaveSettingsToFile();

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            if(WindowState == WindowState.Minimized) {
                notifyIcon.Visible = true;
                ShowInTaskbar = false;
            }
        }

        public void AppendMessageToLog(string pMessage) => Dispatcher.BeginInvoke(new Action(() => tbStatus.AppendText(string.Format("[{0}][{1}]\t{2}\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), pMessage))));

        private Settings LoadSettings()
        {
            if (File.Exists("./settings.bin"))
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("./settings.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
                Settings result = (Settings)formatter.Deserialize(stream);
                stream.Close();

                if (result != null)
                {
                    Dispatcher.BeginInvoke(new Action(() => tbLocalToday.Text = result.LocalFolderToday));
                    Dispatcher.BeginInvoke(new Action(() => tbLocalTomorrow.Text = result.LocalFolderTomorrow));
                    Dispatcher.BeginInvoke(new Action(() => tbFtpFolder.Text = result.RemotePath));
                    Dispatcher.BeginInvoke(new Action(() => tbFtpUser.Text = result.Username));
                    Dispatcher.BeginInvoke(new Action(() => tbFtpPassword.Password = result.Password));
                    _manager = new VertretungsplanManager(result, this);

                }

                AppendMessageToLog("Einstellungen erfolgreich aus settings.bin geladen");
                return result;
            }
            AppendMessageToLog("Keine vorherigen Einstellungen gefunden, bitte konfigurieren!");
            return null;
        }

        private void SaveSettingsToFile()
        {
            if (_settings == null)
                return;
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("./settings.bin", FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, _settings);
            stream.Close();
        }
    }
}
