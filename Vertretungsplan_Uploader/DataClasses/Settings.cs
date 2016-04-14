using System;

namespace Vertretungsplan_Uploader.DataClasses
{
    [Serializable]
    internal class Settings
    {
        internal string LocalFolderToday { get; }
        internal string LocalFolderTomorrow { get; }
        internal string RemotePath { get; }
        internal string Username { get; }
        internal string Password { get; }
        internal string GcmApiKey { get; }

        internal string FilePathToday
        {
            get { return LocalFolderToday + "/schuelerplan.html"; }
        }
        internal string FilePathTomorrow
        {
            get { return LocalFolderTomorrow + "/schuelerplan.html"; }
        }

        internal Settings(string pLocalToday, string pLocalTomorrow, string pRemote, string pUser, string pPass, string pGcmKey)
        {
            LocalFolderToday = pLocalToday;
            LocalFolderTomorrow = pLocalTomorrow;
            RemotePath = pRemote;
            Username = pUser;
            Password = pPass;
            GcmApiKey = pGcmKey;
        }
    }
}
