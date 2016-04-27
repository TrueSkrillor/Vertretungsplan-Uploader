using System;
using System.Timers;
using Vertretungsplan_Uploader.Properties;
using System.IO;
using VertretungsplanUploader;
using System.Diagnostics;
using Vertretungsplan_Uploader.Tools;

namespace Vertretungsplan_Uploader
{
    internal class VertretungsplanManager
    {
        private FtpTools _ftpTools;
        private MainWindow _window;
        private Timer _changeChecker;
        private string _onlineToday;
        private string _onlineTomorrow;
        private DateTime _onlineFileEditToday;
        private DateTime _onlineFileEditTomorrow;
        private GcmTools _gcmTools;
        internal DateTime TodayLastEdited
        {
            get { return new FileInfo(Settings.Default.FilePathToday).LastWriteTime; }
            set { new FileInfo(Settings.Default.FilePathToday).LastWriteTime = value; }
        }
        internal DateTime TomorrowLastEdited
        {
            get { return new FileInfo(Settings.Default.FilePathTomorrow).LastWriteTime; }
            set { new FileInfo(Settings.Default.FilePathTomorrow).LastWriteTime = value; }
        }
        private readonly string[] _daysOfWeek = new string[] { "mo", "di", "mi", "do", "fr" };

        internal VertretungsplanManager(MainWindow pWindow)
        {
            _window = pWindow;
            _ftpTools = new FtpTools();
            _gcmTools = new GcmTools();

            DeleteAllOnlineFilesAsync();
            _onlineToday = "";
            _onlineTomorrow = "";

            _changeChecker = new Timer(10000);
            _changeChecker.AutoReset = true;
            _changeChecker.Elapsed += ChangeChecker_Elapsed;
            _onlineFileEditToday = DateTime.MinValue;
            _onlineFileEditTomorrow = DateTime.MinValue;
            _changeChecker.Start();

            if (File.Exists(Settings.Default.FilePathToday))
                TodayLastEdited = DateTime.Now;
            if (File.Exists(Settings.Default.FilePathTomorrow))
                TomorrowLastEdited = DateTime.Now;
        }

        private void ChangeChecker_Elapsed(object sender, ElapsedEventArgs e)
        {
            _changeChecker.Stop();

            CheckForChanges(true);
            CheckForChanges(false);

            _changeChecker.Start();
        }

        private void CheckForChanges(bool pToday)
        {
            if (File.Exists(pToday ? Settings.Default.FilePathToday : Settings.Default.FilePathTomorrow))
            {
                if ((pToday ? _onlineFileEditToday : _onlineFileEditTomorrow).CompareTo(pToday ? TodayLastEdited : TomorrowLastEdited) >= 0)
                    return;

                Log(string.Format(Resources.DETECTED_CHANGE, pToday ? Settings.Default.LocalFolderToday : Settings.Default.LocalFolderTomorrow, pToday ? TodayLastEdited : TomorrowLastEdited));

                DeleteOnlineFiles(pToday);

                string onlineName = RenameChangedFile(pToday);
                if (onlineName == null)
                    return;

                try
                {
                    Log(string.Format(Resources.SUCCESSFULLY_RENAMED_FILE, onlineName));
                    _ftpTools.UploadFile((pToday ? Settings.Default.LocalFolderToday : Settings.Default.LocalFolderTomorrow) + "/" + onlineName + ".html", Settings.Default.FtpPath + onlineName + ".html");
                    if (pToday) _onlineFileEditToday = TodayLastEdited; else _onlineFileEditTomorrow = TomorrowLastEdited;
                    if (pToday) _onlineToday = onlineName; else _onlineTomorrow = onlineName;
                    Log(Resources.SUCCESSFULLY_UPLOADED_FILE);
                    JsonTools.GenerateJson((pToday ? Settings.Default.LocalFolderToday : Settings.Default.LocalFolderTomorrow), onlineName);
                    Log(Resources.GENERATED_JSON_FILE);
                    _ftpTools.UploadFile((pToday ? Settings.Default.LocalFolderToday : Settings.Default.LocalFolderTomorrow) + "/" + onlineName + ".json", Settings.Default.FtpPath + onlineName + ".json");
                    Log(Resources.DELETE_TEMPORARY_FILES);

                    Log(Resources.NOTIFYING_MOBILE_DEVICES);
                    //Log(string.Format(Resources.NOTIFICATION_COMPLETE, _gcmTools.SendBroadcast(Resources.NOTIFICATION_NEW_VP_ONLINE)));
                }
                catch (System.Net.WebException wex) { Log(string.Format(Resources.ERROR_UPLOADING_FILE,  wex.Message)); }
                catch (IOException ioe) { Log(string.Format(Resources.ERROR_READING_SOURCE_FILE,  ioe.Message)); }
                catch (Exception ex) { Log(string.Format(Resources.ERROR_UNKNOWN,  ex.Message)); }

                File.Delete((pToday ? Settings.Default.LocalFolderToday : Settings.Default.LocalFolderTomorrow) + "/" + onlineName + ".html");
                File.Delete((pToday ? Settings.Default.LocalFolderToday : Settings.Default.LocalFolderTomorrow) + "/" + onlineName + ".json");
            }
            else if (!(pToday ? _onlineToday : _onlineTomorrow).Equals(""))
            {
                Log(Resources.DELETED_LOCAL_FILE);
                _ftpTools.DeleteFile(Settings.Default.FtpPath + (pToday ? _onlineToday : _onlineTomorrow) + ".html");
                _ftpTools.DeleteFile(Settings.Default.FtpPath + (pToday ? _onlineToday : _onlineTomorrow) + ".json");
                if (pToday) _onlineToday = ""; else _onlineTomorrow = "";
            }
        }

