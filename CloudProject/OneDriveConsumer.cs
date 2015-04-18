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
                item.setImageUrl();
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

        public override bool TokenIsOk()
        {
            if (token.access_token == null)
                return false;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/me/skydrive/quota?access_token=" + token.access_token);
                //ClientBase.AuthorizeRequest(request, AuthorizationState.AccessToken);
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

        public override CloudItem SaveOverwriteDocument(Stream content, String fileId, String contentType = null)
        {
            if (content.CanSeek)
                content.Position = 0;

            HttpWebRequest request = null;
            request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/" + fileId + "/content" + "?access_token=" + token.access_token);
            request.Method = "PUT";
            using (var reqStream = request.GetRequestStream())
            {
                content.CopyTo(reqStream);
            }
            request.GetResponse();
            return GetFileMetadata(fileId);
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

        public override List<CloudItem> ListAllFiles(IEnumerable<string> fileExtensions)
        {
            //TODO include shared folder in the search
            var ret = new List<CloudItem>();
            // OMC 01.27.2014 - add shared folder
            string URL = "https://apis.live.net/v5.0/me/skydrive/search?q=.&access_token=" + token.access_token;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);//radsimu TODO this is limited to 100 files per page - must check pagination and make subsequent api calls if necessary
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var retVal = JObject.Parse(body);

            var entries = (JArray)retVal["data"];
            foreach (JObject file in entries)
                if (file["id"].ToString().StartsWith("file."))//radsimu check if it's a folder
                    foreach (string ext in fileExtensions)
                        if (file["name"].ToString().ToLower().EndsWith(ext.ToLower()))
                        {
                            ret.Add(parseMetadataJObject(file));
                            break;
                        }

            return ret;
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

        public override bool HasPermissionToEditFile(string fileId)
        {
            HttpWebRequest request;
            request = (HttpWebRequest) WebRequest.Create("https://apis.live.net/v5.0/" + fileId + "?access_token=" + token.access_token);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var retVal = JObject.Parse(body);
            var user = GetUser();
            if ((retVal["from"])["id"].ToString() == user.Id) // for OneDrive, the only editor through API is the owner of the file
                return true;
            return false; // ideally, a distinction should be made between users that could edit and only they should get the "API limitation" message
        }

        public override void DeleteFile(string fileId)
        {
            HttpWebRequest request;
            request = (HttpWebRequest)WebRequest.Create("https://apis.live.net/v5.0/" + fileId + "?access_token=" + token.access_token);
            request.Method = "DELETE";
            request.GetResponse();
            request.Abort();
        }
    }
}