using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Amazon;
using CloudProject_extensions;

namespace CloudProject
{
    public class AmazonS3Consumer : CloudStorageConsumer
    {
        public AmazonS3Consumer()
        {
            name = "AmazonS3";
            var client = AWSClientFactory.CreateAmazonS3Client();
        }

        public override List<CloudItem> ListAllFiles(IEnumerable<string> fileExtensions)
        {
            throw new NotImplementedException();
        }

        public override List<CloudItem> ListFilesInFolder(string folderId, IEnumerable<string> fileExtensions)
        {
            throw new NotImplementedException();
        }

        public override void ListSubfoldersInFolder(string folderId, string folderName, int outlineLevel, ref List<CloudFolder> list)
        {
            throw new NotImplementedException();
        }

        public override List<CloudFolder> CreateOutlineDirectoryList()
        {
            throw new NotImplementedException();
        }

        public override bool TokenIsOk()
        {
            if (token.access_token == null)
                return false;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.amazon.com/user/profile");
                request.Headers["Authorization"] = "Bearer " + token.access_token;
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //string body = new StreamReader(response.GetResponseStream()).ReadToEnd(); // info on name, email, user id
                //JObject jobj = JObject.Parse(body);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        public override string getRootFolderId()
        {
            throw new NotImplementedException();
        }

        public override CloudItem SaveOverwriteDocument(Stream content, string fileId, string contentType = null)
        {
            throw new NotImplementedException();
        }

        public override CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null)
        {
            throw new NotImplementedException();
        }

        public override UserData GetUser()
        {
            throw new NotImplementedException();
        }

        public override string GetSpaceQuota()
        {
            throw new NotImplementedException();
        }

        public override CloudItem GetFileMetadata(string fileId)
        {
            throw new NotImplementedException();
        }

        public override Stream GetDocument(string fileId)
        {
            throw new NotImplementedException();
        }

        public override int GetFileSize(string fileId)
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
