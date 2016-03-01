using System;
using System.Timers;
using Vertretungsplan_Uploader.DataClasses;
using System.IO;
using VertretungsplanUploader;
using System.Diagnostics;
using Vertretungsplan_Uploader.Tools;

namespace Vertretungsplan_Uploader
{
    public class VertretungsplanManager
    {
        private Settings _settings;
        private FtpTools _ftpTools;
        private MainWindow _window;
        private Timer _changeChecker;
        private string _onlineToday;
        private string _onlineTomorrow;
        private DateTime _onlineFileEditToday;
        private DateTime _onlineFileEditTomorrow;
        public DateTime TodayLastEdited
        {
            get { return new FileInfo(_settings.FilePathToday).LastWriteTime; }
            set { new FileInfo(_settings.FilePathToday).LastWriteTime = value; }
        }
        public DateTime TomorrowLastEdited
        {
            get { return new FileInfo(_settings.FilePathTomorrow).LastWriteTime; }
            set { new FileInfo(_settings.FilePathTomorrow).LastWriteTime = value; }
        }
        private readonly string[] _daysOfWeek = new string[] { "mo", "di", "mi", "do", "fr" };

        public VertretungsplanManager(Settings pSettings, MainWindow pWindow)
        {
            _window = pWindow;
            _settings = pSettings;
            _ftpTools = new FtpTools(_settings);

            DeleteAllOnlineFilesAsync();
            _onlineToday = "";
            _onlineTomorrow = "";

            _changeChecker = new Timer(10000);
            _changeChecker.AutoReset = true;
            _changeChecker.Elapsed += ChangeChecker_Elapsed;
            _onlineFileEditToday = DateTime.MinValue;
            _onlineFileEditTomorrow = DateTime.MinValue;
            _changeChecker.Start();

            if (File.Exists(_settings.FilePathToday))
                TodayLastEdited = DateTime.Now;
            if (File.Exists(_settings.FilePathTomorrow))
                TomorrowLastEdited = DateTime.Now;
        }

        private void ChangeChecker_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckForChanges(true);
            CheckForChanges(false);
        }

        private void CheckForChanges(bool pToday)
        {
            if (File.Exists(pToday ? _settings.FilePathToday : _settings.FilePathTomorrow))
            {
                if ((pToday ? _onlineFileEditToday : _onlineFileEditTomorrow).CompareTo(pToday ? TodayLastEdited : TomorrowLastEdited) >= 0)
                    return;

                Log(string.Format("Es wurde eine Änderung im Pfad {0} erkannt, letzte Änderung: {1}", pToday ? _settings.LocalFolderToday : _settings.LocalFolderTomorrow, pToday ? TodayLastEdited : TomorrowLastEdited));

                DeleteOnlineFiles(pToday);

                string onlineName = RenameChangedFile(pToday);
                if (onlineName == null)
                    return;

                try
                {
                    Log(string.Format("Datei erfolgreich in {0}.html umbenannt, beginne mit dem Upload...", onlineName));
                    _ftpTools.UploadFile((pToday ? _settings.LocalFolderToday : _settings.LocalFolderTomorrow) + "/" + onlineName + ".html", _settings.RemotePath + onlineName + ".html");
                    if (pToday) _onlineFileEditToday = TodayLastEdited; else _onlineFileEditTomorrow = TomorrowLastEdited;
                    if (pToday) _onlineToday = onlineName; else _onlineTomorrow = onlineName;
                    Log("Datei erfolgreich hochgeladen, generiere JSON-Datei...");
                    JsonTools.GenerateJson((pToday ? _settings.LocalFolderToday : _settings.LocalFolderTomorrow), onlineName);
                    Log("JSON-Datei erstellt, beginne mit dem Upload...");
                    _ftpTools.UploadFile((pToday ? _settings.LocalFolderToday : _settings.LocalFolderTomorrow) + "/" + onlineName + ".json", _settings.RemotePath + onlineName + ".json");
                    Log("Lösche temporäre Dateien...");
                }
                catch (System.Net.WebException wex) { Log("Es ist ein Fehler beim Hochladen der Datei aufgetreten: " + wex.Message); }
                catch (IOException ioe) { Log("Es ist ein Fehler beim Lesen der Quelldatei aufgetreten: " + ioe.Message); }
                catch (Exception ex) { Log("Es ist ein unbekannter Fehler aufgetreten: " + ex.Message); }

                File.Delete((pToday ? _settings.LocalFolderToday : _settings.LocalFolderTomorrow) + "/" + onlineName + ".html");
                File.Delete((pToday ? _settings.LocalFolderToday : _settings.LocalFolderTomorrow) + "/" + onlineName + ".json");
            }
            else if (!(pToday ? _onlineToday : _onlineTomorrow).Equals(""))
            {
                Log("Die Datei schuelerplan.html wurde lokal gelöscht. Lösche online...");
                _ftpTools.DeleteFile(_settings.RemotePath + (pToday ? _onlineToday : _onlineTomorrow) + ".html");
                _ftpTools.DeleteFile(_settings.RemotePath + (pToday ? _onlineToday : _onlineTomorrow) + ".json");
                if (pToday) _onlineToday = ""; else _onlineTomorrow = "";
            }
        }

        public void changeSettings(Settings pNew)
        {
            _settings = pNew;
            _ftpTools = new FtpTools(_settings);
        }

        public string RenameChangedFile(bool pToday)
        {
            string file = File.ReadAllText(pToday ? _settings.FilePathToday : _settings.FilePathTomorrow, System.Text.Encoding.Default);
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
                Log("Die Datei enthält keine Merkmale, stoppe Bearbeitung!");
                return null;
            }
            filename += "_" + (pToday ? "heute" : "morgen");

            if (File.Exists((pToday ? _settings.LocalFolderToday : _settings.LocalFolderTomorrow) + filename + ".html"))
                File.Delete((pToday ? _settings.LocalFolderToday : _settings.LocalFolderTomorrow) + filename + ".html");
            File.Copy(pToday ? _settings.FilePathToday : _settings.FilePathTomorrow, (pToday ? _settings.LocalFolderToday : _settings.LocalFolderTomorrow) + "/" + filename + ".html", true);
            return filename;
        }

        public void DeleteAllOnlineFilesAsync()
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
                        _ftpTools.DeleteFile(string.Format("{0}schuelerplan_{1}_{2}.html", _settings.RemotePath, day, pToday ? "heute" : "morgen"));
                        _ftpTools.DeleteFile(string.Format("{0}schuelerplan_{1}_{2}.json", _settings.RemotePath, day, pToday ? "heute" : "morgen"));
                    }
                    catch (Exception e) { Debug.WriteLine(e.Message); }
                }
        }

        private void Log(string pMessage) => _window.AppendMessageToLog(pMessage);
    }
}
