using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using CloudProject;
using Disertatie.AJAX;
using Disertatie.Utils;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Newtonsoft.Json.Linq;

namespace Disertatie
{
    public partial class Default : Page
    {
        #region Declarations
        private static string _amazonRedirectUri = "";
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            #region Initialization
            Session["url"] = Request.Path;

            if (HttpContext.Current.Session["localUploads"] == null)
            {
                HttpContext.Current.Session["localUploads"] = new Dictionary<string, Stream>();
            }
            else
            {
                var dictionary = HttpContext.Current.Session["localUploads"] as Dictionary<string, Stream>;
                try
                {
                    dictionary.Clear();
                }
                catch { }
            }
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
            #region GoogleDrive integration - not used
            //if (Request["state"] != null)
            //{
            //    JavaScriptSerializer jss = new JavaScriptSerializer();
            //    bool parsedIt = true;
            //    Dictionary<string, object> d = null;
            //    try
            //    {
            //        d = (Dictionary<string, object>)jss.Deserialize<dynamic>(Request["state"]);
            //    }
            //    catch (Exception ex)
            //    {
            //        parsedIt = false;
            //    }
            //    if (parsedIt && d["action"].ToString() == "open")
            //    {
            //        string fileId = (string)((Object[])d["ids"])[0];
            //        Session["openGoogleDriveFileId"] = fileId;

            //        Response.Redirect(Request.Path);
            //        Response.End();
            //        return;
            //    }
            //}

            //if (Session["openGoogleDriveFileId"] != null)
            //{
            //    CloudStorageConsumer cloudConsumer = Session["googledriveConsumer"] as CloudStorageConsumer;
            //    DotNetOpenAuth.OAuth2.WebServerClient wsClient = new WebServerClient(new AuthorizationServerDescription()
            //    {
            //        AuthorizationEndpoint = new Uri(cloudConsumer.config.authorizeUri),
            //        TokenEndpoint = new Uri(cloudConsumer.config.tokenUri),
            //        ProtocolVersion = ProtocolVersion.V20
            //    }, cloudConsumer.config.appKey, cloudConsumer.config.appSecret);

            //    wsClient.ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(cloudConsumer.config.appSecret);
            //    IAuthorizationState accessTokenResponse = null;
            //    accessTokenResponse = wsClient.ProcessUserAuthorization();

            //    if (accessTokenResponse == null)
            //    {
            //        // If we don't yet have access, immediately request it.
            //        string[] scopes = null;
            //        if (cloudConsumer.config.scope != null)
            //            scopes = cloudConsumer.config.scope.Split(new char[] { '+', ';' });
            //        Uri callback = MessagingUtilities.GetRequestUrlFromContext().StripQueryArgumentsWithPrefix("oauth_").StripQueryArgumentsWithPrefix("action");
            //        var request = wsClient.PrepareRequestUserAuthorization(scopes, callback);
            //        request.Send();
            //    }
            //    else
            //    {
            //        cloudConsumer.token.access_token = accessTokenResponse.AccessToken;
            //        if (cloudConsumer.token.access_token == null)// the user denied access
            //        {
            //            Response.Write("<script>window.location.href = '" + Session["url"] + "'</script>");
            //            Response.Flush();
            //            Response.End();
            //            return;
            //        }
            //        else
            //        {
            //            //OpenCloudDocument("googledrive", (string)Session["openGoogleDriveFileId"]);
            //            //Session["openGoogleDriveFileId"] = null;
            //        }
            //    }
            //}
            #endregion
            #region Amazon S3 integration - no longer used
            //else if (_amazonRedirectUri != "" && Request["code"] != null && Request["code"] != "")
            //{
            //    // this must be an Amazon code
            //    var consumer = ((AmazonS3Consumer) HttpContext.Current.Session["amazons3Consumer"]);

            //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(consumer.config.tokenUri);
            //    request.Method = "POST";
            //    request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            //    string metaData = "grant_type=authorization_code&code=" + Request["code"] + "&client_id=" + consumer.config.appKey + "&client_secret=" + consumer.config.appSecret + "&redirect_uri=" + _amazonRedirectUri;
            //    byte[] bytes = Encoding.UTF8.GetBytes(metaData);
            //    using (var reqStream = request.GetRequestStream())
            //    {
            //        reqStream.Write(bytes, 0, bytes.Length);
            //    }
            //    var response = request.GetResponse();
            //    var obj = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
            //    consumer.token = new OAuthToken
            //    {
            //        access_token = obj["access_token"].ToString(),
            //        refresh_token = obj["refresh_token"].ToString()
            //    };
            //    _amazonRedirectUri = "";
            //    request.Abort();
            //}
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

            //if (action == "saveas")
            //{
            //    HttpContext.Current.Session["postedFileExtensions"] = ".MPP";
            //}

            // fix for changes in authentication endpoint DNS
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

        [WebMethod]
        public static bool IsAuthAmazon()
        {
            var amazons3ConsumerInstance = HttpContext.Current.Session["amazons3Consumer"] as CloudStorageConsumer;
            return amazons3ConsumerInstance.TokenIsOk();
        }

