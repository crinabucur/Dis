using System;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using CloudProject_extensions;
using System.Text;

namespace CloudProject
{
    public class BoxConsumer : CloudStorageConsumer
    {
        private HttpClientHandler _handler;
        private HttpClient _client;

        public BoxConsumer()
        {
            name = "Box";
            token.access_token = "NRPiR9Xd1VR0gIZyNiskM4uKGBtXVAS5"; // TODO: remove hardcoding

            //PrepareRequests();
        }

        private void PrepareRequests()
        {
            _handler = new HttpClientHandler();

            if (_handler.SupportsAutomaticDecompression)
            {
                _handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            var _client = new HttpClient(_handler);
        }

        public override List<CloudItem> ListFilesInFolder(string folderId, IEnumerable<string> fileExtensions)
        {
            List<CloudItem> ret = new List<CloudItem>();
            WebRequest request = WebRequest.Create("https://api.box.com/2.0/folders/" + folderId + "/items?fields=name,modified_at,modified_by");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";
            request.Method = "GET";

            WebResponse response = request.GetResponse();

            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            JObject jobj = JObject.Parse(body);
            int foldersCount = 0;
            foreach (JObject val in jobj["entries"])
            {
                var item = new CloudItem
                {
                    Id = val["id"].ToString(),
                    Name = val["name"].ToString(),
                    isFolder = val["type"].ToString() == "folder",
                    lastEditor = val["modified_by"].ToString(),
                    lastEdited = val["modified_at"].ToString(),
                    fileVersion = val["modified_at"].ToString(),
                    cloudConsumer = this.name
                };
                item.SetImageUrl();
                if (item.isFolder)
                {//make sure folders are on top
                    ret.Insert(foldersCount, item);
                    foldersCount++;
                }
                else // 23.03.2015 removed filtering
                    //foreach (string ext in fileExtensions)
                        //if (item.Name.ToLower().EndsWith(ext.ToLower()))
                        {
                            ret.Add(item);
                            //break;
                        }
            }
            return ret;
        }

        public override void ListSubfoldersInFolder(string folderId, string folderName, int outlineLevel, ref List<CloudFolder> list) // List<CloudFolder>
        {
            WebRequest request = WebRequest.Create("https://api.box.com/2.0/folders/" + folderId + "/items?fields=name"); // name,modified_at,modified_by
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";
            request.Method = "GET";

            WebResponse response = request.GetResponse();

            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            JObject jobj = JObject.Parse(body);

            list.Add(new CloudFolder { Name = folderName, OutlineLevel = outlineLevel, Id = folderId });

            foreach (JObject val in jobj["entries"])
            {
                if (val["type"].ToString() != "folder") continue;

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
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.box.com/2.0/users/me");
                request.Headers["Authorization"] = "Bearer " + token.access_token;
                //request.Headers["Accept-Encoding"] = "gzip, deflate";
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                JObject jobj = JObject.Parse(body);
                if (jobj["type"] != null && jobj["type"].ToString() != "error")
                    return true;
            }
            catch (Exception)
            {
            }
            return false;
        }



        public override string getRootFolderId()
        {
            return "0";
        }

        public override Stream GetDocument(string fileId)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.box.com/2.0/files/" + fileId + "/content");
            request.Method = "GET";
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";
            return request.GetResponse().GetResponseStream();
        }

        /// <summary>
        /// CCB 18.02.2015
        /// </summary>
        /// <param name="fileId">the id of the file for which the size is needed</param>
        /// <returns>the size of the file (bytes)</returns>
        public override int GetFileSize(string fileId)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.box.com/2.0/files/" + fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";

            string body = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();

            JObject retVal = JObject.Parse(body);
            request.Abort();
            return (retVal["size"] != null) ? (int)retVal["size"] : 0;
        }

        private UserData userData;
        public override UserData GetUser()
        {
            if (userData != null) return userData;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.box.com/2.0/users/me");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var retVal = JObject.Parse(body);
            userData = new UserData()
            {
                Name = retVal["name"].ToString(),
                Email = retVal["login"].ToString()
            };
            return userData;
        }

        public override string GetSpaceQuota()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.box.com/2.0/users/me");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var retVal = JObject.Parse(body);
            var totalSpace = (long)retVal["space_amount"];
            var usedSpace = (long)retVal["space_used"];

