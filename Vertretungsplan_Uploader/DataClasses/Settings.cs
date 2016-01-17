using System;

namespace Vertretungsplan_Uploader.DataClasses
{
    [Serializable]
    public class Settings
    {
        public string LocalFolder { get; }
        public string RemotePath { get; }
        public string Username { get; }
        public string Password { get; }
        public string SavePostfix { get; }
        public string FilePath
        {
            get
            {
                return LocalFolder + "/schuelerplan.html";
            }
        }

        public Settings(string pLocal, string pRemote, string pUser, string pPass, string pPost)
        {
            LocalFolder = pLocal;
            RemotePath = pRemote;
            Username = pUser;
            Password = pPass;
            SavePostfix = pPost;
        }
    }
}
