var viewAsList = false;
var FoldersBreadcrumbs = [];
var cloudsArray = ["GoogleDrive", "OneDrive", "Dropbox", "Box", "SharePoint", "AmazonS3", "BaseCamp"]; //, "Device"];
var cloudRootsArray = ["root", "me/skydrive/", "/", "0", "", "_", "basecamp_root"]; //, "Device"];
var cloudLayoutArray = [0, 0, 0, 0, 0, 0, 0]; // 0 - grid, 1 - list

$(function initialize() {
    //  initialize the global variables used for dialog windows
    InitializeDialogs();
});

$(document).ready(function () {
    if ($(".gridster").length > 0) {

        $(".CloudIconNotLoggedIn").click(function () {
            var cloud = this.id;
            PageMethods.IsAuthCloud(cloud, function (response) {
                if (response) {
                    //alert("logged in!");
                }
                else {
                    if (cloud.toLowerCase() == "sharepoint") {
                        AuthenticateSharepointDialog("open", function () {
                            //OpenFromCloud(site); //what to do after logon
                        });
                    } else if (cloud.toLowerCase() == "amazons3") {
                        AuthenticateAmazonDialog("open", function () {
                            //OpenFromCloud(site); //what to do after logon
                        });
                    } else {
                        //jQuery(window).unbind("beforeunload");
                        window.location.href = " Dialogs/AuthenticateCloudService.aspx?cloud=" + cloud + "&action=open";
                    }
                }
            });
        });

        for (var i = 0; i < cloudsArray.length; i++) {
            //if (FoldersBreadcrumbs.length < cloudsArray.length) { 
                var breadcumbsForCloud = [];
                FoldersBreadcrumbs.push(breadcumbsForCloud);
            //}
                var cell = $("#gridCell" + cloudsArray[i]);
                if (cell.length > 0 && cell.is(":visible"))
                    ListContents(cloudsArray[i], null);
        }

        ShowDevice();

        if($("div.error")[0]){
            createError($("div.error"));
        }

        if($("div.notice")[0]){
            createHighlight($("div.notice"));
        }
    }

    $(document).click(function (e) {
        CloseContextualMenu(e);
    });
});

function CloseContextualMenu(e) {
    var cancelDefaultAction = false;
    if ($(".DropdownArrowSelected").length > 0) {
        $(".DropdownArrowSelected").hide();
        $(".DropdownArrowSelected").removeClass("DropdownArrowSelected");
    }

    // remove contextual menu if it exists
    if ($("#ContextualMenuContainer").length > 0) {
        $("#ContextualMenuContainer").remove();
        cancelDefaultAction = true;
    }
        
    if (typeof e != "undefined" && e != null)
        e.stopPropagation();

    return cancelDefaultAction;
}

function SetLayoutCookie() {
    var clouds = ["GoogleDrive", "OneDrive", "Dropbox", "Box", "SharePoint", "AmazonS3", "BaseCamp", "Device"];
    var updatedCells = new Array();
    clouds.forEach(function (value) {
        var cell = $("#gridCell" + value.toString());
        if (cell.length > 0) {
            var updatedCell = new Object();
            updatedCell.name = value;
            updatedCell.col = cell.attr('data-col');
            updatedCell.row = cell.attr('data-row');
            updatedCell.sizex = cell.attr('data-sizex');
            updatedCell.sizey = cell.attr('data-sizey');
            updatedCells.push(updatedCell);
        }
    });

    setCookie("layoutData", JSON.stringify(updatedCells), 365);
}

function setCookie(cname, cvalue, exdays) {
    var d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    var expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + "; " + expires;
}

