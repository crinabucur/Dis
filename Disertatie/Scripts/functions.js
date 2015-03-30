//$(function initialize() {
//    PageMethods.GetGridLayout(function (response) {

        
//        // get grid container
//        var container = $(".gridster > ul");

//        // render grid layout
//        var cells = response.GridCells;
//        cells.forEach(function(cell) {
//            PageMethods.IsAuthCloud(cell.name, function (isAuth) {
//                var gridcell = "<li id='gridCell" + cell.name + "' data-row='" + cell.row + "' data-col='" + cell.col +
//                    "' data-sizex='" + cell.sizex + "' data-sizey='" + cell.sizey + "'>";

//                if (!isAuth) {
//                    gridcell += "<img id = '" + cell.name + "' class ='CloudIconNotLoggedIn' src='Images/" + cell.name + ".png' title='Click to sign in' />";
//                } else {
//                    // TODO: implement
//                }

//                gridcell += "</li>";
//                //alert(gridcell);
//                container.append(gridcell);
//            });
//        });
//        //for (var i = 0, len = cells.length; i < len; i++) {
//        //    PageMethods.IsAuthCloud(cells[i].name, function (isAuth) {
//        //        var gridcell = "<li id='gridCell" + cells[i].name + "' data-row='" + cells[i].row + "' data-col='" + cells[i].col +
//        //            "' data-sizex='" + cells[i].sizex + "' data-sizey='" + cells[i].sizey + "'>";

//        //        if (!isAuth) {
//        //            gridcell += "<img id = '" + cells[i].name + "' class ='CloudIconNotLoggedIn' src='Images/" + cells[i].name + ".png' title='Click to sign in' />";
//        //        } else {
//        //            // TODO: implement
//        //        }

//        //        gridcell += "</li>";
//        //        container.append(gridcell);
//        //    });
//        //}

//        //if (typeof gridcell != "undefined")
//        //    container.append(gridcell);
        
//        var clouds = ["GoogleDrive", "OneDrive", "Dropbox", "Box", "SharePoint"]; //, "Device"];
//        //clouds.forEach(ListContents);


        
//    });
    
//});

var viewAsList = false;

$(document).ready(function () {
    //alert("ready!");
    
    if ($(".gridster").length > 0) {

        $(".CloudIconNotLoggedIn").click(function () {
            var cloud = this.id;
            PageMethods.IsAuthCloud(cloud, function (response) {
                if (response) {
                    alert("logged in!");
                }
                else {
                    if (cloud.toLowerCase() == "sharepoint")
                        AuthenticateSharepointDialog("open", function () {
                            OpenFromCloud(site); //what to do after logon
                        });
                    else {
                        jQuery(window).unbind("beforeunload");
                        window.location.href = " Dialogs/AuthenticateCloudService.aspx?cloud=" + cloud + "&action=open";
                    }
                }
            });
        });

        var clouds = ["GoogleDrive", "OneDrive", "Dropbox", "Box", "SharePoint"]; //, "Device"];
        //clouds.forEach(ListContents);
        for (var i = 0; i < clouds.length; i++) {
            ListContents(clouds[i], null);
        }
    }
});

