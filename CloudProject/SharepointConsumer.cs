using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using CloudStorage_extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using Newtonsoft.Json.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading;

namespace CloudStorage
{
    public class SharepointConsumer : CloudStorageConsumer
    {
        public Cookie FedAuth;
		public Cookie rtFa;

		public SharepointConsumer(){
			name = "SharePoint";
		}

        public override List<CloudItem> ListAllFiles(IEnumerable<string> fileExtensions)
        {
            throw new NotImplementedException();
        }

        public override List<CloudItem> ListFilesInFolder(string folderId, IEnumerable<string> fileExtensions)
        {
			string uri = null;
			if (folderId == getRootFolderId())
                uri = config.authorizeUri + "/_api/web/lists/?$select=Title&$filter=BaseType eq 1 and Hidden eq false and ItemCount gt 0";
			else
				uri = folderId;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create (uri);
			request.CookieContainer = new CookieContainer();
			request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
			if (rtFa != null)
				request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
			request.Method = "GET";
			request.Accept = "application/json;odata=verbose";
			string json = new StreamReader (request.GetResponse ().GetResponseStream ()).ReadToEnd ();
			JObject jObj = JObject.Parse (json);
			JArray array = null;
			List<CloudItem> ret = new List<CloudItem> ();

			array = jObj ["d"] ["results"] as JArray;
			foreach (JObject folder in array) {
				if (folderId == getRootFolderId ()) {
					ret.Add (new CloudItem () { 
						cloudConsumer = name,
						isFolder = true,
						Name = folder ["Title"].ToString (),
						Id = folder ["__metadata"] ["uri"].ToString () + "/files" //+ "?$select=Url,ETag,TimeLastModified,LastModifiedBy"
					});
				} else {
					if (folder ["__metadata"] ["type"].ToString ().EndsWith(".File")) {
						foreach (string ext in fileExtensions)
							if (folder["Name"] != null && folder ["Name"].ToString ().ToLower ().EndsWith (ext.ToLower ())) { // some folders are missing Name
								ret.Add (new CloudItem () {
									Name = (folder ["Name"] != null) ? folder ["Name"].ToString () : "",
									Id = folder ["Url"].ToString(),
									UniqueId = config.authorizeUri + folder ["Url"],
									cloudConsumer = name,
									fileVersion = folder ["ETag"].ToString (),
									lastEdited = folder ["TimeLastModified"].ToString (),
									lastEditor = folder ["LastModifiedBy"] ["Name"].ToString (),
									FullPath = folder ["Url"].ToString ()
								});
								break;
							}
                    }
                    else if (folder["__metadata"]["type"].ToString() == "MS.FileServices.Folder" && folder["ChildrenCount"] != null && folder["ChildrenCount"].ToString() != "0")
                    {
						ret.Add (new CloudItem () {
							cloudConsumer = name,
							isFolder = true,
							Name = folder ["Name"].ToString (),
							Id = folder ["Children"] ["__deferred"] ["uri"].ToString () + "?$select=Url,ETag,TimeLastModified,LastModifiedBy"
						});
					}
				}
			}

			return ret;
        }

        public override bool TokenIsOk()
        {
            if (FedAuth == null) //  || rtFa == null // rtFa cookie is not mandatory
                return false;
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(config.authorizeUri + "/_api/web/title");
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
				if (rtFa != null)
                	request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
                request.Method = "GET";
                request.Accept = "application/json;odata=verbose";
                request.GetResponse();
            }
            catch (WebException)
            {
                FedAuth = null;
                rtFa = null;
                return false;
            }
            finally
            {
                if (request != null) request.Abort();
            }
            return true;
        }

        public override string getRootFolderId()
        {
			return "";
        }

        public override CloudItem SaveOverwriteDocument(System.IO.Stream content, string fileId, string contentType = null)
        {
            if (content.CanSeek)
                content.Position = 0;

			HttpWebRequest request = WebRequest.Create (fileId) as HttpWebRequest;
			request.Method = "PUT";
			request.Headers ["Overwrite"] = "F";
			request.Accept = "*/*";
			request.ContentType = "multipart/form-data; charset=utf-8";
			request.CookieContainer = new CookieContainer ();
			request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
			if (rtFa != null)
				request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
			request.Headers ["Accept-Language"] = "en-us";

			using (var str = request.GetRequestStream ()) {
				content.CopyTo (str);
			}

            var response = request.GetResponse();
            return GetFileMetadata(fileId);
        }

