<%@ Page Title="Contact" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="Disertatie.Contact" %>

<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <hgroup class="title">
        <h1><%: Title %></h1>
        <%--<h2>Your contact page.</h2>--%>
    </hgroup>

    <section class="contact">
        <br/>
        <span style="color: #008096; font-size:19px;">Crina-Cătălina Bucur</span>
        <header>
            <h3>Phone:</h3>
        </header>
        <p>
            <span class="label">RO:</span>
            <span>(+40)726.729.345</span>
        </p>
        <p>
            <span class="label">UK:</span>
            <span>(+44)7460.658.008</span>
        </p>
    </section>

    <section class="contact">
        <header>
            <h3>Email:</h3>
        </header>
        <p>
            <span class="label">Personal:</span>
            <span><a href="mailto:bucur.crina@gmail.com">bucur.crina@gmail.com</a></span>
        </p>
        <p>
            <span class="label">Work:</span>
            <span><a href="mailto:crina@housatonic.microsoftonline.com">crina@housatonic.microsoftonline.com</a></span>
        </p>
    </section>

    <section class="contact">
        <header>
            <h3>Address:</h3>
        </header>
        <p>
            2 Elmira Street<br />
            London, UK SE137FW
        </p>
    </section>
</asp:Content>