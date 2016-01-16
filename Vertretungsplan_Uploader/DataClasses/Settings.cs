using System;

namespace Vertretungsplan_Uploader.DataClasses
{
    [Serializable]
    public class Settings
    {
        public string LocalPath { get; set; }
        public string RemotePath { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SavePostfix { get; set; }

        public Settings(string pLocal, string pRemote, string pUser, string pPass, string pPost)
        {
            LocalPath = pLocal;
            RemotePath = pRemote;
            Username = pUser;
            Password = pPass;
            SavePostfix = pPost;
        }
    }
}
