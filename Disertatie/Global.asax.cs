using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using CloudStorage;
using Disertatie;

namespace Disertatie
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AuthConfig.RegisterOpenAuth();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown

        }

        void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs

        }

        private void Session_Start(object sender, EventArgs e)
        {
            #region Cloud Consumers Init
            //dropbox
            DropboxConsumer dropbox = new DropboxConsumer();
            dropbox.config.appKey = ConfigurationManager.AppSettings["DropBoxAppKey"];
            dropbox.config.appSecret = ConfigurationManager.AppSettings["DropBoxAppSecret"];
            dropbox.config.authorizeUri = "https://www.dropbox.com/1/oauth2/authorize";
            dropbox.config.tokenUri = "https://api.dropbox.com/1/oauth2/token";
            Session["dropboxConsumer"] = dropbox;

            //box
            BoxConsumer box = new BoxConsumer();
            box.config.appKey = ConfigurationManager.AppSettings["BoxClientId"];
            box.config.appSecret = ConfigurationManager.AppSettings["BoxClientSecret"];
            box.config.authorizeUri = "https://www.box.com/api/oauth2/authorize";
            box.config.tokenUri = "https://www.box.com/api/oauth2/token";
            Session["boxConsumer"] = box;

            //google drive
            GoogleDriveConsumer googledrive = new GoogleDriveConsumer();
            googledrive.config.appKey = ConfigurationManager.AppSettings["googleConsumerKey"];
            googledrive.config.appSecret = ConfigurationManager.AppSettings["googleConsumerSecret"];
            googledrive.config.authorizeUri = "https://accounts.google.com/o/oauth2/auth";
            googledrive.config.tokenUri = "https://accounts.google.com/o/oauth2/token";
            googledrive.config.scope = "https://www.googleapis.com/auth/drive+https://www.googleapis.com/auth/drive.install";
            Session["googledriveConsumer"] = googledrive;

            //onedrive
            OneDriveConsumer onedrive = new OneDriveConsumer();
            onedrive.config.appKey = ConfigurationManager.AppSettings["OneDriveClientId"]; //null;//initiated on page load depending on the url domain
            onedrive.config.appSecret = ConfigurationManager.AppSettings["OneDriveClientSecret"];// null;//initiated on page load depending on the url domain
            onedrive.config.authorizeUri = "https://login.live.com/oauth20_authorize.srf";
            onedrive.config.tokenUri = "https://login.live.com/oauth20_token.srf";
            onedrive.config.scope = "wl.skydrive_update wl.contacts_skydrive";
            Session["onedriveConsumer"] = onedrive;

            //sharepoint
            SharepointConsumer sharepoint = new SharepointConsumer();
            Session["sharepointConsumer"] = sharepoint;

            //basecamp
            BasecampConsumer basecamp = new BasecampConsumer();
            basecamp.appName = ConfigurationManager.AppSettings["baseCampApplicationName"]; //basecamp specific
            basecamp.config.appKey = ConfigurationManager.AppSettings["baseCampClientConsumerKey"];
            basecamp.config.appSecret = ConfigurationManager.AppSettings["baseCampConsumerSecret"];
            basecamp.config.authorizeUri = "https://launchpad.37signals.com/authorization/new?type=web_server";
            basecamp.config.tokenUri = "https://launchpad.37signals.com/authorization/token?type=web_server";
            Session["basecampConsumer"] = basecamp;

            //set the onedrive key and secret based on the current deploy domain - onedrive doesn't allow more than one redirect uri per app (key-secret pair)
            //string domain = HttpContext.Current.Request.Url.Authority;
            //onedrive.config.appKey = ConfigurationManager.AppSettings["pp365OneDriveClientId"];
            //onedrive.config.appSecret = ConfigurationManager.AppSettings["pp365OneDriveClientSecret"];
            //if (domain.Contains("localhost"))
            //{
            //    if (ConfigurationManager.AppSettings["localhostBaseCampClientConsumerKey"] != null && ConfigurationManager.AppSettings["localhostBaseCampConsumerSecret"] != null)
            //    {
            //        basecamp.config.appKey = ConfigurationManager.AppSettings["localhostBaseCampClientConsumerKey"].ToString();
            //        basecamp.config.appSecret = ConfigurationManager.AppSettings["localhostBaseCampConsumerSecret"].ToString();
            //    }
            //}

            #endregion
        }
    }
}
