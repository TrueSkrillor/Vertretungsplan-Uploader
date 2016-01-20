using System;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace VUUpdate
{
    class Program
    {
        private static string urlBase = "https://github.com/TrueSkrillor/Vertretungsplan-Uploader/releases/download/{0}/Binaries.zip";
        private static Version currentUploaderVersion {
            get
            {
                FileVersionInfo versionInfo;
                try { versionInfo = FileVersionInfo.GetVersionInfo("./Vertretungsplan_Uploader.exe"); }
                catch (FileNotFoundException e) { Console.WriteLine("Die Datei Vertretungsplan_Uploader.exe existiert im aktuellen Verzeichnis nicht, beende Updater!"); return null; }

                return new Version(versionInfo.ProductVersion);
            }
        }

        static void Main(string[] args)
        {
            bool performedUpdate;
            do
            {
                performedUpdate = false;
                for (int versionType = 0; versionType < 4 || performedUpdate; versionType++)
                {
                    Console.WriteLine("Checking for new update, VersionType: " + versionType);
                    if (CheckForUpdate((VersionTypes)versionType))
                    {
                        Console.WriteLine(string.Format("New Update detected; Release type: {0}", versionType));
                        PerformUpdate((VersionTypes)versionType);
                        performedUpdate = true;
                    }
                }
            } while (performedUpdate);
            Console.WriteLine("If an update was available, everything is fine now!");
        }

        private static void PerformUpdate(VersionTypes newVersionType)
        {
            KillProcessIfRunning();
            DownloadAndApplyUpdate(newVersionType);
            Process.Start("./Vertretungsplan_Uploader.exe");
        }

        private static void DownloadAndApplyUpdate(VersionTypes newVersionType)
        {
            WebClient downloadClient = new WebClient();
            downloadClient.DownloadFile(string.Format(urlBase, GetNewVersion(newVersionType)), "./temp.zip");
            Console.WriteLine("Update downloaded successfully");

            ZipFile.ExtractToDirectory("./temp.zip", "temp");
            foreach(string filename in Directory.GetFiles("./temp"))
            {
                if (File.Exists("./" + filename))
                    File.Delete("./" + filename);
                File.Move("./temp/" + filename, "./" + filename);
                Console.WriteLine("Successfully updated " + filename);
            }
            Directory.Delete("./temp");
            Console.WriteLine("Update completed");
        }
        private static Version GetNewVersion(VersionTypes versionType)
        {
            var fileVersion = currentUploaderVersion;
            Version toBeChecked = null;
            switch (versionType)
            {
                case VersionTypes.Major:
                    toBeChecked = new Version(fileVersion.Major + 1, fileVersion.Minor, fileVersion.Build, fileVersion.Revision);
                    break;
                case VersionTypes.Minor:
                    toBeChecked = new Version(fileVersion.Major, fileVersion.Minor + 1, fileVersion.Build, fileVersion.Revision);
                    break;
                case VersionTypes.Build:
                    toBeChecked = new Version(fileVersion.Major, fileVersion.Minor, fileVersion.Build + 1, fileVersion.Revision);
                    break;
                case VersionTypes.Revision:
                    toBeChecked = new Version(fileVersion.Major, fileVersion.Minor, fileVersion.Build, fileVersion.Revision + 1);
                    break;
            }
            return toBeChecked;
        }
        private static void KillProcessIfRunning()
        {
            foreach (Process process in Process.GetProcessesByName("Vertretungsplan_Uploader"))
            {
                process.Kill();
                Console.WriteLine("Killed process \"Vertretungsplan_Uploader\" successfully.");
            }
        }
        private static bool CheckForUpdate(VersionTypes versionType) => CheckForOnlineFile(string.Format(urlBase, GetNewVersion(versionType)));
        private static bool CheckForOnlineFile(string address)
        {
            HttpWebRequest checkRequest = (HttpWebRequest)WebRequest.Create(address);
            checkRequest.Method = "HEAD";

            try { checkRequest.GetResponse(); }
            catch (WebException we)
            {
                HttpWebResponse httpResponse = we.Response as HttpWebResponse;
                //If the response contains a 404 Not Found, file does not exist
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    return false;
            }
            return true;
        }
    }

    enum VersionTypes { Major, Minor, Build, Revision }
}