function ShowDevice() {
    var uploader = new qq.FileUploader({
        element: $("#FileUploadContainer")[0],
        action: 'Handlers/FileUploadHandler.ashx', // ?formName=' + new Date().getTime()
        multiple: true,
        sizeLimit: 20971520, // 20 MB
        uploadButtonText: "Upload a file",
        onComplete: function (id, filename, result) {
            $('#progress').hide();
            $("#gridCellDevice").find(".RemoveUploadedFilesButton").show();
            $("#gridCellDevice").find(".CopyTo").show();
            if (result.success) {
                CloseModalDialog();
                
                //  call the actual open file method
                //if (result.success.substring)
                //    alert(result.success);
                //OpenFile();
            }
            else if (result.password) {
                $('#browse').hide();
                $('#passRow').show();
                $('#pass').focus().val('');
            }
            else if (result.sizeError) {
                CloseModalDialog();
                //var sizeLimit = formatSize(projectSize);
                //alert(filename + " is too large, maximum file size is 20MB.");
                showError(filename + " is too large, maximum file size is 20MB.");
            }
        }
    });

    var container = $("#FileUploadContainer");
    container.append("<span class='CopyTo' style='display:none; margin-left:10px; margin-bottom:10px;'>Copy uploads to: <select id='cloudselect' name='cloud' style='width:150px; height:24px;'>" + // $("#gridCellDevice").append
        "<option disabled selected> -- select an option -- </option><option value='GoogleDrive'>Google Drive</option><option value='Box'>Box</option>" +
        "<option value='Dropbox'>Dropbox</option><option value='AmazonS3'>Amazon S3</option><option value='SharePoint'>SharePoint</option>" +
        "<option value='BaseCamp'>BaseCamp</option><option value='OneDrive'>OneDrive</option></span>");
    container.append("<div class='RemoveUploadedFilesButton' style='display:none' onclick='return DiscardUploads();'>Discard Files</div></select></span>"); // $("#gridCellDevice").append
    
    $("#cloudselect").change(function () {
        $("select option:selected").each(function () {
            LocalUploadToCloudDialog($(this).val());
        });
    });
}

function DiscardUploads() {
    PageMethods.DiscardLocalUploads(function () {
        //var container = $("#gridCellDevice").find(".FileUploadContainer")[0];
        //container.empty();
        ShowDevice();
    });
}

