<%@ Page Title="About" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="Disertatie.About" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <hgroup class="title">
        <h1><%: Title %></h1>
        <%--<h2>Your app description page.</h2>--%>
    </hgroup>

    <article>
        <p style="width:513px; text-align: justify; text-justify: inter-word;">        
            CloudSphere is a project I developed for my Software Engineering master thesis. It was designed as a universal platform that integrates major cloud storage services and allows managing files and directories within these services, as well as inter-cloud transfers.
        </p>

        <p style="width:513px; text-align: justify; text-justify: inter-word;">        
            CloudSphere offers secure authentication via the OAuth 2.0 protocol and does not require creating an account inside the application.
        </p>

        <p style="width:513px; text-align: justify; text-justify: inter-word;">        
            The most common file formats can be rendered to further facilitate file identification.
        </p>
    </article>

    <aside>
        <h3>Cloud services</h3>
        <p>        
            Integrated cloud storage services:
        </p>
        <ul>
            <li><a runat="server" target="_blank" href="https://developers.google.com/drive/web">Google Drive</a></li>
            <li><a runat="server" target="_blank" href="https://box-content.readme.io">Box</a></li>
            <li><a runat="server" target="_blank" href="https://msdn.microsoft.com/en-us/library/office/dn631844.aspx">OneDrive</a></li>
            <li><a runat="server" target="_blank" href="https://www.dropbox.com/developers/core">Dropbox</a></li>
            <li><a runat="server" target="_blank" href="http://aws.amazon.com/s3/">Amazon S3</a></li>
            <li><a runat="server" target="_blank" href="https://msdn.microsoft.com/en-us/library/office/dn833469">SharePoint Online</a></li>
            <li><a runat="server" target="_blank" href="https://basecamp.com/">BaseCamp</a></li>
            <%--<li><a runat="server" href="~/">Home</a></li>
            <li><a runat="server" href="~/About">About</a></li>
            <li><a runat="server" href="~/Contact">Contact</a></li>--%>
        </ul>
    </aside>
</asp:Content>