            return Utils.FormatQuota(usedSpace) + " of " + Utils.FormatQuota(totalSpace);
        } 

        public override CloudItem GetFileMetadata(string fileId)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.box.com/2.0/files/" + fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";

            string body = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();

            JObject retVal = JObject.Parse(body);
            return parseMetadataJObject(retVal);
        }

        private CloudItem parseMetadataJObject(JObject obj)
        {
            string fullPath = null;
            if (obj["path_collection"] != null)
            {
                StringBuilder filenamepath = new StringBuilder();
                var path = obj["path_collection"]["entries"] as JArray;
                foreach (JObject folder in path)
                {
                    filenamepath.Append(folder["name"].ToString()).Append("\\");
                }
                filenamepath.Append(obj["name"].ToString());
            }

            return new CloudItem()
            {
                Id = obj["id"].ToString(),
                UniqueId = obj["id"].ToString(),
                FullPath = fullPath,
                Name = obj["name"].ToString(),
                lastEditor = obj["modified_by"].ToString(),
                isFolder = obj["type"].ToString() != "file",
                cloudConsumer = this.name,
                fileVersion = obj["modified_at"].ToString(),
                lastEdited = obj["modified_at"].ToString()
            };
        }

        public override CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null)
        {
			if (content.CanSeek)
				content.Position = 0;
            HttpWebRequest request = null;
            HttpWebResponse response = null;

            string url = "https://upload.box.com/api/2.0/files/content";

            request = (HttpWebRequest)WebRequest.Create(url);
            var boundary = "----WebKitFormBoundarySkAQdHysJKel8YBM";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";

            //prepare a multipart pos message content

            if (folderId == null)
            {
                folderId = getRootFolderId();
            }

            string requestContent = string.Empty;
            requestContent += "--" + boundary + "\r\n" + "Content-Disposition: form-data;name=\"parent_id\"\r\n\r\n" + folderId + "\r\n";
            requestContent += "--" + boundary + "\r\n" + "Content-Disposition: form-data;name=\"filename\"; filename=\"" + fileName + "\"\r\nContent-Type:application/vnd.ms-project\r\n\r\n";

            byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(requestContent);
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Length);
                content.CopyTo(requestStream);
                bytes = System.Text.UTF8Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
                requestStream.Write(bytes, 0, bytes.Length);
            }
            response = (HttpWebResponse)request.GetResponse();

            var retVal = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
            var file = retVal["entries"][0] as JObject;
            return parseMetadataJObject(file);
        }

        public override void DeleteFile(string fileId)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.box.com/2.0/files/" + fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";
            request.Method = "DELETE";
            request.GetResponse();
            request.Abort();
        }

        public override bool DeleteFolder(string folderId)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.box.com/2.0/folders/" + folderId + "?recursive=true"); // recursive deletion
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";
            request.Method = "DELETE";
            try
            {
                if (((HttpWebResponse)request.GetResponse()).StatusCode == HttpStatusCode.NoContent)
                    return true;
            }
            finally
            {
                request.Abort();
            }
            return false;
        }

        public bool MoveFilesAndFolders(List<string> ids, string newParentId)
        {
            HttpWebRequest request;
            HttpWebResponse response;

            try
            {
                foreach (var id in ids)
                {
                    request = (HttpWebRequest) WebRequest.Create("https://api.box.com/2.0/files/" + id); // TODO: folders case
                    request.Headers["Authorization"] = "Bearer " + token.access_token;
                    request.Method = "PUT";

                    string metaData = "{";
                    metaData += "\"parent\": {\"id\":\"" + newParentId + "\"}}";
                    byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(metaData);
                    using (var reqStream = request.GetRequestStream())
                    {
                        reqStream.Write(bytes, 0, bytes.Length);
                    }

                    response = (HttpWebResponse) request.GetResponse();
                    if (response.StatusCode != HttpStatusCode.OK)
                        return false;
                }
                return true;
            }
            catch(Exception e)
            {
                return false; // TODO: manage exception
            }
        }

        public void AddComment(string fileId, string comment)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.box.com/2.0/comments");
            request.Method = "POST";
            request.ContentType = "text/json";
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            //request.Headers["Accept-Encoding"] = "gzip, deflate";

            string json = "{\"item\":{\"type\":\"file\"," +
                          "\"id\":\"" + fileId + "\"}," +
                          "\"message\":\"" + comment + "\"}";
            byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(json);
            using (var reqStream = request.GetRequestStream())
            {
                reqStream.Write(bytes, 0, bytes.Length);
            }
            request.GetResponse();
        }
    }
}