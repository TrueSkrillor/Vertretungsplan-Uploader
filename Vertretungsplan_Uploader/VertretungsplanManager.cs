using System;
using System.Timers;
using Vertretungsplan_Uploader.DataClasses;
using System.IO;
using VertretungsplanUploader;

namespace Vertretungsplan_Uploader
{
    public class VertretungsplanManager
    {
        private Settings mainSettings;
        private FtpTools tools;
        private MainWindow root;
        private DateTime fileLastEdited;
        private Timer changeChecker;
        private string currentlyOnlineFilename;

        private readonly string[] filenames = new string[]
        {
            "schuelerplan_mo",
            "schuelerplan_di",
            "schuelerplan_mi",
            "schuelerplan_do",
            "schuelerplan_fr"
        };

        public VertretungsplanManager(Settings pSettings, MainWindow pRoot)
        {
            root = pRoot;
            mainSettings = pSettings;
            tools = new FtpTools(mainSettings);

            DeleteAllOnlineFilesAsync();
            currentlyOnlineFilename = "";

            changeChecker = new Timer(10000);
            changeChecker.AutoReset = true;
            changeChecker.Elapsed += ChangeChecker_Elapsed;
            fileLastEdited = new FileInfo(mainSettings.LocalPath + "/schuelerplan.html").LastWriteTime;
            changeChecker.Start();

            if (File.Exists(mainSettings.LocalPath + "/schuelerplan.html"))
                new FileInfo(mainSettings.LocalPath + "/schuelerplan.html").LastWriteTime = DateTime.Now;
        }

        private void ChangeChecker_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(File.Exists(mainSettings.LocalPath + "/schuelerplan.html"))
            {
                if (fileLastEdited.CompareTo(new FileInfo(mainSettings.LocalPath + "/schuelerplan.html").LastWriteTime) >= 0)
                    return;
                fileLastEdited = new FileInfo(mainSettings.LocalPath + "/schuelerplan.html").LastWriteTime;
                root.AppendMessageToLog(string.Format("Es wurde eine Änderung im Pfad {0} erkannt, letzte Änderung: {1}", mainSettings.LocalPath, fileLastEdited.ToLongTimeString()));

                DeleteAllOnlineFiles();

                currentlyOnlineFilename = renameChangedFile();
                if (currentlyOnlineFilename == null)
                    return;

                try
                {
                    root.AppendMessageToLog(string.Format("Datei erfolgreich in {0}.html umbenannt, beginne mit dem Upload...", currentlyOnlineFilename));
                    tools.UploadFile(mainSettings.LocalPath + "/" + currentlyOnlineFilename + ".html", mainSettings.RemotePath + currentlyOnlineFilename + ".html");
                    root.AppendMessageToLog("Datei erfolgreich hochgeladen, lösche temporäre Datei...");
                }
                catch (System.Net.WebException wex)
                {
                    root.AppendMessageToLog("Es ist ein Fehler beim Hochladen der Datei aufgetreten: " + wex.Message);
                }
                catch (IOException ioe)
                {
                    root.AppendMessageToLog("Es ist ein Fehler beim Lesen der Quelldatei aufgetreten: " + ioe.Message);
                }
                catch (Exception ex)
                {
                    root.AppendMessageToLog("Es ist ein unbekannter Fehler aufgetreten: " + ex.Message);
                }
                File.Delete(mainSettings.LocalPath + "/" + currentlyOnlineFilename + ".html");
            }
            else if (!currentlyOnlineFilename.Equals(""))
            {
                root.AppendMessageToLog("Die Datei schuelerplan.html wurde lokal gelöscht. Lösche online...");
                tools.DeleteFile(mainSettings.RemotePath + currentlyOnlineFilename + ".html");
                currentlyOnlineFilename = "";
            }
        }

        public void changeSettings(Settings pNew)
        {
            mainSettings = pNew;
            tools = new FtpTools(mainSettings);
        }

        public string renameChangedFile()
        {
            string file = File.ReadAllText(mainSettings.LocalPath + "/schuelerplan.html", System.Text.Encoding.Default);
            string filename = "";

            if (file.Contains("Vertretungsplan f&uuml;r Montag") || file.Contains("Vertretungsplan für Montag"))
            {
                filename = "schuelerplan_mo_" + mainSettings.SavePostfix;
            }
            else if (file.Contains("Vertretungsplan f&uuml;r Dienstag") || file.Contains("Vertretungsplan für Dienstag"))
            {
                filename = "schuelerplan_di_" + mainSettings.SavePostfix;
            }
            else if (file.Contains("Vertretungsplan f&uuml;r Mittwoch") || file.Contains("Vertretungsplan für Mittwoch"))
            {
                filename = "schuelerplan_mi_" + mainSettings.SavePostfix;
            }
            else if (file.Contains("Vertretungsplan f&uuml;r Donnerstag") || file.Contains("Vertretungsplan für Donnerstag"))
            {
                filename = "schuelerplan_do_" + mainSettings.SavePostfix;
            }
            else if (file.Contains("Vertretungsplan f&uuml;r Freitag") || file.Contains("Vertretungsplan für Freitag"))
            {
                filename = "schuelerplan_fr_" + mainSettings.SavePostfix;
            }

            if (filename.Equals(""))
            {
                root.AppendMessageToLog("Die Datei enthält keine Merkmale, stoppe Bearbeitung!");
                return null;
            }
            if (File.Exists(mainSettings.LocalPath + filename + ".html"))
                File.Delete(mainSettings.LocalPath + filename + ".html");
            File.Copy(mainSettings.LocalPath + "/schuelerplan.html", mainSettings.LocalPath + "/" + filename + ".html", true);
            return filename;
        }

        public void DeleteAllOnlineFilesAsync() => new Action(() => DeleteAllOnlineFiles()).BeginInvoke(null, this);

        private void DeleteAllOnlineFiles()
        {
            foreach (string name in filenames)
            {
                try
                {
                    tools.DeleteFile(string.Format("{0}{1}_{2}.html", mainSettings.RemotePath, name, mainSettings.SavePostfix));
                }
                catch (Exception e) { System.Diagnostics.Debug.WriteLine(e.Message); }
            }
        }
    }
}
