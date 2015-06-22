using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CloudProject_extensions;

namespace CloudProject
{
    public class BasecampConsumer : CloudStorageConsumer
    {

        public string appName; //should be used by basecamp to send a user agent string can't easily set user agent in pcl
        private string accountId;

        private string _currentProject = "";

        public BasecampConsumer()
        {
            name = "Basecamp";
        }

        private List<CloudItem> getProjects()
        {
            _currentProject = "";
            List<CloudItem> ret = new List<CloudItem>();

            try
            {
                string url = string.Format("https://basecamp.com/{0}/api/v1/projects.json", accountId);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers["Authorization"] = "Bearer " + token.access_token;
                request.SetHeader("User-Agent", appName);
                var retVal = JArray.Parse(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());

                foreach (JObject obj in retVal)
                {
                    var item = new CloudItem
                    {
                        Id = obj["id"].ToString(),
                        cloudConsumer = this.name,
                        isFolder = true,
                        Name = obj["name"].ToString()
                    };
                    item.SetImageUrl();
                    ret.Add(item);
                }
            }
            catch (WebException e)
            {
                if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotFound) // the account's trial period has expired!
                {
                    GetLogOutEndpoint();
                    return null;
                }
            }

            return ret;
        }

        public override List<CloudItem> ListFilesInFolder(string projectId)
        {
            List<CloudItem> items = new List<CloudItem>();
            if (projectId == getRootFolderId())
            {
                return getProjects();
            }

            string url = string.Format("https://basecamp.com/{0}/api/v1/projects/{1}/attachments.json", accountId, projectId);
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.SetHeader("User-Agent", appName);
            string responseString;
            using (Stream stream = request.GetResponse().GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                responseString = reader.ReadToEnd();
            }

            var docs = JArray.Parse(responseString);
            foreach (JObject file in docs)
            {
                items.Add(GetFileMetadata(file["url"].ToString()));
            }

            _currentProject = projectId;

            return items;
        }

        public override void ListSubfoldersInFolder(string folderId, string folderName, int outlineLevel, ref List<CloudFolder> list)
        {
            list.Add(new CloudFolder { Name = folderName, OutlineLevel = outlineLevel, Id = folderId });

            string url = string.Format("https://basecamp.com/{0}/api/v1/projects.json", accountId);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.SetHeader("User-Agent", appName);
            var retVal = JArray.Parse(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());

            foreach (JObject obj in retVal)
            {
                list.Add(new CloudFolder { Name = obj["name"].ToString(), OutlineLevel = outlineLevel + 1, Id = obj["id"].ToString() });
            }
        }

        public override List<CloudFolder> CreateOutlineDirectoryList()
        {
            string rootFolder = getRootFolderId();
            var list = new List<CloudFolder>();
            ListSubfoldersInFolder(rootFolder, "All Projects", 0, ref list);
            return list;
        }

