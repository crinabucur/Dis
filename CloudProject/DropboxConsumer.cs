using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using CloudStorage;
using Newtonsoft.Json.Linq;
using System.IO;
using CloudStorage_extensions;

namespace CloudStorage
{
    public class DropboxConsumer : CloudStorageConsumer
    {
        private string baseUri = "https://api.dropbox.com/1/";
        public string actualFileId; // the actual path file for the current user (used for PP365Share links)

        public DropboxConsumer()
        {
            name = "Dropbox";
        }


        public override bool TokenIsOk()
        {
            if (token.access_token == null)
                return false;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.dropbox.com/1/account/info");
                request.Method = "GET";
                request.Headers["Authorization"] = "Bearer " + token.access_token;
                request.GetResponse();
				request.Abort ();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public override string getRootFolderId()
        {
            return "/";
        }

        public override void DeleteFile(string fileId)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.dropbox.com/1/fileops/delete");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            string metaData = "root=auto";
            metaData += "&path=" + fileId;
            byte[] bytes = Encoding.UTF8.GetBytes(metaData);
            using (var reqStream = request.GetRequestStream())
            {
                reqStream.Write(bytes, 0, bytes.Length);
            }
            request.GetResponse();
            request.Abort();
        }

        public override bool DeleteFolder(string folderId)
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

        public override CloudFileData GetDocument(CloudItem item)
        {
            actualFileId = item.Id;
            return new CloudFileData()
            {
                fileStream = GetDocument(item.Id),
                cloudItem = GetFileMetadata(actualFileId) // get file metadata using the actual path to the file, not the initial one
            };
        }

        public override List<CloudItem> ListFilesInFolder(string folderId, IEnumerable<string> fileExtensions)
        {

            List<CloudItem> ret = new List<CloudItem>();

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUri + "/metadata/dropbox" + folderId);
            request.Method = "GET";
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            JObject metadata = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());

            int foldersCount = 0;
            foreach (JObject val in metadata["contents"])
            {
                CloudItem item = parseMetadataJObject(val);
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
			request.Abort ();

            return ret;
        }

        public override Stream GetDocument(string fileId)
        {
            try
            {
                HttpWebRequest request =
                    (HttpWebRequest) WebRequest.Create("https://api-content.dropbox.com/1/files/dropbox" + fileId);
                request.Method = "GET";
                request.Headers["Authorization"] = "Bearer " + token.access_token;
				var ret = request.GetResponse().GetResponseStream();
				return ret;
            }
            catch (WebException we)
            {
                var errorResponse = we.Response as HttpWebResponse;
                if (errorResponse != null && errorResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    // For Dropbox, 404 - NotFound error can mean an inaccurate path of the file for the current user
                    // a search is attempted by eliminating the folders from the path, one by one
                    string fileName = fileId.Substring(fileId.LastIndexOf("/", StringComparison.Ordinal) + 1);
                    string folderPath = fileId.Replace(fileName, "").Substring(1);

                    while (folderPath.IndexOf("/", StringComparison.Ordinal) != -1)
                    {
                        folderPath = folderPath.Substring(folderPath.IndexOf("/", StringComparison.Ordinal) + 1);
                        CloudItem ci = null;
                        try
                        {
                            ci = SearchFileByPathFragment(folderPath, fileName);
                        }
                        catch{}

                        if (ci != null)
                        {
                            actualFileId = ci.Id; // this will be used when getting the file metadata, instead of the initial id
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api-content.dropbox.com/1/files/dropbox" + ci.Id);
                            request.Method = "GET";
                            request.Headers["Authorization"] = "Bearer " + token.access_token;
							var r = request.GetResponse().GetResponseStream();
							request.Abort ();
							return r;
                        }
                    }
                }
                throw we;
            }
        }

