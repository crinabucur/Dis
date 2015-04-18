using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;

namespace Disertatie.Utils
{
    public class SharepointOnline
    {
        private ClientContext sharepoint = null;
        public Web site = null;
        public string user = "";
        public string saveFolder = "";
        public bool canEditCurrentFile;
        private string _knownSecurityToken; // 10.06.2014

        public MsOnlineClaimsHelper ClaimsHelper;

        public SharepointOnline()
        {
        }

        public string GetURL()
        {
            return site != null ? site.Url : "";
        }

        public SharepointOnline(string siteURL, string username, string password)
        {
            Logon(siteURL, username, password);
        }

        public SharepointOnline(string siteURL, string knownSecurityToken)
        {
            _knownSecurityToken = knownSecurityToken;
            Logon(siteURL, "", "");
        }

        public bool Logon(string siteURL, string username, string password)
        {
            bool mRet = true;
            try
            {
                sharepoint = new ClientContext(siteURL);
                ClaimsHelper = new MsOnlineClaimsHelper(siteURL, username, password);

                // CCB 10.06.2014 for Office 365 login
                if (!String.IsNullOrEmpty(_knownSecurityToken))
                    ClaimsHelper.KnownSecurityToken = _knownSecurityToken;

                sharepoint.ExecutingWebRequest += ClaimsHelper.clientContext_ExecutingWebRequest;

                sharepoint.Load(sharepoint.Web);
                sharepoint.ExecuteQuery();

                site = sharepoint.Web;

                // CCB added for multi-users task update feature - no longer used in current Functional Spec model
                //ClientResult<Microsoft.SharePoint.Client.Utilities.PrincipalInfo> persons = Microsoft.SharePoint.Client.Utilities.Utility.ResolvePrincipal(sharepoint, sharepoint.Web, username, Microsoft.SharePoint.Client.Utilities.PrincipalType.User, Microsoft.SharePoint.Client.Utilities.PrincipalSource.All, null, true);
                //sharepoint.ExecuteQuery();
                //Microsoft.SharePoint.Client.Utilities.PrincipalInfo person = persons.Value;
                //user = person.DisplayName;
            }
            catch
            {
                mRet = false;
            };

            return mRet;
        }

        public bool IsAuth()
        {
            return site != null;
        }

        public void ChangeServer()
        {
            site = null;
        }

        public Stream GetDocument(string path)
        {
            FileInformation fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(sharepoint, path);

            // CCB detect editing rights
            SetEditingRightsForCurrentFile(path);

            return fileInfo.Stream;
        }

        public void UpdateDocument(string path, Stream stream)
        {
            Microsoft.SharePoint.Client.File.SaveBinaryDirect(sharepoint, path, stream, true);
        }

        public void CreateDocument(string path, Stream stream)
        {
            string key = "";
            ListCollection collList = site.Lists;

            sharepoint.Load(collList);
            sharepoint.ExecuteQuery();

            foreach (List oList in collList)
            {
                if (oList.BaseType == BaseType.DocumentLibrary)
                    if (!oList.Hidden)
                        if (oList.ItemCount > 0)
                        {
                            CamlQuery camlQuery = new CamlQuery();
                            camlQuery.ViewXml = "<View><Query><Where><Geq><FieldRef Name='DocIcon'/>" +
                                                "<Value Type='Text'>mpp</Value></Geq></Where>" +
                                                "</Query><RowLimit>100</RowLimit></View>";

                            ListItemCollection collListItem = oList.GetItems(camlQuery);

                            sharepoint.Load(collListItem);

                            sharepoint.ExecuteQuery();

                            foreach (ListItem oListItem in collListItem)
                            {
                                string it = oListItem["FileRef"].ToString();
                                //if (!mRet.ContainsKey(it))
                                key = it;

                                break;
                            }
                            //int i = 0;
                        }
                //break;
            }

            saveFolder = key.Substring(1, key.LastIndexOf("/"));

            if (path.IndexOf("/") != path.LastIndexOf("/"))
            {
                Microsoft.SharePoint.Client.File.SaveBinaryDirect(sharepoint, path, stream, true);
            }
            else
            {
                Microsoft.SharePoint.Client.File.SaveBinaryDirect(sharepoint, key.Substring(0, key.LastIndexOf("/")) + path, stream, true);
            }
        }

        public IDictionary<string, string> GetMppFiles(string parentFolder, string[] approvedFileExtensions)
        {
            SortedDictionary<string, string> mRet = new SortedDictionary<string, string>();

            ListCollection collList = site.Lists;

            sharepoint.Load(collList);
            sharepoint.ExecuteQuery();

            foreach (List oList in collList)
            {
                if (oList.BaseType == BaseType.DocumentLibrary)
                    if (!oList.Hidden)
                        if (oList.ItemCount > 0)
                        {
                            CamlQuery camlQuery = new CamlQuery();
                            camlQuery.ViewXml = "<View><Query><Where><Geq><FieldRef Name='DocIcon'/>" +
                                                "<Value Type='Text'>mpp</Value></Geq></Where>" +
                                                "</Query><RowLimit>100</RowLimit></View>";

                            ListItemCollection collListItem = oList.GetItems(camlQuery);

                            sharepoint.Load(collListItem);

                            sharepoint.ExecuteQuery();

                            foreach (ListItem oListItem in collListItem)
                            {
                                string it = oListItem["FileRef"].ToString();
                                //if (!mRet.ContainsKey(it))
                                mRet.Add(it, it.Substring(1));

                            }
                            //int i = 0;
                        }
            }
            return mRet;
        }

        public static IDictionary<string, string> GetMppFiles(SharepointOnline sharepointonline, string parentFolder, string[] approvedFileExtensions)
        {
            return sharepointonline.GetMppFiles(parentFolder, approvedFileExtensions);
        }

        private void SetEditingRightsForCurrentFile(string path)
        {
            try
            {
                bool found = false;
                var spath = path.Substring(1);
                List oList = site.Lists.GetByTitle(spath.Substring(0, spath.IndexOf("/", StringComparison.Ordinal)));
                if (oList != null)
                {
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = "<View/>";

                    ListItemCollection listItemCollection = oList.GetItems(query);

                    sharepoint.Load(listItemCollection);
                    sharepoint.ExecuteQuery();

                    foreach (ListItem oListItem in listItemCollection)
                    {
                        if (oListItem["FileRef"].ToString() == path)
                        {
                            // Load the permissions
                            sharepoint.Load(oListItem, t => t.EffectiveBasePermissions);
                            sharepoint.ExecuteQuery();

                            canEditCurrentFile = oListItem.EffectiveBasePermissions.Has(PermissionKind.EditListItems);
                            found = true;
                            break;
                        }
                    }
                    if (!found) canEditCurrentFile = true; // the rights couldn't be tested, the access on Task Update should not be restricted
                }
            }
            catch (Exception e)
            {
                // if the method fails, the access on Task Update should not be restricted
                canEditCurrentFile = true;
                Debug.WriteLine("Error in determining user rights on current Sharepoint file", e.Message);
            }
        }
    }
}