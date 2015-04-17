using System;
using System.Collections.Generic;
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

namespace Disertatie
{
    public partial class _Default : Page
    {
        #region Declarations
        private bool loggedInGoogleDrive = false;
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            #region Initialization
            Session["url"] = Request.Path;
            #endregion
        }

        [WebMethod]
        public static bool IsAuthCloud(string cloud)
        {
        //    if (cloud.ToLower() == "sharepoint")
        //        return IsAuthSharepoint() == "true";
            if (cloud == "Device") return true;
            return (HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer).TokenIsOk();
        }

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
        public static string GetSpaceQuota(string cloud)
        {
            if (cloud.ToLower() != "box") return ""; // TODO: remove
            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            return cloudConsumer.GetSpaceQuota();
        }

        [WebMethod]
        public static GridLayoutPackage GetGridLayout()
        {
            return new GridLayoutPackage();
        }
    }
}