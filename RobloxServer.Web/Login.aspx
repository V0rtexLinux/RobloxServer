<%@ Page Title="Log In" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="RobloxServer.Pages.Login" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Log In</h1>
    <asp:Panel ID="LoginPanel" runat="server" DefaultButton="LoginButton">
        <div class="form-row">
            <label for="<%= UserNameBox.ClientID %>">Username</label>
            <asp:TextBox ID="UserNameBox" runat="server" MaxLength="20" />
        </div>
        <div class="form-row">
            <label for="<%= PasswordBox.ClientID %>">Password</label>
            <asp:TextBox ID="PasswordBox" runat="server" TextMode="Password" MaxLength="200" />
        </div>
        <asp:Label ID="ErrorLabel" runat="server" CssClass="error" />
        <asp:Button ID="LoginButton" runat="server" Text="Log In" CssClass="btn blue" OnClick="LoginButton_Click" />
        <p>Don't have an account? <a href="Register.aspx">Sign up</a></p>
    </asp:Panel>
</asp:Content>
