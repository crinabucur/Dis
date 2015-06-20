//  the modal dialog and modal dialog content variables
var $ModalDialog;
var ModalDialogID;
var $ModalDialogContent;
var ModalDialogContentID;

function AuthenticateSharepointDialog(cloudActionOnOffice365Redirect, callback) {
    PageMethods.IsAuthSharepoint(function (response) {
        if (response != 'true') {

            var objectWidth = 450;
            var objectHeight = 80; //150

            var dialogPaddingWidth = 50;
            var dialogPaddingHeight = 20;

            var dialogWidth = objectWidth + dialogPaddingWidth;
            var dialogHeight = objectHeight + dialogPaddingHeight;

            // Create a modal dialog with predefined height, width and title
            CreateModalDialog(dialogHeight, dialogWidth, "SharePoint Login");
            // The created modal dialog is accessible through $ModalDialog global variable
            // The created modal dialog content is accessible through $ModalDialogContent global variable

            // Create the dialog content ($ModalDialogContent)
            var site = "<input id='site' type='text' value='' style='width:100%'>";

            // Set the modal dialog content
            var content = "<div style='margin-top:20px;margin-left:5px;margin-right:5px' id='maindiv'>"
                + "<table style='font-size:12px; margin-left:0px; margin-right:0px;width:100%;'>"
                + "<tbody style='text-align:left;'>";

            content += "<tr>"
                + "<td style='width:31%'>SharePoint site URL: </td>"
                + "<td>" + site + "</td>"
                + "</tr>";

            content += "</tbody></table></div>";
            $ModalDialogContent.html(content);

            // Set the modal dialog content width and height
            $ModalDialogContent.width(objectWidth);
            $ModalDialogContent.height(objectHeight);

            //  Set dialog buttons and actions.
            SetModalDialogOption("buttons", [
        {
            text: "Ok",
            click: function () {
                var url = ($("#site") != undefined) ? $("#site").val() : "";

                if (typeof (url) != "undefined" && url != "") {
                    PageMethods.SetSharepointUrlAndAction(url, cloudActionOnOffice365Redirect, function (wreply) {
                        var wreplyParam;
                        if (typeof (wreply) == "undefined" || wreply == "") {
                            wreplyParam = url + "/_layouts/15/landing.aspx";
                        } else {
                            wreplyParam = wreply;
                        }

                        window.location.href = "https://login.microsoftonline.com/login.srf?wa=wsignin1%2E0&rpsnv=3&rver=6%2E1%2E6206%2E0&wp=MBI&wreply=" + wreplyParam + "%3FSource%3D" + window.location;
                        CloseModalDialog();
                    });
                }
            }
        }, {
            text: "Cancel",
            click: function () {
                // Dismiss the modal dialog when the user cancels the action. 
                CloseModalDialog();
            }
        }]);

            // The dialog is set up. Time to present the dialog.
            ShowModalDialog(dialogHeight, dialogWidth);

            if ($("#site") != undefined) {
                if (response != "false" && response != "") $("#site").val(response);
                else $("#site").val("https://");
                $("#site").focus();
            }
        }
        else {
            CloseModalDialog();
            callback();
        }
    });
}

