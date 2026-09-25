<%@ Page Title="Build" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Develop.aspx.cs" Inherits="RobloxServer.Pages.Develop" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Build</h1>
    <div class="PageBox">
        <div class="StandardBoxHeader">Publish a new Place</div>
        <div class="StandardBox">
            <p>Upload a place file (.rbxl or XML .rbxlx). You can also publish from Studio 2015 with <i>File &rarr; Publish to ROBLOX</i>.</p>
            <div class="form-row">
                <label for="<%= NameBox.ClientID %>">Name:</label>
                <asp:TextBox ID="NameBox" runat="server" MaxLength="50" CssClass="text-box text-box-medium" />
            </div>
            <div class="form-row">
                <label for="<%= DescriptionBox.ClientID %>">Description:</label>
                <asp:TextBox ID="DescriptionBox" runat="server" TextMode="MultiLine" MaxLength="1000" CssClass="text-box" />
            </div>
            <div class="form-row">
                <label for="<%= GenreList.ClientID %>">Genre:</label>
                <asp:DropDownList ID="GenreList" runat="server" />
            </div>
            <div class="form-row">
                <label for="<%= ClientList.ClientID %>">Client (the launcher uses this version to play it):</label>
                <asp:DropDownList ID="ClientList" runat="server" />
            </div>
            <div class="form-row">
                <label for="<%= MaxPlayersBox.ClientID %>">Max players per server:</label>
                <asp:TextBox ID="MaxPlayersBox" runat="server" Text="12" MaxLength="3" CssClass="text-box text-box-medium short" />
            </div>
            <div class="form-row">
                <asp:CheckBox ID="PublicBox" runat="server" Checked="true" Text=" Public" />
                &nbsp; <asp:CheckBox ID="FilteringBox" runat="server" Text=" FilteringEnabled" />
            </div>
            <div class="form-row">
                <label for="<%= PlaceUpload.ClientID %>">Place file:</label>
                <asp:FileUpload ID="PlaceUpload" runat="server" />
            </div>
            <asp:Label ID="ErrorLabel" runat="server" CssClass="error" />
            <asp:Button ID="PublishButton" runat="server" Text="Publish" CssClass="btn-medium btn-primary" OnClick="PublishButton_Click" />
        </div>
    </div>

    <h2 id="MyPlaces" class="title">My Places</h2>
    <asp:PlaceHolder ID="NoPlaces" runat="server" Visible="false"><p>You have not published any places yet.</p></asp:PlaceHolder>
    <table class="table grid">
        <asp:Repeater ID="MyPlacesRepeater" runat="server">
            <HeaderTemplate><tr class="table-header"><th class="first">Name</th><th>Id</th><th>Genre</th><th>Client</th><th>Version</th><th>Visits</th><th>Public</th></tr></HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><a href="PlaceItem.aspx?id=<%# Eval("Id") %>"><%# Enc("Name") %></a></td>
                    <td><%# Eval("Id") %></td>
                    <td><img class="GenreIcon" src="<%# GenreIcon(Eval("Genre")) %>" alt="" /> <%# GenreName(Eval("Genre")) %></td>
                    <td><%# Enc("Client") %></td>
                    <td><%# Eval("Version") %></td>
                    <td><%# Eval("Visits") %></td>
                    <td><%# (bool)Eval("IsPublic") ? "Yes" : "No" %></td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
    </table>
</asp:Content>
