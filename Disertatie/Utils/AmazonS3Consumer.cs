using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CloudProject;
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
        }

        public bool CreateClient(string accessKey, string secretKey, string region)
        {
            try
            {
                RegionEndpoint regionEndpoint = RegionEndpoint.EnumerableAllRegions.FirstOrDefault(reg => reg.DisplayName == region);
                _client = AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey, regionEndpoint);
                ListBuckets();
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
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
                Debug.WriteLine("An error occurred while listing the buckets! Error code: {0}, message '{1}", amazonS3Exception.ErrorCode, amazonS3Exception.Message);
            }
            return ret;
        } 

        public override List<CloudItem> ListFilesInFolder(string folderId)
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
                    Id = entry.Key,
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
                    Id = entry.Key,
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

        private void ListSubfoldersInFolder(string folderId, string folderName, string bucketName, int outlineLevel, ref List<CloudFolder> list)
        {
            list.Add(new CloudFolder { Name = folderName.TrimEnd('/'), OutlineLevel = outlineLevel, Id = folderId });

            var request = new ListObjectsRequest { BucketName = bucketName, Prefix = folderId };
            var response = _client.ListObjects(request);

            foreach (S3Object entry in response.S3Objects)
            {
                if (string.Equals(entry.Key, folderId) || !entry.Key.EndsWith("/")) // this very same folder or not even a folder
                    continue;

                var item = new CloudItem
                {
                    Id = entry.Key,
                    Name = entry.Key.TrimEnd('/')
                };

                if (item.Name.Split('/').Length != folderId.Split('/').Length)
                    continue; // this is not a directly contained file / folder, it should not be shown at this level

                item.Name = item.Name.Replace(folderId, "");

                ListSubfoldersInFolder(item.Id, item.Name, bucketName, outlineLevel + 1, ref list);
            }
        }

        public override List<CloudFolder> CreateOutlineDirectoryList()
        {
            var listOfBuckets = ListBuckets();
            var list = new List<CloudFolder> { new CloudFolder { Name = "All Buckets", OutlineLevel = 0, Id = "/" } };

            foreach (var bucket in listOfBuckets)
            {
                list.Add(new CloudFolder { Name = bucket.Name, OutlineLevel = 1, Id = bucket.Name });

                var request = new ListObjectsRequest { BucketName = bucket.Name };
                var response = _client.ListObjects(request);

                // get the objects at the TOP LEVEL, i.e. not inside any folders
                var objects = response.S3Objects.Where(o => !o.Key.Contains(@"/"));

                // get the folders at the TOP LEVEL only
                var folders = response.S3Objects.Except(objects).Where(o => o.Key.Last() == '/' && o.Key.IndexOf(@"/") == o.Key.LastIndexOf(@"/"));

                foreach (S3Object entry in folders)
                {
                    ListSubfoldersInFolder(entry.Key, entry.Key, bucket.Name, 2, ref list);
                }
            }

            return list;
        }

        public override bool TokenIsOk()
        {
            return _client != null;
        }

        public override string getRootFolderId()
        {
            return "_"; // conventional, as bucket names cannot contain the underscore character
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
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = _currentBucket,
                Key = fileId
            };

            GetObjectResponse response = _client.GetObject(request);

            var item = new CloudItem
            {
                Id = response.Key,
                isFolder = response.Key.EndsWith("/"),
                Name = response.Key.TrimEnd('/'),
                lastEdited = response.LastModified.ToString(),
                cloudConsumer = name
            };

            return item;
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

        public override void DeleteFile(string fileId)
        {
            DeleteObjectRequest deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _currentBucket,
                Key = fileId
            };

            _client.DeleteObject(deleteObjectRequest);
        }

        public override bool DeleteFolder(string folderId)
        {
            if (_currentBucket != "")
            {
                // this is a folder
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = _currentBucket;
                request.Prefix = folderId;
                ListObjectsResponse response = _client.ListObjects(request);

                List<KeyVersion> listOfKeys = new List<KeyVersion>();

                foreach (S3Object entry in response.S3Objects)
                {
                    listOfKeys.Add(new KeyVersion { Key = entry.Key, VersionId = null});
                }

                DeleteObjectsRequest multiObjectDeleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _currentBucket,
                    Objects = listOfKeys // This includes the object keys and null version IDs.
                };

                try
                {
                    DeleteObjectsResponse resp = _client.DeleteObjects(multiObjectDeleteRequest);
                    Console.WriteLine("Successfully deleted all {0} items", resp.DeletedObjects.Count);
                    return true;
                }
                catch (DeleteObjectsException e)
                {
                    return false;
                }
            }
            else
            {
                // this is a bucket (it cannot be deleted unless empty)
                ListObjectsRequest request = new ListObjectsRequest { BucketName = folderId };
                ListObjectsResponse response = _client.ListObjects(request);

                List<KeyVersion> listOfKeys = new List<KeyVersion>();

                foreach (S3Object entry in response.S3Objects)
                {
                    listOfKeys.Add(new KeyVersion { Key = entry.Key, VersionId = null });
                }

                DeleteObjectsRequest multiObjectDeleteRequest = new DeleteObjectsRequest
                {
                    BucketName = folderId,
                    Objects = listOfKeys // This includes the object keys and null version IDs.
                };

                try
                {
                    DeleteObjectsResponse resp = _client.DeleteObjects(multiObjectDeleteRequest);
                    Console.WriteLine("Successfully deleted all {0} items", resp.DeletedObjects.Count);

                    DeleteBucketResponse res = _client.DeleteBucket(folderId);
                    return true;
                }
                catch (DeleteObjectsException e)
                {
                    return false;
                }
            }
        }

        public override ResponsePackage AddFolder(string parentFolderId, string _name)
        {
            throw new NotImplementedException();
        }

        public override string GetLogOutEndpoint()
        {
            _client = null;
            return "";
        }
    }
}
