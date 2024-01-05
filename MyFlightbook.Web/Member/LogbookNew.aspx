﻿<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Trace="false" Codebehind="LogbookNew.aspx.cs" Inherits="MyFlightbook.MemberPages.LogbookNew" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbLogbook.ascx" tagname="mfbLogbook" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbEditFlight.ascx" tagname="mfbEditFlight" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc4" %>
<%@ Register src="../Controls/mfbCurrency.ascx" tagname="mfbCurrency" tagprefix="uc5" %>
<%@ Register src="../Controls/mfbChartTotals.ascx" tagname="mfbChartTotals" tagprefix="uc6" %>
<%@ Register src="../Controls/mfbQueryDescriptor.ascx" tagname="mfbQueryDescriptor" tagprefix="uc7" %>
<%@ Register src="../Controls/mfbAccordionProxyExtender.ascx" tagname="mfbAccordionProxyExtender" tagprefix="uc8" %>
<%@ Register src="../Controls/mfbAccordionProxyControl.ascx" tagname="mfbAccordionProxyControl" tagprefix="uc9" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc1" TagName="popmenu" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblUserName" runat="server"></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:HiddenField ID="hdnLastViewedPaneIndex" runat="server" />
    <script>
        function onAccordionPaneShown(idx) {
            if (idx != 1)
                document.getElementById("<% =hdnLastViewedPaneIndex.ClientID %>").value = idx;
        }
    </script>
    <uc8:mfbAccordionProxyExtender ID="mfbAccordionProxyExtender1" AccordionControlID="AccordionCtrl" HeaderProxyIDs="apcNewFlight,apcFilter,apcTotals,apcCurrency,apcAnalysis,apcPrintView,apcMore" runat="server" />
    <asp:Panel ID="pnlAccordionMenuContainer" CssClass="accordionMenuContainer" runat="server">
        <uc9:mfbAccordionProxyControl ID="apcNewFlight" LabelText="<%$ Resources:LocalizedText, LogTabNewFlight %>" runat="server" />
        <uc9:mfbAccordionProxyControl ID="apcFilter" LabelText="<%$ Resources:LocalizedText, LogTabFilter %>" runat="server" />
        <uc9:mfbAccordionProxyControl ID="apcTotals" LabelText="<%$ Resources:LocalizedText, LogTabTotals %>" runat="server" OnControlClicked="apcTotals_ControlClicked" LazyLoad="true" />
        <uc9:mfbAccordionProxyControl ID="apcCurrency" LabelText="<%$ Resources:LocalizedText, LogTabCurrency %>" runat="server" OnControlClicked="apcCurrency_ControlClicked" LazyLoad="true" />
        <uc9:mfbAccordionProxyControl ID="apcAnalysis" LabelText="<%$ Resources:LocalizedText, LogTabAnalysis %>" runat="server" OnControlClicked="apcAnalysis_ControlClicked" LazyLoad="true" />
        <uc9:mfbAccordionProxyControl ID="apcPrintView" LabelText="<%$ Resources:LocalizedText, LogTabPrint %>" runat="server" />
        <uc9:mfbAccordionProxyControl ID="apcMore" LabelText="<%$ Resources:LocalizedText, LogTabMore %>" runat="server" />
    </asp:Panel>
    <asp:Panel ID="pnlFilter" runat="server" CssClass="filterApplied" >
        <div style="display:inline-block;"><%=Resources.LocalizedText.ResultsFiltered %></div>
            <uc7:mfbQueryDescriptor ID="mfbQueryDescriptor1" runat="server" OnQueryUpdated="mfbQueryDescriptor1_QueryUpdated" />
    </asp:Panel>
    <ajaxToolkit:Accordion ID="AccordionCtrl" RequireOpenedPane="False" SelectedIndex="-1" runat="server" HeaderCssClass="accordionMenuHeader" HeaderSelectedCssClass="accordionMenuHeaderSelected" ContentCssClass="accordionMenuContent" TransitionDuration="250">
        <Panes>
            <ajaxToolkit:AccordionPane ID="acpNewFlight" runat="server">
                <Content>
                    <uc2:mfbEditFlight ID="mfbEditFlight1" runat="server" OnFlightUpdated="mfbEditFlight1_FlightUpdated" OnFlightEditCanceled="mfbEditFlight1_FlightEditCanceled" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acpSearch" runat="server">
                <Content>
                    <uc3:mfbSearchForm ID="mfbSearchForm1" InitialCollapseState="True" runat="server" OnQuerySubmitted="mfbSearchForm1_QuerySubmitted" OnReset="mfbSearchForm1_Reset" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acpTotals" runat="server">
                <Content>
                    <div style="float:right;">
                        <uc1:popmenu runat="server" ID="popmenu" OffsetX="-100">
                            <MenuContent>
                                <asp:RadioButtonList ID="rblTotalsMode" runat="server" OnSelectedIndexChanged="rblTotalsMode_SelectedIndexChanged" AutoPostBack="true">
                                    <asp:ListItem Text="<%$ Resources:Totals, TotalsModeFlat %>" Value="False" Selected="True"></asp:ListItem>
                                    <asp:ListItem Text="<%$ Resources:Totals, TotalsModeGrouped %>" Value="True"></asp:ListItem>
                                </asp:RadioButtonList>
                            </MenuContent>
                        </uc1:popmenu>
                    </div>
                    <uc4:mfbTotalSummary ID="mfbTotalSummary1" runat="server" Visible="false" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acpCurrency" runat="server">
                <Content>
                    <uc5:mfbCurrency ID="mfbCurrency1" runat="server" SuppressAutoRefresh="true" Visible="false" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acpAnalysis" runat="server">
                <Content>
                    <uc6:mfbChartTotals ID="mfbChartTotals1" runat="server" Visible="false" />
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acpPrint" runat="server">
                <Content>
                    <script type="text/javascript">
                        function updPrint() {
                            var ckEndorsements = $('#<% =ckEndorsements.ClientID %>')[0];
                            var ckIncludeImgs = $('#<% =ckIncludeEndorsementImages.ClientID %>')[0];
                            if (!ckEndorsements.checked)
                                ckIncludeImgs.checked = false;
                            ckIncludeImgs.disabled = !ckEndorsements.checked;
                            var ckTotals = $('#<% = ckTotals.ClientID %>')[0];
                            var ckCompactTotals = $('#<% = ckCompactTotals.ClientID %>')[0]
                            if (!ckTotals.checked)
                                ckCompactTotals.checked = false;
                            ckCompactTotals.disabled = !ckTotals.checked;
                            var sects = new Object();
                            sects["Endorsements"] = ckEndorsements.checked ? (ckIncludeImgs.checked ? "DigitalAndPhotos" : "DigitalOnly") : "None";
                            sects["IncludeCoverPage"] = $('#<% = ckIncludeCoverSheet.ClientID %>')[0].checked;
                            sects["IncludeFlights"] = true;
                            sects["IncludeTotals"] = $('#<% =ckTotals.ClientID %>')[0].checked;
                            sects["CompactTotals"] = ckCompactTotals.checked;

                            var lnkPreview = $('#<% =lnkPrintView.ClientID %>')[0];

                            var params = new Object();
                            params.szExisting = lnkPreview.href;
                            params.ps = sects;

                            var d = JSON.stringify(params);

                            $.ajax(
                                {
                                    url: '<% =ResolveUrl("~/Member/Ajax.asmx/PrintLink") %>',
                                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                                    error: function (xhr, status, error) {
                                        window.alert(xhr.responseJSON.Message);
                                    },
                                    complete: function (response) { },
                                    success: function (response) {
                                        lnkPreview.href = response.d
                                    }
                                });
                            return false;
                        }
                    </script>
                    <div style="margin-left: auto; margin-right:auto; text-align:center">
                        <div style="padding:5px; margin-left: auto; margin-right:auto; text-align:left; width: 50%">
                            <h3><%=Resources.LocalizedText.PrintViewTabFilter %></h3>
                            <table>
                                <tr>
                                    <td>
                                        <asp:CheckBox ID="ckIncludeCoverSheet" runat="server" Checked="true" onclick="updPrint();" />
                                    </td>
                                    <td>
                                        <asp:Label ID="lblCoverSheet" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeCoverSheet %>" AssociatedControlID="ckIncludeCoverSheet" />
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:CheckBox ID="ckTotals" runat="server" Checked="true" onclick="updPrint();" />
                                    </td>
                                    <td>
                                        <asp:Label ID="lblIncludeTotals" AssociatedControlID="ckTotals" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeTotals %>" />
                                        <asp:CheckBox ID="ckCompactTotals" runat="server" onclick="updPrint();" />
                                        <asp:Label ID="lblTotalsCompact" runat="server" Text="<%$ Resources:LocalizedText, PrintViewTotalsCompact %>" AssociatedControlID="ckCompactTotals" />
                                        <span class="fineprint"><% =Resources.LocalizedText.PrintViewTotalsCompactNote %></span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="vertical-align:top;">
                                        <asp:CheckBox ID="ckEndorsements" runat="server" Checked="true" onclick="updPrint();" />
                                    </td>
                                    <td>
                                        <div>
                                            <asp:Label ID="lblIncludeEndorsements" AssociatedControlID="ckEndorsements" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeEndorsements %>" />
                                            <asp:CheckBox ID="ckIncludeEndorsementImages" runat="server" Checked="true" onclick="updPrint();" />
                                            <asp:Label ID="lblIncludeWhat" AssociatedControlID="ckIncludeEndorsementImages" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeJPEGEndorsements %>" />
                                        </div>
                                        <div class="fineprint"><% =Resources.LocalizedText.PrintViewNoEmbeddedPDFsNote %></div>
                                </tr>
                            </table>
                            <ul class="nextStep">
                                <li><asp:HyperLink ID="lnkPrintView" runat="server" Target="_blank" Text="<%$ Resources:LocalizedText, OpenPrintView %>" /></li>
                            </ul>
                        </div>
                    </div>
                </Content>
            </ajaxToolkit:AccordionPane>
            <ajaxToolkit:AccordionPane ID="acpMore" runat="server">
                <Content>
                    <table style="border-spacing: 10px;">
                        <tr>
                            <td style="text-align:center;">
                                <asp:Image ID="imgDownload" ImageUrl="~/images/download.png" AlternateText="<%$ Resources:Tabs, LogbookDownload %>" runat="server" />
                            </td>
                            <td>
                                <asp:HyperLink ID="lnkDownload" Font-Bold="true" runat="server" NavigateUrl="~/Member/Download.aspx?lm=Accordion" Text="<%$ Resources:Tabs, LogbookDownload %>"></asp:HyperLink>
                            </td>
                            <td>
                                <% =Resources.LocalizedText.LogbookDownloadDescription %>
                            </td>
                        </tr>
                        <tr>
                            <td style="text-align:center;">
                                <asp:Image ID="imgImport" ImageUrl="~/images/import.png" runat="server" AlternateText="<%$ Resources:Tabs, LogbookImport %>" />
                            </td>
                            <td>
                                <asp:HyperLink ID="lnkImport" Font-Bold="true" runat="server" NavigateUrl="~/Member/Import.aspx?lm=Accordion" Text="<%$ Resources:Tabs, LogbookImport %>"></asp:HyperLink>
                            </td>
                            <td>
                                <% =Branding.ReBrand(Resources.LocalizedText.LogbookImportDescription) %>
                            </td>
                        </tr>
                        <tr>
                            <td style="text-align:center;">
                                <asp:Image ID="imgCheckFlights" runat="server" ImageUrl="~/images/CheckFlights.png" AlternateText="<%$ Resources:FlightLint, TitleCheckFlights %>" />
                            </td>
                            <td>
                                <asp:HyperLink ID="lnkCheckFlights" Font-Bold="true" runat="server" Text="<%$ Resources:FlightLint, TitleCheckFlights %>" NavigateUrl="~/Member/CheckFlights.aspx"></asp:HyperLink>
                            </td>
                            <td>
                                <% =Branding.ReBrand(Resources.FlightLint.CheckFlightsShortDescription) %>
                            </td>
                        </tr>
                        <tr>
                            <td style="text-align:center;">
                                <asp:Image ID="imgStartingTotals" ImageUrl="~/images/startingtotals.png" AlternateText="<%$ Resources:LocalizedText, StartingTotalsLink %>" runat="server" />
                            </td>
                            <td>
                                <asp:HyperLink ID="lnkStartingTotals" Font-Bold="true" NavigateUrl="~/Member/StartingTotals.aspx?lm=Accordion" runat="server" Text="<%$ Resources:LocalizedText, StartingTotalsLink %>"></asp:HyperLink>
                            </td>
                            <td>
                                <% =Branding.ReBrand(Resources.LocalizedText.LogbookStartingTotalsDescription) %>
                            </td>
                        </tr>
                        <tr>
                            <td style="text-align:center;">
                                <asp:Image ID="imgPendingFlights" ImageUrl="~/images/pendingflights.png" AlternateText="<%$ Resources:LocalizedText, PendingFlightsLink %>" runat="server" />
                            </td>
                            <td>
                                <asp:HyperLink ID="lnkPendingFlights" Font-Bold="true" NavigateUrl="~/Member/ReviewPendingFlights.aspx" runat="server" Text="<%$ Resources:LocalizedText, PendingFlightsLink %>"></asp:HyperLink>
                            </td>
                            <td>
                                <% =Branding.ReBrand(Resources.LocalizedText.LogbookPendingFlightsDescription) %>
                            </td>
                        </tr>
                    </table>
                </Content>
            </ajaxToolkit:AccordionPane>
        </Panes>
    </ajaxToolkit:Accordion>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <uc1:mfbLogbook ID="mfbLogbook1" runat="server" OnItemDeleted="mfbLogbook1_ItemDeleted" DetailsPageUrlFormatString="~/Member/FlightDetail.aspx/{0}"
         EditPageUrlFormatString="~/Member/LogbookNew.aspx/{0}" AnalysisPageUrlFormatString="~/Member/FlightDetail.aspx/{0}?tabID=Chart" SendPageTarget="~/Member/LogbookNew.aspx" />
    <asp:Panel ID="pnlWelcomeNewUser" runat="server" style="display:none;">
        <p><%=Resources.LocalizedText.WelcomeThanks %></p>
        <p><%=Resources.LocalizedText.WelcomeNextSteps %></p>
        <table style="border-spacing: 10px;">
            <tr>
                <td>
                    <asp:Image ID="imgLogbook" runat="server" ImageUrl="~/Public/tabimages/logbookTab.png" Width="24" AlternateText="<%$ Resources:LocalizedText, WelcomeEnterFlights %>"  />
                </td>
                <td><% =Resources.LocalizedText.WelcomeEnterFlights %><br />
                <% =Resources.LocalizedText.ORSeparator %></td>
            </tr>
            <tr>
                <td>
                    <asp:Image ID="imgAircraft" runat="server" ImageUrl="~/Public/tabimages/AircraftTab.png" Width="24" AlternateText="<%$ Resources:LocalizedText, WelcomeEnterAircraft %>" />
                </td>
                <td><asp:HyperLink ID="lnkAddAircraft" NavigateUrl="~/mvc/Aircraft" runat="server" Text="<%$ Resources:LocalizedText, WelcomeEnterAircraft %>" /><br />
                <% =Resources.LocalizedText.ORSeparator %></td>
            </tr>
            <tr>
                <td>
                    <asp:Image ID="imgImport2" runat="server" ImageUrl="~/images/import.png" AlternateText="<%$ Resources:LocalizedText, WelcomeImportFlights %>"  />
                </td>
                <td><asp:HyperLink ID="lnkImportWelcome" NavigateUrl="~/Member/Import.aspx" runat="server" Text="<%$ Resources:LocalizedText, WelcomeImportFlights %>" /><br />
                <% =Resources.LocalizedText.ORSeparator %></td>
            </tr>
            <tr>
                <td>
                    <asp:Image ID="imgStartingTotals2" runat="server" ImageUrl="~/images/startingtotals.png" AlternateText="<%$ Resources:LocalizedText, WelcomeSetStartingTotals %>" />
                </td>
                <td><asp:HyperLink ID="lnkSetStartingTotals" runat="server" Text="<%$ Resources:LocalizedText, WelcomeSetStartingTotals %>" NavigateUrl="~/Member/StartingTotals.aspx" /></td>
            </tr>
        </table>

        <div style="text-align:center">
        <asp:Button ID="btnClose" runat="server" Text="<%$ Resources:LocalizedText, Close %>" OnClientClick="return closeWelcome();" /></div>
    </asp:Panel>
    <script type="text/javascript">
        function closeWelcome() {
            dismissDlg("#" + "<%=pnlWelcomeNewUser.ClientID %>");
            return false;
        }
        $(function () {
            if (document.getElementById('<%=pnlWelcomeNewUser.ClientID %>'))
                showModalById('<%=pnlWelcomeNewUser.ClientID %>', '<%=Resources.LocalizedText.WelcomeHeader %>', '320');
        })
    </script>
</asp:Content>

