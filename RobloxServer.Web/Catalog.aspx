<%@ Page Title="Catalog" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Catalog.aspx.cs" Inherits="RobloxServer.Pages.Catalog" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div style="width: 970px; margin: 20px auto; font-family: Arial, Helvetica, sans-serif; font-size: 14px; line-height: 20px;">
        <h1>Catalog</h1>
        <p>The catalog (hats, gear, faces and shirts) is not part of this server: every member plays with the default character look.</p>
        <p>What you can do here: <a href="<%: ResolveUrl("~/Games") %>">play games</a>, <a href="<%: ResolveUrl("~/Develop.aspx") %>">build and publish your own</a> and talk in the <a href="<%: ResolveUrl("~/Forum.aspx") %>">forum</a>.</p>
    </div>
</asp:Content>