function ListContents(value, folderId) {
    if ($("#" + value.toString()).length == 0 || !($("#" + value.toString()).is(":visible"))) { // if the cloud is logged in and the page is Default

        if (value == "Device") {
            ShowDevice();
            return;
        }

        var container = $('#gridCell' + value.toString());
        var menuSpan = container.find('.CloudMenuSpan');
        var menuSpanContainer = menuSpan.find('.CloudMenuSpanContainer');

        // remove existing items
        container.find(".TableItems").remove();  //container.find(".TableItems, .SwitchLayoutLink, .CloudMiniIcon").remove();

        menuSpan.show();

        if (container.find(".ScrollableContainer").length == 0) {
            container.append("<div class='ScrollableContainer' style='height:100%; overflow-y:auto;'></div>");
        }

        var scrollableContainer = container.find(".ScrollableContainer");

        // determine item width
        var width = container.width();
        var itemsPerLine = Math.round(width / 100);
        var itemWidth = Math.floor((width - 10) / itemsPerLine);

        var cloudIndex = cloudsArray.indexOf(value);
        var layout = cloudLayoutArray[cloudIndex];

        folderId = folderId || null;
        if (folderId == null) {
            folderId = cloudRootsArray[cloudIndex]; // initialize with root folder

            // empty the array
            FoldersBreadcrumbs[cloudIndex] = [];

            FoldersBreadcrumbs[cloudIndex].push({
                name: "/",
                id: folderId
            });
        }

        container.attr("currentFolder", folderId);

        var success = false;
        var itemsCount;
        var items;

        $.ajax({
            type: "POST",
            url: "../Default.aspx/ListFilesInFolder",
            data: '{ "cloud" : "' + value + '" , "folderId" :' + JSON.stringify(folderId) + '}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            //dataType: "html",
            async: false,
            success: function(response) {
                items = response.d;
                itemsCount = items.length;
                success = true;
                $("#gridCell" + value + " .Loader").remove(); // $("#gridCellBox .Loader").length
                //alert(itemsCount);
            }
            //error: function (response) { // TODO: add error icon instead of loader

            //}
            //,
            //complete: function (response) {

            //    var list = response.d;
            //    alert(list.length);
            //}
        });

        if (!success) {
            alert("Error in retrieving files for " + value + "!!!");
            return;
        }

        if(folderId != cloudRootsArray[cloudIndex])
        {
            var goBackFolder = (layout == 0) ? { isFolder: true, Name: "..", Id: "..", imageUrl: "Images/folderIcon_64x64.png" }
                                             : { isFolder: true, Name: "..", Id: "..", imageUrl: "Images/folderIcon_32x32.png" }; 
            items.unshift(goBackFolder);
            itemsCount++;
        }

        if (menuSpanContainer.find(".CloudMiniIcon").length == 0) {
            menuSpanContainer.append("<img src='Images/" + value.toString().toLowerCase() + "_mini.png' class='CloudMiniIcon' title='" + value + "'/>");
            menuSpanContainer.append("<a><img class='CloudMenuIcon GridIcon' src='Images/Menu Icons/Grid.png' title='View as Grid'/></a>");
            menuSpanContainer.append("<a><img class='CloudMenuIcon ListIcon' src='Images/Menu Icons/List.png' title='View as List'/></a>");
            menuSpanContainer.append("<a><img class='CloudMenuIcon RefreshFolderMenuIcon' src='Images/Menu Icons/Refresh.png' title='Refresh' /></a>");
            menuSpanContainer.append("<a onclick='return SignOut(&quot;" + value.toString() + "&quot;)'><img class='CloudMenuIcon' src='Images/Menu Icons/Sign out.png' title='Sign Out'/></a>");
            //menuSpan.append("<a><img src='Images/Menu Icons/Refresh.png' /></a>");
            //menuSpan.append("<a><img src='Images/Menu Icons/Preview.png' /></a>");
            //menuSpan.append("<a class='SwitchLayoutLink'>View as List</a>");
            menuSpanContainer.append("<a><img class='CloudMenuIcon AddFolderMenuIcon' src='Images/Menu Icons/Add Folder.png' title='Add Folder'></a>");  // onclick='return AddFolderDialog(&quot;" + value.toString() + "&quot;, &quot;" + folderId.replace(new RegExp("'", "g"), "&apos;") + "&quot;);'
        }

        var miniCloudIcon = menuSpanContainer.find(".CloudMiniIcon");
        miniCloudIcon.unbind("click");
        miniCloudIcon.bind(
            'click', function (e) {
                // empty the array
                FoldersBreadcrumbs[cloudIndex] = [];

                var rootId = cloudRootsArray[cloudIndex];

                FoldersBreadcrumbs[cloudIndex].push({
                    name: "/",
                    id: rootId
                });
                RefreshFolder(value.toString(), rootId);
            }
        );
        
        var gridIcon = menuSpanContainer.find(".GridIcon");
        var listIcon = menuSpanContainer.find(".ListIcon");

        if (layout == 0) {
            gridIcon.addClass("LayoutIconDisabled");
            listIcon.removeClass("LayoutIconDisabled");
            listIcon.unbind("click");
            gridIcon.unbind("click");
            listIcon.bind(
            'click', function (e) {
                cloudLayoutArray[cloudIndex] = 1;
                RefreshFolder(value.toString(), folderId);
            }
            );
        } else {
            listIcon.addClass("LayoutIconDisabled");
            gridIcon.removeClass("LayoutIconDisabled");
            listIcon.unbind("click");
            gridIcon.unbind("click");
            gridIcon.bind(
            'click', function (e) {
                cloudLayoutArray[cloudIndex] = 0;
                RefreshFolder(value.toString(), folderId);
            }
            );
        }

        var addFolder = menuSpanContainer.find(".AddFolderMenuIcon");
        addFolder.unbind("click");
        addFolder.bind(
            'click', function (e) {
                AddFolderDialog(value.toString(), folderId); // folderId.replace(new RegExp("'", "g"))
            }
        );

        var refreshFolder = menuSpanContainer.find(".RefreshFolderMenuIcon");
        refreshFolder.unbind("click");
        refreshFolder.bind(
            'click', function (e) {
                RefreshFolder(value.toString(), folderId);
            }
        );
       
        //menuSpan.append("<a class='SwitchLayoutLink' onclick='return SignOut(&quot;" + value.toString() + "&quot;)'>LogOut</a>");

        //container.append("<img src='Images/" + value.toString().toLowerCase() + "_mini.png' class='CloudMiniIcon'/>");
        //container.append("<img src='Images/Menu Icons/Download.png' class='CloudMiniIcon'/>");
        //container.append("<img src='Images/Menu Icons/Refresh.png' class='CloudMiniIcon'/>");
        //container.append("<img src='Images/Menu Icons/Preview.png' class='CloudMiniIcon'/>");
        //container.append("<a class='SwitchLayoutLink'>View as List</a>");
        //container.append("<a class='SwitchLayoutLink' onclick='return AddFolderDialog(&quot;" + value.toString() + "&quot;, &quot;" + folderId.replace(new RegExp("'", "g"), "&apos;") + "&quot;);'>Add Folder</a>");
        //container.append("<a class='SwitchLayoutLink' onclick='return SignOut(&quot;" + value.toString() + "&quot;)'>LogOut</a>");

        // TODO: optimization - this doesn't need to be retrieved with every listing (?)
        menuSpanContainer.find(".Quota").remove();
        menuSpanContainer.append("<span class='Quota' id='quota" + value + "'></span>");

        $.ajax({
            type: "POST",
            url: "../Default.aspx/GetSpaceQuota",
            data: '{ "cloud" : "' + value + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: true,
            success: function(resp) {
                var quota = resp.d;
                container.find('#quota' + value.toString()).text(quota);
            }
        });


        var table = $("<table id='" + value.toString() + "TableItems' class='TableItems' style='margin:7px 1% 0px; max-width: 98%; position:relative;' ondrop='drop(event)' ondragover='allowDrop(event)'></table>");

        if (layout == 0) {

            // Grid layout
            var row = $("<tr></tr>");
            for (var k = 0; k < itemsCount; k++) {
                var type = items[k].isFolder ? "Folder" : "File";
                var dropdownOffset = itemWidth - 13;
                var id = (items[k].Id != null) ? items[k].Id.replace(new RegExp("'", "g"), "&apos;") : null;
                var cell = "<td class='" + type + "Cell' style='padding:6px 1px 10px; width:" + (itemWidth - 1) + "px; min-width:" + (itemWidth - 1) + "px; height:" + itemWidth + "px;'>" +
                    "<div style='display:table-cell'><div style='position:relative'><div class='DropdownArrow' style='left:" + dropdownOffset + "px;' type='" + type + "' known='" + items[k].IsKnownType + "'></div></div></div>" +
                    "<div id='item" + k + value + "' cloud='" + value + "' style='text-align:center; max-width:" + (itemWidth - 1) + "px;'>";
                if (items[k].isFolder) {
                    cell += "<img draggable='false' fileId='" + id + "' class='FolderIcon' src='" + items[k].imageUrl + "' cloud='" + value + "' type='" + items[k].Type + "' " +
                        "isBucket='" + items[k].isBucket + "' name='" + items[k].Name + "'/></div>"; // bucket='" + items[k].bucketName + "'
                } else {
                    cell += "<img draggable='true' ondragstart='drag(event)' fileId='" + id + "' class='FileIcon' src='" + items[k].imageUrl + "' known='" + items[k].IsKnownType + "' cloud='" + value + "' type='" + items[k].Type + "' " +
                        "isBucket='" + items[k].isBucket + "' name='" + items[k].Name + "'/></div>"; // bucket='" + items[k].bucketName + "'
                }

                cell += "<div style='width:100%; max-width:" + (itemWidth - 1) + "px; height:40px; text-align:center; text-size:12px; overflow-x:hidden; overflow-y:hidden;' title='" + items[k].Name + "'>" +
                    items[k].Name + "</div></td>";

                if ((k % itemsPerLine == 0)) {
                    table.append(row);
                    row = $("<tr></tr>");
                }

                if (k == itemsCount - 1) {
                    row.append(cell);
                    table.append(row);
                    break;
                }

                row.append(cell);
            }

            scrollableContainer.css("overflow-y", "hidden");
            scrollableContainer.append(table);

        } else {

            // List layout

            table.css({ "max-width": "96%", "min-width": "94%" });

            var row = $("<tr></tr>");
            for (var k = 0; k < itemsCount; k++) {
                var type = items[k].isFolder ? "Folder" : "File";
                //var dropdownOffset = itemWidth - 13;
                var id = (items[k].Id != null) ? items[k].Id.replace(new RegExp("'", "g"), "&apos;") : null;
                var cell = "<td class='" + type + "Cell' style='padding:6px 1px 10px; height:32px;'>" +
                    "<div id='item" + k + value + "' cloud='" + value + "' style='width:100%; min-width:100%; display: inline'>";
                if (items[k].isFolder) {
                    cell += "<img draggable='false' fileId='" + id + "' class='FolderIcon' src='" + items[k].imageUrl + "' style='height:32px; width:32px; vertical-align:middle; margin-right:5px;' cloud='" + value + "' type='" + items[k].Type + "' " +
                        "isBucket='" + items[k].isBucket + "' name='" + items[k].Name + "'/>"; // bucket='" + items[k].bucketName + "'
                    cell += "<span fileId='" + id + "'  style='height:32px; text-size:12px; overflow-x:hidden; overflow-y:hidden;' class='FolderIcon' title='" + items[k].Name + "' name='" + items[k].Name + "' cloud='" + value + "' type='" + items[k].Type + "' " + "isBucket='" + items[k].isBucket + "'>" +
                    items[k].Name + "</span></div></td>";
                } else {
                    cell += "<img draggable='true' ondragstart='drag(event)' fileId='" + id + "' class='FileIcon' src='" + items[k].imageUrl + "' known='" + items[k].IsKnownType + "' style='height:32px; width:32px; vertical-align:middle; margin-right:5px;' " +
                        "cloud='" + value + "' type='" + items[k].Type + "' " + "isBucket='" + items[k].isBucket + "' name='" + items[k].Name + "'/>"; // bucket='" + items[k].bucketName + "'
                    cell += "<span fileId='" + id + "' style='height:32px; text-size:12px; overflow-x:hidden; overflow-y:hidden;' class='FileIcon' known='" + items[k].IsKnownType + "' title='" + items[k].Name + "' name='" + items[k].Name + "' cloud='" + value + "' type='" + items[k].Type + "' " + "isBucket='" + items[k].isBucket + "'>" +
                    items[k].Name + "</span></div></td>";
                }
                
                row.append(cell);
                table.append(row);
                row = $("<tr></tr>");
            }

            scrollableContainer.css("overflow-y", "auto");
            scrollableContainer.append(table);
        }
        
        var folderIcons = container.find(".FolderIcon");
        folderIcons.bind(
            'click', function (e) {
                var cancelDefaultAction = CloseContextualMenu(e);
                //if (cancelDefaultAction != true) {
                    var cid = $(this).attr("fileId");
                    if (cid != "..") {
                        FoldersBreadcrumbs[cloudIndex].push({
                            name: $(this).attr("name"),
                            id: cid
                        });
                        ListContents($($(this).parent()).attr("cloud"), cid);
                    } else {
                        FoldersBreadcrumbs[cloudIndex].pop();
                        ListContents($($(this).parent()).attr("cloud"), FoldersBreadcrumbs[cloudIndex][FoldersBreadcrumbs[cloudIndex].length - 1].id);
                    }
                //}
            }
        );

        var fileIcons = container.find(".FileIcon");
        fileIcons.bind('click', function (e) {
            var cancelDefaultAction = CloseContextualMenu(e);
            //if (cancelDefaultAction != true) {
                var fileId = $(this).attr("fileId");
                var cloud = $(this).attr("cloud");

                var presenting;
                var doNotResize = false;
                switch ($(this).attr("type")) {
                    case "video":
                        {
                            presenting = $("<video controls id='presenting' style='text-align:center; width: 500px; height:900px; max-width:100%; max-height:100%;'><source src='Handlers/mp4.ashx?fileId=" + fileId + "&cloud=" + cloud + "' type='video/mp4'></video>"); // autoplay='autoplay'
                            break;
                        }
                    case "audio":
                        {
                            presenting = $("<audio controls id='presenting' style='text-align:center; max-width:100%; max-height:100%; position:absolute; top:50%; left:40%;'><source src='Handlers/mp3.ashx?fileId=" + fileId + "&cloud=" + cloud + "' type='audio/mpeg'></audio>"); // autoplay='autoplay'
                            break;
                        }
                    case "image":
                        {
                            presenting = $("<img id='presenting' style='text-align:center; max-width:100%; max-height:100%; position:absolute; top:50%; left:50%; display:none;' src='Handlers/jpg.ashx?fileId=" + fileId + "&cloud=" + cloud + "'/>");
                            break;
                        }
                    case "pdf":
                        {
                            //presenting = $("<iframe src='../Pdf/web/viewer.html?file=http://mozilla.github.io/pdf.js/web/compressed.tracemonkey-pldi-09.pdf' width='100%' height='864px' />");
                            //$('.blockUI.blockMsg.blockPage').css("cursor", "wait");
                            //$("body").append("<img src='Images/loader.gif' style='position:absolute;'/>");
                            presenting = $("<object width='100%' height='864px' data='Handlers/pdf.ashx?fileId=" + fileId + "&cloud=" + cloud + "' style='cursor:wait!important;' type='application/pdf'>" +
                                    "<embed src='Handlers/pdf.ashx?fileId=" + fileId + "&cloud=" + cloud + "' type='application/pdf' style='cursor:wait!important;>" +
                                        "<noembed>Your browser does not support embedded PDF files. </noembed>" +
                                        //"<img src='Images/loader.gif'/>" +
                                    "</embed>" +
                                "</object>");

                            //var pdfDocument;
                            //PDFJS.getDocument("Handlers/pdf.ashx?fileId=" + fileId + "&cloud=" + cloud).then(function (pdf) {
                            //    pdfDocument = pdf;
                            //    //var url = URL.createObjectURL(blob);
                            //    PDFView.load(pdfDocument, 1.5);
                            //});

                            doNotResize = true;
                            break;
                        }
                    case "text":
                        {
                            presenting = $("<iframe id='presenting' allowtransparency='false' style='text-align:center; width:1706px; max-width:100%; height:904px; max-height:98%; position:absolute; top:50%; left:50%; display:none; background-color:white' src='Handlers/txt.ashx?fileId=" + fileId + "&cloud=" + cloud + "'/>");
                            break;
                        }
                    case "doc":
                        {
                            presenting = $("<object width='100%' height='864px' data='Handlers/doc.ashx?fileId=" + fileId + "&cloud=" + cloud + "' style='cursor:wait!important;' type='application/pdf'>" +
                                    "<embed src='Handlers/doc.ashx?fileId=" + fileId + "&cloud=" + cloud + "' type='application/pdf' style='cursor:wait!important;>" +
                                        "<noembed>Your browser does not support embedded PDF files. </noembed>" +
                                        //"<img src='Images/loader.gif'/>" +
                                    "</embed>" +
                                "</object>");


                            //presenting = $("<iframe id='presenting' allowtransparency='false' style='text-align:center; width:1706px; max-width:100%; height:904px; max-height:98%; position:absolute; top:50%; left:50%; display:none; background-color:white' src='Handlers/doc.ashx?fileId=" + fileId + "&cloud=" + cloud + "'/>");
                            break;
                        }
                    case "docx":
                        {
                            presenting = $("<object width='100%' height='864px' data='Handlers/docx.ashx?fileId=" + fileId + "&cloud=" + cloud + "' style='cursor:wait!important;' type='application/pdf'>" +
                                    "<embed src='Handlers/docx.ashx?fileId=" + fileId + "&cloud=" + cloud + "' type='application/pdf' style='cursor:wait!important;>" +
                                        "<noembed>Your browser does not support embedded PDF files. </noembed>" +
                                        //"<img src='Images/loader.gif'/>" +
                                    "</embed>" +
                                "</object>");


                            //presenting = $("<iframe id='presenting' allowtransparency='false' style='text-align:center; width:1706px; max-width:100%; height:904px; max-height:98%; position:absolute; top:50%; left:50%; display:none; background-color:white' src='Handlers/docx.ashx?fileId=" + fileId + "&cloud=" + cloud + "'/>");
                            break;
                        }
                    case "unknown":
                        {
                            break;
                        }
                }

                //var presenting = $("<img id='presenting' style='text-align:center; max-width:100%; max-height:100%; position:absolute; top:50%; left:50%; display:none;' src='Handlers/jpg.ashx?fileId=" + fileId + "&cloud=" + cloud + "'/>");

                if (typeof presenting != "undefined") {
                    $.blockUI({
                        css: {
                            border: 'none',
                            backgroundColor: '#000',
                            '-webkit-border-radius': '10px',
                            '-moz-border-radius': '10px',
                            color: '#fff',
                            'min-width': '90%',
                            'min-height': '90%',
                            'display': 'table',
                            cursor: 'wait'
                        },
                        overlayCSS: {
                            backgroundColor: '#000',
                            cursor: 'pointer'
                        }
                    });

                    if ($(this).attr("type") == "video" || $(this).attr("type") == "audio")
                        $('.blockUI.blockMsg.blockPage').css('cursor', 'default');

                    $('.blockOverlay').click($.unblockUI);

                    $('.blockUI.blockMsg.blockPage').append(presenting);

                    $('.blockUI.blockMsg.blockPage').css("overflow", "hidden");

                    if (!doNotResize) {
                        presenting.load(function() {
                            var h = presenting.height();
                            var w = presenting.width();
                            presenting.css('margin-top', +h / -2 + "px");
                            presenting.css('margin-left', +w / -2 + "px");
                            presenting.css('cursor', 'pointer');
                            presenting.show();
                            $('.blockUI.blockMsg.blockPage').css('cursor', 'default');
                        });
                    } else {
                        presenting.show();
                        $('.blockUI.blockMsg.blockPage').css('cursor', 'default');
                    }
                }
            //}
        });

        // Cells (folders, files) events - in grid layout only
        var folderAndFileCells = container.find(".FolderCell, .FileCell");
        folderAndFileCells.bind({
            mouseenter: function () {
                if ($(".DropdownArrowSelected").length > 0 && jQuery(this).find(".DropdownArrowSelected").length > 0) {
                    return;
                }
                jQuery(this).find(".DropdownArrow").show();
            },
            mouseleave: function () {
                if ($(".DropdownArrowSelected").length > 0 && jQuery(this).find(".DropdownArrowSelected").length > 0) {
                    return;
                }
                jQuery(this).find(".DropdownArrow").hide();
            }
        });

        // Arrow events (in grid layout only?)
        var arrows = container.find(".DropdownArrow");
        arrows.bind({
            click: function (e) {
                if (jQuery(this).hasClass("DropdownArrowSelected")) {
                    CloseContextualMenu(e);
                } else {
                    ShowContextualMenu(jQuery(this));
                    jQuery(this).addClass("DropdownArrowSelected");
                }
                
                e.stopPropagation();
            }
        });
    }
}

