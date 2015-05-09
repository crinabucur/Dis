using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using CloudStorage_extensions;

namespace CloudStorage
{
    public class GoogleDriveConsumer : CloudStorageConsumer
    {
        public GoogleDriveConsumer()
        {
            name = "GoogleDrive";
        }

        public override List<CloudItem> ListFilesInFolder(string folderId, IEnumerable<string> fileExtensions)
        {
            List<CloudItem> ret = new List<CloudItem>();
            HttpWebRequest request = null;
            if (folderId == "sharedWithMe")
                request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/drive/v2/files?maxResults=1000&q=sharedWithMe%3Dtrue&key=" + config.appKey);
            else
                request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/drive/v2/files?maxResults=1000&q='" + folderId + "'+in+parents+and+trashed%3Dfalse&key=" + config.appKey);;
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            JObject jobj = JObject.Parse(body);
            int foldersCount = 0;
            foreach (JObject val in jobj["items"])
            {
                CloudItem item = new CloudItem();
                item.Id = val["id"].ToString();
                item.Name = val["title"].ToString();
                item.isFolder = val["mimeType"].ToString().EndsWith(".folder");
                item.cloudConsumer = this.name;
                item.fileVersion = val["modifiedDate"].ToString();
                item.lastEdited = val["modifiedDate"].ToString();
                item.setImageUrl();
                if (item.isFolder)
                {//make sure folders are on top
                    ret.Insert(foldersCount, item);
                    foldersCount++;
                }
                else
                    //foreach (string ext in fileExtensions)
                        //if (item.Name.ToLower().EndsWith(ext.ToLower()))
                        {
                            ret.Add(item);
                            //break;
                        }
            }

            if (folderId == getRootFolderId())
            {
                ret.Insert(foldersCount, new CloudItem()
                {
                    Id = "sharedWithMe",
                    Name = "Shared with Me",
                    isFolder = true
                });
            }

            return ret;
        }

        public override void ListSubfoldersInFolder(string folderId, string folderName, int outlineLevel, ref List<CloudFolder> list)
        {
            WebRequest request = WebRequest.Create("https://api.box.com/2.0/folders/" + folderId + "/items?fields=name");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Method = "GET";

            WebResponse response = request.GetResponse();

            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            JObject jobj = JObject.Parse(body);

            list.Add(new CloudFolder { Name = folderName, OutlineLevel = outlineLevel, Id = folderId });

            foreach (JObject val in jobj["entries"])
            {
                if (!val["mimeType"].ToString().EndsWith(".folder")) continue;

                ListSubfoldersInFolder(val["id"].ToString(), val["name"].ToString(), outlineLevel + 1, ref list);
            }
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
            if (token.access_token == null)
                return false;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/oauth2/v1/tokeninfo?access_token=" + token.access_token);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                JObject jobj = JObject.Parse(body);
                return jobj["error"] == null;
            }
            catch (Exception)
            {
            }
            return false;
        }

        public override string getRootFolderId()
        {
            return "root";
        }

        public override string GetSpaceQuota()
        {
            throw new NotImplementedException();
        }

        public override CloudItem GetFileMetadata(string fileId)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/drive/v2/files/" + fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;

