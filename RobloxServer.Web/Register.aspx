<%@ Page Title="Sign Up" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="RobloxServer.Pages.Register" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Sign Up and Start Having Fun!</h1>
    <asp:Panel ID="RegisterPanel" runat="server" DefaultButton="RegisterButton">
        <div class="form-row">
            <label for="<%= UserNameBox.ClientID %>">Username</label>
            <asp:TextBox ID="UserNameBox" runat="server" MaxLength="20" />
        </div>
        <div class="form-row">
            <label for="<%= PasswordBox.ClientID %>">Password</label>
            <asp:TextBox ID="PasswordBox" runat="server" TextMode="Password" MaxLength="200" />
        </div>
        <div class="form-row">
            <label for="<%= ConfirmBox.ClientID %>">Confirm Password</label>
            <asp:TextBox ID="ConfirmBox" runat="server" TextMode="Password" MaxLength="200" />
        </div>
        <div class="form-row">
            <asp:CheckBox ID="Under13Box" runat="server" Text=" I am under 13 (enables SuperSafeChat)" />
        </div>
        <asp:Label ID="ErrorLabel" runat="server" CssClass="error" />
        <asp:Button ID="RegisterButton" runat="server" Text="Sign Up" CssClass="btn" OnClick="RegisterButton_Click" />
    </asp:Panel>
    <asp:PlaceHolder ID="ClosedMessage" runat="server" Visible="false">
        <div class="notice">Registration is closed on this server.</div>
    </asp:PlaceHolder>
</asp:Content>
