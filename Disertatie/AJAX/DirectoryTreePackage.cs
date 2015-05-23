using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Web;
using CloudProject;

namespace Disertatie.AJAX
{
    public class DirectoryTreePackage : Package
    {
        public string TreeData;
        private StringBuilder _sb;
        private CloudStorageConsumer _consumer;

        public DirectoryTreePackage(CloudStorageConsumer csc)
        {
            _sb = new StringBuilder();
            _consumer = csc;

            CreateDirectoryTreeNode();
        }

        /// <summary>
        /// returns the root node of the tree of tasks with all its subnodes attached
        /// </summary>
        /// <returns></returns>
        public void CreateDirectoryTreeNode()
        {
            string rootDirectory = _consumer.getRootFolderId();

            TreeNode root = null;
            int currentOutline = 0;
            TreeNode currentNode = new TreeNode(rootDirectory) { Tag = rootDirectory };
            TreeNode previousNode = null;

            var list = _consumer.CreateOutlineDirectoryList();

            for (int i = 0; i < list.Count; i++)
            {
                var folder = list[i];

                currentNode = new TreeNode(folder.Name) { Tag = folder.Id };

                if (i == 0)
                {
                    root = currentNode;
                    currentOutline = folder.OutlineLevel;
                    previousNode = root;
                }
                else
                {
                    if (folder.OutlineLevel > currentOutline)
                    {
                        previousNode.Nodes.Add(currentNode);
                    }
                    else if (folder.OutlineLevel == currentOutline)
                    {
                        previousNode.Parent.Nodes.Add(currentNode);
                    }
                    else if (folder.OutlineLevel < currentOutline)
                    {
                        var aux = currentOutline;
                        TreeNode correctParent = previousNode; 
                        while (aux >= folder.OutlineLevel)
                        {
                            aux--;
                            correctParent = correctParent.Parent;
                        }

                        correctParent.Nodes.Add(currentNode); //previousNode.Parent.Parent.Nodes.Add(currentNode);
                    }

                    previousNode = currentNode;
                    currentOutline = folder.OutlineLevel;
                }
            }
            TreeData = CreateTreeData(root);
        }

        private string CreateTreeData(TreeNode root)
        {
            _sb.Append("[");
            GenerateJson(root);
            _sb.Append("]");

            return _sb.ToString();
        }

        private void GenerateJson(TreeNode treeNode)
        {
            if (treeNode.Parent != null && treeNode.Parent.Nodes[0].Tag != treeNode.Tag)
            {
                _sb.Append(",");
            }

            // Create the JSON for the node
            _sb.Append("{ \"title\": ");
            _sb.Append("\"" + HttpUtility.JavaScriptStringEncode(treeNode.Text) + "\"");
            _sb.Append(",");
            _sb.Append("\"key\": ");
            _sb.Append("\"" + treeNode.Tag + "\"");
            _sb.Append(",");
            _sb.Append("\"select\": ");
            _sb.Append(treeNode.Checked ? "true" : "false");

            _sb.Append(",");
            _sb.Append("\"expanded\": ");
            _sb.Append("true");

            _sb.Append(",");
            _sb.Append("\"isFolder\": ");
            _sb.Append("true");

            if (treeNode.Nodes.Count > 0)
            {
                _sb.Append(",");
                _sb.Append("\"children\": ");
                _sb.Append("[");
            }

            // Generate JSON recursively.
            foreach (TreeNode tn in treeNode.Nodes)
            {
                GenerateJson(tn);
            }
            if (treeNode.Nodes.Count > 0)
                _sb.Append("]");
            _sb.Append("}");
        }
    }
}