            WebResponse fileResponse = request.GetResponse();
            string body = new StreamReader(fileResponse.GetResponseStream()).ReadToEnd();
            JObject retVal = JObject.Parse(body);
            return parseMetadataJObject(retVal);
        }

        public override Stream GetDocument(string fileId)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/drive/v2/files/" + fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;

            JObject retVal = null;
            using (WebResponse fileResponse = request.GetResponse())
            {
                string body = new StreamReader(fileResponse.GetResponseStream()).ReadToEnd();
                retVal = JObject.Parse(body);
            }
            request = (HttpWebRequest)WebRequest.Create(retVal["downloadUrl"].ToString());
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Headers["GData-Version"] = "3.0";            
            return request.GetResponse().GetResponseStream();
        }

        /// <summary>
        /// CCB 18.02.2015
        /// </summary>
        /// <param name="fileId">the id of the file for which the size is needed</param>
        /// <returns>the size of the file (bytes)</returns>
        public override int GetFileSize(string fileId)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/drive/v2/files/" + fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;

            WebResponse fileResponse = request.GetResponse();
            string body = new StreamReader(fileResponse.GetResponseStream()).ReadToEnd();
            request.Abort();
            JObject retVal = JObject.Parse(body);

            return (retVal["fileSize"] != null) ? (int)retVal["fileSize"] : ((retVal["quotaBytesUsed"] != null) ? (int)retVal["quotaBytesUsed"] : 0);
        }

        public override CloudItem SaveOverwriteDocument(Stream content, String fileId, String contentType = null)
        {
			if (content.CanSeek)
				content.Position = 0;
            if (contentType == null)
                contentType = "application/vnd.ms-project";

            HttpWebRequest request = null;
            HttpWebResponse response = null;

            string url = "https://www.googleapis.com/drive/v2/files/" + fileId;

            request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/json";
            request.Method = "PUT";
            request.Headers["Authorization"] = "Bearer " + token.access_token;

            string metaData = "{";
            metaData += "mimeType:'" + contentType + "'}";
            byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(metaData);
            using (var reqStream = request.GetRequestStream())
            {
                reqStream.Write(bytes, 0, bytes.Length);
            }

            response = (HttpWebResponse)request.GetResponse();
            JObject retVal = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                retVal = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
                fileId = retVal["id"].ToString();
            }
            else
            {
                throw new Exception("Failed to create the document");
            }

            //update contents now
            request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/upload/drive/v2/files/" + fileId + "?uploadType=media");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.ContentType = contentType;
            request.Method = "PUT";
            using (var reqStream = request.GetRequestStream())
            {
                content.CopyTo(reqStream);
            }
            using (response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Failed to upload content to Google Drive");
                }
                retVal = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
                return parseMetadataJObject(retVal);
            }
        }

        private CloudItem parseMetadataJObject(JObject obj)
        {
            return new CloudItem()
            {
                Id = obj["id"].ToString(),
                UniqueId = obj["id"].ToString(),
                Name = obj["title"].ToString(),
                isFolder = obj["mimeType"].ToString().EndsWith(".folder"),
                cloudConsumer = this.name,
                fileVersion = obj["modifiedDate"].ToString(),
                lastEdited = obj["modifiedDate"].ToString(),
                lastEditor = obj["lastModifyingUserName"].ToString()
            };
        }

        private UserData userData;
        public override UserData GetUser()
        {
            if (userData != null) return userData;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/drive/v2/about");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var retVal = JObject.Parse(body);
            userData = new UserData()
            {
                Id = retVal["permissionId"].ToString(),
                Name = retVal["name"].ToString()
            };
            return userData;
        }

        public override List<CloudItem> ListAllFiles(IEnumerable<string> fileExtensions)
        {
            var ret = new List<CloudItem>();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/drive/v2/files?trashed = false");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var retVal = JObject.Parse(body);

            var entries = (JArray)retVal["items"];
            foreach (JObject file in entries)
            {
                foreach (string approovedExt in fileExtensions)
                    if (file["title"].ToString().ToLower().EndsWith(approovedExt.ToLower()))
                    {
                        ret.Add(parseMetadataJObject(file));
                        break;
                    }
            }
            return ret;
        }

        public override CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null)
        {
			if (content.CanSeek)
				content.Position = 0;
            HttpWebRequest request = null;
            HttpWebResponse response = null;

            string url = "https://www.googleapis.com/drive/v2/files";

            request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.Headers["Authorization"] = "Bearer " + token.access_token;

            string metaData = "{";
            metaData += "title:'" + fileName + "', ";
            if (folderId != null)
                metaData += "parents: [{'id' : '" + folderId + "'}], ";
            metaData += "mimeType:'" + contentType + "'}";
            byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(metaData);
            using (var reqStream = request.GetRequestStream())
            {
                reqStream.Write(bytes, 0, bytes.Length);
            }

            response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return SaveOverwriteDocument(content, JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd())["id"].ToString(), contentType);
            }
            else
            {
                throw new Exception("Failed to create the document");
            }
        }

        public override bool HasPermissionToEditFile(string fileId)
        {
            HttpWebRequest request = null;
            request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/drive/v2/files/" + fileId + "/permissions");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var retVal = JObject.Parse(body);
            var entries = (JArray)retVal["items"];
            var user = GetUser();
            foreach (JObject permission in entries)
            {
                if (permission["id"].ToString() == user.Id) // one entry with the primary role for this user (possible values: owner, reader, writer)
                {
                    if (permission["role"].ToString() == "owner" || permission["role"].ToString() == "writer")
                        return true;
                }
                else if (permission["id"].ToString() == "anyone" && permission["role"].ToString() == "writer") // general permission
                    return true;
            }
            return false;
        }

        public override void DeleteFile(string fileId) // TODO: TEST!!!!!
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/drive/v2/files/" + fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Method = "DELETE";
            request.GetResponse();
            request.Abort();
        }

        public override bool DeleteFolder(string folderId) // TODO: TEST!!!!!
        {
            try
            {
                DeleteFile(folderId);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}