        [WebMethod]
        public static bool AuthenticateAmazonS3(string accessKey, string secretKey, string region)
        {
            var amazons3ConsumerInstance = HttpContext.Current.Session["amazons3Consumer"] as AmazonS3Consumer;
            return amazons3ConsumerInstance.CreateClient(accessKey, secretKey, region);
        }

        //[WebMethod]
        //public static string GetAmazonAuthenticationUrl(string currentLocation)
        //{
        //    _amazonRedirectUri = currentLocation;
        //    return "https://www.amazon.com/ap/oa?client_id=" + ((AmazonS3Consumer)HttpContext.Current.Session["amazons3Consumer"]).config.appKey + "&redirect_uri=" + currentLocation + "&scope=profile&response_type=code";
        //}
        #endregion Clouds authentication

        [WebMethod]
        public static List<CloudItem> ListFilesInFolder(string cloud, string folderId)
        {
            //if (extensions == null)
            //    extensions = new string[] { ".mpp", ".mpx", ".xml" }; //TODO: change

            IDictionary<string, string> ret = null;
            
            var cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            //if (folderId == null || folderId == "null")
            //    folderId = cloudConsumer.getRootFolderId(); // should be fixed
            return cloudConsumer.ListFilesInFolder(folderId);
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
        public static ResponsePackage NewFolder(string cloud, string parentFolderId, string _name)
        {
            var cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            if (cloudConsumer == null) return null;

            return cloudConsumer.AddFolder(parentFolderId, _name);
        }

        [WebMethod]
        public static ArrayList GetLocalUploads()
        {
            var list = new ArrayList();
            var dict = HttpContext.Current.Session["localUploads"] as Dictionary<string, Stream>;

            foreach (var key in dict.Keys)
            {
                list.Add(key);
            }

            return list;
        }

        [WebMethod]
        public static void DiscardLocalUploads()
        {
            var dictionary = HttpContext.Current.Session["localUploads"] as Dictionary<string, Stream>;
            try
            {
                dictionary.Clear();
            }
            catch { }
        }  

        [WebMethod]
        public static ResponsePackage UploadToCloud(string[] list, string cloud, string currentCloudFolder)
        {
            var ret = new ResponsePackage();
            var cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            if (cloudConsumer == null)
            {
                ret.Error = true;
                ret.ErrorMessage = "An error has occurred.";
                return ret;
            }
            
            if (!cloudConsumer.TokenIsOk())
            {
                ret.Error = true;
                ret.ErrorMessage = "You are not logged in into " + cloud + "!";
                return ret;
            }

            var dict = HttpContext.Current.Session["localUploads"] as Dictionary<string, Stream>;

            foreach (var filename in list)
            {
                try
                {
                    var stream = dict[filename];
                    cloudConsumer.SaveCreateDocument(stream, filename, null, currentCloudFolder);
                }
                catch
                {
                    ret.Error = true;
                    ret.ErrorMessage = "An error has occurred.";
                    return ret;
                }
            }

            return ret;
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
            if (cloud.ToLower() != "box" && cloud.ToLower() != "dropbox" && cloud.ToLower() != "googledrive") return "";
            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            return cloudConsumer.GetSpaceQuota();
        }

        [WebMethod]
        public static DirectoryTreePackage GetDirectoryTree(string cloud)
        {
            CloudStorageConsumer cloudConsumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            return new DirectoryTreePackage(cloudConsumer);
        }

        [WebMethod]
        public static bool MoveFilesAndFolders(List<string> ids, string newParentId, string cloud)
        {
            if (cloud.ToLower() != "box") return false; // TODO: remove

            BoxConsumer consumer = HttpContext.Current.Session[cloud.ToLower() + "Consumer"] as BoxConsumer;
            return consumer.MoveFilesAndFolders(ids, newParentId);
        }

        [WebMethod]
        public static ResponsePackage CopyFileToAnotherCloud(string sourceCloud, string fileId, string name, string destinationCloud, string destinationFolder)
        {
            ResponsePackage ret = new ResponsePackage();

            CloudStorageConsumer sourceCloudConsumer = HttpContext.Current.Session[sourceCloud.ToLower() + "Consumer"] as CloudStorageConsumer;
            CloudStorageConsumer destinationCloudConsumer = HttpContext.Current.Session[destinationCloud.ToLower() + "Consumer"] as CloudStorageConsumer;

            try
            {
                var stream = sourceCloudConsumer.GetDocument(fileId);
                destinationCloudConsumer.SaveCreateDocument(stream, name, null, destinationFolder);
            }
            catch (Exception e)
            {
                ret.Error = true;
                ret.ErrorMessage = "An error has occurred!";
            }

            return ret;
        }

        [WebMethod]
        public static string SignOut(string cloudService)
        {
            return (HttpContext.Current.Session[cloudService.ToLower() + "Consumer"] as CloudStorageConsumer).GetLogOutEndpoint();
        }
        #endregion Clouds integration

        [WebMethod]
        public static GridLayoutPackage GetGridLayout()
        {
            return new GridLayoutPackage();
        }
    }
}