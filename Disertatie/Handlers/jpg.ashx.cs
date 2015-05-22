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
    /// Summary description for jpg
    /// </summary>
    public class jpg : IHttpHandler, IRequiresSessionState
    {

        public void ProcessRequest(HttpContext context)
        {
            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[context.Request["cloud"] + "Consumer"] as CloudStorageConsumer;
            Stream imageStream = cloudConsumer.GetDocument(context.Request["fileId"]);
            byte[] imageArray;

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = imageStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                imageArray = ms.ToArray();
            }

            HttpContext.Current.Response.ContentType = "image/png";
            HttpContext.Current.Response.OutputStream.Write(imageArray, 0, imageArray.Length);
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