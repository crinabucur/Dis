using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using CloudProject;

namespace Disertatie.Handlers
{
    /// <summary>
    /// Summary description for mp4
    /// </summary>
    public class mp4 : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[context.Request["cloud"] + "Consumer"] as CloudStorageConsumer;
            Stream videoStream = cloudConsumer.GetDocument(context.Request["fileId"]);
            byte[] videoArray;
            
            byte[] buffer = new byte[16*1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = videoStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                videoArray = ms.ToArray();
            }

            HttpContext.Current.Response.ContentType = "video/mp4";
            HttpContext.Current.Response.OutputStream.Write(videoArray, 0, videoArray.Length);
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