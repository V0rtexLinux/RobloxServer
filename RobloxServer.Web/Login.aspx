<%@ Page Title="Login" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="RobloxServer.Pages.Login" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="LoginPage">
        <div class="LoginColumn">
            <div class="StandardBoxHeader">Member Login</div>
            <div class="StandardBox">
                <asp:Panel ID="LoginPanel" runat="server" DefaultButton="LoginButton">
                    <div class="form-row">
                        <label for="<%= UserNameBox.ClientID %>">Username:</label>
                        <asp:TextBox ID="UserNameBox" runat="server" MaxLength="20" CssClass="text-box text-box-large" />
                    </div>
                    <div class="form-row">
                        <label for="<%= PasswordBox.ClientID %>">Password:</label>
                        <asp:TextBox ID="PasswordBox" runat="server" TextMode="Password" MaxLength="200" CssClass="text-box text-box-large" />
                    </div>
                    <asp:Label ID="ErrorLabel" runat="server" CssClass="error" />
                    <asp:Button ID="LoginButton" runat="server" Text="Log In" CssClass="btn-medium btn-neutral" OnClick="LoginButton_Click" />
                </asp:Panel>
            </div>
        </div>
        <div class="SignupColumn">
            <div class="StandardBoxHeader">Not a member?</div>
            <div class="StandardBox">
                <p>Build anything you can imagine and play games made by other members of <%: RobloxServer.Config.SiteName %>.</p>
                <a class="btn-large btn-primary" href="Register.aspx">Sign Up</a>
            </div>
        </div>
    </div>
</asp:Content>
