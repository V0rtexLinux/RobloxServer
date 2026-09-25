<%@ Page Title="Develop" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Develop.aspx.cs" Inherits="RobloxServer.Pages.Develop" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Develop</h1>
    <h2>Publish a new game</h2>
    <p>Upload a place file (.rbxl or XML .rbxlx). You can also publish from Studio 2015 with <i>File &rarr; Publish to ROBLOX</i>.</p>
    <div class="form-row">
        <label for="<%= NameBox.ClientID %>">Name</label>
        <asp:TextBox ID="NameBox" runat="server" MaxLength="50" />
    </div>
    <div class="form-row">
        <label for="<%= DescriptionBox.ClientID %>">Description</label>
        <asp:TextBox ID="DescriptionBox" runat="server" TextMode="MultiLine" MaxLength="1000" />
    </div>
    <div class="form-row">
        <label for="<%= ClientList.ClientID %>">Client (the launcher uses this version to play it)</label>
        <asp:DropDownList ID="ClientList" runat="server" />
    </div>
    <div class="form-row">
        <label for="<%= MaxPlayersBox.ClientID %>">Max players per server</label>
        <asp:TextBox ID="MaxPlayersBox" runat="server" Text="12" MaxLength="3" />
    </div>
    <div class="form-row">
        <asp:CheckBox ID="PublicBox" runat="server" Checked="true" Text=" Public" />
        &nbsp; <asp:CheckBox ID="FilteringBox" runat="server" Text=" FilteringEnabled" />
    </div>
    <div class="form-row">
        <label for="<%= PlaceUpload.ClientID %>">Place file</label>
        <asp:FileUpload ID="PlaceUpload" runat="server" />
    </div>
    <asp:Label ID="ErrorLabel" runat="server" CssClass="error" />
    <asp:Button ID="PublishButton" runat="server" Text="Publish" CssClass="btn" OnClick="PublishButton_Click" />

    <h2>My games</h2>
    <asp:PlaceHolder ID="NoPlaces" runat="server" Visible="false"><p>You have not published any games yet.</p></asp:PlaceHolder>
    <table class="grid">
        <asp:Repeater ID="MyPlacesRepeater" runat="server">
            <HeaderTemplate><tr><th>Name</th><th>Id</th><th>Client</th><th>Version</th><th>Visits</th><th>Public</th></tr></HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><a href="PlaceItem.aspx?id=<%# Eval("Id") %>"><%# Enc("Name") %></a></td>
                    <td><%# Eval("Id") %></td>
                    <td><%# Enc("Client") %></td>
                    <td><%# Eval("Version") %></td>
                    <td><%# Eval("Visits") %></td>
                    <td><%# (bool)Eval("IsPublic") ? "Yes" : "No" %></td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
    </table>
</asp:Content>