function allowDrop(ev) {
    ev.preventDefault();
}

function drag(ev) {
    //alert(ev.target.attributes["fileId"].nodeValue);
    ev.dataTransfer.setData("fileId", ev.target.attributes["fileId"].nodeValue);
    ev.dataTransfer.setData("fileName", ev.target.attributes["name"].nodeValue);
    ev.dataTransfer.setData("cloud", ev.target.attributes["cloud"].nodeValue);
}

function drop(ev) {
    ev.preventDefault();
    var fileId = ev.dataTransfer.getData("fileId");
    var cloud = ev.dataTransfer.getData("cloud");
    var name = ev.dataTransfer.getData("fileName");
    var destinationCloud = ev.currentTarget.attributes["id"].nodeValue.replace("TableItems", "");
    if (cloud != destinationCloud) {
        var currentFolder = $("#gridCell" + destinationCloud).attr("currentfolder");
        //alert(currentFolder);
        $('body').toggleClass('waiting');
        PageMethods.CopyFileToAnotherCloud(cloud, fileId, name, destinationCloud, currentFolder, function (response) { 
            if (response.Error) {
                $('body').toggleClass('waiting');
                showError(response.ErrorMessage);
            } else {
                $('body').toggleClass('waiting');
                RefreshFolder(destinationCloud, currentFolder);
            }
        });
    }
}

