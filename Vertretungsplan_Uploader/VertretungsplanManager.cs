using System;
using System.Timers;
using Vertretungsplan_Uploader.DataClasses;
using System.IO;
using VertretungsplanUploader;

namespace Vertretungsplan_Uploader
{
    public class VertretungsplanManager
    {
        private Settings _settings;
        private FtpTools _ftpTools;
        private MainWindow _rootWindow;
        private DateTime _currentFileEdit;
        private Timer _changeChecker;
        private string _onlineFilename;
        public DateTime FileLastEdited
        {
            get
            {
                return new FileInfo(_settings.FilePath).LastWriteTime;
            }
            set
            {
                new FileInfo(_settings.FilePath).LastWriteTime = value;
            }
        }
        private readonly string[] _filenames = new string[]
        {
            "schuelerplan_mo",
            "schuelerplan_di",
            "schuelerplan_mi",
            "schuelerplan_do",
            "schuelerplan_fr"
        };

        public VertretungsplanManager(Settings pSettings, MainWindow pRoot)
        {
            _rootWindow = pRoot;
            _settings = pSettings;
            _ftpTools = new FtpTools(_settings);

            DeleteAllOnlineFilesAsync();
            _onlineFilename = "";

            _changeChecker = new Timer(10000);
            _changeChecker.AutoReset = true;
            _changeChecker.Elapsed += ChangeChecker_Elapsed;
            _currentFileEdit = FileLastEdited;
            _changeChecker.Start();

            if (File.Exists(_settings.FilePath))
                FileLastEdited = DateTime.Now;
        }

        private void ChangeChecker_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(File.Exists(_settings.FilePath))
            {
                if (_currentFileEdit.CompareTo(FileLastEdited) >= 0)
                    return;
                _currentFileEdit = FileLastEdited;
                Log(string.Format("Es wurde eine Änderung im Pfad {0} erkannt, letzte Änderung: {1}", _settings.FilePath, _currentFileEdit.ToLongTimeString()));

                DeleteAllOnlineFiles();

                _onlineFilename = RenameChangedFile();
                if (_onlineFilename == null)
                    return;

                try
                {
                    Log(string.Format("Datei erfolgreich in {0}.html umbenannt, beginne mit dem Upload...", _onlineFilename));
                    _ftpTools.UploadFile(_settings.LocalFolder + "/" + _onlineFilename + ".html", _settings.RemotePath + _onlineFilename + ".html");
                    Log("Datei erfolgreich hochgeladen, lösche temporäre Datei...");
                }
                catch (System.Net.WebException wex) { Log("Es ist ein Fehler beim Hochladen der Datei aufgetreten: " + wex.Message); }
                catch (IOException ioe) { Log("Es ist ein Fehler beim Lesen der Quelldatei aufgetreten: " + ioe.Message); }
                catch (Exception ex) { Log("Es ist ein unbekannter Fehler aufgetreten: " + ex.Message); }

                File.Delete(_settings.LocalFolder + "/" + _onlineFilename + ".html");
            }
            else if (!_onlineFilename.Equals(""))
            {
                Log("Die Datei schuelerplan.html wurde lokal gelöscht. Lösche online...");
                _ftpTools.DeleteFile(_settings.RemotePath + _onlineFilename + ".html");
                _onlineFilename = "";
            }
        }

        public void changeSettings(Settings pNew)
        {
            _settings = pNew;
            _ftpTools = new FtpTools(_settings);
        }

        public string RenameChangedFile()
        {
            string file = File.ReadAllText(_settings.FilePath, System.Text.Encoding.Default);
            string filename = "";

            if (file.Contains("Vertretungsplan f&uuml;r Montag") || file.Contains("Vertretungsplan für Montag"))
                filename = _filenames[0];
            else if (file.Contains("Vertretungsplan f&uuml;r Dienstag") || file.Contains("Vertretungsplan für Dienstag"))
                filename = _filenames[1];
            else if (file.Contains("Vertretungsplan f&uuml;r Mittwoch") || file.Contains("Vertretungsplan für Mittwoch"))
                filename = _filenames[2];
            else if (file.Contains("Vertretungsplan f&uuml;r Donnerstag") || file.Contains("Vertretungsplan für Donnerstag"))
                filename = _filenames[3];
            else if (file.Contains("Vertretungsplan f&uuml;r Freitag") || file.Contains("Vertretungsplan für Freitag"))
                filename = _filenames[4];

            if (filename.Equals(""))
            {
                Log("Die Datei enthält keine Merkmale, stoppe Bearbeitung!");
                return null;
            }
            filename += "_" + _settings.SavePostfix;

            if (File.Exists(_settings.LocalFolder + filename + ".html"))
                File.Delete(_settings.LocalFolder + filename + ".html");
            File.Copy(_settings.FilePath, _settings.LocalFolder + "/" + filename + ".html", true);
            return filename;
        }

        public void DeleteAllOnlineFilesAsync() => new Action(() => DeleteAllOnlineFiles()).BeginInvoke(null, this);

        private void DeleteAllOnlineFiles()
        {
            foreach (string name in _filenames)
            {
                try
                {
                    _ftpTools.DeleteFile(string.Format("{0}{1}_{2}.html", _settings.RemotePath, name, _settings.SavePostfix));
                }
                catch (Exception e) { System.Diagnostics.Debug.WriteLine(e.Message); }
            }
        }

        private void Log(string pMessage) => _rootWindow.AppendMessageToLog(pMessage);
    }
}