        public void ChangeSettings()
        {
            _ftpTools = new FtpTools();
        }

        internal string RenameChangedFile(bool pToday)
        {
            string file = File.ReadAllText(pToday ? Settings.Default.FilePathToday : Settings.Default.FilePathTomorrow, System.Text.Encoding.UTF8);
            string filename = "schuelerplan_";

            if (file.Contains("Vertretungsplan f&uuml;r Montag") || file.Contains("Vertretungsplan für Montag"))
                filename += _daysOfWeek[0];
            else if (file.Contains("Vertretungsplan f&uuml;r Dienstag") || file.Contains("Vertretungsplan für Dienstag"))
                filename += _daysOfWeek[1];
            else if (file.Contains("Vertretungsplan f&uuml;r Mittwoch") || file.Contains("Vertretungsplan für Mittwoch"))
                filename += _daysOfWeek[2];
            else if (file.Contains("Vertretungsplan f&uuml;r Donnerstag") || file.Contains("Vertretungsplan für Donnerstag"))
                filename += _daysOfWeek[3];
            else if (file.Contains("Vertretungsplan f&uuml;r Freitag") || file.Contains("Vertretungsplan für Freitag"))
                filename += _daysOfWeek[4];

            if (filename.Equals("schuelerplan_"))
            {
                Log(Resources.NO_CHARACTERISTICS_FOUND);
                return null;
            }
            filename += "_" + (pToday ? "heute" : "morgen");

            if (File.Exists((pToday ? Settings.Default.LocalFolderToday : Settings.Default.LocalFolderTomorrow) + filename + ".html"))
                File.Delete((pToday ? Settings.Default.LocalFolderToday : Settings.Default.LocalFolderTomorrow) + filename + ".html");
            File.Copy(pToday ? Settings.Default.FilePathToday : Settings.Default.FilePathTomorrow, (pToday ? Settings.Default.LocalFolderToday : Settings.Default.LocalFolderTomorrow) + "/" + filename + ".html", true);
            return filename;
        }

        internal void DeleteAllOnlineFilesAsync()
        {
            new Action(() => DeleteOnlineFiles(true)).BeginInvoke(null, this);
            new Action(() => DeleteOnlineFiles(false)).BeginInvoke(null, this);
        }

        private void DeleteOnlineFiles(bool pToday)
        {
                foreach (string day in _daysOfWeek)
                {
                    try
                    {
                        _ftpTools.DeleteFile(string.Format("{0}schuelerplan_{1}_{2}.html", Settings.Default.FtpPath, day, pToday ? "heute" : "morgen"));
                        _ftpTools.DeleteFile(string.Format("{0}schuelerplan_{1}_{2}.json", Settings.Default.FtpPath, day, pToday ? "heute" : "morgen"));
                    }
                    catch (Exception e) { Debug.WriteLine(e.Message); }
                }
        }

        private void Log(string pMessage) => _window.AppendMessageToLog(pMessage);
    }
}
