using System;
using CloudProject;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;

namespace Disertatie.Dialogs
{
    public partial class AuthenticateCloudService : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request["action"] != null)
                    Session["postedCloudAction"] = Request["action"];
                if (Request["cloud"] != null)
                    Session["postedCloudType"] = Request["cloud"];
                if (Request["fileExtensions"] != null)
                    Session["postedFileExtensions"] = Request["fileExtensions"];
                Session["backFromAuth"] = null;

                CloudStorageConsumer cloudConsumer = Session[Session["postedCloudType"].ToString().ToLower() + "Consumer"] as CloudStorageConsumer;

                if (cloudConsumer.token.access_token == null || !cloudConsumer.TokenIsOk())
                {
                    DotNetOpenAuth.OAuth2.WebServerClient wsClient = new WebServerClient(new AuthorizationServerDescription()
                    {
                        AuthorizationEndpoint = new Uri(cloudConsumer.config.authorizeUri),
                        TokenEndpoint = new Uri(cloudConsumer.config.tokenUri),
                        ProtocolVersion = ProtocolVersion.V20
                    }, cloudConsumer.config.appKey);

                    wsClient.ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(cloudConsumer.config.appSecret);
                    IAuthorizationState accessTokenResponse = null;
                    
                    accessTokenResponse = wsClient.ProcessUserAuthorization();

                    if (accessTokenResponse == null)
                    {
                        // If we don't yet have access, immediately request it.
                        string[] scopes = null;
                        if (cloudConsumer.config.scope != null)
                            scopes = cloudConsumer.config.scope.Split(new char[] { '+', ';', ' ' });

                        Uri callback = MessagingUtilities.GetRequestUrlFromContext().StripQueryArgumentsWithPrefix("oauth_").StripQueryArgumentsWithPrefix("action").StripQueryArgumentsWithPrefix("cloud").StripQueryArgumentsWithPrefix("fileExtensions");

                        // 31.05.2015 workaround for OneDrive not working on localhost
                        //if (cloudConsumer.name == "OneDrive")
                        //    callback = new Uri("http://www.localhost34342.com/Disertatie/dialogs/authenticatecloudservice.aspx");// new Uri("http://www.localhost34342.com/Dialogs/AuthenticateCloudService.aspx");
                        
                        var request = wsClient.PrepareRequestUserAuthorization(scopes, callback);
                        request.Send();
                    }
                    else
                    {
                        cloudConsumer.token.access_token = accessTokenResponse.AccessToken;
                        if (cloudConsumer.token.access_token != null)// user has granted access
                            Session["backFromAuth"] = true;//this is used on redirect to trigger the action that the user wanted to make before authentication
                    }
                }
                
                Response.Write("<script>window.location.href = '" + Session["url"] + "'</script>");
                Response.Flush();
                Response.End();
            }
        }
    }
}