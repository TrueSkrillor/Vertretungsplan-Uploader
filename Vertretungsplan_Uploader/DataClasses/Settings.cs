using System;

namespace Vertretungsplan_Uploader.DataClasses
{
    [Serializable]
    public class Settings
    {
        public string LocalFolderToday { get; }
        public string LocalFolderTomorrow { get; }
        public string RemotePath { get; }
        public string Username { get; }
        public string Password { get; }

        public string FilePathToday
        {
            get { return LocalFolderToday + "/schuelerplan.html"; }
        }
        public string FilePathTomorrow
        {
            get { return LocalFolderTomorrow + "/schuelerplan.html"; }
        }

        public Settings(string pLocalToday, string pLocalTomorrow, string pRemote, string pUser, string pPass)
        {
            LocalFolderToday = pLocalToday;
            LocalFolderTomorrow = pLocalTomorrow;
            RemotePath = pRemote;
            Username = pUser;
            Password = pPass;
        }
    }
}
