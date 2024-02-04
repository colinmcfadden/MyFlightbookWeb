﻿<%@ Page Language="C#" AutoEventWireup="true" Codebehind="TotalsAndCurrencyEmail.aspx.cs" Async="true" Inherits="MyFlightbook.Subscriptions.TotalsAndCurrencyEmail" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register src="../Controls/mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbCurrency.ascx" tagname="mfbCurrency" tagprefix="uc3" %>
<%@ Register Src="~/Controls/mfbRecentAchievements.ascx" TagPrefix="uc1" TagName="mfbRecentAchievements" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link id="cssRef" runat="server" rel="stylesheet" type="text/css" />
    <base id="baseRef" runat="server" />
</head>
<body>
    <form id="form1" runat="server" enableviewstate="false">
        <asp:ScriptManager ID="ToolkitScriptManager1" runat="server" EnableViewState="false"></asp:ScriptManager>
    <div style="margin-left: auto; margin-right:auto; width:600px;">

    <h1><asp:Label ID="lblIntroHeader" runat="server" Text="" EnableViewState="false"></asp:Label></h1>
    <div style="margin-left: auto; margin-right:auto; margin-top: 5px; margin-bottom: 5px; text-align:center; width:80%; border: 1px solid black; background-color:#EEEEEE">
        <asp:MultiView ID="mvDonations" runat="server" EnableViewState="false">
            <asp:View ID="vwThankyou" runat="server" EnableViewState="false">
                <p><asp:Label ID="lblThankyou" runat="server" Text="" EnableViewState="false"></asp:Label></p>
            </asp:View>
            <asp:View ID="vwPleaseGive" runat="server" EnableViewState="false">
                <p><asp:Label ID="lblSolicitDonation" runat="server" Text="" EnableViewState="false"></asp:Label></p>
                <p class="style1"><asp:HyperLink ID="lnkDonateNow" runat="server" Font-Bold="true" EnableViewState="false"></asp:HyperLink></p>
            </asp:View>
        </asp:MultiView>
    </div>
    <asp:Panel ID="pnlCurrency" runat="server" Visible="false" EnableViewState="false">
        <h2><asp:Label ID="lblCurrency" runat="server" Text=""></asp:Label></h2>
        <asp:Panel ID="pnlExpiringCurrencies" runat="server" Visible="false">
            <h3><asp:Label ID="lblExpiring" runat="server" Text="<%$ Resources:Currency, CurrencyExpiringHeader %>"></asp:Label></h3>
            <ul>
                <asp:Repeater ID="rptExpiring" runat="server">
                    <ItemTemplate>
                        <li><%#: Eval("Attribute") %></li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>
        </asp:Panel>
        <uc3:mfbCurrency ID="mfbCurrency" UseInlineFormatting="true" runat="server" EnableViewState="false" />
    </asp:Panel>
    <asp:Panel ID="pnlTotals" runat="server" Visible="false" EnableViewState="false">
        <h2><asp:Label ID="lblTotal" runat="server" Text=""></asp:Label></h2>
        <asp:Literal runat="server" ID="litTotalsByTime"></asp:Literal>
        <h2><asp:Label ID="lblRecentAchievementsTitle" runat="server"></asp:Label></h2>
        <uc1:mfbRecentAchievements runat="server" ID="mfbRecentAchievements" />
    </asp:Panel>
    <div>
        <asp:Label ID="lblErr" runat="server" Text="You are not authorized to view this." CssClass="error" Visible="false" EnableViewState="false"></asp:Label>
        <asp:Label ID="lblSuccess" runat="server" Text="Success" CssClass="success" Visible="false" EnableViewState="false"></asp:Label>
    </div>
    <p><asp:HyperLink ID="lnkUnsubscribe" CssClass="fineprint" runat="server" Text="<%$ Resources:Profile, EmailWeeklyMailUnsubscribeLink %>" EnableViewState="false"></asp:HyperLink></p>
    <p><asp:HyperLink ID="lnkQuickUnsubscribe" CssClass="fineprint" runat="server" Text="<%$ Resources:Profile, EmailQuickUnsubscribeLink %>" EnableViewState="false"></asp:HyperLink></p>
    <p>MyFlightbook LLC<br />Woodinville, WA, 98072</p>
    <!-- SuccessToken -->
    </div>
    </form>
</body>
</html>
