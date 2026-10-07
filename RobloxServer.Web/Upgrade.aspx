<%@ Page Title="Upgrade" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Upgrade.aspx.cs" Inherits="RobloxServer.Pages.Upgrade" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div style="width: 970px; margin: 20px auto; font-family: Arial, Helvetica, sans-serif; font-size: 14px; line-height: 20px;">
        <h1>Builders Club</h1>
        <p>There is nothing to buy on this server: every member already has everything. Publishing places, hosting servers and using the forum are free for all accounts.</p>
        <p><a href="<%: ResolveUrl("~/Develop.aspx") %>">Start building</a> or <a href="<%: ResolveUrl("~/Games") %>">play a game</a>.</p>
    </div>
</asp:Content>
