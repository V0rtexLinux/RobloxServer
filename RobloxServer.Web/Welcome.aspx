<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Welcome.aspx.cs" Inherits="RobloxServer.Pages.Welcome" %>
<asp:Content ID="Head" ContentPlaceHolderID="HeadContent" runat="server">
    <link href="<%: ResolveUrl("~/Content/Pages/Welcome.css") %>" rel="stylesheet" type="text/css" />
</asp:Content>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div id="wl">
        <div id="wl-left">
            <div class="wl-row">
                <div id="wl-login">
                    <div class="wl-login-title">MEMBER LOGIN</div>
                    <label for="wl_user">Username:</label>
                    <input type="text" name="hl_user" id="wl_user" maxlength="20" autocomplete="username" />
                    <label for="wl_pass">Password:</label>
                    <input type="password" name="hl_pass" id="wl_pass" maxlength="200" autocomplete="current-password" />
                    <input type="hidden" name="hl_return" value="/" />
                    <a class="wl-forgot" href="<%: ForgotUrl %>">forgot your password?</a>
                    <input type="submit" id="wl_go" class="wl-signin" value="Sign In" formaction="<%: ResolveUrl("~/Login/HeaderLogin.ashx") %>" formnovalidate="formnovalidate" />
                    <div class="wl-notmember">
                        <span>Not a member?</span>
                        <a class="wl-signup" href="<%: ResolveUrl("~/Register.aspx") %>">Sign Up</a>
                    </div>
                </div>
                <div id="wl-video">
                    <a href="<%: ResolveUrl("~/Games") %>" title="Browse games">
                        <img src="<%: ResolveUrl("~/Images/Thumbs/Place1_420x230.jpg") %>" width="360" height="197" alt="Games" />
                        <span class="wl-play-overlay"></span>
                    </a>
                </div>
            </div>

            <div id="wl-banner">
                <span class="wl-banner-logo"><%: RobloxServer.Config.SiteName %></span>
                <span class="wl-banner-text">The Free Online Building Game</span>
                <a class="wl-banner-btn" href="<%: PlayUrl %>"><span class="wl-arrow">&#9654;</span> Play Now</a>
            </div>

            <h1 id="wl-featured-title">Featured Free Game</h1>
            <% if (HasFeatured) { %>
            <div id="wl-featured">
                <div class="wl-feat-info">
                    <div class="wl-feat-name"><a href="<%: FeaturedUrl %>"><%: FeaturedName %></a> <span>by <a href="<%: FeaturedByUrl %>"><%: FeaturedBy %></a></span></div>
                    <a class="wl-play-this" href="<%: FeaturedUrl %>">Play This</a>
                    <dl>
                        <dt>Updated:</dt><dd><%: FeaturedUpdated %></dd>
                        <dt>Visited:</dt><dd><%: FeaturedVisits %> times</dd>
                    </dl>
                </div>
                <a class="wl-feat-thumb" href="<%: FeaturedUrl %>"><img src="<%: FeaturedThumb %>" width="330" height="181" alt="<%: FeaturedName %>" /></a>
            </div>
            <% } else { %>
            <div id="wl-featured" class="wl-empty">No games have been published yet. Sign up and <a href="<%: ResolveUrl("~/Develop.aspx") %>">publish the first one</a>!</div>
            <% } %>
        </div>

        <div id="wl-right">
            <a id="wl-bigplay" href="<%: PlayUrl %>"><span>Play<br />Now</span></a>
            <div class="wl-promo wl-promo-download">
                <b>Play on your PC</b>
                <p>Get the launcher once, then press <b>Play</b> on any game page.</p>
                <a href="<%: ResolveUrl("~/Install/Download.ashx?client=Launcher") %>">Download <%: RobloxServer.Config.SiteName %></a>
            </div>
            <div class="wl-promo wl-promo-forum">
                <b>Join the conversation</b>
                <p>Ask for help, show your builds and make friends in the forum.</p>
                <a href="<%: ResolveUrl("~/Forum.aspx") %>">Open the Forum</a>
            </div>
            <div id="wl-news">
                <div class="wl-news-title"><%: RobloxServer.Config.SiteName.ToUpperInvariant() %> NEWS</div>
                <%= NewsHtml %>
            </div>
        </div>
        <div style="clear: both"></div>
    </div>
    <script type="text/javascript">
        (function () {
            ['wl_user', 'wl_pass'].forEach(function (id) {
                document.getElementById(id).addEventListener('keydown', function (e) {
                    if (e.keyCode === 13) { e.preventDefault(); document.getElementById('wl_go').click(); }
                });
            });
        })();
    </script>
</asp:Content>
