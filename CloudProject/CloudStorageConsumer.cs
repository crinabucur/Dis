using System;
using System.Collections.Generic;
using System.IO;

namespace CloudProject
{
	public struct OAuthServiceConfig
	{
		public string appKey;
		public string appSecret;
		public string authorizeUri;
		public string tokenUri;
		public string redirectUri; //most oauth services require a redirect url which is registered in the dev console. For client side auth this can be a fictive https url, but must be registerred anyway, if the auth service requires
		public string scope;
	}

	public struct OAuthToken
	{
		public string access_token;
		public string refresh_token;
	}

	public class CloudItem{
		public string cloudConsumer;
		public string Name;
        public string FullPath; //this can be obtained for some clouds - can be null
		public string Id; //used with GetFileMetada and GetDocument
        public string UniqueId; //used for uniquely identifying this file across multiple users
		public bool isFolder;
	    public bool isBucket; //true for AmazonS3 only
	    public String bucketName; // only used for AmazonS3
		public String fileVersion; //used for identifying file version - can be a date string but not necessarily (Dropbox is different)
		public String lastEditor; //user who last edited
        public String lastEdited; //date string of last update
	    public string imageUrl;
	    public bool IsKnownType = true;
	    public string Type;

	    public void SetImageUrl()
	    {
	        if (isFolder)
            {
                imageUrl = "Images/folderIcon_64x64.png";
                return;
            }
	        if (isBucket)
	        {
	            isFolder = true;
                imageUrl = "Images/bucketIcon_64x64.png";
	            return;
	        }

	        var extension = Name.Substring(Name.LastIndexOf(".") + 1).ToLower();
	        switch (extension)
	        {
                case "bmp":
                case "gif":
                case "jpeg":
                case "jpg":
                case "png":
                    {
                        imageUrl = "Images/File Icons/" + extension + ".png";
                        Type = "image";
                        break;
                    }
                case "doc":
	                {
                        imageUrl = "Images/File Icons/" + extension + ".png";
                        Type = "doc";
                        break;
	                }
                case "docx":
                    {
                        imageUrl = "Images/File Icons/" + extension + ".png";
                        Type = "docx";
                        break;
                    }
                case "pdf":
                    {
                        imageUrl = "Images/File Icons/" + extension + ".png";
                        Type = "pdf";
                        break;
                    }
                case "mp3":
                    {
                        imageUrl = "Images/File Icons/" + extension + ".png";
                        Type = "audio";
                        break;
                    }
                case "mp4":
                    {
                        imageUrl = "Images/File Icons/mpeg.png";
                        Type = "video";
                        break;
                    }
                case "txt":
                    {
                        imageUrl = "Images/File Icons/" + extension + ".png";
                        Type = "text";
                        break;
                    }
                case "rar": 
                case "zip": 
	                {
                        imageUrl = "Images/File Icons/" + extension + ".png";
                        Type = "archive";
                        IsKnownType = false;
	                    break;
	                }
                default:
	                {
                        imageUrl = "Images/File Icons/unknown.png";
                        Type = "unknown";
	                    IsKnownType = false;
	                    break;
	                }
	        }
	    }
	}

	public class CloudFileData{
		public CloudItem CloudItem;
		public Stream FileStream;
	}

	public class UserData{
        public string Id;
		public string Name;
		public string Email;
	}

    public class CloudFolder
    {
        public int OutlineLevel;
        public string Name;
        public string Id;
    }

    public class ResponsePackage
    {
        public bool Error;
        public string ErrorMessage;
    }

	public abstract class CloudStorageConsumer
	{
        public OAuthServiceConfig config = new OAuthServiceConfig();

		public OAuthToken token; //store the token info in the cloud storage consumer instance
		public string name;

        public abstract List<CloudItem> ListFilesInFolder(string folderId);
        public abstract void ListSubfoldersInFolder(string folderId, string folderName, int outlineLevel, ref List<CloudFolder> list); // List<CloudFolder> ListSubfoldersInFolder(string folderId, int outlineLevel);
	    public abstract List<CloudFolder> CreateOutlineDirectoryList();
		public abstract bool TokenIsOk();
		public abstract string getRootFolderId();
        public abstract CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null);
		public abstract UserData GetUser();
        public abstract string GetSpaceQuota();
        public abstract CloudItem GetFileMetadata(string fileId);
        public abstract Stream GetDocument(string fileId);
	    public abstract int GetFileSize(string fileId);
	    public abstract void DeleteFile(string fileId);
        public abstract bool DeleteFolder(string folderId);
        public abstract ResponsePackage AddFolder(string parentFolderId, string _name);
        public abstract string GetLogOutEndpoint(); 

        /// <summary>
        /// A version of the GetDocument method which returns a CloudFileData - a convenient way to stick file metadata and content stream together
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual CloudFileData GetDocument(CloudItem item)
        {
            return new CloudFileData()
            {
                FileStream = GetDocument(item.Id),
				CloudItem = GetFileMetadata(item.Id)
            };
        }

		public virtual string GenerateShareUrlParam(CloudItem item){
			return name + "://" + item.Id;
		}

        public virtual CloudItem GetCloudItemFromParam(string urlParam)
        {
            int aux = urlParam.IndexOf("://");
            return new CloudItem()
            {
                cloudConsumer = urlParam.Substring(0, aux),
                UniqueId = urlParam.Substring(aux + 3),
                Id = urlParam.Substring((aux + 3))
            };
        }
        
        public virtual string GetUniqueIdFromUrlParam(string parameter){
            return parameter.Remove(0, name.Length + 3);
        }
	}
}