function ShowContextualMenu(callingItem) {
    // close any existing contextual menu
    CloseContextualMenu(null);

    var type = callingItem.attr("type");
    var leftOffset = callingItem.offset().left;
    var topOffset = callingItem.offset().top;
    var $contextualMenu = $("<div id='ContextualMenuContainer' style='position:absolute; left:" + (leftOffset + 1) + "px; top:" + (topOffset + 10) + "px; z-index:999'/>");
    var $content =  $('<ul id="ContextualMenu" aria-expanded="true" role="menu"></ul>');
    var options;

    // construct contextual menu depending on item type
    if (type.toLowerCase() == "file") {
        options = (callingItem.attr("known") == "true") ? "<li id='menuOptionPreviewFile'>Preview</li>" : "<li class='ui-state-disabled'>Preview</li>";
        options += "<li id='menuOptionDownloadFile'>Download</li>";
        options += "<li id='menuOptionShare'>Share</li>";
        options += "<li id='menuOptionMoveCopyFile'>Move or Copy</li>";
        options += "<li id='menuOptionDeleteFile'>Delete</li>"; 
    } else {
        var cell = callingItem.parent().parent().parent();
        var icon = cell.find("img");
        var name = icon.attr("name");

        options = "<li id='menuOptionOpenFolder'>Open</li>";
        //options += (name != "..") ? "<li id='menuOptionDownloadFolder'>Download</li>" : "<li class='ui-state-disabled'>Download</li>";
        options += (name != "..") ? "<li id='menuOptionRemoveFolder'>Remove Folder</li>" : "<li class='ui-state-disabled'>Remove Folder</li>";
    }

    $content.append(options);
    $contextualMenu.append($content);
    $("body").append($contextualMenu);

    // bind events
    $("#menuOptionPreviewFile").bind({
        click: function () {
            var cell = callingItem.parent().parent().parent();
            var icon = cell.find("img");
            icon.trigger("click");
        }
    });

    $("#menuOptionDownloadFile").bind({
        click: function () {
            FileDownload(callingItem);
        }
    });

    $("#menuOptionShare").bind({
        click: function () {
            FileShare(callingItem);
        }
    });

    $("#menuOptionMoveCopyFile").bind({
        click: function () {
            MoveOrCopyDialog(callingItem);
        }
    });

    $("#menuOptionDeleteFile").bind({
        click: function () {
            FileDelete(callingItem);
        }
    });

    // folder events
    $("#menuOptionOpenFolder").bind({
        click: function () {
            FolderOpen(callingItem);
        }
    });

    $("#menuOptionRemoveFolder").bind({
        click: function () {
            FolderDelete(callingItem);
        }
    });

    $("#ContextualMenu").menu();
}

