﻿<%@ Control Language="C#" AutoEventWireup="true" Codebehind="layoutPortrait.ascx.cs" Inherits="MyFlightbook.Printing.Layouts.LayoutPortrait" %>
<%@ Register Src="~/Controls/mfbLogbook.ascx" TagPrefix="uc1" TagName="mfbLogbook" %>
<%@ Register Src="~/Controls/mfbImageList.ascx" TagPrefix="uc1" TagName="mfbImageList" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageHeader.ascx" TagPrefix="uc1" TagName="pageHeader" %>
<%@ Register Src="~/Controls/PrintingLayouts/pageFooter.ascx" TagPrefix="uc1" TagName="pageFooter" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>

<asp:Repeater ID="rptPages" runat="server" OnItemDataBound="rptPages_ItemDataBound" EnableViewState="false">
    <ItemTemplate>
        <uc1:pageHeader runat="server" ID="pageHeader" UserName="<%# CurrentUser.UserName %>" />
        <table class="pageTable" style="width:100%">
            <thead class="printheader bordered">
                <tr>
                    <td colspan="<% =Math.Min(OptionalColumns.Count(), 4) + 10 %>"></td>
                    <th colspan="3" class="rowHeader"><% =Resources.LogbookEntry.PrintHeaderExperience %></th>
                    <th colspan="4" class="rowHeader"><% =Resources.LogbookEntry.PrintHeaderInstrumentTime %></th>
                </tr>
                <tr>
                    <th><%=Resources.LogbookEntry.PrintHeaderDate %></th>
                    <th><%=Resources.LogbookEntry.PrintHeaderModel %></th>
                    <th><%=Resources.LogbookEntry.PrintHeaderCategoryClassShort %></th>
                    <th><%=Resources.LogbookEntry.PrintHeaderAircraft %></th>
                    <th class="rowHeader" runat="server" id="optColumn1" Visible="<%# ShowOptionalColumn(0) %>"><div><%# OptionalColumnName(0) %></div></th>
                    <th class="rowHeader" runat="server" id="optColumn2" Visible="<%# ShowOptionalColumn(1) %>"><div><%# OptionalColumnName(1) %></div></th>
                    <th class="rowHeader" runat="server" id="optColumn3" Visible="<%# ShowOptionalColumn(2) %>"><div><%# OptionalColumnName(2) %></div></th>
                    <th class="rowHeader" runat="server" id="optColumn4" Visible="<%# ShowOptionalColumn(3) %>"><div><%# OptionalColumnName(3) %></div></th>
                    <th class="rowHeader"><div><%=Resources.LogbookEntry.FieldTotal %></div></th>
                    <th class="rowHeader"><div><%=Resources.LogbookEntry.FieldPIC %></div></th>
                    <th class="rowHeader" runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><div><%=Resources.LogbookEntry.FieldSIC %></div></th>
                    <th class="rowHeader" runat="server" visible="<%# CurrentUser.IsInstructor %>"><div><%=Resources.LogbookEntry.FieldCFI %></div></th>
                    <th class="rowHeader"><div><%=Resources.LogbookEntry.FieldDual %></div></th>
                    <th class="rowHeader"><div><%=Resources.LogbookEntry.FieldGroundSim %></div></th>

                    <th class="rowHeader"><div><%=Resources.LogbookEntry.FieldXCountry %></div></th>
                    <th class="rowHeader"><div><%=Resources.LogbookEntry.FieldNight %></div></th>
                    <th class="rowHeader"><div><%=Resources.LogbookEntry.PrintHeaderLandingsShort %></div></th>

                    <th class="rowHeader"><div><%=Resources.LogbookEntry.FieldIMC %></div></th>
                    <th class="rowHeader"><div><%=Resources.LogbookEntry.FieldSimIMC %></div></th>
                    <th class="rowHeader"><div><%=Resources.LogbookEntry.PrintHeaderApproachesShort %></div></th>
                    <th class="rowHeader"><div><%=Resources.LogbookEntry.FieldHold %></div></th>
                </tr>
            </thead>
            <asp:Repeater ID="rptFlight" runat="server" OnItemDataBound="rptFlight_ItemDataBound">
                <ItemTemplate>
                    <tr class="bordered" <%# ColorForFlight(Container.DataItem) %>>
                        <td class="row1"><%# ChangeMarkerForFlight(Container.DataItem) %><asp:Label Font-Bold="true" EnableViewState="false" ID="lblStaticDate" runat="server" Text='<%# ((DateTime) Eval("Date")).ToShortDateString() %>'></asp:Label></td>
                        <td class="row1"><asp:Label ID="lblModel" runat="server" Text='<%#: Eval("ModelDisplay") %>'></asp:Label></td>
                        <td class="row1"><asp:Label ID="lblCatClass" runat="server" Text='<%#: Eval("CatClassDisplay") %>'></asp:Label></td>
                        <td class="row1"><%#: Eval("TailNumOrSimDisplay") %></td>
                        <td class="numericColumn" runat="server" id="tdoptColumn1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(0) %></div></td>
                        <td class="numericColumn" runat="server" id="tdoptColumn2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(1) %></div></td>
                        <td class="numericColumn" runat="server" id="tdoptColumn3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(2) %></div></td>
                        <td class="numericColumn" runat="server" id="tdoptColumn4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnDisplayValue(3) %></div></td>
                        <td class="numericColumn"><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("PIC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn" runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><%# Eval("SIC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn" runat="server" visible="<%# CurrentUser.IsInstructor %>"><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM)%></td>

                        <td class="numericColumn"><%# Eval("CrossCountry").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("LandingDisplay") %></td>

                        <td class="numericColumn"><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM)%></td>
                        <td class="numericColumn"><%# Eval("Approaches").FormatInt() %></td>
                        <td class="numericColumn"><asp:Image ID="imgHolding" runat="server" ImageUrl="~/images/checkmark.png" style="height:10pt; width: 10pt;" GenerateEmptyAlternateText="true" Visible='<%# (bool) Eval("fHoldingProcedures") %>' /></td>
                    </tr>
                    <tr class="bordered">
                        <td colspan="<% =ColumnCount %>">
                            <span><%#: Eval("Route") %></span>
                            <span runat="server" id="divComments" style="white-space:pre-line;" dir="auto"><%# Eval("RedactedCommentWithReplacedApproaches") %></span>
                            <asp:Panel ID="pnlFlightTimes" runat="server" Visible="<%# CurrentUser.DisplayTimesByDefault %>">
                                <asp:Panel EnableViewState="false" ID="pnlEngineTime" runat="server">
                                    <%# Eval("EngineTimeDisplay") %>
                                </asp:Panel>
                                <asp:Panel EnableViewState="false" ID="pnlFlightTime" runat="server">
                                    <%# Eval("FlightTimeDisplay") %>
                                </asp:Panel>
                                <asp:Panel EnableViewState="false" ID="pnlHobbs" runat="server">
                                    <%# Eval("HobbsDisplay") %>
                                </asp:Panel>
                            </asp:Panel>
                            <div style="white-space:pre-line;"><%#: Eval("CustPropertyDisplay").ToString() %></div>
                            <uc1:mfbSignature runat="server" ID="mfbSignature" EnableViewState="false" />
                            <asp:Panel EnableViewState="false" ID="pnlFlightImages" runat="server" Visible="<%# IncludeImages %>">
                                <uc1:mfbImageList ID="mfbilFlights" runat="server" Columns="3" CanEdit="false" ImageClass="Flight" IncludeDocs="false" MaxImage="3" />
                            </asp:Panel>
                        </td>
                    </tr>
                    <tr class="spacerMargin">
                        <td colspan="<% =ColumnCount %>"></td>
                    </tr>
                    <tr class="spacer">
                        <td colspan="<% =ColumnCount %>"></td>
                    </tr>
                    <tr class="spacerMargin">
                        <td colspan="<% =ColumnCount %>"></td>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Repeater ID="rptSubtotalCollections" runat="server" EnableViewState="false" OnItemDataBound="rptSubtotalCollections_ItemDataBound">
                <ItemTemplate>
                    <tr class="subtotal">
                        <td class="noborder" colspan="2" rowspan='<%# Eval("SubtotalCount") %>'></td>
                        <td class="subtotalLabel" rowspan='<%# Eval("SubtotalCount") %>'><%# Eval("GroupTitle") %></td>
                        <asp:Repeater ID="rptSubtotals" runat="server">
                            <ItemTemplate>
                                <%# (Container.ItemIndex != 0) ? "<tr class=\"subtotal\">" : string.Empty %>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).CatClassDisplay %></td>
                                <td runat="server" id="tdoptColumnTotal1" visible="<%# ShowOptionalColumn(0) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(0, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal2" visible="<%# ShowOptionalColumn(1) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(1, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal3" visible="<%# ShowOptionalColumn(2) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(2, CurrentUser.UsesHHMM) %></div></td>
                                <td runat="server" id="tdoptColumnTotal4" visible="<%# ShowOptionalColumn(3) %>"><div><%# ((LogbookEntryDisplay) Container.DataItem).OptionalColumnTotalDisplayValue(3, CurrentUser.UsesHHMM) %></div></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).TotalFlightTime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).PIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td runat="server" visible="<%# CurrentUser.TracksSecondInCommandTime %>"><%# ((LogbookEntryDisplay) Container.DataItem).SIC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td runat="server" visible="<%# CurrentUser.IsInstructor %>"><%# ((LogbookEntryDisplay) Container.DataItem).CFI.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).Dual.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).GroundSim.FormatDecimal(CurrentUser.UsesHHMM) %></td>

                                <td><%# ((LogbookEntryDisplay) Container.DataItem).CrossCountry.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).Nighttime.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).Landings.ToString(System.Globalization.CultureInfo.CurrentCulture) %></td>

                                <td><%# ((LogbookEntryDisplay) Container.DataItem).IMC.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).SimulatedIFR.FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                <td><%# ((LogbookEntryDisplay) Container.DataItem).Approaches.ToString(System.Globalization.CultureInfo.CurrentCulture) %></td>
                                <td style="border:none"></td>
                                <%# (Container.ItemIndex != 0) ? "</tr>" : string.Empty %>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
        </table>
        <uc1:pageFooter runat="server" ID="pageFooter" ShowFooter="<%# ShowFooter %>" UserName="<%# CurrentUser.UserName %>" PageNum='<%#Eval("PageNum") %>' TotalPages='<%# Eval("TotalPages") %>' />
    </ItemTemplate>
</asp:Repeater>
