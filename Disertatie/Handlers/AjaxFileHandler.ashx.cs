using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Disertatie.Handlers
{
    public class AjaxFileHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.Files.Count > 0)
            {
                string path = context.Server.MapPath("~/Temp");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var file = context.Request.Files[0];
                string filename = Path.Combine(path, file.FileName);
                file.SaveAs(filename);

                context.Response.ContentType = "text/plain";
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var result = new { name = file.FileName };
                context.Response.Write(serializer.Serialize(result));
            }
        }

        public bool IsReusable { get { return false; } }
    }
}