        public override Stream GetDocument(string fileId)
        {
            HttpWebRequest request = WebRequest.CreateHttp(fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.SetHeader("User-Agent", appName);
            return request.GetResponse().GetResponseStream();
        }

        /// <param name="fileId">the id of the file for which the size is needed</param>
        /// <returns>the size of the file (bytes)</returns>
        public override int GetFileSize(string fileId)
        {
            HttpWebRequest request = WebRequest.CreateHttp(fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.SetHeader("User-Agent", appName);
            request.Method = "HEAD";
            System.Net.WebResponse resp = request.GetResponse();
            request.Abort();
            int ContentLength;
            if (int.TryParse(resp.Headers["Content-Length"], out ContentLength))
            {
                return ContentLength;
            }
            return 0;
        }

        public override string getRootFolderId()
        {
            return "basecamp_root";
        }

        private UserData userData;
        public override UserData GetUser()
        {
            if (userData != null) return userData;
            //https://launchpad.37signals.com/people/me.json
            throw new Exception("The method or operation is not implemented.");
        }

        public override string GetSpaceQuota()
        {
            // not available via BaseCamp REST API
            throw new NotImplementedException();
        }

        public override bool TokenIsOk()
        {
            if (token.access_token == null)
                return false;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://launchpad.37signals.com/authorization.json");
                request.Headers["Authorization"] = "Bearer " + token.access_token;
                request.Method = "GET";
                var retVal = JObject.Parse(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());
                userData = new UserData()
                {
                    Email = retVal["identity"]["email_address"].ToString(),
                    Name = retVal["identity"]["first_name"].ToString() + " " + retVal["identity"]["last_name"].ToString()
                };
                accountId = (retVal["accounts"] as JArray)[0]["id"].ToString();
                return true;
            }
            catch (Exception) { }
            return false;
        }

        public override CloudItem GetFileMetadata(string fileId)
        {
            var item = new CloudItem()
            {
                Id = fileId,
                UniqueId = fileId,
                fileVersion = fileId,
                cloudConsumer = this.name,
                isFolder = false,
                Name = Uri.UnescapeDataString(fileId.Substring(fileId.LastIndexOf("/") + 1))
            };
            item.SetImageUrl();
            return item;
        }

        public override CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null)
        {
            if (content.CanSeek)
                content.Position = 0;

            // First step - create attachment and receive token
            string url = string.Format("https://basecamp.com/{0}/api/v1/attachments.json", accountId);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.SetHeader("User-Agent", appName);
            request.Method = "POST";
            request.ContentType = "application/vnd.ms-project"; //contentType; -- "application/xml" gets 504 gateway timeout every time

            using (var reqStream = request.GetRequestStream())
            {
                content.CopyTo(reqStream);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            JObject retVal;
            string newFileToken;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                retVal = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
                newFileToken = retVal["token"].ToString();

                // Second step - associate the attachment to an upload
                url = string.Format("https://basecamp.com/{0}/api/v1/projects/{1}/uploads.json", accountId, folderId);

                request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = "application/json; charset=utf-8";
                request.SetHeader("User-Agent", appName);
                request.Method = "POST";
                request.Headers["Authorization"] = "Bearer " + token.access_token;

                string metaData = "{";
                metaData += "\"attachments\": [{ \"token\" : \"" + newFileToken + "\", ";
                metaData += "\"name\":\"" + fileName + "\"}]";
                metaData += "}";

                byte[] bytes = UTF8Encoding.UTF8.GetBytes(metaData);
                using (var reqStream = request.GetRequestStream())
                {
                    reqStream.Write(bytes, 0, bytes.Length);
                }

                using (response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.Created)
                    {
                        throw new Exception("Failed to upload content to BaseCamp");
                    }
                    retVal = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
                    return GetFileMetadata(retVal["attachments"][0]["url"].ToString());
                }
            }
            throw new Exception("Failed to create the document");
        }

        public override void DeleteFile(string fileId)
        {
            try
            {
                fileId = GetAttachmentUid(fileId);
                string url = string.Format("https://basecamp.com/{0}/api/v1/projects/{1}/attachments/{2}.json", accountId, _currentProject, fileId);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers["Authorization"] = "Bearer " + token.access_token;
                request.SetHeader("User-Agent", appName);

                request.Method = "DELETE";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException we)
            {
                var errorResponse = we.Response as HttpWebResponse; // 204 No Content if successful, possibly 403 Forbidden
                Debug.WriteLine(errorResponse.ToString());
            }
        }

        public override bool DeleteFolder(string folderId)
        {
            string url = string.Format("https://basecamp.com/{0}/api/v1/projects/{1}.json", accountId, folderId);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.SetHeader("User-Agent", appName);

            request.Method = "DELETE";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode != HttpStatusCode.NoContent) // 204 No Content if successful
                {
                    return false;
                }
            }
            catch (WebException we)
            {
                var errorResponse = we.Response as HttpWebResponse; // possibly 403 Forbidden
                return false;
            }
            return true;
        }

        public override ResponsePackage AddFolder(string parentFolderId, string _name)
        {
            var ret = new ResponsePackage();

            if (parentFolderId != getRootFolderId())
            {
                ret.Error = true;
                ret.ErrorMessage = "Projects can only be created in the Root! BaseCamp does not allow subfolders.";
                return ret;
            }

            string url = string.Format("https://basecamp.com/{0}/api/v1/projects.json", accountId);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.SetHeader("User-Agent", appName);

            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";

            string json = "{\"name\":\"" + _name + "\"," +
                          "\"description\":\"Created with CloudSphere\"}";
            byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(json);
            using (var reqStream = request.GetRequestStream())
            {
                reqStream.Write(bytes, 0, bytes.Length);
            }

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode != HttpStatusCode.Created)
                {
                    ret.Error = true;
                    ret.ErrorMessage = "The BaseCamp project couldn't be created!";
                }
            }
            catch (WebException we)
            {
                ret.Error = true;

                var errorResponse = we.Response as HttpWebResponse;
                if (errorResponse != null && errorResponse.StatusCode == HttpStatusCode.Forbidden) 
                {
                    ret.Error = true;
                    ret.ErrorMessage = "You do not have enough rights to create a new project!";
                }
                else
                {
                    // possibly not enough space left (507 Insufficient Storage)
                    ret.ErrorMessage = "The BaseCamp project couldn't be created! Please check that the folder name is valid and you have enough storage to create it.";
                }
            }
            return ret;
        }

        public override string GetLogOutEndpoint()
        {
            const string logOutUrl = "https://launchpad.37signals.com/signout";

            token.access_token = null;
            token.refresh_token = null;
            userData = null;
            accountId = null;

            return logOutUrl;
        }

        private string GetAttachmentUid(string attachmentId)
        {
            attachmentId = attachmentId.Substring(attachmentId.IndexOf("attachments") + 12);
            return attachmentId.Substring(0, attachmentId.IndexOf("/"));
        }
    }
}
