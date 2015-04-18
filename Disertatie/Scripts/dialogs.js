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

function CreateModalDialog(dialogHeight, dialogWidth, dialogTitle) {
    //  create the modal dialog
    $ModalDialog.dialog({
        autoOpen: false,
        title: dialogTitle,
        width: dialogWidth,
        height: dialogHeight,
        modal: true,
        resizable: true, // false
        zIndex: 900,
        show: "",
        hide: "",
        closeOnEscape: true,
        buttons: "",
        position: ['middle', 50],
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
    if (typeof $ModalDialog != 'undefined' && $ModalDialog.is(':data(dialog)'))
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