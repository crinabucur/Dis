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
using CloudStorage_extensions;

namespace CloudStorage
{
    public class BasecampConsumer : CloudStorageConsumer
    {

        public string appName; //should be used by basecamp to send a user agent string can't easily set user agent in pcl
        private string accountId;

        public BasecampConsumer()
        {
            name = "Basecamp";
        }

        private List<CloudItem> getProjects()
        {
            List<CloudItem> ret = new List<CloudItem>();
            string url = string.Format("https://basecamp.com/{0}/api/v1/projects.json", accountId);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.SetHeader("User-Agent", appName);
            var retVal = JArray.Parse(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());

            foreach (JObject obj in retVal)
            {
                ret.Add(new CloudItem()
                {
                    Id = obj["id"].ToString(),
                    cloudConsumer = this.name,
                    isFolder = true,
                    Name = obj["name"].ToString()
                });
            }

            return ret;
        }

        public override List<CloudItem> ListFilesInFolder(string projectId, IEnumerable<string> fileExtensions)
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
                foreach (string ext in fileExtensions)
                {
                    if (file["name"].ToString().ToLower().EndsWith(ext.ToLower()))
                    {
                        items.Add(GetFileMetadata(file["url"].ToString()));
                        break;
                    }
                }
            }

            return items;
        }

        public override void ListSubfoldersInFolder(string folderId, string folderName, int outlineLevel, ref List<CloudFolder> list)
        {
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

        public override CloudItem SaveOverwriteDocument(Stream content, String fileId, String contentType = null)
        {
            throw new Exception("The method or operation is not implemented.");
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
                    email = retVal["identity"]["email_address"].ToString(),
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
            return new CloudItem()
            {
                Id = fileId,
                UniqueId = fileId,
                fileVersion = fileId,
                cloudConsumer = this.name,
                isFolder = false,
                Name = Uri.UnescapeDataString(fileId.Substring(fileId.LastIndexOf("/") + 1))
            };
        }

        public override List<CloudItem> ListAllFiles(IEnumerable<string> fileExtensions)
        {
            var ret = new List<CloudItem>();

            string url = string.Format("https://basecamp.com/{0}/api/v1/attachments.json", accountId);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.SetHeader("User-Agent", appName);
            var retVal = JArray.Parse(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());

            foreach (JObject obj in retVal)
                foreach (string ext in fileExtensions)
                    if (obj["name"].ToString().ToLower().EndsWith(ext.ToLower()))
                    {
                        ret.Add(GetFileMetadata(obj["url"].ToString()));
                        break;
                    }
            return ret;
        }

        public override CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null)
        {
            throw new NotImplementedException();
        }

        public override bool HasPermissionToEditFile(string fileId)
        {
            throw new NotImplementedException();
        }

        public override void DeleteFile(string fileId)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteFolder(string folderId)
        {
            throw new NotImplementedException();
        }
    }
}