function FileDownload(callingItem) {
    var cell = callingItem.parent().parent().parent();
    var icon = cell.find("img");
    var fileId = icon.attr("fileid");
    var cloud = icon.attr("cloud");
    window.location = "Handlers/FileDownloadHandler.ashx?cloud=" + cloud + "&fileId=" +  fileId;
}

function FileDelete(callingItem) {
    var cell = callingItem.parent().parent().parent();
    var icon = cell.find("img");
    var fileId = icon.attr("fileid");
    var cloud = icon.attr("cloud");
    var r = confirm("You are about to delete this file. Do you want to continue?"); // TODO: this can be skipped once undo is implemented
    if (r == true) {
        PageMethods.DeleteFile(cloud, fileId, function () {
            var cloudContainer = $("#gridCell" + cloud);
            var currentFolder = cloudContainer.attr("currentfolder");
            ListContents(cloud, currentFolder);
        });
    }
}
                                                                                                        
function FileShare(callingItem){
    var cell = callingItem.parent().parent().parent();
    var icon = cell.find("img");
    var fileId = icon.attr("fileid");
    var cloud = icon.attr("cloud");
    PageMethods.ShareFileLink(cloud, fileId, function (response) {
        alert(response);
    });
}

function FolderOpen(callingItem) {
    var cell = callingItem.parent().parent().parent();
    var icon = cell.find("img");
    var folderId = icon.attr("fileid");
    var cloud = icon.attr("cloud");
    var cloudIndex = cloudsArray.indexOf(cloud);

    CloseContextualMenu(null);

    if (folderId != "..") {
        var name = icon.attr("name");
        FoldersBreadcrumbs[cloudIndex].push({
            name: name,
            id: folderId
        });
        ListContents(cloud, folderId);
    } else {
        FoldersBreadcrumbs[cloudIndex].pop();
        ListContents(cloud, FoldersBreadcrumbs[cloudIndex][FoldersBreadcrumbs[cloudIndex].length - 1].id);
    }
}

