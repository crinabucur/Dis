<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Disertatie.Default" %>


<asp:Content runat="server" ID="FeaturedContent" ContentPlaceHolderID="FeaturedContent">
    <%--<section class="featured">
        <div class="content-wrapper">
            <hgroup class="title">
                <h1><%: Title %>.</h1>
                <h2>Modify this template to jump-start your ASP.NET application.</h2>
            </hgroup>
            <p>
                To learn more about ASP.NET, visit <a href="http://asp.net" title="ASP.NET Website">http://asp.net</a>.
                The page features <mark>videos, tutorials, and samples</mark> to help you get the most from ASP.NET.
                If you have any questions about ASP.NET visit
                <a href="http://forums.asp.net/18.aspx" title="ASP.NET Forum">our forums</a>.
            </p>
        </div>
    </section>--%>
    
</asp:Content>
<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <%--<h3>We suggest the following:</h3>
    <ol class="round">
        <li class="one">
            <h5>Getting Started</h5>
            ASP.NET Web Forms lets you build dynamic websites using a familiar drag-and-drop, event-driven model.
            A design surface and hundreds of controls and components let you rapidly build sophisticated, powerful UI-driven sites with data access.
            <a href="http://go.microsoft.com/fwlink/?LinkId=245146">Learn more…</a>
        </li>
        <li class="two">
            <h5>Add NuGet packages and jump-start your coding</h5>
            NuGet makes it easy to install and update free libraries and tools.
            <a href="http://go.microsoft.com/fwlink/?LinkId=245147">Learn more…</a>
        </li>
        <li class="three">
            <h5>Find Web Hosting</h5>
            You can easily find a web hosting company that offers the right mix of features and price for your applications.
            <a href="http://go.microsoft.com/fwlink/?LinkId=245143">Learn more…</a>
        </li>
    </ol>--%>
    
    <div class="gridster" style="top:10px;">
    <ul>
        <% foreach (var cell in GetGridLayout().GridCells)
           {%>
               <li id="gridCell<%= cell.name %>" data-row="<%= cell.row %>" data-col="<%= cell.col %>" data-sizex="<%= cell.sizex %>" data-sizey="<%= cell.sizey %>">
                   <div class="dragHandle"></div>
                   <% if (!IsAuthCloud(cell.name))
                      {%>
                          <img id="<%= cell.name %>" class="CloudIconNotLoggedIn" src="Images/<%= cell.name %>.png" title="Click to sign in" />
                          <span class="CloudMenuSpan" style="display:none"><span class="CloudMenuSpanContainer" ></span></span>
                     <% } else 
                        { %>
                          <img id="<%= cell.name %>" class="CloudIconNotLoggedIn" src="Images/<%= cell.name %>.png" title="Click to sign in" style="display:none"/>
                          <%if (cell.name != "Device") { %> <img src="Images/loader.gif" class="Loader" />
                          <span class="CloudMenuSpan"><span class="CloudMenuSpanContainer"></span></span>
                     <%}
                    }%>
               </li>
          <% }%>
    </ul>
    </div>
    
    <script type="text/javascript">
        if ('<%= Session["backFromAuth"] %>' != '') {
            var cloudService = '<%= Session["postedCloudType"] %>';
            var cloudAction = '<%= Session["postedCloudAction"] %>';
            var cloudFileExtensions = '<%= Session["postedFileExtensions"] %>';
        }
        <%
            Session["backFromAuth"] = null;    
            Session["postedCloudType"] = null;
            Session["postedCloudAction"] = null;
            Session["postedFileExtensions"] = null;
        %>

        $(".gridster > ul").gridster({
            widget_margins: [10, 10],
            widget_base_dimensions: [190, 190],
            min_cols: 6,
            resize: {
                enabled: true
            },
            draggable: {
                handle: '.dragHandle'
            }
        }).data('gridster');

    </script>
    <script>
        <%--$(function() {
            $('#fileUploadDiv').fileupload({
                replaceFileInput: false,
                dataType: 'json',
                url:'<%= ResolveUrl("AjaxFileHandler.ashx") %>',
                done: function(e, data) {
                    $.each(data.result, function(index, file) {
                        $('<p/>').text(file).appendTo('body');
                    });
                }
            });
        });--%>
        
    </script>
</asp:Content>
