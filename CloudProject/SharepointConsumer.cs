using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using CloudProject_extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using Newtonsoft.Json.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading;

namespace CloudProject
{
    public class SharepointConsumer : CloudStorageConsumer
    {
        public Cookie FedAuth;
		public Cookie rtFa;

		public SharepointConsumer(){
			name = "SharePoint";
		}

        public override List<CloudItem> ListFilesInFolder(string folderId)
        {
            string uri;
            if (folderId == getRootFolderId())
                uri = config.authorizeUri + "/_api/web/lists/?$select=Title&$filter=BaseType eq 1 and Hidden eq false and ItemCount gt 0";
            else
                uri = folderId;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
            if (rtFa != null)
                request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
            request.Method = "GET";
            request.Accept = "application/json;odata=verbose";
            string json = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
            JObject jObj = JObject.Parse(json);
            JArray array = null;
            List<CloudItem> ret = new List<CloudItem>();

            array = jObj["d"]["results"] as JArray;

            if (folderId.EndsWith("/files", StringComparison.OrdinalIgnoreCase)) // this is a folder, must retrieve subfolders as well
            {
                uri = folderId.Substring(0, folderId.Length - 6) + "/folders";
                request = (HttpWebRequest)WebRequest.Create(uri);
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
                if (rtFa != null)
                    request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
                request.Method = "GET";
                request.Accept = "application/json;odata=verbose";
                json = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
                jObj = JObject.Parse(json);
                if (jObj["d"]["results"] != null)
                {
                    if (array == null)
                    {
                        array = jObj["d"]["results"] as JArray;
                    }
                    else
                    {
                        foreach (var subfolder in jObj["d"]["results"])
                        {
                            array.AddFirst(subfolder);
                        }
                    }
                }
            }

            foreach (JObject folder in array)
            {
                if (folderId == getRootFolderId())
                {
                    var item = new CloudItem { 
                        cloudConsumer = name,
                        isFolder = true,
                        Name = (folder["Name"] != null) ? folder["Name"].ToString() : ((folder["Title"] != null) ? folder["Title"].ToString() : ""),
                        Id = folder["__metadata"]["uri"] + "/RootFolder/Files"
                    };
                    item.SetImageUrl();
                    ret.Add(item);
                }
                else
                {
                    if (folder["__metadata"]["type"].ToString().EndsWith(".File", StringComparison.OrdinalIgnoreCase))
                    {
                        //foreach (string ext in fileExtensions)
                            //if (folder["Name"] != null && folder["Name"].ToString().ToLower().EndsWith(ext.ToLower()))
                            { // some folders are missing Name
                                var item = new CloudItem {
                                    Name = (folder["Name"] != null) ? folder["Name"].ToString() : "",
                                    Id = config.authorizeUri + folder["ServerRelativeUrl"],
                                    UniqueId = config.authorizeUri + folder["ServerRelativeUrl"],
                                    cloudConsumer = name,
                                    fileVersion = folder["ETag"].ToString(),
                                    lastEdited = (folder["TimeLastModified"] != null) ? folder["TimeLastModified"].ToString() : "",
                                    lastEditor = (folder["LastModifiedBy"] != null && folder["LastModifiedBy"]["Name"] != null) ? folder["LastModifiedBy"]["Name"].ToString() : "",
                                    FullPath = config.authorizeUri + folder["ServerRelativeUrl"]
                                };
                                item.SetImageUrl();
                                ret.Add(item);
                                break;
                            }
                    }
                    else if (folder["__metadata"]["type"].ToString().EndsWith(".Folder") && folder["ItemCount"] != null && folder["ItemCount"].ToString() != "0")
                    {
                        var item = new CloudItem
                        {
                            cloudConsumer = name,
                            isFolder = true,
                            Name = (folder["Name"] != null) ? folder["Name"].ToString() : "",
                            Id = folder["__metadata"]["uri"] + "/files"
                        };
                        item.SetImageUrl();
                        ret.Add(item);
                    }
                }
            }

            return ret;
        }

        public override void ListSubfoldersInFolder(string folderId, string folderName, int outlineLevel, ref List<CloudFolder> list)
        {
            throw new NotImplementedException();
        }

        public override List<CloudFolder> CreateOutlineDirectoryList()
        {
            string rootFolder = getRootFolderId();
            var list = new List<CloudFolder>();
            ListSubfoldersInFolder(rootFolder, "All Folders", 0, ref list);
            return list;
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

        public override CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null)
        {
            if (content.CanSeek)
                content.Position = 0;

            bool isFolder = false; // indicates whether this is a folder or a list

            if (folderId == null)
            {
                folderId = getRootFolderId();
            }
            else
            {
                if (folderId.EndsWith("/RootFolder/Files", StringComparison.OrdinalIgnoreCase))
                {
                    var regex = new Regex("/RootFolder/Files", RegexOptions.IgnoreCase);
                    folderId = regex.Replace(folderId, "");
                }
                else if (folderId.EndsWith("/Files", StringComparison.OrdinalIgnoreCase))
                {
                    var regex = new Regex("/Files", RegexOptions.IgnoreCase);
                    folderId = regex.Replace(folderId, "");
                }

                try
                {
                    HttpWebRequest request = WebRequest.Create(folderId + "/RootFolder") as HttpWebRequest;
                    request.CookieContainer = new CookieContainer();
                    request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
                    if (rtFa != null)
                        request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
                    request.Method = "GET";
                    request.Accept = "application/json;odata=verbose";
                    request.GetResponse();
                }
                catch { isFolder = true; }
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
                string uri = folderId + ((isFolder) ? "/Files" : "/RootFolder/Files") + "/Add(url='" + fileName + "', overwrite=true)";
                request = WebRequest.Create(uri) as HttpWebRequest;

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

                request = WebRequest.Create(folderId + "/?$select=Name,Title") as HttpWebRequest; // SP.Folder objects have Name, while SP.List objects have Title
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
                if (rtFa != null)
                    request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
                
                request.Method = "GET";
                request.Accept = "application/json;odata=verbose";
                response = request.GetResponse() as HttpWebResponse;
                metadata = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
                string folderName = (metadata["d"]["Title"] != null) ? metadata["d"]["Title"].ToString() : metadata["d"]["Name"].ToString();
                string fileId = "/" + folderName + "/" + fileName;

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
                Email = metadata["d"]["Email"].ToString()
            };
            return userData;
        }