        /// <summary>
        /// CCB 18.02.2015
        /// </summary>
        /// <param name="fileId">the id of the file for which the size is needed</param>
        /// <returns>the size of the file (bytes)</returns>
        public override int GetFileSize(string fileId)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUri + "/metadata/dropbox" + fileId);
            request.Method = "GET";
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            JObject retVal = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
            request.Abort();
            return (retVal["bytes"] != null) ? (int)retVal["bytes"] : 0;
        }

        public override CloudItem SaveOverwriteDocument(Stream content, String fileId, String contentType = null)
        {
			if (content.CanSeek)
				content.Position = 0;
            if (contentType == null)
                contentType = "application/vnd.ms-project";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api-content.dropbox.com/1/files_put/dropbox" + fileId);
            request.Headers["Authorization"] = "Bearer " + token.access_token;

            request.ContentType = contentType;
            request.Method = "PUT";
            using (var reqStream = request.GetRequestStream())
            {
                content.CopyTo(reqStream);
            }

            var retVal = JObject.Parse(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());
            return parseMetadataJObject(retVal);
        }

        private UserData userData;
        public override UserData GetUser()
        {
            if (userData != null) return userData;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.dropbox.com/1/account/info");
            request.Method = "GET";
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            var response = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
			request.Abort ();
            var retVal = JObject.Parse(response);
            userData = new UserData()
            {
                Name = retVal["display_name"].ToString()
            };
            return userData;
        }

        public override string GetSpaceQuota()
        {
            throw new NotImplementedException();
        }

        public override CloudItem GetFileMetadata(string fileId)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUri + "/metadata/dropbox" + fileId);
            request.Method = "GET";
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            JObject retVal = JObject.Parse(new StreamReader(response.GetResponseStream()).ReadToEnd());
			request.Abort ();
            return parseMetadataJObject(retVal);
        }

        private CloudItem parseMetadataJObject(JObject obj)
        {
            CloudItem item = new CloudItem
            {
                Id = obj["path"].ToString(),
                UniqueId = obj["rev"].ToString(),
                Name = obj["path"].ToString().Substring(obj["path"].ToString().LastIndexOf("/") + 1),
                isFolder = (bool)(obj["is_dir"]),
                cloudConsumer = this.name,
                fileVersion = obj["rev"].ToString(),
                lastEdited = obj["modified"].ToString()
            };

            item.setImageUrl();
            return item;
        }

        public override List<CloudItem> ListAllFiles(IEnumerable<string> fileExtensions)
        {
            //TODO this is hardcoded for *.mp* and *.xml file names only

            var ret = new List<CloudItem>();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.dropbox.com/1/search/dropbox?query=.mp");
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Method = "GET";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
			request.Abort ();
            var retVal = JArray.Parse(body);

            foreach (JObject file in retVal)
                foreach (string ext in fileExtensions)
                    if (((string)file["path"]).ToLower().EndsWith(ext.ToLower()))
                    {
                        ret.Add(parseMetadataJObject(file));
                        break;
                    }

            request = (HttpWebRequest)WebRequest.Create("https://api.dropbox.com/1/search/dropbox?query=.xml");
            request.Headers["Authorization"] = "Bearer " + token.access_token;

            request.Method = "GET";

            response = (HttpWebResponse)request.GetResponse();
            body = new StreamReader(response.GetResponseStream()).ReadToEnd();
			request.Abort ();
            retVal = JArray.Parse(body);

            foreach (JObject file in retVal)
                foreach (string ext in fileExtensions)
                    if (((string)file["path"]).ToLower().EndsWith(ext.ToLower()))
                    {
                        ret.Add(parseMetadataJObject(file));
                        break;
                    }
            return ret;
        }

        public List<string> GetFileRevisions(string path)
        {
            //radsimu - to detect if a file is shared between two users is to search through its rev codes. A file has more rev codes (each corresponding to a revision). So when opening a dropbox file must check all its rev codes with the codes saved in FileLocation.cloudFilesAlreadyOpened (in this dictionary a Dropbox file is identified by its latest rev code at the time it was opened - but it might have been modified meanwhile)
            List<string> ret = new List<string>();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.dropbox.com/1/revisions/dropbox" + path);
            request.Headers["Authorization"] = "Bearer " + token.access_token;
            request.Method = "GET";
            var metadatas = JArray.Parse(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());
			request.Abort ();
            foreach (JObject obj in metadatas)
                ret.Add(obj["rev"].ToString());

            return ret;
        }

        public override CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null)
        {
			if (content.CanSeek)
				content.Position = 0;
            if (folderId == null)
            {
                folderId = getRootFolderId();
            }
            else if (!folderId.EndsWith("/"))
            {
                folderId = folderId + "/";
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api-content.dropbox.com/1/files_put/dropbox" + folderId + fileName);
            request.Headers["Authorization"] = "Bearer " + token.access_token;

            request.ContentType = contentType;
            request.Method = "PUT";
            using (var reqStream = request.GetRequestStream())
            {
                content.CopyTo(reqStream);
            }
            var retVal = JObject.Parse(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());
			request.Abort ();
            return parseMetadataJObject(retVal);
        }

        public override bool HasPermissionToEditFile(string fileId)
        {
            // All collaborators can edit a file (https://www.dropbox.com/help/60/en)
            return true;
        }

        public override string GenerateShareUrlParam(CloudItem item)
        {
			return name + "://" + Uri.EscapeDataString(item.Id.Substring(1));
        }

        public override string GetUniqueIdFromUrlParam(string parameter)
        {
            return parameter.Remove(0, name.Length + 2);
        }

        public override CloudItem GetCloudItemFromParam(string urlParam)
        {
            int aux = urlParam.IndexOf("://");
            return new CloudItem()
            {
                cloudConsumer = urlParam.Substring(0, aux),
                UniqueId = urlParam.Substring(aux + 2),
                Id = urlParam.Substring((aux + 2))
            };
        }

        // CCB if a file is not found at a given path, we attempt to find it at other possible paths (removing external folders one by one)
        private CloudItem SearchFileByPathFragment(string path, string fileName)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUri + "search/dropbox/" + path + "?query=" + fileName + "&access_token=" + token.access_token + "&oauth_consumer_key=" + config.appKey); 
            request.Method = "GET";
			//request.Headers["Accept-Encoding"] = "gzip, deflate, compress";
            request.Accept = "*/*";
            request.SetHeader("User-Agent", "runscope/0.1");
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream responseStream = response.GetResponseStream();
            //if (response.ContentEncoding.ToLower().Contains("gzip"))
			//responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
            //else if (response.ContentEncoding.ToLower().Contains("deflate"))
            //    responseStream = new DeflateStream(responseStream, CompressionMode.Decompress); // todo deflate case?

            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);

            string json = reader.ReadToEnd();
			request.Abort ();
            responseStream.Dispose();

            JArray retVal = JArray.Parse(json);
            JObject entry = JObject.Parse(retVal.First.ToString()); // multiple entries indicate that a collision occurred. at this point there is no way to tell which was the file we were looking for
            return parseMetadataJObject(entry);
        }
    }
}

