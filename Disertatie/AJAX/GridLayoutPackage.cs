using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Disertatie.AJAX
{
    public class GridLayoutPackage : Package
    {
        public List<GridCellLayoutData> GridCells;

        public GridLayoutPackage()
        {
            HttpCookie layoutCookie = HttpContext.Current.Request.Cookies["layoutData"];
            if (layoutCookie != null)
            {
                // get existing layout
                GridCells = new List<GridCellLayoutData>();

                var jArray = JArray.Parse(layoutCookie.Value);
                foreach (JObject cell in jArray)
                {
                    var gridCellLayoutData = new GridCellLayoutData(cell["name"].ToString(), cell["sizex"].ToString(), cell["sizey"].ToString(), cell["col"].ToString(), cell["row"].ToString());
                    GridCells.Add(gridCellLayoutData);
                }
            }
            else
            {
                // create default layout
                GridCells = new List<GridCellLayoutData>();

                for (int i = 0; i < 6; i++)
                {
                    int row = (i < 3) ? 1 : 3;
                    int col = (i * 2) % 6 + 1;
                    var gridCellLayoutData = new GridCellLayoutData(((CloudStorages)i).ToString(), "2", "2", col.ToString(), row.ToString());
                    GridCells.Add(gridCellLayoutData);
                }

                string cookie = JsonConvert.SerializeObject(GridCells);
                HttpContext.Current.Response.Cookies["layoutData"].Value = cookie;
                HttpContext.Current.Response.Cookies["layoutData"].Expires = DateTime.Now.AddYears(1);
            }
        }
    }

    public class GridCellLayoutData
    {
        public string name;
        public string sizex;
        public string sizey;
        public string col;
        public string row;

        public GridCellLayoutData(string _name, string _sizex, string _sizey, string _col, string _row)
        {
            this.name = _name;
            this.sizex = _sizex;
            this.sizey = _sizey;
            this.col = _col;
            this.row = _row;
        }
    }

    enum CloudStorages
    {
        GoogleDrive,
        OneDrive,
        SharePoint,
        Dropbox,
        Box,
        Device
    }
}