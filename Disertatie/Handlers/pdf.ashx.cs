using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using CloudStorage;

namespace Disertatie.Handlers
{
    /// <summary>
    /// Summary description for pdf
    /// </summary>
    public class pdf : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[context.Request["cloud"] + "Consumer"] as CloudStorageConsumer;
            Stream pdfStream = cloudConsumer.GetDocument(context.Request["fileId"]);
            byte[] pdfArray;
            
            byte[] buffer = new byte[16*1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = pdfStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                pdfArray = ms.ToArray();
            }

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