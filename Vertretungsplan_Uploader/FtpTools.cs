using System.Text;
using System.Net;
using Vertretungsplan_Uploader.DataClasses;
using System.IO;
using System.Diagnostics;

namespace Vertretungsplan_Uploader
{
    public class FtpTools
    {
        private NetworkCredential _credentials;
        
        public FtpTools(string pUser, string pPassword) { _credentials = new NetworkCredential(pUser, pPassword); }
        public FtpTools(Settings pSettings) : this(pSettings.Username, pSettings.Password) { }

        public void UploadFile(string pLocalFile, string pRemoteFile)
        {
            Debug.WriteLine(string.Format("Request to upload {0} to {1}", pLocalFile, pRemoteFile), "FTP-Tools");
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(pRemoteFile);
            ftpWebRequest.Method = "STOR";
            ftpWebRequest.Credentials = _credentials;

            StreamReader streamReader = new StreamReader(pLocalFile);
            byte[] bytes = Encoding.UTF8.GetBytes(streamReader.ReadToEnd());
            streamReader.Close();
            ftpWebRequest.ContentLength = bytes.LongLength;
            Stream requestStream = ftpWebRequest.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();

            FtpWebResponse ftpWebResponse = null;
            try { ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse(); }
            catch (WebException ex) { Debug.WriteLine(string.Format("A webexception occured while trying to upload {0}: {1}", pLocalFile, ex.Message), "FTP-Tools"); return; }

            Debug.WriteLine(string.Format("Uploaded file successfully (response status: {0})" ,ftpWebResponse.StatusDescription, "FTP-Tools"));
            ftpWebResponse.Close();
        }

        public void DeleteFile(string pRemoteFile)
        {
            Debug.WriteLine(string.Format("Requested to delete {0}", pRemoteFile), "FTP-Tools");
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(pRemoteFile);
            ftpWebRequest.Method = "DELE";
            ftpWebRequest.Credentials = _credentials;

            FtpWebResponse ftpWebResponse = null;
            try { ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse(); }
            catch (WebException ex) { Debug.WriteLine(string.Format("A webexception occured while trying to delete {0}: {1}", pRemoteFile, ex.Message), "FTP-Tools"); return; }

            Debug.WriteLine(string.Format("Deleted file successfully (response status: {0})", ftpWebResponse.StatusDescription), "FTP-Tools");
            ftpWebResponse.Close();
        }
    }
}