function SetLayoutCookie() {
    var clouds = ["GoogleDrive", "OneDrive", "Dropbox", "Box", "SharePoint", "Device"];
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
    if ($("#" + value.toString()).length == 0) { // if the cloud is logged in and the page is Default

        var container = $('#gridCell' + value.toString());
        
        // remove existing items
        container.find(".TableItems, .SwitchLayoutLink").remove();

        // determine item width
        var width = container.width();
        var itemsPerLine = Math.round(width / 100);
        var itemWidth = Math.floor((width - 10) / itemsPerLine);

        folderId = folderId || null;

        container.attr("currentFolder", folderId);

        var success = false;
        var itemsCount;
        var items;

        $.ajax({
            type: "POST",
            url: "../Default.aspx/ListFilesInFolder",
            data: '{ "cloud" : "' + value + '" , "folderId" :' + folderId + ', "extensions" : null}',
            //data: { cloud:value, folderId:folderId, extensions:null },
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

        //PageMethods.ListFilesInFolder(value, null, null, function(response) {
        //    alert(response.length);
        //});
        if (folderId != null && folderId != "null") {
            var goBackFolder = { isFolder: true, Name: "..", Id: null, imageUrl: "Images/folderIcon_64x64.png" }; // TODO: support for list mode
            items.unshift(goBackFolder);
            itemsCount++;
        }
        
        container.append("<a class='SwitchLayoutLink'>View as List</a>");

        // TODO: optimization - this doesn't need to be retrieved with every listing (?)
        container.append("<span id='quota" + value + "' style='float:right; margin-right:3px; font-weight:bold; color:darkgray'></span>"); // TODO: add class for styles

        $.ajax({
            type: "POST",
            url: "../Default.aspx/GetSpaceQuota",
            data: '{ "cloud" : "' + value + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: true,
            success: function (resp) {
                var quota = resp.d;
                container.find('#quota' + value.toString()).text(quota);
            }
        });


        var table = $("<table id='" + value.toString() + "TableItems' class='TableItems' style='margin:20px 1% 0px; max-width: 98%; position:relative;'></table>");

        var row = $("<tr></tr>");
        for (var k = 0; k < itemsCount; k++) {
            var cell = "<td style='padding:6px 1px 10px; width:" + (itemWidth - 1) + "px; min-width:" + (itemWidth - 1) + "px; height:" + itemWidth + "px;'><div id='item" + k + value + "' cloud='" + value + "' style='text-align:center; max-width:" + (itemWidth - 1) + "px;'>";
            if (items[k].isFolder) {
                cell += "<img fileId='" + items[k].Id + "' class='FolderIcon' src='" + items[k].imageUrl + "'/></div>"; // TODO: get from server side the URL to the appropriate image (switch case) / extend CloudItem
            } else {
                cell += "<img fileId='" + items[k].Id + "' class='FileIcon' src='" + items[k].imageUrl + "'/></div>"; // TODO: get from server side the URL to the appropriate image (switch case)/ extend CloudItem
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

        $(".FolderIcon").bind('click', function () {
            //alert($($(this).parent()).attr("cloud"));
            ListContents($($(this).parent()).attr("cloud"), $(this).attr("fileId"));
        });

        $(".FileIcon").bind('click', function () {
            //alert($($(this).parent()).attr("cloud"));

            $.blockUI({
                css: {
                    border: 'none',
                    //padding: '15px',
                    backgroundColor: '#000', // '#000'
                    '-webkit-border-radius': '10px',
                    '-moz-border-radius': '10px',
                    //opacity: .5,
                    color: '#fff',
                    //top: '',
                    //left:'',
                    width: '',                    
                    'max-width':'90%',
                    'max-height': '90%'
                },
                overlayCSS: {
                    backgroundColor: '#000',
                    cursor:'pointer'
                }
            });
            $('.blockOverlay').click($.unblockUI);

            var video = $("<video controls autoplay='autoplay' style='text-align:center; position:relative;'><source src='Handlers/mp4.ashx' type='video/mp4'></video>");
            $('.blockUI.blockMsg.blockPage').append(video);
            $('.blockUI.blockMsg.blockPage').width(video.width());
            //alert(video.width());
            //

            //var video = $("<video width='320' height='240' controls autoplay='autoplay'><source src='Handlers/mp4.ashx' type='video/mp4'></video>");
            //$("#body").append(video);
        });


         
    }
}





function ListFiles(container, cloudService, callback, saveMode, extensions, filename) {
    PageMethods.GetFilePickerLabels(saveMode, function (response) {
        if (extensions == undefined)
            extensions = null;

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
            PageMethods.ListFilesInFolder(cloudService, folderId, extensions, function (files) {
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