using System;
using System.Collections.Generic;
using System.IO;

namespace CloudStorage
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
		public String fileVersion; //used for identifying file version - can be a date string but not necessarily (Dropbox is different)
		public String lastEditor; //user who last edited
        public String lastEdited; //date string of last update
	    public string imageUrl;

	    public void setImageUrl()
	    {
	        if (isFolder)
            {
                imageUrl = "Images/folderIcon_64x64.png";
                return;
            }

	        var extension = Name.Substring(Name.LastIndexOf(".") + 1).ToLower();
	        switch (extension)
	        {
                // TODO: check all
                case "bmp":
                case "doc":
                case "docx":
                case "gif":
                case "jpeg":
                case "jpg":
                case "mp3":
                case "pdf":
                case "png":
                case "rar": 
                case "txt":
                case "zip": 
	            {
                    imageUrl = "Images/File Icons/" + extension + ".png";
	                break;
	            }
                // TODO: add video, multimedia, etc
                default:
	            {
                    imageUrl = "Images/unknownIcon_64x64.png";
	                break;
	            }
	        }
	    }
	}

	public class CloudFileData{
		public CloudItem cloudItem;
		public Stream fileStream;
	}

	public class UserData{
        public string Id;
		public string Name;
		public string email;
	}

	public abstract class CloudStorageConsumer
	{
        public OAuthServiceConfig config = new OAuthServiceConfig();

		public OAuthToken token; //store the token info in the cloud storage consumer instance
		public string name;

        [Obsolete("It is best advised to retrieve files in a folder based manner, using ListFilesInFolder() in conjunction with GetRootFolderId()")]
        public abstract List<CloudItem> ListAllFiles(IEnumerable<string> fileExtensions);
        public abstract List<CloudItem> ListFilesInFolder(string folderId, IEnumerable<string> fileExtensions);
		public abstract bool TokenIsOk ();
		public abstract string getRootFolderId ();
        /// <summary>
        /// save the content stream in the location specified by cloudItem.Id. cloudItem fields lastEdited and fileVersion will be updated after call
        /// </summary>
        /// <param name="cloudItem"></param>
        /// <param name="content"></param>
		public abstract CloudItem SaveOverwriteDocument(Stream content, String fileId, String contentType = null);
        public abstract CloudItem SaveCreateDocument(Stream content, string fileName, string contentType = null, string folderId = null);
		public abstract UserData GetUser ();
        public abstract string GetSpaceQuota();
        public abstract CloudItem GetFileMetadata(string fileId);
        public abstract Stream GetDocument(string fileId);
	    public abstract int GetFileSize(string fileId);
	    public abstract bool HasPermissionToEditFile(string fileId);

        /// <summary>
        /// A version of the GetDocument method which returns a CloudFileData - a convenient way to stick file metadata and content stream together
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual CloudFileData GetDocument(CloudItem item)
        {
            return new CloudFileData()
            {
                fileStream = GetDocument(item.Id),
				cloudItem = GetFileMetadata(item.Id)
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
