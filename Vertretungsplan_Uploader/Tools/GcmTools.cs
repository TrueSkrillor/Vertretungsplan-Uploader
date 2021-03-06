﻿using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using System.IO;
using Vertretungsplan_Uploader.Properties;

namespace Vertretungsplan_Uploader.Tools
{
    internal class GcmTools
    {
        private readonly string API_KEY;

        internal GcmTools()
        {
            API_KEY = Settings.Default.GcmApiKey;
        }

        internal string SendBroadcast(string message)
        {
            JObject jGcmData = new JObject();
            JObject jData = new JObject();

            jData.Add(new JProperty("message", message));
            jGcmData.Add(new JProperty("to", "/topics/vertretungsplan"));
            jGcmData.Add("data", jData);

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] data = encoding.GetBytes(jGcmData.ToString());

            HttpWebRequest messageRequest = WebRequest.CreateHttp("https://android.googleapis.com/gcm/send");
            messageRequest.ContentType = "application/json";
            messageRequest.Headers.Add(HttpRequestHeader.Authorization, "key=" + API_KEY);
            messageRequest.Method = "POST";

            Stream requestStream = messageRequest.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            WebResponse response = messageRequest.GetResponse();
            StreamReader responseReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            return responseReader.ReadToEnd();
        } 
    }
}
