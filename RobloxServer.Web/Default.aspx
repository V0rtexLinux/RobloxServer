<%@ Page Title="Games" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="RobloxServer.Pages.Default" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Games</h1>
    <p>Play these games with the <b>RobloxServerLauncher</b>: open the launcher, go to <i>Server Browser &rarr; RobloxServer Games</i>.</p>
    <asp:PlaceHolder ID="EmptyMessage" runat="server" Visible="false">
        <div class="notice">No games have been published yet. <a href="Develop.aspx">Publish the first one!</a></div>
    </asp:PlaceHolder>
    <div class="games">
        <asp:Repeater ID="GamesRepeater" runat="server">
            <ItemTemplate>
                <div class="game-card">
                    <a href="PlaceItem.aspx?id=<%# Eval("Id") %>">
                        <div class="thumb"><%# Enc("Client") %></div>
                        <span class="name"><%# Enc("Name") %></span>
                    </a>
                    <div class="meta">by <%# Enc("CreatorName") %></div>
                    <div class="meta"><%# Eval("Playing") %> playing &middot; <%# Eval("Visits") %> visits</div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
</asp:Content>
