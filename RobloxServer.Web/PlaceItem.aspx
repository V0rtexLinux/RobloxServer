<%@ Page Title="Game" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PlaceItem.aspx.cs" Inherits="RobloxServer.Pages.PlaceItem" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <asp:PlaceHolder ID="NotFoundPanel" runat="server" Visible="false">
        <div class="PageBox">
            <h1>Game not found</h1>
            <p>This game does not exist or is private. <a href="Default.aspx">Back to Games</a></p>
        </div>
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="PlacePanel" runat="server">
        <div id="ItemContainer" class="PlaceItem">
            <h1 class="ItemTitle"><asp:Literal ID="NameLiteral" runat="server" /></h1>
            <div class="ItemBuilder">Builder: <b><asp:Literal ID="CreatorLiteral" runat="server" /></b></div>

            <div class="ItemLeft">
                <div class="ItemThumbnail"><img id="ThumbnailImage" runat="server" width="420" height="230" alt="" /></div>
                <div class="ItemDescription">
                    <div class="StandardBoxHeader">Description</div>
                    <div class="StandardBox"><p class="DescriptionText"><asp:Literal ID="DescriptionLiteral" runat="server" /></p></div>
                </div>
            </div>

            <div class="ItemRight">
                <a class="btn-large btn-play" href="#PlayInstructions">Play</a>
                <div id="PlayInstructions" class="PlayInstructions">
                    To play: open <b>RobloxServerLauncher</b> &rarr; <i>Server Browser</i> &rarr; <i>RobloxServer Games</i>, log in and pick
                    <b><asp:Literal ID="NameLiteral2" runat="server" /></b> (place id <asp:Literal ID="IdLiteral" runat="server" />).
                </div>
                <table class="ItemDetails">
                    <tr><td class="Label">Visited:</td><td><asp:Literal ID="VisitsLiteral" runat="server" /></td></tr>
                    <tr><td class="Label">Updated:</td><td><asp:Literal ID="UpdatedLiteral" runat="server" /></td></tr>
                    <tr><td class="Label">Client:</td><td><asp:Literal ID="ClientLiteral" runat="server" /></td></tr>
                    <tr><td class="Label">Max Players:</td><td><asp:Literal ID="MaxPlayersLiteral" runat="server" /></td></tr>
                    <tr><td class="Label">Genre:</td><td><img id="GenreImage" runat="server" class="GenreIcon" alt="" /> <asp:Literal ID="GenreLiteral" runat="server" /></td></tr>
                    <tr><td class="Label">Allowed Gear:</td><td><img src="<%: ResolveUrl("~/Images/Icons/NoSuitcase16x16.png") %>" class="GearIcon" alt="No gear" title="No gear allowed" /></td></tr>
                </table>
                <div class="ItemDownload"><a id="DownloadLink" runat="server" class="btn-control btn-control-medium">Download .rbxl</a></div>
            </div>
            <div style="clear: both"></div>

            <div class="ItemTabs">
                <div class="ItemTab SelectedTab">Games</div>
            </div>
            <div class="ItemTabContent">
                <h2 class="light">Running Games</h2>
                <asp:PlaceHolder ID="NoServers" runat="server"><p>No servers are running this game right now. Host one from the launcher.</p></asp:PlaceHolder>
                <table class="table grid">
                    <asp:Repeater ID="ServersRepeater" runat="server">
                        <HeaderTemplate><tr class="table-header"><th class="first">Server</th><th>Host</th><th>Players</th><th>Started</th></tr></HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td><img src="<%# ResolveUrl("~/Images/Icons/online.png") %>" alt="" /> <%# Enc("Name") %></td>
                                <td><%# Enc("HostUserName") %></td>
                                <td><%# Eval("PlayerCount") %> / <%# Eval("MaxPlayers") %></td>
                                <td><%# Eval("Started", "{0:u}") %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </table>
            </div>

            <asp:PlaceHolder ID="EditPanel" runat="server" Visible="false">
                <div class="PageBox">
                    <div class="StandardBoxHeader">Configure this Place</div>
                    <div class="StandardBox">
                        <div class="form-row"><label>Name:</label><asp:TextBox ID="NameBox" runat="server" MaxLength="50" CssClass="text-box text-box-medium" /></div>
                        <div class="form-row"><label>Description:</label><asp:TextBox ID="DescriptionBox" runat="server" TextMode="MultiLine" CssClass="text-box" /></div>
                        <div class="form-row"><label>Genre:</label><asp:DropDownList ID="GenreList" runat="server" /></div>
                        <div class="form-row"><label>Client:</label><asp:DropDownList ID="ClientList" runat="server" /></div>
                        <div class="form-row"><label>Max players per server:</label><asp:TextBox ID="MaxPlayersBox" runat="server" MaxLength="3" CssClass="text-box text-box-medium short" /></div>
                        <div class="form-row">
                            <asp:CheckBox ID="PublicBox" runat="server" Text=" Public" />
                            &nbsp; <asp:CheckBox ID="FilteringBox" runat="server" Text=" FilteringEnabled" />
                        </div>
                        <div class="form-row"><label>Upload a new version (optional):</label><asp:FileUpload ID="PlaceUpload" runat="server" /></div>
                        <asp:Label ID="ErrorLabel" runat="server" CssClass="error" />
                        <asp:Button ID="SaveButton" runat="server" Text="Save" CssClass="btn-medium btn-primary" OnClick="SaveButton_Click" />
                        <asp:Button ID="DeleteButton" runat="server" Text="Delete" CssClass="btn-medium btn-negative" OnClick="DeleteButton_Click"
                            OnClientClick="return confirm('Delete this game forever?');" />
                    </div>
                </div>
            </asp:PlaceHolder>
        </div>
    </asp:PlaceHolder>
</asp:Content>