function FolderDelete(callingItem) {
    var cell = callingItem.parent().parent().parent();
    var icon = cell.find("img");
    var fileId = icon.attr("fileid");
    var cloud = icon.attr("cloud");
    var r = confirm("Removing a folder will recursively delete all of its contents. De you want to continue?"); // TODO: this can be skipped once undo is implemented
    if (r == true) {
        PageMethods.DeleteFolder(cloud, fileId, function () {
            var cloudContainer = $("#gridCell" + cloud);
            var currentFolder = cloudContainer.attr("currentfolder");
            ListContents(cloud, currentFolder);
        });
    }
}

function SignOut(cloudName) {
    PageMethods.SignOut(cloudName, function (logOutUrl) {
        var cloud = cloudName.toLowerCase();
        if (cloud == "basecamp" || cloud == "sharepoint" || cloud == "onedrive") {
            callLogOutIframe(logOutUrl, function () {
                $('iframe#logOutIframe').remove();
                $('#' + cloudName).show();
                var container = $('#gridCell' + cloudName.toString());
                container.find(".TableItems").remove();  //container.find(".TableItems, .SwitchLayoutLink, .CloudMiniIcon, .Quota").remove(); // remove existing items
                container.find(".CloudMenuSpan").hide();
            }); 
        }
        else {
            $('#' + cloudName).show();
            var container = $('#gridCell' + cloudName.toString());
            container.find(".TableItems").remove();  //container.find(".TableItems, .SwitchLayoutLink, .CloudMiniIcon, .Quota").remove(); // remove existing items
            container.find(".CloudMenuSpan").hide();
        }
    });
}

