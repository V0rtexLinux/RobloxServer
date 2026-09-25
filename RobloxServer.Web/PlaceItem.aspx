<%@ Page Title="Game" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PlaceItem.aspx.cs" Inherits="RobloxServer.Pages.PlaceItem" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <asp:PlaceHolder ID="NotFoundPanel" runat="server" Visible="false">
        <h1>Game not found</h1>
        <p>This game does not exist or is private.</p>
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="PlacePanel" runat="server">
        <h1><asp:Literal ID="NameLiteral" runat="server" /></h1>
        <p>By <b><asp:Literal ID="CreatorLiteral" runat="server" /></b> &middot; Client <b><asp:Literal ID="ClientLiteral" runat="server" /></b>
           &middot; <asp:Literal ID="VisitsLiteral" runat="server" /> visits &middot; Updated <asp:Literal ID="UpdatedLiteral" runat="server" /></p>
        <p style="white-space: pre-wrap"><asp:Literal ID="DescriptionLiteral" runat="server" /></p>
        <div class="notice">
            To play: open <b>RobloxServerLauncher</b> &rarr; <i>Server Browser</i> &rarr; <i>RobloxServer Games</i>, log in and pick
            <b><asp:Literal ID="NameLiteral2" runat="server" /></b> (place id <asp:Literal ID="IdLiteral" runat="server" />).
            <br /><a id="DownloadLink" runat="server">Download .rbxl</a>
        </div>

        <h2>Running servers</h2>
        <asp:PlaceHolder ID="NoServers" runat="server"><p>No servers are running this game right now. Host one from the launcher.</p></asp:PlaceHolder>
        <table class="grid">
            <asp:Repeater ID="ServersRepeater" runat="server">
                <HeaderTemplate><tr><th>Server</th><th>Host</th><th>Players</th><th>Started</th></tr></HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td><%# Enc("Name") %></td>
                        <td><%# Enc("HostUserName") %></td>
                        <td><%# Eval("PlayerCount") %> / <%# Eval("MaxPlayers") %></td>
                        <td><%# Eval("Started", "{0:u}") %></td>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
        </table>

        <asp:PlaceHolder ID="EditPanel" runat="server" Visible="false">
            <h2>Configure this game</h2>
            <div class="form-row"><label>Name</label><asp:TextBox ID="NameBox" runat="server" MaxLength="50" /></div>
            <div class="form-row"><label>Description</label><asp:TextBox ID="DescriptionBox" runat="server" TextMode="MultiLine" /></div>
            <div class="form-row"><label>Client</label><asp:DropDownList ID="ClientList" runat="server" /></div>
            <div class="form-row"><label>Max players per server</label><asp:TextBox ID="MaxPlayersBox" runat="server" MaxLength="3" /></div>
            <div class="form-row">
                <asp:CheckBox ID="PublicBox" runat="server" Text=" Public" />
                &nbsp; <asp:CheckBox ID="FilteringBox" runat="server" Text=" FilteringEnabled" />
            </div>
            <div class="form-row"><label>Upload a new version (optional)</label><asp:FileUpload ID="PlaceUpload" runat="server" /></div>
            <asp:Label ID="ErrorLabel" runat="server" CssClass="error" />
            <asp:Button ID="SaveButton" runat="server" Text="Save" CssClass="btn blue" OnClick="SaveButton_Click" />
            <asp:Button ID="DeleteButton" runat="server" Text="Delete game" CssClass="btn red" OnClick="DeleteButton_Click"
                OnClientClick="return confirm('Delete this game forever?');" />
        </asp:PlaceHolder>
    </asp:PlaceHolder>
</asp:Content>