        public override string GetSpaceQuota()
        {
            // not available via SharePoint REST API
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

        public override void DeleteFile(string fileId)
        {
            // Get Request Digest token
            string formDigestValue = GetFormDigestToken();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(config.authorizeUri + "/_api/web/getFileByServerRelativeUrl('" + fileId.Replace(config.authorizeUri, "") + "')");
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
            if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
            request.Method = "POST";
            //request.Headers["IF-MATCH"] = "*";
            request.Headers["X-HTTP-Method"] = "DELETE";
            request.Headers["X-RequestDigest"] = formDigestValue;
            request.Accept = "application/json;odata=verbose";
            using (var reqStream = request.GetRequestStream())// solves the issue of setting content length to 0 in PCL - sharepoint wants this...
            {
            }

            request.GetResponse();
            request.Abort();
        }

        public override bool DeleteFolder(string folderId)
        {
            HttpWebRequest request = null;
            try
            {
                if (folderId.EndsWith("/RootFolder/Files", StringComparison.OrdinalIgnoreCase))
                {
                    var regex = new Regex("/RootFolder/Files", RegexOptions.IgnoreCase);
                    folderId = regex.Replace(folderId, "");
                }
                else if (folderId.EndsWith("/Files", StringComparison.OrdinalIgnoreCase))
                {
                    var regex = new Regex("/Files", RegexOptions.IgnoreCase);
                    folderId = regex.Replace(folderId, "");
                }

                // Get Request Digest token
                string formDigestValue = GetFormDigestToken();

                request = (HttpWebRequest) WebRequest.Create(config.authorizeUri + "/_api/web/GetFolderByServerRelativeUrl('" + folderId.Replace(config.authorizeUri, "") + "')");
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
                if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
                request.Method = "POST";
                request.Headers["X-HTTP-Method"] = "DELETE";
                request.Headers["X-RequestDigest"] = formDigestValue;
                request.Accept = "application/json;odata=verbose";
                using (var reqStream = request.GetRequestStream()) // solves the issue of setting content length to 0 in PCL - sharepoint wants this...
                { }

                request.GetResponse();
            }
            catch
            {
                return false;
            }
            finally
            {
               if (request!= null) request.Abort();
            }
            return true;
        }

        public override ResponsePackage AddFolder(string parentFolderId, string _name)
        {
//            url: http://site url/_api/web/folders
//            method: POST
//            body: { '__metadata': { 'type': 'SP.Folder' }, 'ServerRelativeUrl': '/document library relative url/folder name'}
//            Headers: 
//    Authorization: "Bearer " + accessToken
//    X-RequestDigest: form digest value
//    accept: "application/json;odata=verbose"
//    content-type: "application/json;odata=verbose"
//    content-length:length of post body

            var ret = new ResponsePackage();
            HttpWebRequest request = null;
            try
            {
                //if (parentFolderId.EndsWith("/RootFolder/Files", StringComparison.OrdinalIgnoreCase))
                //{
                //    var regex = new Regex("/RootFolder/Files", RegexOptions.IgnoreCase);
                //    parentFolderId = regex.Replace(parentFolderId, "");
                //}
                //else 
                    if (parentFolderId.EndsWith("/Files", StringComparison.OrdinalIgnoreCase))
                {
                    var regex = new Regex("/Files", RegexOptions.IgnoreCase);
                    parentFolderId = regex.Replace(parentFolderId, "");
                }

                // Get Request Digest token
                string formDigestValue = GetFormDigestToken();

                request = (HttpWebRequest)WebRequest.Create(config.authorizeUri + "/_api/web/folders");
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Uri("http://" + FedAuth.Domain), FedAuth);
                if (rtFa != null) request.CookieContainer.Add(new Uri("http://" + rtFa.Domain), rtFa);
                request.Method = "POST";
                request.Headers["X-RequestDigest"] = formDigestValue;
                request.Accept = "application/json;odata=verbose";

                string json = "[{\"__metadata\":[\"type\":\"SP.Folder\"], \"ServerRelativeUrl\":\"" + parentFolderId.Replace(config.authorizeUri, "") + "/" + _name + "\"}]";

                byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(json);
                using (var reqStream = request.GetRequestStream())
                {
                    reqStream.Write(bytes, 0, bytes.Length);
                }

                request.GetResponse();
            }
            catch
            {
                ret.Error = true;
                ret.ErrorMessage = "The folder couldn't be created!";
            }
            finally
            {
                if (request != null) request.Abort();
            }
            return ret;
        }

        public override string GetLogOutEndpoint()
        {
            const string logOutUrl = "https://login.microsoftonline.com/logout.srf?wa=wsignoutcleanup1.0";

            token.access_token = null;
            config.authorizeUri = null;
            userData = null;
            FedAuth = null;
            rtFa = null;

            return logOutUrl;
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

        private string GetFormDigestToken()
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
            request.Abort();

            return metadata["d"]["GetContextWebInformation"]["FormDigestValue"].ToString();
        }
    }
}