function AuthenticateAmazonDialog(cloudActionOnOffice365Redirect, callback) {
    PageMethods.IsAuthAmazon(function (response) {
        if (!response) {
            var objectWidth = 450;
            var objectHeight = 150;

            var dialogPaddingWidth = 50;
            var dialogPaddingHeight = 20;

            var dialogWidth = objectWidth + dialogPaddingWidth;
            var dialogHeight = objectHeight + dialogPaddingHeight;

            // Create a modal dialog with predefined height, width and title
            CreateModalDialog(dialogHeight, dialogWidth, "Amazon S3 Login");
            
            // Create the dialog content ($ModalDialogContent)
            var acesskey = "<input id='acesskey' name='access' type='text' value='' style='width:280px'>";
            var secretkey = "<input id='secretkey' name='secret' type='password' style='width:280px'>";
            var region = "<select id='regionselect' name='region' style='width:150px; height:24px;'> " +
                "<option>US East (Virginia)</option>" +
                "<option>US West (N. California)</option>" +
                "<option>US West (Oregon)</option>" +
                "<option>EU West (Ireland)</option>" +
                "<option>EU Central (Frankfurt)</option>" +
                "<option>Asia Pacific (Tokyo)</option>" +
                "<option>Asia Pacific (Singapore)</option>" +
                "<option>Asia Pacific (Sydney)</option>" +
                "<option>South America (Sao Paulo)</option>" +
                "<option>US GovCloud West (Oregon)</option>" +
                "<option>China (Beijing)</option>" +
            "</select> ";


            // Set the modal dialog content
            var content = "<form id='awsform' action='#'><div style='margin-top:20px;margin-left:5px;margin-right:5px' id='maindiv'>"
                + "<table cellspacing='1' style='font-size:12px; margin-left:0px; margin-right:0px;width:100%;'>"
                + "<tbody style='text-align:left;'>";

            content += "<tr>"
                + "<td style='width:140px'>AWS access key: </td>"
                + "<td>" + acesskey + "</td>"
                + "</tr>"
                + "<tr><td style='width:140px'>AWS secret key: </td>"
                + "<td>" + secretkey + "</td>"
                + "</tr>"
                + "<tr><td style='width:140px'>Region: </td>"
                + "<td>" + region + "</td>"
                + "</tr>"
                + "<tr><td colspan='2'><span id='errorfield' style='color:crimson; display:none; height:30px;'></span></td></tr>";

            content += "</tbody></table></div></form>";
            $ModalDialogContent.html(content);

            // Set the modal dialog content width and height
            $ModalDialogContent.width(objectWidth);
            $ModalDialogContent.height(objectHeight);

            //  Set dialog buttons and actions.
            SetModalDialogOption("buttons", [
        {
            text: "Ok",
            click: function () {
                var id = $ModalDialogContent.find("#acesskey");
                id = (id != undefined) ? id.val() : "";
                if (id == "") {
                    $ModalDialogContent.find("#errorfield").show();
                    $ModalDialogContent.find("#errorfield").text("Error: Invalid access key, field cannot be left empty!");
                } else {
                    $ModalDialogContent.find("#errorfield").hide();
                    var form = $ModalDialogContent.find("#awsform");
                    form.submit();
                }
            }
        }, {
            text: "Cancel",
            click: function () {
                // Dismiss the modal dialog when the user cancels the action. 
                CloseModalDialog();
            }
        }]);

            // The dialog is set up. Time to present the dialog.
            ShowModalDialog(dialogHeight, dialogWidth);
        }
        else {
            CloseModalDialog();
            callback();
        }

        $('#awsform').submit(function (e) {
            e.preventDefault();
            PageMethods.AuthenticateAmazonS3(this.access.value, this.secret.value, this.region.value, function (resp) {
                $("#AmazonS3").hide();
                ListContents("AmazonS3", null);
                CloseModalDialog();
            });
        });
    });
}

function CreateModalDialog(dialogHeight, dialogWidth, dialogTitle) {
    //  create the modal dialog
    $ModalDialog.dialog({
        autoOpen: false,
        title: dialogTitle,
        width: dialogWidth,
        height: dialogHeight,
        modal: true,
        resizable: false,
        zIndex: 900,
        show: {
            effect: "fadeIn",
            duration: 100
        },
        hide: {
            effect: "fadeOut",
            duration: 0
        },
        closeOnEscape: true,
        buttons: "",
        //position: ['middle', 50],
        position:
            {
                my: "center",
                at: "center",
                of: window
            },
        close: function (event, ui) {
            // set the content of the dialog to an empty div
            $ModalDialog.html('<div id="' + ModalDialogContentID + '" class="ui-widget-content"/>');

            // reinitialize the content variable
            $ModalDialogContent = $("#ModalDialogContent");
        }
    });
    $(".ui-dialog-titlebar-close").show();
}

/*
*   sets an option to the modal dialog; the modal dialog should be already created
*/
function SetModalDialogOption(optionName, optionObject) {
    if (typeof $ModalDialog != 'undefined')
        //  create the modal dialog
        $ModalDialog.dialog("option", optionName, optionObject);
}

/*
*   open the dialog created by a call to createModalDialog
*/
function ShowModalDialog(dialogHeight, dialogWidth) {
    if (typeof $ModalDialog != 'undefined') {
        //  show the dialog
        $ModalDialog.dialog("open");

        //set the dialog dimensions
        //$ModalDialog.width(dialogWidth);
        $ModalDialog.height(dialogHeight);
    }
    $('#ModalDialogContent').css('border', '0');
}

/*
*   closes an open modal dialog
*/
function CloseModalDialog() {
    if (typeof $ModalDialog != 'undefined') // && $ModalDialog.is(':data(dialog)'))
        // close the dialog
        $ModalDialog.dialog("close");
}

/*
*   initializes the variables used for dialogs
*/
function InitializeDialogs() {
    $ModalDialog = $("#ModalDialog");
    ModalDialogID = "ModalDialog";

    $ModalDialogContent = $("#ModalDialogContent");
    ModalDialogContentID = "ModalDialogContent";
    if (cloudService && cloudService != 'null') {
        //if (cloudAction == "open") {
        //    OpenFromCloud(cloudService)
        //} else if (cloudAction == "save") {
        //    QuickSave();
        //} else if (cloudAction == "saveas") {
        //    SaveToCloudDialog(cloudService, cloudFileExtensions);
        //}

        cloudService = 'null';
    }
}

