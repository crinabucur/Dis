using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using CloudStorage;
using Disertatie.AJAX;
using Disertatie.Utils;

namespace Disertatie
{
    public partial class Default : Page
    {
        #region Declarations
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            #region Initialization
            Session["url"] = Request.Path;
            #endregion

            #region Office 365 SharePoint login
            // CCB 17.06.2014
            if (Request["t"] != null)
            {
                if (HttpContext.Current.Session["postedCloudType"] != null)
                {
                    SharepointOnline authorizationState = new SharepointOnline((string)HttpContext.Current.Session["SharepointURL"], "t=" + Request["t"]);

                    var consumer = (SharepointConsumer)HttpContext.Current.Session["sharepointConsumer"];
                    consumer.config.authorizeUri = (string)HttpContext.Current.Session["SharepointURL"];
                    consumer.FedAuth = authorizationState.ClaimsHelper.FedAuth;
                    consumer.rtFa = authorizationState.ClaimsHelper.rtFA;
                }
            }
            #endregion
        }

        #region Clouds integration
        #region Clouds authentication
        [WebMethod]
        public static bool IsAuthCloud(string cloud)
        {
            if (cloud.ToLower() == "sharepoint")
                return IsAuthSharepoint() == "true";
            if (cloud == "Device") return true;
            return (HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer).TokenIsOk();
        }

        [WebMethod]
        public static string IsAuthSharepoint()
        {
            string URL = "";
            if (HttpContext.Current.Session["SharepointURL"] == null)
            {
                if (HttpContext.Current.Request.Cookies["SharepointURL"] != null)
                {
                    URL = HttpContext.Current.Request.Cookies["SharepointURL"].Value;
                }
                HttpContext.Current.Session["SharepointURL"] = URL;
            }
            else
            {
                URL = (string)HttpContext.Current.Session["SharepointURL"];
            }

            var sharePointConsumerInstance = HttpContext.Current.Session["sharepointConsumer"] as CloudStorageConsumer;
            return (sharePointConsumerInstance.TokenIsOk() ? "true" : ((URL != string.Empty) ? URL : "false"));
        }

        [WebMethod]
        public static string SetSharepointUrlAndAction(string url, string action)
        {
            var wreply = "";

            if (url != "") HttpContext.Current.Session["SharepointURL"] = url;
            HttpContext.Current.Session["backFromAuth"] = true;
            HttpContext.Current.Session["postedCloudType"] = "sharepoint";
            HttpContext.Current.Session["postedCloudAction"] = action;

            if (action == "saveas")
            {
                HttpContext.Current.Session["postedFileExtensions"] = ".MPP";
            }

            // 14.07.2014 - fix for changes in authentication endpoint DNS
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + "/_forms/default.aspx");
                request.Method = "GET";
                request.AllowAutoRedirect = false;
                request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
                request.Accept = "*/*";

                using (var resp = request.GetResponse())
                {
                    Uri myUri = new Uri(resp.Headers["Location"]);
                    string wreplyWithQuery = HttpUtility.ParseQueryString(myUri.Query).Get("wreply");
                    myUri = new Uri(wreplyWithQuery);
                    wreply = myUri.GetLeftPart(UriPartial.Authority) + "/_layouts/15/landing.aspx";
                }
            }
            catch { Debug.WriteLine("Error in getting the wreply parameter for Office365 authentication!"); }

            return wreply;
        }
        #endregion Clouds authentication

        [WebMethod]
        public static List<CloudItem> ListFilesInFolder(string cloud, string folderId, string[] extensions)
        {
            if (extensions == null)
                extensions = new string[] { ".mpp", ".mpx", ".xml" }; //TODO: change

            IDictionary<string, string> ret = null;
            
            var cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            if (folderId == null || folderId == "null")
                folderId = cloudConsumer.getRootFolderId();
            return cloudConsumer.ListFilesInFolder(folderId, extensions);
        }

        [WebMethod]
        public static void DeleteFile(string cloud, string fileId)
        {
            var cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            if (cloudConsumer == null) return;

            cloudConsumer.DeleteFile(fileId);
        }

        [WebMethod]
        public static void DeleteFolder(string cloud, string folderId)
        {
            var cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            if (cloudConsumer == null) return;

            cloudConsumer.DeleteFolder(folderId);
        }

        [WebMethod]
        public static string ShareFileLink(string cloud, string fileId)
        {
            var cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            string baseUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path);
            baseUrl = baseUrl.Replace("/ShareFileLink", ""); // remove method names from context url
            string shareUrl = cloudConsumer.GenerateShareUrlParam(cloudConsumer.GetFileMetadata(fileId));
            return baseUrl + "?xid=" + Uri.EscapeDataString(shareUrl); 
        }

        [WebMethod]
        public static string GetSpaceQuota(string cloud)
        {
            if (cloud.ToLower() != "box") return ""; // TODO: remove
            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            return cloudConsumer.GetSpaceQuota();
        }
        #endregion Clouds integration

        [WebMethod]
        public static GridLayoutPackage GetGridLayout()
        {
            return new GridLayoutPackage();
        }
    }
}