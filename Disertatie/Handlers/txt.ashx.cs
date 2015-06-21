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
    /// Summary description for txt
    /// </summary>
    public class txt : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[context.Request["cloud"] + "Consumer"] as CloudStorageConsumer;
            Stream txtStream = cloudConsumer.GetDocument(context.Request["fileId"]);
            byte[] txtArray;
            
            byte[] buffer = new byte[16*1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = txtStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                txtArray = ms.ToArray();
            }

            HttpContext.Current.Response.ContentType = "text/plain";
            HttpContext.Current.Response.OutputStream.Write(txtArray, 0, txtArray.Length);
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