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
    /// Summary description for FileDownloadHandler
    /// </summary>
    public class FileDownloadHandler : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            string cloud = context.Request["cloud"];
            string fileId = context.Request["fileId"];
            string fileName = (context.Request["fileName"] != null) ? context.Request["fileName"] : "download.jpg";

            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            CloudItem metadata = cloudConsumer.GetFileMetadata(fileId);
            CloudFileData cfd = cloudConsumer.GetDocument(metadata);
            Stream fileStream = cfd.FileStream;
            MemoryStream memStream = new MemoryStream();
            fileStream.CopyTo(memStream);


            context.Response.ClearContent();
            context.Response.Clear();
            context.Response.ContentType = "application/octet-stream";
            context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + cfd.CloudItem.Name + "\";");

            context.Response.AddHeader("Content-Length", memStream.Length.ToString());
            memStream.WriteTo(context.Response.OutputStream);
            context.Response.Flush();
            context.Response.Close();
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