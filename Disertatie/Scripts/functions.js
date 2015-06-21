var viewAsList = false;
var FoldersBreadcrumbs = [];
var cloudsArray = ["GoogleDrive", "OneDrive", "Dropbox", "Box", "SharePoint", "AmazonS3"]; //, "Device"];
var cloudRootsArray = ["root", "me/skydrive/", "/", "0", "", "_"]; //, "Device"];

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
                    alert("logged in!");
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
            //if (FoldersBreadcrumbs.length < cloudsArray.length) { // initialize breadcrumbs array, only on first page access
                var breadcumbsForCloud = [];
                FoldersBreadcrumbs.push(breadcumbsForCloud);
            //}
            ListContents(cloudsArray[i], null);
        }

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
    var clouds = ["GoogleDrive", "OneDrive", "Dropbox", "Box", "SharePoint", "AmazonS3", "Device"];
    var updatedCells = new Array();
    clouds.forEach(function (value) {
        var cell = $("#gridCell" + value.toString());
        if (cell.length > 0) { // TODO: maybe change
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

function ListContents(value, folderId) {
    if ($("#" + value.toString()).length == 0 || !($("#" + value.toString()).is(":visible"))) { // if the cloud is logged in and the page is Default

        var container = $('#gridCell' + value.toString());

        // remove existing items
        container.find(".TableItems, .SwitchLayoutLink, .CloudMiniIcon").remove();

        // determine item width
        var width = container.width();
        var itemsPerLine = Math.round(width / 100);
        var itemWidth = Math.floor((width - 10) / itemsPerLine);

        var cloudIndex = cloudsArray.indexOf(value);
        //alert(cloudIndex);

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
            var goBackFolder = { isFolder: true, Name: "..", Id: "..", imageUrl: "Images/folderIcon_64x64.png" }; // TODO: support for list mode
            items.unshift(goBackFolder);
            itemsCount++;
        }

        container.append("<img src='Images/" + value.toString().toLowerCase() + "_mini.png' class='CloudMiniIcon'/>");
        container.append("<a class='SwitchLayoutLink'>View as List</a>");
        container.append("<a class='SwitchLayoutLink' onclick='return AddFolderDialog(&quot;" + value.toString() + "&quot;, &quot;" + folderId.replace(new RegExp("'", "g"), "&apos;") + "&quot;);'>Add Folder</a>");
        container.append("<a class='SwitchLayoutLink' onclick='return SignOut(&quot;" + value.toString() + "&quot;)'>LogOut</a>");
        //container.append("<img src='Images/gridIcon.png'/>");
        //container.append("<img src='Images/listIcon.png'/ style='margin-left:5px;'>");

        // TODO: optimization - this doesn't need to be retrieved with every listing (?)
        container.append("<span class='Quota' id='quota" + value + "'></span>"); // TODO: add class for styles

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


        var table = $("<table id='" + value.toString() + "TableItems' class='TableItems' style='margin:7px 1% 0px; max-width: 98%; position:relative;'></table>");

        var row = $("<tr></tr>");
        for (var k = 0; k < itemsCount; k++) {
            var type = items[k].isFolder ? "Folder" : "File";
            var dropdownOffset = itemWidth - 13;
            var id = (items[k].Id != null) ? items[k].Id.replace(new RegExp("'", "g"), "&apos;") : null;
            var cell = "<td class='" + type + "Cell' style='padding:6px 1px 10px; width:" + (itemWidth - 1) + "px; min-width:" + (itemWidth - 1) + "px; height:" + itemWidth + "px;'>" +
                "<div style='display:table-cell'><div style='position:relative'><div class='DropdownArrow' style='left:" + dropdownOffset + "px;' type='" + type + "' known='" + items[k].IsKnownType + "'></div></div></div>" +
                "<div id='item" + k + value + "' cloud='" + value + "' style='text-align:center; max-width:" + (itemWidth - 1) + "px;'>";
            if (items[k].isFolder) {
                cell += "<img fileId='" + id + "' class='FolderIcon' src='" + items[k].imageUrl + "' cloud='" + value + "' type='" + items[k].Type + "' " +
                                                  "isBucket='" + items[k].isBucket + "' name='" + items[k].Name +"'/></div>"; // bucket='" + items[k].bucketName + "'
            } else {
                cell += "<img fileId='" + id + "' class='FileIcon' src='" + items[k].imageUrl + "' known='" + items[k].IsKnownType + "' cloud='" + value + "' type='" + items[k].Type + "' " +
                                                  "isBucket='" + items[k].isBucket + "'/></div>"; // bucket='" + items[k].bucketName + "'
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
        container.append(table);

        $(".FolderIcon").bind(
            'click', function (e) {
                var cancelDefaultAction = CloseContextualMenu(e);
                if (cancelDefaultAction != true) {
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
                }
            }
        );

        $(".FileIcon").bind('click', function (e) {
            var cancelDefaultAction = CloseContextualMenu(e);
            //if (cancelDefaultAction != true) {
                var fileId = $(this).attr("fileId");
                var cloud = $(this).attr("cloud");

                var presenting;
                var doNotResize = false;
                switch ($(this).attr("type")) {
                    case "video":
                        {
                            presenting = $("<video controls id='presenting' style='text-align:center; max-width:100%; max-height:100%; position:absolute; top:50%; left:50%;'><source src='Handlers/mp4.ashx?fileId=" + fileId + "&cloud=" + cloud + "' type='video/mp4'></video>"); // autoplay='autoplay'
                            break;
                        }
                    case "audio":
                        {
                            presenting = $("<audio controls id='presenting' style='text-align:center; max-width:100%; max-height:100%; position:absolute; top:50%; left:50%;'><source src='Handlers/mp3.ashx?fileId=" + fileId + "&cloud=" + cloud + "' type='audio/mpeg'></audio>"); // autoplay='autoplay'
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
        $(".FolderCell, .FileCell").bind({
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
        $(".DropdownArrow").bind({
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

        options = "<li id='menuOptionOpenFolder'>Open</li>";u
        options += (name != "..") ? "<li id='menuOptionDownloadFolder'>Download</li>" : "<li class='ui-state-disabled'>Download</li>";
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
    var r = confirm("You are about to delete this file. De you want to continue?"); // TODO: this can be skipped once undo is implemented
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
                container.find(".TableItems, .SwitchLayoutLink, .CloudMiniIcon, .Quota").remove(); // remove existing items
            }); 
        }
        else {
            $('#' + cloudName).show();
            var container = $('#gridCell' + cloudName.toString());
            container.find(".TableItems, .SwitchLayoutLink, .CloudMiniIcon, .Quota").remove(); // remove existing items
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


function ListFiles(container, cloudService, callback, saveMode, filename) {
    PageMethods.GetFilePickerLabels(saveMode, function (response) {
        
        if (saveMode == undefined)
            saveMode = false;

        //if (window.location.href.toLowerCase().indexOf("pviews.aspx") != -1) {
        //    CloseModalDialog();
        //    var dialogWidth = dial_width - 100;
        //    var dialogHeight = dial_height - 80;
        //} else {
            var objectWidth = container.width - 10; //820;
            var objectHeight = container.height - 20;  //400;

            //var dialogPaddingWidth = 40;
            //var dialogPaddingHeight = 30;

            //var dialogWidth = objectWidth + dialogPaddingWidth;
            //var dialogHeight = objectHeight + dialogPaddingHeight;
        //}

        //var dialogTitle = ((saveMode) ? response.SaveAsLabel : response.ChooseDocumentLabel);

        //CreateModalDialog(dialogHeight, dialogWidth, cloudService + " - " + dialogTitle);
        //$ModalDialogContent.css('height', '100%');
        //$ModalDialogContent.css('width', '100%');
        //SetModalDialogOption("buttons", [
        //    {
        //        text: response.OptionsButtonLabel,
        //        click: function () {
        //            FilePublishDialog(cloudService, callback, saveMode, extensions, response.OptionsButtonLabel, $(".ui-dialog-buttonpane #fileNameInput").val());
        //        }
        //    },
        //    {
        //        text: ((saveMode) ? response.SaveButtonLabel : response.OpenButtonLabel),
        //        click: function () {
        //            if ($("#ModalDialogContent .selectedFile").length > 0) {
        //                var fileId = $("#ModalDialogContent .selectedFile").attr('fileId');
        //                var fileName = (selectedLayoutIcon == "listIcon") ? $("#ModalDialogContent .selectedFile").text() : $("#ModalDialogContent .selectedFile").next().text();
        //                CloseModalDialog();
        //                callback(fileId, fileName);
        //            } else {
        //                if (saveMode) {
        //                    // save to current folder 
        //                    var name = $(".ui-dialog-buttonpane #fileNameInput").val();
        //                    if (name == "") {
        //                        alert(response.MissingFileName);
        //                    } else {
        //                        var ext = $(".ui-dialog-buttonpane #extensionDropdown option:selected").text();
        //                        var currentFolder;
        //                        if (FoldersBreadcrumbs.length > 0) currentFolder = FoldersBreadcrumbs[FoldersBreadcrumbs.length - 1];

        //                        var fullName;
        //                        if (name.lastIndexOf(ext) == name.length - ext.length) { // if the filename ends with the selected extension
        //                            fullName = name;
        //                            name = name.substring(0, name.lastIndexOf(ext));
        //                        } else {
        //                            fullName = name + ext;
        //                        }

        //                        // search for existing items with the same name
        //                        var existingItem;
        //                        if (selectedLayoutIcon == "listIcon") {
        //                            $("#ModalDialog .selectableFile").each(function () {
        //                                if ($(this).text() == fullName) {
        //                                    existingItem = this;
        //                                    return false;
        //                                }
        //                            });
        //                        } else {
        //                            $("#ModalDialog .subText").each(function () {
        //                                if ($(this).text() == fullName) {
        //                                    existingItem = this;
        //                                    return false;
        //                                }
        //                            });
        //                        }

        //                        if (existingItem != undefined) {
        //                            var r = confirm(fullName + " " + response.FileAlreadyExists);
        //                            if (r == false) {
        //                                return;
        //                            }
        //                        }

        //                        var fileType = ext.replace(".", "");
        //                        ShowStandbyDialog("(" + cloudService + ") " + SavingText, false);
        //                        PageMethods.SaveCloudNewDocument(cloudService, name, fileType, currentFolder.id, function (resp) {
        //                            if (resp.SessionExpired) {
        //                                ShowError(sessionExpiredErrorMessage);
        //                                return;
        //                            }
        //                            if (resp.Error) {
        //                                ShowError(resp.ErrorMessage);
        //                                return;
        //                            }
        //                            MppFileName = resp.mppFileName;
        //                            document.title = resp.pageTitle;
        //                            var pt = document.getElementById('ProjectTitle');
        //                            if (pt != null) pt.innerHTML = resp.pageHeader;
        //                            ShowStandbyDialog("(" + cloudService + ") " + resp.SuccessMessage, true);
        //                            $ModalDialogContent.click(function () { CloseModalDialog(); });
        //                        });
        //                    }
        //                }
        //            }
        //        }
        //    },
        //    {
        //        text: cancelLabel,
        //        click: function () { $(this).dialog("close"); }
        //    }]);

        var div = $("<table style='display:table;table-layout:fixed; width:100%; height:100%'/>");
        //var breadcrumbsDiv = $("<td style='width: 100%; height:40px; border-bottom: 1px solid #A6C9E2; font-size: large; background: url(Images/FilePicker/" + cloudService.toLowerCase() + ".png) no-repeat left center; text-align: left; padding-left:40px'>" + cloudService + "<div style='float:right; height:70%; display:inline'>" +
        //                            "<img id='listIcon' class='layoutStyle' src='Images/FilePicker/listIcon.png' title='List view layout'>&nbsp;<img id='gridIcon' style='background-color:#ADD8E6' class='layoutStyle' src='Images/FilePicker/gridIcon.png' title='Grid view layout'>&nbsp;&nbsp;&nbsp;</div></td>");

        var content;
        //if (window.location.href.toLowerCase().indexOf("pviews.aspx") != -1)
        //    content = $("<div style='margin: 0 auto; width:96%; height:100%; overflow-y:auto; overflow-x:hidden; border: 1px solid #cccccc; background-color:#fafafa; text-align: left'>");
        //else
            content = $("<div style='margin: 0 auto; width:90%; height:80%; overflow-y:auto; overflow-x:hidden;'/>");

        div.append($("<tr>").append(breadcrumbsDiv));
        div.append($("<tr>").append($("<td style='height:" + (dialogHeight - 48) + "px; white-space:nowrap'>").append(content)));
        //$ModalDialogContent.html(div);
        content.append(div);

        //ShowModalDialog(dialogHeight, dialogWidth);
        //$("#ModalDialog").css("overflow-x", "hidden");
        //$("#ModalDialog").css("-ms-overflow-y", "hidden"); // attempt to fix IE11 scrolling on outer container instead (still happening)

        var availablePx = dialogWidth - $(".ui-dialog-buttonset").width() - 125;
        var fileNameTextBox = "<input id='fileNameInput' type='text' style='width:" + availablePx + "px; height:16px; position: relative; top: 12px; left: 5px; border-color: lightgray;' onkeyup='filenameChange()'/>";
        var extensionDropdown = "<select id='extensionDropdown' style='width:65px; position:relative; top:12px; left:15px;' >";

        // build extensions dropdown
        if (extensions != null) {
            $.each(extensions, function (index, value) {
                if (index == 0) {
                    extensionDropdown += "<option value='" + value.toLowerCase() + "' selected>" + value.toLowerCase() + "</option>";
                } else {
                    extensionDropdown += "<option value='" + value.toLowerCase() + "'>" + value.toLowerCase() + "</option>";
                }
            });
        } else {
            extensionDropdown += "<option value='.mpp' selected>.mpp</option>";
        }
        extensionDropdown += "</select>";

        // hide options button when not in save mode
        if (!saveMode) {
            $("'.ui-button:contains(" + response.OptionsButtonLabel + ")'").hide();
        } else {
            $(".ui-dialog-buttonpane").append(fileNameTextBox);
            if (filename != undefined && filename != '') $(".ui-dialog-buttonpane #fileNameInput").val(filename);
            $(".ui-dialog-buttonpane").append(extensionDropdown);
        }

        // set default layout
        selectedLayoutIcon = "gridIcon";

        FoldersBreadcrumbs = [{
            name: cloudService,
            id: null
        }];

        var list = function (folderId) {
            content.empty();
            PageMethods.ListFilesInFolder(cloudService, folderId, function (files) {
                var breadCrumbsText = "";
                for (var folder = 0; folder < FoldersBreadcrumbs.length; folder++)
                    breadCrumbsText += FoldersBreadcrumbs[folder].name + "\\";
                breadcrumbsDiv.find("td").text(breadCrumbsText);

                var file;
                /********************************************List Layout design****************************************/
                if (selectedLayoutIcon == "listIcon") {
                    var ol = $("<ol id='FileSelectList' style='list-style-type: none; margin: 0; padding: 0; padding-top:11px; width: 100%; font-family:Arial; font-size: large; text-align: left'/>");

                    content.append(ol);

                    if (folderId != null) {
                        ol.append("<li class='selectableFolder'>..</li>");
                    }

                    for (var i = 0; i < files.length; i++) {
                        file = files[i];
                        file.Id = file.Id.replace(new RegExp("'", "g"), "&apos;");
                        ol.append("<li class='" + (file.isFolder ? "selectableFolder" : "selectableFile") + "' fileId='" + file.Id + "'>" + file.Name + "</li>"); // (file.FullPath ? file.FullPath : file.Name)
                    }
                } /********************************************Grid Layout design****************************************/
                else {
                    var grid = $("<table id='FileSelectGrid' style='margin: 0; padding: 0; padding-top:11px; width: 100%; font-family:Arial; font-size:12px;'/>"); // font-size:small
                    content.append(grid);
                    var count = 0;
                    var width = dialogWidth - 100;

                    if (folderId != null) {
                        var row = $("<tr id='row" + Math.floor(count / 5) + "' style='max-width:" + width + "px'><td style='height:130px;'><div style='height:70%; width:100%; max-width:" + (width / 5) + "px; min-width:" + (width / 5) + "px;' class='selectableFolderGrid'></div>" +
                                                                                                                                          "<div style='height:30%; width:100%; text-align:center; font-weight:bold; max-width:" + (width / 5) + "px; min-width:" + (width / 5) + "px;' class='subText'>..</div></td></tr>");
                        grid.append(row);

                        count++;
                    }

                    for (i = 0; i < files.length; i++) {
                        file = files[i];
                        file.Id = file.Id.replace(new RegExp("'", "g"), "&apos;");
                        var cell = $("<td style='white-space:nowrap; height:130px;'><div style='height:70%; width:100%; max-width:" + (width / 5) + "px; min-width:" + (width / 5) + "px;' class='" + (file.isFolder ? "selectableFolderGrid" : "selectableFileGrid") + "' fileId='" + file.Id + "'></div>" +
                                                                                   "<div style='height:30%; width:100%; text-align:center; max-width:" + (width / 5) + "px; min-width:" + (width / 5) + "px;' class='subText'>" + file.Name + "</div></td>"); // (file.FullPath ? file.FullPath : file.Name)
                        // create new row for every fifth file
                        if (count % 5 == 0) {
                            row = $("<tr id='row" + Math.floor(count / 5) + "' style='max-width:" + width + "px'></tr>");
                            row.append(cell);
                            grid.append(row);
                        } else {
                            $('#FileSelectGrid tr:last').append(cell);
                        }

                        count++;
                    }

                    if (count < 5)
                        for (i = count; i < 5; i++) {
                            cell = $("<td style='white-space:nowrap; height:130px;'><div style='height:100%; width:100%; max-width:" + (width / 5) + "px; min-width:" + (width / 5) + "px;'></div></td>");
                            $('#FileSelectGrid tr:last').append(cell);
                        }
                }

                /*********************************************** Events ************************************************/
                $(".selectableFile, .selectableFileGrid").click(function () {
                    if ($(this).is(".selectedFile")) return;

                    $("#ModalDialogContent .selectedFile").css('background-color', 'transparent');
                    $("#ModalDialogContent .selectedFile").removeClass("selectedFile");
                    $(this).addClass("selectedFile");
                    $(this).css('background-color', '#ADD8E6');
                    if (saveMode) {
                        var fileName = (selectedLayoutIcon == "listIcon") ? $(this).text() : $(this).next().text();
                        var pos = fileName.lastIndexOf('.');
                        if (pos > 0) fileName = fileName.substring(0, pos);
                        $(".ui-dialog-buttonpane #fileNameInput").val(fileName);
                    }
                });

                $(".selectableFolder").click(function () {
                    if ($(this).text() == "..") {
                        FoldersBreadcrumbs.pop();
                        list(FoldersBreadcrumbs[FoldersBreadcrumbs.length - 1].id);
                    } else {
                        FoldersBreadcrumbs.push({
                            name: $(this).text(),
                            id: $(this).attr('fileId')
                        });
                        list($(this).attr('fileId'));
                    }
                });

                $(".selectableFolderGrid").click(function () {
                    if (($(this).parent().children())[1].innerHTML == "..") {
                        FoldersBreadcrumbs.pop();
                        list(FoldersBreadcrumbs[FoldersBreadcrumbs.length - 1].id);
                    } else {
                        FoldersBreadcrumbs.push({
                            name: $(this).text(),
                            id: $(this).attr('fileId')
                        });
                        list($(this).attr('fileId'));
                    }
                });

                $(".selectableFile, .selectableFolder, .selectableFileGrid, .selectableFolderGrid, #gridIcon, #listIcon").hover(function () {
                    $(this).css('cursor', 'pointer');
                });
                $(".subText").hover(function () {
                    $(this).css('cursor', 'default');
                });
                $(".layoutStyle").click(function () {
                    var preferredLayout = $(this).attr('id');
                    $(".layoutStyle").css('background-color', 'transparent');
                    $("#" + preferredLayout).css('background-color', '#ADD8E6');
                    if (preferredLayout == selectedLayoutIcon)
                        return;
                    selectedLayoutIcon = preferredLayout;
                    list(FoldersBreadcrumbs[FoldersBreadcrumbs.length - 1].id);
                    $(".ui-dialog-buttonpane #fileNameInput").val("");
                });
            });
        };

        list(null); //null will get root folder contents
    });
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