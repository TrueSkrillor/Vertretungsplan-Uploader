using System;
using System.Windows;
using MahApps.Metro.Controls;
using Vertretungsplan_Uploader;
using System.IO;
using System.Windows.Forms;
using Vertretungsplan_Uploader.Properties;

namespace VertretungsplanUploader
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private VertretungsplanManager _manager;

        private NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Vertretungsplan_Uploader.Properties.Resources.notifyIcon;
            notifyIcon.Text = Vertretungsplan_Uploader.Properties.Resources.NOTIFY_ICON_TEXT;
            notifyIcon.Visible = false;
            notifyIcon.Click += NotifyIcon_Click;

            new Action(() => ApplySettings()).BeginInvoke(null, this);
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
            if (Settings.Default.SettingsConfigured && _manager != null)
            {
                try
                {
                    _manager.DeleteAllOnlineFilesAsync();
                    if (File.Exists(Settings.Default.FilePathToday))
                        _manager.TodayLastEdited = DateTime.Now;
                    if (File.Exists(Settings.Default.FilePathTomorrow))
                        _manager.TomorrowLastEdited = DateTime.Now;
                }
                catch (IOException ex) { AppendMessageToLog(Vertretungsplan_Uploader.Properties.Resources.ERROR_CHANGING_LAST_EDIT_TIME + ex.Message); }
            }
            else
                AppendMessageToLog(Vertretungsplan_Uploader.Properties.Resources.ERROR_MANUAL_SYNC_MISSING_SETTINGS);
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e) => new Action(() => SaveSettings()).Invoke();

        private void SaveSettings()
        {
            Settings.Default.LocalFolderToday = tbLocalToday.Text;
            Settings.Default.LocalFolderTomorrow = tbLocalTomorrow.Text;
            Settings.Default.FtpPath = tbFtpFolder.Text;
            Settings.Default.FtpUsername = tbFtpUser.Text;
            Settings.Default.FtpPassword = tbFtpPassword.Password;
            Settings.Default.GcmApiKey = tbGcmApiKey.Text;

            Settings.Default.Save();

            if (tbLocalToday.Text.Equals("") || tbLocalTomorrow.Text.Equals("") || tbFtpFolder.Text.Equals("") || tbFtpPassword.Password.Equals("") || tbFtpUser.Text.Equals(""))
            {
                AppendMessageToLog(Vertretungsplan_Uploader.Properties.Resources.ERROR_SAVE_SETTINGS_MISSING_SETTINGS);
                return;
            }

            Settings.Default.SettingsConfigured = true;
            Settings.Default.Save();

            AppendMessageToLog(Vertretungsplan_Uploader.Properties.Resources.SAVED_SETTINGS);

            if (_manager != null)
                _manager.ChangeSettings();
            else
                _manager = new VertretungsplanManager(this);

            AppendMessageToLog(Vertretungsplan_Uploader.Properties.Resources.SETTINGS_DELIVERED_TO_MANAGER);
            flyoutSettings.IsOpen = false;
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            if(WindowState == WindowState.Minimized) {
                notifyIcon.Visible = true;
                ShowInTaskbar = false;
            }
        }

        public void AppendMessageToLog(string pMessage)
        {
            Dispatcher.BeginInvoke(new Action(() => tbStatus.AppendText(string.Format("[{0}][{1}]\t{2}\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), pMessage))));
            Dispatcher.BeginInvoke(new Action(() => tbStatus.ScrollToEnd()));
        }

        private void ApplySettings()
        {
            Dispatcher.BeginInvoke(new Action(() => tbLocalToday.Text = Settings.Default.LocalFolderToday));
            Dispatcher.BeginInvoke(new Action(() => tbLocalTomorrow.Text = Settings.Default.LocalFolderTomorrow));
            Dispatcher.BeginInvoke(new Action(() => tbFtpFolder.Text = Settings.Default.FtpPath));
            Dispatcher.BeginInvoke(new Action(() => tbFtpUser.Text = Settings.Default.FtpUsername));
            Dispatcher.BeginInvoke(new Action(() => tbFtpPassword.Password = Settings.Default.FtpPassword));
            Dispatcher.BeginInvoke(new Action(() => tbGcmApiKey.Text = Settings.Default.GcmApiKey));

            if (!Settings.Default.SettingsConfigured)
                AppendMessageToLog(Vertretungsplan_Uploader.Properties.Resources.LOAD_SETTINGS_NO_SETTINGS_CONFIGURED);
            else
            {
                _manager = new VertretungsplanManager(this);
                AppendMessageToLog(Vertretungsplan_Uploader.Properties.Resources.SUCCESSFULLY_LOADED_SETTINGS);
            }
        }

        private void btnLocalToday_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            DialogResult result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
                tbLocalToday.Text = folderDialog.SelectedPath;
        }

        private void btnLocalTomorrow_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            DialogResult result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
                tbLocalTomorrow.Text = folderDialog.SelectedPath;
        }
    }
}
