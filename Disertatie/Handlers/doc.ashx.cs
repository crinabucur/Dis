using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.SessionState;
using CloudProject;

namespace Disertatie.Handlers
{
    /// <summary>
    /// Summary description for doc
    /// </summary>
    public class doc : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[context.Request["cloud"] + "Consumer"] as CloudStorageConsumer;
            Stream docStream = cloudConsumer.GetDocument(context.Request["fileId"]);
            
            byte[] buffer = new byte[16*1024];
            MemoryStream ms = new MemoryStream();
            
            int read;
            while ((read = docStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }

            ms.Position = 0;

            var boxViewID = "";
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            String url_ = @"https://upload.view-api.box.com/1/documents";
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url_);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Headers.Add("Authorization:Token " + "oufc00ht4ccaag8urwxjlqlx6pykhzvw");
            wr.Headers.Add("Accept-Encoding", "gzip, deflate");
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
            wr.Timeout = 1000000;
            wr.SendChunked = true;
            DateTime start = DateTime.Now;
            Exception exc = null;
            Stream rs = wr.GetRequestStream();
            try
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);

                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n"; // non_svg=\"true\"; 
                string header = string.Format(headerTemplate, "file", "preview.doc", "application/msword");
                Console.WriteLine(header);
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                rs.Write(headerbytes, 0, headerbytes.Length);
                
                byte[] buff = new byte[40960];
                int bytesRead = 0;

                int totalSent = 0;
                int totalLength = (int)ms.Length;

                while ((bytesRead = ms.Read(buff, 0, buff.Length)) != 0)
                {
                    totalSent += bytesRead;
                    rs.Write(buff, 0, bytesRead);
                }
                ms.Close();
                docStream.Close();
            }
            catch (Exception ex)
            {
                exc = ex;
            }
            DateTime end = DateTime.Now;
            int seconds = (int)(end - start).TotalSeconds;
            if (seconds >= 0)
            {
                if (exc != null)
                {
                    throw exc;
                }
            }
            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                var res = reader2.ReadToEnd();
                var docRes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(res);
                if (docRes["id"] != null)
                    boxViewID = docRes["id"];
            }
            catch (Exception ex)
            {
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr.Abort();
                wr = null;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://view-api.box.com/1/documents/" + boxViewID + "/content.pdf");
            request.Headers["Authorization"] = "Token " + "oufc00ht4ccaag8urwxjlqlx6pykhzvw";
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            int hitcount = 0;
            while (response.ContentLength <= 4)
            {
                request.Abort();
                request = (HttpWebRequest)WebRequest.Create("https://view-api.box.com/1/documents/" + boxViewID + "/content.pdf");
                request.Headers["Authorization"] = "Token " + "oufc00ht4ccaag8urwxjlqlx6pykhzvw";
                request.Method = "GET";
                response = (HttpWebResponse)request.GetResponse();
                if (hitcount == 10)
                    break;
                hitcount++;
            }


            var stream = response.GetResponseStream();

            byte[] pdfArray;
            using (MemoryStream mems = new MemoryStream())
            {
                byte[] buffer2 = new byte[16 * 1024];
                int read2;

                while ((read2 = stream.Read(buffer2, 0, buffer2.Length)) > 0)
                {
                    mems.Write(buffer2, 0, read2);
                }
                pdfArray = mems.ToArray();
            }

            request.Abort();

            HttpContext.Current.Response.ContentType = "application/pdf";
            HttpContext.Current.Response.OutputStream.Write(pdfArray, 0, pdfArray.Length);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}