        public override CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null)
        {
            // CCB 14.05.2014
            if (content.CanSeek)
                content.Position = 0;

            if (folderId == null)
            {
                folderId = getRootFolderId();
            }
            else
            {
                if (folderId.EndsWith("/Files", StringComparison.OrdinalIgnoreCase))
                {
                    var regex = new Regex("/Files", RegexOptions.IgnoreCase);
                    folderId = regex.Replace(folderId, "");
                }
            }

            try
            {
                // Get Request Digest token
                HttpWebRequest request = WebRequest.Create(config.authorizeUri + "/_api/contextinfo") as HttpWebRequest;

                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
				if (rtFa != null)
					request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
                request.Method = "POST";
                request.Accept = "application/json;odata=verbose";
                using (var reqStream = request.GetRequestStream())// solves the issue of setting content length to 0 in PCL - sharepoint wants this...
                {
                }
                var response = request.GetResponse() as HttpWebResponse;

                JObject metadata = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());

                string formDigestValue = metadata["d"]["GetContextWebInformation"]["FormDigestValue"].ToString();

                request.Abort();

                // Create a PUT Web request to upload the file
                request =
                    WebRequest.Create(folderId + "/RootFolder/Files/Add(url='" + fileName + "', overwrite=true)") as
                    HttpWebRequest;

                request.Headers["Overwrite"] = "T";
                request.Headers["X-RequestDigest"] = formDigestValue;
                request.Accept = "*/*";
                request.Method = "PUT";
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
                if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);

                using (var str = request.GetRequestStream())
                {
                    content.CopyTo(str);
                }

                try
                {
                    response = request.GetResponse() as HttpWebResponse;
                }
                catch (WebException we)
                {
                    var errorResponse = we.Response as HttpWebResponse;

                    if (errorResponse != null && errorResponse.StatusCode == HttpStatusCode.NotFound) // SharePoint is different from the rest of the clouds, in the sense that an invalid fileName will cause 404 NotFound at this point
                    {
                        throw new FormatException("");
                    }
                    if (errorResponse == null || errorResponse.StatusCode != HttpStatusCode.BadRequest) // mysterious 400 Bad Request is returned even when the save is successfully done
                    {
                        throw we;
                    }
                }

                request = WebRequest.Create(folderId + "/Title") as HttpWebRequest;
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
                if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
                request.Method = "GET";
                request.Accept = "application/json;odata=verbose";
                response = request.GetResponse() as HttpWebResponse;
                metadata = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
                string fileId = "/" + metadata["d"]["Title"] + "/" + fileName;

                return GetFileMetadata(fileId);
            }
            catch(FormatException)
            {
                // Invalid URI: The format of the URI could not be determined - occurs when trying to save in the root of the filepicker (i.e. no folder)
                throw new WebException("", WebExceptionStatus.UnknownError);
            }
        }

        private UserData userData;
        public override UserData GetUser()
        {
            // CCB 25.06.2014
            if (userData != null) return userData;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(config.authorizeUri + "/_api/SP.UserProfiles.PeopleManager/GetMyProperties?$select=DisplayName,Email");
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
            if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
            request.Method = "GET";
            request.Accept = "application/json;odata=verbose";
            var resp = request.GetResponse();
            JObject metadata = JObject.Parse(new StreamReader(resp.GetResponseStream()).ReadToEnd());
            
            userData = new UserData()
            {
                Name = metadata["d"]["DisplayName"].ToString(),
                email = metadata["d"]["Email"].ToString()
            };
            return userData;
        }

        public override string GetSpaceQuota()
        {
            throw new NotImplementedException();
        }

        public override CloudItem GetFileMetadata(string fileId)
        {
            JObject jobj;
            try
            {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(config.authorizeUri + "/_api/web/getFileByServerRelativeUrl('" + fileId.Replace(config.authorizeUri,"") + "')/ListItemAllFields/File");
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
                if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
                request.Method = "GET";
                request.Accept = "application/json;odata=verbose";
                var resp = request.GetResponse();
                var r = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                jobj = JObject.Parse(r)["d"] as JObject;

                return new CloudItem()
                    {
                        Name = jobj["Name"].ToString(),
                        Id = fileId,
                        UniqueId = config.authorizeUri + fileId,
                        cloudConsumer = name,
                        fileVersion = jobj["ETag"].ToString(),
                        lastEdited = jobj["TimeLastModified"].ToString(),
                        FullPath = fileId
                    };
            }
            catch (WebException we)
            {
                var errorResponse = we.Response as HttpWebResponse;
                if (errorResponse != null && errorResponse.StatusCode == HttpStatusCode.InternalServerError)
                {
                    // this sometimes happens after SaveCreateDocument in the library called Site Pages, although the file gets to be created
                    int pos = fileId.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                    string fileName = (pos > -1) ? fileId.Remove(0, pos + 1) : fileId;

                    return new CloudItem()
                    {
                        Name = fileName,
                        Id = fileId,
                        UniqueId = config.authorizeUri + fileId,
                        cloudConsumer = name,
                        FullPath = fileId
                    };
                }
            }
            return null;
        }

        public override System.IO.Stream GetDocument(string fileId)
        {
			var request = (HttpWebRequest)WebRequest.Create(fileId);
			request.Method = "GET";
			request.CookieContainer = new CookieContainer();
			request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
            if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
			return request.GetResponse().GetResponseStream();
        }

        /// <summary>
        /// CCB 18.02.2015
        /// </summary>
        /// <param name="fileId">the id of the file for which the size is needed</param>
        /// <returns>the size of the file (bytes)</returns>
        public override int GetFileSize(string fileId)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(config.authorizeUri + "/_api/web/getFileByServerRelativeUrl('" + fileId.Replace(config.authorizeUri, "") + "')/ListItemAllFields/File");
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
            if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
            request.Method = "GET";
            request.Accept = "application/json;odata=verbose";
            var resp = request.GetResponse();
            var r = new StreamReader(resp.GetResponseStream()).ReadToEnd();
            request.Abort();
            var jobj = JObject.Parse(r)["d"] as JObject;

            return (jobj["Length"] != null) ? (int)jobj["Length"] : 0;
        }

        public override bool HasPermissionToEditFile(string fileId)
		{
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(config.authorizeUri + "/_api/web/getFileByServerRelativeUrl('" + fileId.Replace(config.authorizeUri,"") + "')/ListItemAllFields/effectiveBasePermissions");
			request.CookieContainer = new CookieContainer ();
			request.CookieContainer.Add (new Uri ("http://" + FedAuth.Domain), FedAuth);
            if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
			request.Method = "GET";
			request.Accept = "application/json;odata=verbose";
			var resp = request.GetResponse ();
			var r = new StreamReader (resp.GetResponseStream ()).ReadToEnd ();
			JObject effectiveBasePermissions = JObject.Parse (r) ["d"]["EffectiveBasePermissions"] as JObject;
			UInt32 low = UInt32.Parse(effectiveBasePermissions["Low"].ToString());
			return (low | 4) == low;
		}

		public override string GenerateShareUrlParam (CloudItem item)
		{
		    string filePath = item.Id.Replace(config.authorizeUri, "");
            return name + "://" + config.authorizeUri + "://" + Uri.EscapeDataString(filePath.Substring(1));
            //return name + "://" + config.authorizeUri + ":/" + item.Id; // CCB 03.10.2014 replaced with the line above, as the item.Id is now the full path, as opposed to before
    	}

    	public override CloudItem GetCloudItemFromParam (string urlParam)
    	{
            if (!urlParam.StartsWith(name + "://"))
                return null;
            urlParam = urlParam.Substring (name.Length + 3);

            // CCB 03.10.2014
            string relativePath = urlParam.Substring(urlParam.LastIndexOf("://", StringComparison.Ordinal) + 2);
            config.authorizeUri = urlParam.Remove(urlParam.LastIndexOf("://", StringComparison.Ordinal));

            return new CloudItem()
            {
                cloudConsumer = name,
                Id = config.authorizeUri + relativePath
            };

            //int aux = urlParam.IndexOf ("//");
            //config.authorizeUri = urlParam.Substring (0, aux);

            //return new CloudItem(){
            //    cloudConsumer =  name,
            //    Id = urlParam.Substring(aux + 3)
            //};
    	}
    }
}
