using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Amazon;
using Amazon.EC2.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CloudProject;
using CloudProject_extensions;
using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;
using Newtonsoft.Json.Converters;
using UserData = CloudProject.UserData;

namespace Disertatie.Utils
{
    public class AmazonS3Consumer : CloudStorageConsumer
    {
        private IAmazonS3 _client = null;
        private string _currentBucket = "";

        public AmazonS3Consumer()
        {
            name = "AmazonS3";
            //Amazon.Util.ProfileManager.RegisterProfile("crina", "AKIAIEXSQM2VUJMASPSQ", "txaULBobX4VE98w9EfnNsbveaAx/TNBTrcUJOViR"); // profileName, accessKey, secretKey
            CreateClient("AKIAIEXSQM2VUJMASPSQ", "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", "US East (Virginia)");
            ListBuckets();
        }

        public bool CreateClient(string accessKey, string secretKey, string region)
        {
            try
            {
                RegionEndpoint regionEndpoint = RegionEndpoint.EnumerableAllRegions.FirstOrDefault(reg => reg.DisplayName == region);
                _client = AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey, regionEndpoint);
            }
            catch (Exception e)
            {
                throw e;
            }
            return true;
        }

        [Obsolete("It is best advised to retrieve files in a folder based manner, using ListFilesInFolder() in conjunction with GetRootFolderId()")]
        public override List<CloudItem> ListAllFiles(IEnumerable<string> fileExtensions)
        {
            throw new NotImplementedException();
        }

        public List<CloudItem> ListBuckets()
        {
            _currentBucket = "";
            var ret = new List<CloudItem>();
            try
            {
                ListBucketsResponse response = _client.ListBuckets();
                foreach (S3Bucket bucket in response.Buckets)
                {
                    var item = new CloudItem
                    {
                        Id = bucket.BucketName,
                        Name = bucket.BucketName,
                        isBucket = true,
                        cloudConsumer = name
                    };
                    item.SetImageUrl();
                    ret.Add(item);
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                Debug.WriteLine("An error occurred when listing buckets! Code: {0}, message '{1}", amazonS3Exception.ErrorCode, amazonS3Exception.Message);
            }
            return ret;
        } 

        public override List<CloudItem> ListFilesInFolder(string folderId, IEnumerable<string> fileExtensions)
        {
            if (folderId == getRootFolderId())
                return ListBuckets();

            var ret = new List<CloudItem>();
            ListObjectsRequest request;
            ListObjectsResponse response;

            var folderIdIsBucket = (!folderId.Contains("/"));
            if (!folderIdIsBucket)
            {
                // different flow - get objects in a regular folder
                request = new ListObjectsRequest { BucketName = _currentBucket, Prefix = folderId };
                response = _client.ListObjects(request);
                int foldersCount = 0;

                foreach (S3Object entry in response.S3Objects)
                {
                    if (string.Equals(entry.Key, folderId)) // this very same folder
                        continue;  

                    var item = new CloudItem
                    {
                        Id = entry.Key,
                        isFolder = entry.Key.EndsWith("/"),
                        Name = entry.Key.TrimEnd('/'),
                        lastEdited = entry.LastModified.ToString(),
                        cloudConsumer = name
                    };

                    if (item.Name.Split('/').Length != folderId.Split('/').Length)
                        continue; // this is not a directly contained file / folder, it should not be shown at this level

                    item.Name = item.Name.Replace(folderId, "");
                    item.SetImageUrl();
                    if (item.isFolder)
                    {//make sure folders are on top
                        ret.Insert(foldersCount, item);
                        foldersCount++;
                    }
                    else
                    {
                        ret.Add(item);
                    }
                }

                return ret;
            }
           
            _currentBucket = folderId;

            request = new ListObjectsRequest { BucketName = folderId };
            response = _client.ListObjects(request);

            // get the objects at the TOP LEVEL, i.e. not inside any folders
            var objects = response.S3Objects.Where(o => !o.Key.Contains(@"/"));

            // get the folders at the TOP LEVEL only
            var folders = response.S3Objects.Except(objects).Where(o => o.Key.Last() == '/' && o.Key.IndexOf(@"/") == o.Key.LastIndexOf(@"/"));

            foreach (S3Object entry in folders)
            {
                var item = new CloudItem
                {
                    Id = entry.Key, // ETag
                    Name = entry.Key.TrimEnd('/'),
                    isFolder = true,
                    lastEdited = entry.LastModified.ToString(),
                    cloudConsumer = name
                };
                item.SetImageUrl();
                ret.Add(item);
            }

            foreach (S3Object entry in objects)
            {
                var item = new CloudItem
                {
                    Id = entry.Key, // ETag
                    Name = entry.Key,
                    lastEdited = entry.LastModified.ToString(),
                    cloudConsumer = name
                };
                item.SetImageUrl();
                ret.Add(item);
            }

            return ret;
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
            if (_client == null)
                return false;

            try
            {
            }
            catch
            {
                return false;
            }
            return true;
        }

        public override string getRootFolderId()
        {
            return "_"; // conventional, as bucket names cannot contain the underscore character
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
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = _currentBucket,
                Key = fileId
            };

            GetObjectResponse response = _client.GetObject(request);
            return response.ResponseStream;
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
