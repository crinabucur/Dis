using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.SessionState;

namespace Disertatie.Handlers
{
    /// <summary>
    /// Summary description for FileUploadHandler
    /// </summary>
    public class FileUploadHandler : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            string response = "{'success':true}";
            string comment = null;
            string formName = "";
            string fileName = "";
            System.IO.Stream fileStream = null;
            HttpPostedFile UploadedFile = null;
            bool isResourcePool = false;
            bool isLinkBetweenProject = false;
            bool isCompare = false;
            int linkType = -1;
            string linkValue = "";
            try
            {
                if (context.Request.Files.Count > 0)
                {
                    formName = context.Request["formName"];

                    // get the uploaded file
                    UploadedFile = context.Request.Files[context.Request.Files.Count - 1];

                    // check size
                    //if (UploadedFile.ContentLength >
                    //    Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxProjectSize"]))
                    //{
                    //    response = "{'sizeError':true}";
                    //    //response = "{'error':true}";
                    //    context.Response.Write(response);
                    //    context.Response.Flush();
                    //    return;
                    //}

                    if (UploadedFile.ContentLength == 0 && context.Session[formName] != null)
                    {
                        fileStream = (System.IO.MemoryStream) context.Session[formName];
                        fileName = (string) context.Session[formName + "name"];
                        fileStream.Seek(0, System.IO.SeekOrigin.Begin);
                    }
                    else
                    {
                        fileStream = UploadedFile.InputStream;
                        fileName = UploadedFile.FileName;
                    }
                }
                else
                {
                    
                        // check size
                        //if (context.Request.ContentLength >
                        //    Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxProjectSize"]))
                        //{
                        //    response = "{'sizeError':true}";
                        //    //response = "{'error':true}";
                        //    context.Response.Write(response);
                        //    context.Response.Flush();
                        //    return;
                        //}

                        fileName = context.Request["qqfile"];
                        //if (string.IsNullOrEmpty(fileName))
                        //    fileName = fileName = (string) context.Session[formName + "name"];
                        fileStream = context.Request.InputStream;


                        if (HttpContext.Current.Session["localUploads"] == null)
                        {
                            HttpContext.Current.Session["localUploads"] = new Dictionary<string, Stream>();
                        }
                        
                        var dict = HttpContext.Current.Session["localUploads"] as Dictionary<string, Stream>;
                        if (!dict.ContainsKey(fileName))
                        {
                            dict.Add(fileName, fileStream);
                        }
                }

                // get the session object
                System.Web.SessionState.HttpSessionState Session = context.Session;

            }
            catch (Exception exc)
            {
                response = "{'error':''}"; // "{'error':'" + Microsoft.JScript.GlobalObject.escape(exc.Message) + "'}";
            }

            //if (response == "{'success':true}" && comment != null)
            //    response = "{'success':'" + comment + "'}";

            context.Response.Write(response);
            context.Response.Flush();
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}
