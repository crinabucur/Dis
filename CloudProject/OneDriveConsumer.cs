using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using CloudProject_extensions;

namespace CloudProject
{
    public class OneDriveConsumer : CloudStorageConsumer
    {
        public OneDriveConsumer()
        {
            name = "OneDrive";
        }

        public override List<CloudItem> ListFilesInFolder(string folderId, IEnumerable<string> fileExtensions)
        {
            List<CloudItem> ret = new List<CloudItem>();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/" + folderId + "/files?access_token=" + token.access_token);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            JObject jobj = JObject.Parse(body);
            int foldersCount = 0;
            foreach (JObject val in jobj["data"])
            {
                CloudItem item = parseMetadataJObject(val);
                item.SetImageUrl();
                if (item.isFolder)
                {//make sure folders are on top
                    ret.Insert(foldersCount, item);
                    foldersCount++;
                }
                else
                    //foreach (string ext in fileExtensions)
                        //if (item.Name.ToLower().EndsWith(ext.ToLower())){
                            ret.Add(item);
                            //break;
                        //}
            }

            if (folderId == getRootFolderId())//add shared folder
            {
                ret.Insert(foldersCount, new CloudItem() {
                    Id = "me/skydrive/shared",
                    Name = "Shared",
                    isFolder = true
                });
            }
            return ret;
        }

        public override void ListSubfoldersInFolder(string folderId, string folderName, int outlineLevel, ref List<CloudFolder> list)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/" + folderId + "/files?access_token=" + token.access_token);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            JObject jobj = JObject.Parse(body);

            list.Add(new CloudFolder { Name = folderName, OutlineLevel = outlineLevel, Id = folderId });

            if (folderId == getRootFolderId()) //add shared folder
            {
                ListSubfoldersInFolder("me/skydrive/shared", "Shared", outlineLevel + 1, ref list);
            }

            foreach (JObject val in jobj["data"])
            {
                CloudItem item = parseMetadataJObject(val); // TODO: optimize, no need for this!
                if (!item.isFolder) continue;

                ListSubfoldersInFolder(item.Id, item.Name, outlineLevel + 1, ref list);
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
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/me/skydrive/quota?access_token=" + token.access_token);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                JObject jobj = JObject.Parse(body);
                if (jobj["error"] == null)
                    return true;
            }
            catch (Exception) { }

            return false;
        }

        public override string getRootFolderId()
        {
            return "me/skydrive/";
        }

        public override Stream GetDocument(string fileId)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/" + fileId + "/content?access_token=" + token.access_token);
            request.Method = "GET";
            return request.GetResponse().GetResponseStream();
        }

        /// <summary>
        /// CCB 18.02.2015
        /// </summary>
        /// <param name="fileId">the id of the file for which the size is needed</param>
        /// <returns>the size of the file (bytes)</returns>
        public override int GetFileSize(string fileId)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/" + fileId + "/?access_token=" + token.access_token);
            request.Method = "GET";

            var contentResponse = (HttpWebResponse)request.GetResponse();
            JObject retVal = JObject.Parse(new StreamReader(contentResponse.GetResponseStream()).ReadToEnd());
            request.Abort();
            return (retVal["size"] != null) ? (int)retVal["size"] : 0;
        }

        public override string GetSpaceQuota()
        {
            throw new NotImplementedException();
        }

        public override CloudItem GetFileMetadata(string fileId)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/" + fileId + "/?access_token=" + token.access_token);
            request.Method = "GET";

            var contentResponse = (HttpWebResponse)request.GetResponse();
            JObject retVal = JObject.Parse(new StreamReader(contentResponse.GetResponseStream()).ReadToEnd());
            return parseMetadataJObject(retVal);
        }

        private CloudItem parseMetadataJObject(JObject retVal)
        {
            return new CloudItem()
            {
                Id = retVal["id"].ToString(),
                UniqueId = retVal["id"].ToString(),
                Name = retVal["name"].ToString(),
                isFolder = retVal["type"].ToString() == "folder",
                cloudConsumer = this.name,
                fileVersion = retVal["updated_time"].ToString(),
                lastEdited = retVal["updated_time"].ToString()
            };
        }

        private UserData userData;
        public override UserData GetUser()
        {
            if (userData != null) return userData;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/me/?access_token=" + token.access_token);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var retVal = JObject.Parse(body);
            string name = "name_placeholder";
            string id = "";
            if (retVal["name"] != null)
                name = retVal["name"].ToString();
            if (retVal["id"] != null)
                id = retVal["id"].ToString();
            userData = new UserData()
            {
                Id = id,
                Name = name
            };
            return userData;
        }

        public override CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null)
        {
            if (folderId == null)
            {
                folderId = getRootFolderId();
            }

            var request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/" + folderId + "/files/" + fileName + "?access_token=" + token.access_token);
            
            request.Method = "PUT";
            using (var reqStream = request.GetRequestStream())
            {
                content.CopyTo(reqStream);
            }
            var response = (HttpWebResponse)request.GetResponse();
            //get the created fileId and update fileLocation
            var retVal = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
            return GetFileMetadata(retVal["id"].ToString());
        }

        public override void DeleteFile(string fileId)  // TODO: TEST!!!!!
        {
            HttpWebRequest request;
            request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/" + fileId + "?access_token=" + token.access_token);
            request.Method = "DELETE";
            request.GetResponse();
            request.Abort();
        }

        public override bool DeleteFolder(string folderId)  // TODO: TEST!!!!!
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