<%@ Page Title="Admin" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="RobloxServer.Pages.Admin" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Admin</h1>
    <asp:Label ID="MessageLabel" runat="server" CssClass="error" />

    <h2>Moderate a user</h2>
    <div class="form-row"><label>Username</label><asp:TextBox ID="BanUserBox" runat="server" MaxLength="20" /></div>
    <div class="form-row"><label>Reason</label><asp:TextBox ID="BanReasonBox" runat="server" MaxLength="200" /></div>
    <div class="form-row"><label>Days (empty = Account Deleted / permanent)</label><asp:TextBox ID="BanDaysBox" runat="server" MaxLength="4" /></div>
    <asp:Button ID="BanButton" runat="server" Text="Ban" CssClass="btn red" OnClick="BanButton_Click" />
    <asp:Button ID="UnbanButton" runat="server" Text="Unban" CssClass="btn blue" OnClick="UnbanButton_Click" />
    <asp:Button ID="MakeAdminButton" runat="server" Text="Toggle admin" CssClass="btn blue" OnClick="MakeAdminButton_Click" />

    <h2>IP bans</h2>
    <div class="form-row"><label>IP address</label><asp:TextBox ID="IpBox" runat="server" MaxLength="45" /></div>
    <div class="form-row"><label>Reason</label><asp:TextBox ID="IpReasonBox" runat="server" MaxLength="200" /></div>
    <asp:Button ID="IpBanButton" runat="server" Text="Ban IP" CssClass="btn red" OnClick="IpBanButton_Click" />
    <asp:Button ID="IpUnbanButton" runat="server" Text="Unban IP" CssClass="btn blue" OnClick="IpUnbanButton_Click" />
    <table class="grid">
        <asp:Repeater ID="IpBansRepeater" runat="server">
            <HeaderTemplate><tr><th>IP</th><th>Reason</th><th>Date</th></tr></HeaderTemplate>
            <ItemTemplate><tr><td><%# Enc("Ip") %></td><td><%# Enc("Reason") %></td><td><%# Eval("Created", "{0:u}") %></td></tr></ItemTemplate>
        </asp:Repeater>
    </table>

    <h2>Users</h2>
    <table class="grid">
        <asp:Repeater ID="UsersRepeater" runat="server">
            <HeaderTemplate><tr><th>Id</th><th>Name</th><th>Last IP</th><th>Created</th><th>Status</th></tr></HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><%# Eval("Id") %></td><td><%# Enc("Name") %></td><td><%# Enc("LastIp") %></td>
                    <td><%# Eval("Created", "{0:u}") %></td><td><%# Enc("Status") %></td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
    </table>

    <h2>Running servers</h2>
    <table class="grid">
        <asp:Repeater ID="ServersRepeater" runat="server" OnItemCommand="ServersRepeater_ItemCommand">
            <HeaderTemplate><tr><th>Name</th><th>Place</th><th>Address</th><th>Client</th><th>Host</th><th>Players</th><th></th></tr></HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><%# Enc("Name") %></td><td><%# Eval("PlaceId") %></td><td><%# Enc("Address") %>:<%# Eval("Port") %></td>
                    <td><%# Enc("Client") %></td><td><%# Enc("HostUserName") %></td><td><%# Eval("PlayerCount") %></td>
                    <td><asp:LinkButton runat="server" CommandName="Remove" CommandArgument='<%# Eval("JobId") %>'>Remove</asp:LinkButton></td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
    </table>

    <h2>Script signing key</h2>
    <p>Patch this PUBLICKEYBLOB into the 2015 clients so they accept scripts signed by this server
       (also available at <a href="Keys/PublicKey.ashx">Keys/PublicKey.ashx</a>).</p>
    <p class="mono"><asp:Literal ID="PublicKeyLiteral" runat="server" /></p>
</asp:Content>