function callLogOutIframe(url, callback) {
    if (url.lastIndexOf("https://login.live.com", 0) === 0) {
        url = decodeURIComponent(url) + "&redirect_uri=" + window.location;
    }
    $(document.body).append('<iframe id="logOutIframe" style="display:none"></iframe>');
    $('iframe#logOutIframe').attr('src', url);

    $('iframe#logOutIframe').load(function () {
        callback(this);
    });
}

function RefreshFolder(cloud, currentFolder) {
    ListContents(cloud, currentFolder);
}

function createHighlight(obj) {
    obj.addClass('ui-state-highlight ui-corner-all');
    obj.html('<p><span class="ui-icon ui-icon-alert" style="float: left; margin-right:.3em;"></span>' + obj.html() + '</p>');
}

function createError(obj) {
    obj.addClass('ui-state-error ui-corner-all');
    obj.html('<p><span class="ui-icon ui-icon-alert" style="float: left; margin-right:.3em;"></span>' + obj.html() + '</p>');
}

function showError(errorMessage) {
    var errorDiv = $($("div.error")[0]);
    errorDiv.find("#errorMessage").html("<b>ERROR:</b> " + errorMessage);
    //errorDiv.show("highlight", { color: 'crimson' }, 100); // .slideDown("fast");
    errorDiv.slideDown(500);
    setTimeout(function () { errorDiv.slideUp(500); }, 4000);
}