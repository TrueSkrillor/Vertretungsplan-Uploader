using System;
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
        
        public FtpTools(string pUser, string pPassword)
        {
            _credentials = new NetworkCredential(pUser, pPassword);
        }

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
            try
            {
                ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
            }
            catch (WebException ex)
            {
                Console.WriteLine("Ein Webfehler ist aufgetreten: " + ex.Message);
                return;
            }
            Console.WriteLine("Datei hochgeladen, Status " + ftpWebResponse.StatusDescription);
            ftpWebResponse.Close();
        }

        public void DeleteFile(string pRemoteFile)
        {
            Debug.WriteLine("Requested to delete " + pRemoteFile, "FTP-Tools");
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(pRemoteFile);
            ftpWebRequest.Method = "DELE";
            ftpWebRequest.Credentials = _credentials;
            FtpWebResponse ftpWebResponse = null;
            try
            {
                ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
            }
            catch (WebException ex)
            {
                Console.WriteLine("Ein Webfehler ist aufgetreten: " + ex.Message);
                return;
            }
            Console.WriteLine("Datei gelöscht, Status " + ftpWebResponse.StatusDescription);
            ftpWebResponse.Close();
        }
    }
}