function AddFolderDialog(cloud, parentFolderId) {
    var objectWidth = 450;
    var objectHeight = 80; //150

    var dialogPaddingWidth = 50;
    var dialogPaddingHeight = 20;

    var dialogWidth = objectWidth + dialogPaddingWidth;
    var dialogHeight = objectHeight + dialogPaddingHeight;

    // Create a modal dialog with predefined height, width and title
    CreateModalDialog(dialogHeight, dialogWidth, "Add Folder");

    // Create the dialog content ($ModalDialogContent)
    var name = "<input id='foldername' type='text' value='' style='width:332px; margin-left:5px;'>";

    // Set the modal dialog content
    var content = "<div style='margin-top:20px;margin-left:5px;margin-right:5px' id='maindiv'>";

    content += "Folder name: "  + name;

    content += "</div>";
    $ModalDialogContent.html(content);

    // Set the modal dialog content width and height
    $ModalDialogContent.width(objectWidth);
    $ModalDialogContent.height(objectHeight);

    //  Set dialog buttons and actions.
    SetModalDialogOption("buttons", [
    {
        text: "Ok",
        click: function () {
            var foldername = ($("#foldername") != undefined) ? $("#foldername").val() : "";

            if (foldername != "") {

                var success = false;
                var resp;

                $.ajax({
                    type: "POST",
                    url: "../Default.aspx/NewFolder",
                    data: '{ "cloud" : "' + cloud + '" , "parentFolderId" :' + JSON.stringify(parentFolderId) + ', "_name" :' + JSON.stringify(foldername) + '}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    async: false,
                    success: function (response) {
                        resp = response.d;
                        if (!resp.Error) {
                            success = true;
                            CloseModalDialog();
                            ListContents(cloud, parentFolderId);
                        }
                    }
                });

                if (!success) {
                    showError(resp.ErrorMessage);
                    CloseModalDialog();
                }
            } 
        }
    },
    {
        text: "Cancel",
        click: function () {
            // Dismiss the modal dialog when the user cancels the action. 
            CloseModalDialog();
    }
    }]);

    // The dialog is set up. Time to present the dialog.
    ShowModalDialog(dialogHeight, dialogWidth);
}

function MoveOrCopyDialog(callingItem) {
    var cell = callingItem.parent().parent().parent();
    var icon = cell.find("img");
    var fileId = icon.attr("fileid");
    var cloud = icon.attr("cloud");

    PageMethods.GetDirectoryTree(cloud, function(response) {
        if (response.Error) {
            ShowError(response.ErrorMessage);
            return;
        }

        var objectWidth = 520;
        var objectHeight = 300;

        var dialogPaddingWidth = 50;
        var dialogPaddingHeight = 20;

        var dialogWidth = objectWidth + dialogPaddingWidth;
        var dialogHeight = objectHeight + dialogPaddingHeight;

        var treeData = JSON && JSON.parse(response.TreeData) || $.parseJSON(response.TreeData);

        CreateModalDialog(dialogHeight, dialogWidth, "Move / Copy Selected Item");

        // Set the modal dialog content
        $ModalDialogContent.append("<div style='text-align:left;'><br/><div>" + "Move / Copy the selected item to this folder:" + "</div><div id='tree3' style='max-height:250px; overflow:auto;'></div></div>");

        $("#tree3").dynatree({
            minExpandLevel: 2,
            //checkbox: true,
            selectMode: 1,
            children: treeData,
            onSelect: function (select, node) {
                // Display list of selected nodes
                //var selNodes = node.tree.getSelectedNodes();
                //// convert to title/key array
                //var selKeys = $.map(selNodes, function (node) {
                //    return "[" + node.data.key + "]: '" + node.data.title + "'";
                //});
                //$("#echoSelection2").text(selKeys.join(", "));
            },
            onClick: function (node, event) {
                // We should not toggle, if target was "checkbox", because this
                // would result in double-toggle (i.e. no toggle)
                //if (node.getEventTargetType(event) == "title")
                //    node.toggleSelect();
            },
            onKeydown: function (node, event) {
                if (event.which == 32) {
                    node.toggleSelect();
                    return false;
                }
            },
            clickFolderMode: 1, // activate
            debugLevel: 0 // quiet
        });

        // expand the root node by default
        //$("#tree3").dynatree("getRoot").visit(function (node) {
        //    node.expand(true);
        //});

        //  Set dialog buttons and actions.
        SetModalDialogOption("buttons", [
            {
                text: "Move",
                click: function () {
                    PageMethods.MoveFilesAndFolders([fileId], $("#tree3").dynatree("getActiveNode").data.key, cloud, function (resp) {  /// List<string> ids, string newParentId, string cloud //TODO: error if no node selected
                        CloseModalDialog();
                        var cloudContainer = $("#gridCell" + cloud);
                        var currentFolder = cloudContainer.attr("currentfolder");
                        ListContents(cloud, currentFolder);
                    });
                }
            },
            {
                text: "Copy",
                click: function () {
                    // TODO: add
                }
            },
            {
                text: "Cancel",
                click: function () {
                    // Dismiss the modal dialog.
                    CloseModalDialog();
                }
            }
        ]);

        // The dialog is set up. Time to present the dialog.
        ShowModalDialog(dialogHeight, dialogWidth);
    });
}