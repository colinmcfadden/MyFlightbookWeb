﻿<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Async="true" AsyncTimeout="300"
    Codebehind="Admin.aspx.cs" Inherits="MyFlightbook.Web.Admin.Member_Admin" Title="Administer MyFlightbook" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:MultiView ID="mvAdmin" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwUsers" runat="server">
        </asp:View>
        <asp:View ID="vwModels" runat="server">
        </asp:View>
        <asp:View ID="vwManufacturers" runat="server">
            <h2>
                Manufacturers</h2>
            <asp:UpdatePanel ID="UpdatePanel3" runat="server">
                <ContentTemplate>
                    <p>
                        Possible dupes:
                        <asp:SqlDataSource ID="sqlDataSourceDupeMan" runat="server" 
                            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                            SelectCommand="SELECT m.*
                FROM manufacturers m
                WHERE m.manufacturer IN (SELECT manufacturer FROM (SELECT * FROM manufacturers m2 GROUP BY manufacturer HAVING COUNT(manufacturer) &gt; 1) as dupes);">
                        </asp:SqlDataSource>
                        <asp:SqlDataSource ID="sqlDupeManstoMatch" runat="server" 
                            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                            SelectCommand="SELECT idmanufacturer,
                CAST(CONCAT(idmanufacturer, ' - ', manufacturer) AS CHAR) AS 'DisplayName'
                FROM manufacturers
                WHERE manufacturer IN
                  (SELECT manufacturer FROM manufacturers GROUP BY manufacturer HAVING count(manufacturer) &gt; 1) ORDER BY idmanufacturer;">
                        </asp:SqlDataSource>
                        <asp:SqlDataSource ID="sqlModelsToRemap" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                            SelectCommand="SELECT * FROM models WHERE idmanufacturer=?idman"
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" runat="server">
                            <SelectParameters>
                                <asp:ControlParameter ControlID="cmbManToKill" PropertyName="SelectedValue" Name="idman" />
                            </SelectParameters>
                        </asp:SqlDataSource>
                        <asp:GridView ID="gvDupeMan" runat="server" AutoGenerateColumns="False" 
                            DataSourceID="sqlDataSourceDupeMan" EnableModelValidation="True">
                            <Columns>
                                <asp:BoundField DataField="idManufacturer" HeaderText="idManufacturer" 
                                    InsertVisible="False" SortExpression="idManufacturer" />
                                <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" 
                                    SortExpression="manufacturer" />
                            </Columns>
                        </asp:GridView>
                    </p>
                    <p>
                        Keep
                        <asp:DropDownList ID="cmbManToKeep" runat="server" DataSourceID="sqlDupeManstoMatch"
                            DataTextField="DisplayName" DataValueField="idmanufacturer">
                        </asp:DropDownList>
                        And kill
                        <asp:DropDownList ID="cmbManToKill" runat="server" DataSourceID="sqlDupeManstoMatch"
                            DataTextField="DisplayName" DataValueField="idmanufacturer">
                        </asp:DropDownList>
                        (will be deleted)
                        <asp:Button ID="btnPreviewMan" runat="server" OnClick="btnPreviewDupeMans_Click" Text="Preview"
                            ValidationGroup="PreviewDupeMans" />
                        <asp:CustomValidator ID="CustomValidator2" runat="server" ValidationGroup="PreviewDupeMans"
                            ErrorMessage="These don't appear to be duplicates" 
                            OnServerValidate="ValidateDupeMans"></asp:CustomValidator>
                    </p>
                    <asp:Label ID="lblPreviewDupeMan" runat="server" Text=""></asp:Label>
                    <asp:Panel ID="pnlPreviewDupeMan" runat="server" Visible="false">
                        <asp:GridView ID="gvModelsToRemap" runat="server" 
                            DataSourceID="sqlModelsToRemap">
                        </asp:GridView>
                        <asp:Button ID="btnDeleteDupeMan" runat="server" 
                            Text="Delete Duplicate Manufacturer" OnClick="btnDeleteDupeMan_Click"
                            ValidationGroup="PreviewDupeMans" />
                    </asp:Panel>
                </ContentTemplate>
            </asp:UpdatePanel>
            <asp:Label ID="lblError" runat="server" CssClass="error"></asp:Label><br />
        </asp:View>
        <asp:View ID="vwMisc" runat="server">
            <h2>Fix duplicate properties on flights</h2><asp:SqlDataSource ID="sqlDSDupeProps" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" OnSelecting="sql_SelectingLongTimeout" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="select fp.idflight, fp.idproptype, count(fp.idProp) AS numProps, cast(group_concat(fp.idprop) as char) AS PropIDs
    from flightproperties fp
    group by fp.idflight, fp.idproptype
    having numProps &gt; 1;"></asp:SqlDataSource>
            <asp:GridView ID="gvDupeProps" runat="server" AutoGenerateColumns="False" 
                EnableModelValidation="True">
                <Columns>
                    <asp:HyperLinkField DataTextField="idflight" DataNavigateUrlFields="idflight" DataTextFormatString="{0}" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}?a=1&oldProps=1" Target="_blank" HeaderText="Flight" SortExpression="idflight" />
                    <asp:BoundField DataField="idproptype" HeaderText="idproptype" 
                        SortExpression="idproptype" />
                    <asp:BoundField DataField="numProps" HeaderText="numProps" 
                        SortExpression="numProps" />
                    <asp:BoundField DataField="PropIDs" 
                        HeaderText="PropIDs" 
                        SortExpression="PropIDs" />
                </Columns>
                <EmptyDataTemplate>
                    <p class="success">No duplicate properties found.</p></EmptyDataTemplate></asp:GridView><h2>Empty properties</h2><asp:SqlDataSource ID="sqlDSEmptyProps" runat="server" OnSelecting="sql_SelectingLongTimeout"
            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>"
            SelectCommand="SELECT * FROM flightproperties WHERE intvalue = 0 AND decvalue = 0.0 AND (datevalue IS NULL OR YEAR(datevalue) &lt; 10) AND (stringvalue = '' OR stringvalue IS NULL);" >
            </asp:SqlDataSource>
            <asp:GridView ID="gvEmptyProps" runat="server" AutoGenerateColumns="False" 
                EnableModelValidation="True">
                <Columns>
                    <asp:HyperLinkField DataTextField="idflight" DataNavigateUrlFields="idflight" DataTextFormatString="{0}" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}?a=1&oldProps=1" Target="_blank" HeaderText="Flight" SortExpression="idflight" />
                    <asp:BoundField DataField="idproptype" HeaderText="idproptype" 
                        SortExpression="idproptype" />
                </Columns>
                <EmptyDataTemplate>
                    <p class="success">No empty properties found.</p>
                </EmptyDataTemplate>
            </asp:GridView>
            <div><asp:Button ID="btnRefreshProps" runat="server" Text="Refresh empty/dupe props" OnClick="btnRefreshProps_Click" /></div>
            <h2>Invalid signatures</h2>
            <script type="text/javascript">
                function startScan() {
                    document.getElementById('<% =btnRefreshInvalidSigs.ClientID %>').click();
                }
            </script>
            <asp:Button ID="btnRefreshInvalidSigs" runat="server" OnClick="btnRefreshInvalidSigs_Click" Text="Refresh" />
            <asp:HiddenField ID="hdnSigOffset" runat="server" Value="0" />
            <p><asp:Label ID="lblSigResults" runat="server" Text=""></asp:Label></p>
            <asp:Label ID="lblSigProgress" runat="server" />
            <asp:MultiView ID="mvCheckSigs" runat="server" ActiveViewIndex="1">
                <asp:View ID="vwSigProgress" runat="server">
                    <asp:Image ID="imgProgress" runat="server" ImageUrl="~/images/ajax-loader.gif" />
                    <script type="text/javascript">
                        $(document).ready(function () { startScan(); });
                    </script>
                </asp:View>
                <asp:View ID="vwInvalidSigs" runat="server">
                    <p>Flights with signatures to fix:</p>
                    <asp:GridView ID="gvInvalidSignatures" runat="server" AutoGenerateColumns="false" OnRowCommand="gvInvalidSignatures_RowCommand">
                        <Columns>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <a href='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://{0}/logbook/member/LogbookNew.aspx/{1}?a=1", Branding.CurrentBrand.HostName, Eval("FlightID")) %>' target="_blank"><%# Eval("FlightID").ToString() %></a>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <%# Eval("User") %><br />
                                    <%# ((DateTime) Eval("Date")).ToShortDateString() %><br />
                                    Saved State: <%# Eval("CFISignatureState") %><br /><%# Eval("AdminSignatureSanityCheckState").ToString() %></ItemTemplate></asp:TemplateField><asp:TemplateField>
                                <ItemTemplate>
                                    <asp:Label ID="Label5" runat="server" Width="60px" Text="Saved:"></asp:Label><%# Eval("DecryptedFlightHash") %><br /><asp:Label ID="Label6" runat="server" Width="60px" Text="Current:"></asp:Label><%# Eval("DecryptedCurrentHash") %></ItemTemplate></asp:TemplateField><asp:TemplateField>
                                <ItemTemplate>
                                    <asp:Button ID="btnSetValidity" runat="server" Text="Fix" CommandArgument='<%# Bind("FlightID") %>' CommandName="FixValidity" /><br />
                                    <asp:Button ID="btnForceValidSig" runat="server" Text="Force Valid" CommandArgument='<%# Bind("FlightID") %>' CommandName="ForceValidity" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <p>No invalid signatures found!</p>
                        </EmptyDataTemplate>
                    </asp:GridView>
                    <p>Auto-fixed flights:</p>
                    <asp:GridView ID="gvAutoFixed" runat="server" AutoGenerateColumns="false">
                        <Columns>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <a href='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://{0}/logbook/member/LogbookNew.aspx/{1}?a=1", Branding.CurrentBrand.HostName, Eval("FlightID")) %>' target="_blank"><%# Eval("FlightID").ToString() %></a>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <%# Eval("User") %> <%# ((DateTime) Eval("Date")).ToShortDateString() %>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <p>No autofixed signatures found!</p>
                        </EmptyDataTemplate>
                    </asp:GridView>
                </asp:View>
            </asp:MultiView>
            <h2>Cache management</h2>
            <div><asp:Label ID="lblMemStats" runat="server" /></div>
            <uc1:expando runat="server" id="Expando">
                <Header>Object Summary</Header>
                <Body>
                    <asp:GridView ID="gvCacheData" runat="server" />
                </Body>
            </uc1:expando>
            <p><asp:Button ID="btnFlushCache" runat="server" Text="Flush Cache" OnClick="btnFlushCache_Click" /> <span class="fineprint">Removes all entries from the cache; will make things slow, but useful for picking up DB changes or debugging</span></p>
            <div><asp:Label ID="lblCacheFlushResults" runat="server" EnableViewState="false" /></div>
            <h2>Nightly Run</h2>
            <p><asp:Button ID="btnNightlyRun" runat="server" Text="Kick Off Nightly Run" OnClick="btnNightlyRun_Click" /> <span class="fineprint">BE CAREFUL! This can spam users.  Only click once, and only if it DIDN'T run last night.</span></p>
            <div><asp:Label ID="lblNightlyRunResult" runat="server"></asp:Label></div>
        </asp:View>
    </asp:MultiView>
    <asp:HiddenField ID="hdnActiveTab" runat="server" />
</asp:Content>
<asp:Content runat="server" ID="content3" ContentPlaceHolderID="cpMain">
    <asp:MultiView ID="mvMain" runat="server">
        <asp:View ID="vwMainUsers" runat="server">
        </asp:View>
        <asp:View ID="vwMainModels" runat="server">
            <h2>Makes/models that should be sims:</h2>
            <asp:GridView ID="gvAircraftShouldBeSims" runat="server" AutoGenerateColumns="False" CssClass="stickyHeaderTable" 
                DataSourceID="sqlSimMakes" EnableModelValidation="True">
                <Columns>
                    <asp:HyperLinkField DataNavigateUrlFormatString="~/Member/EditMake.aspx?id={0}&a=1" DataNavigateUrlFields="idmodel" DataTextFormatString="Edit" DataTextField="model" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="manufacturer" HeaderText="manufacturer"  HeaderStyle-CssClass="headerBase gvhDefault gvhCentered"
                        SortExpression="manufacturer" />
                    <asp:BoundField DataField="model" HeaderText="model" SortExpression="model"  HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="typename" HeaderText="typename" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="typename" />
                    <asp:BoundField DataField="modelname" HeaderText="modelname" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="modelname" />
                    <asp:BoundField DataField="idcategoryclass" HeaderText="idcategoryclass" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="idcategoryclass" />
                    <asp:BoundField DataField="fSimOnly" HeaderText="fSimOnly" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="fSimOnly" />
                </Columns>
                <EmptyDataTemplate>
                    <div class="success">(No suspect makes/models found)</div>
                </EmptyDataTemplate>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlSimMakes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT man.manufacturer, m.*
FROM models m INNER JOIN manufacturers man ON m.idmanufacturer=man.idManufacturer
WHERE man.DefaultSim &lt;&gt; 0 AND m.fSimOnly= 0"></asp:SqlDataSource>
            <h2>Orphaned makes/models (i.e., no airplanes using them):</h2>
            <asp:GridView ID="gvOrphanMakes" runat="server"
                AutoGenerateDeleteButton="True" DataKeyNames="idmodel,idmanufacturer" CellPadding="3" CssClass="stickyHeaderTable" 
                DataSourceID="sqlDSOrphanMakes" EnableModelValidation="True" OnRowDeleting="gvOrphanMakes_RowDeleting"
                AutoGenerateColumns="False">
                <Columns>
                    <asp:BoundField DataField="NumberAircraft" HeaderText="NumberAircraft" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        ReadOnly="True" SortExpression="NumberAircraft" />
                    <asp:BoundField DataField="manufacturer" HeaderText="Manufacturer" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="manufacturer" />
                    <asp:BoundField DataField="model" HeaderText="model" SortExpression="Name" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="typename" HeaderText="Type Rating" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="typename" />
                    <asp:BoundField DataField="idmodel" HeaderText="idmodel" ReadOnly="True" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="idmodel" />
                    <asp:BoundField DataField="idcategoryclass" HeaderText="Cat/Class ID" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="idcategoryclass" />
                    <asp:BoundField DataField="idmanufacturer" HeaderText="Mfr. ID" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        ReadOnly="True" SortExpression="idmanufacturer" />
                    <asp:BoundField DataField="modelname" HeaderText="Mktg Name" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="modelname" />
                    <asp:BoundField DataField="fcomplex" HeaderText="Complex" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="fcomplex" />
                    <asp:BoundField DataField="fHighPerf" HeaderText="High Perf" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="fHighPerf" />
                    <asp:BoundField DataField="fTailwheel" HeaderText="Tailwheel" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="fTailwheel" />
                    <asp:BoundField DataField="fConstantProp" HeaderText="CS Prop" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="fConstantProp" />
                    <asp:BoundField DataField="fTurbine" HeaderText="Turbine" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="fTurbine" />
                    <asp:BoundField DataField="fRetract" HeaderText="Retract" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="fRetract" />
                    <asp:BoundField DataField="fCowlFlaps" HeaderText="Flaps" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" 
                        SortExpression="fCowlFlaps" />
                    <asp:BoundField DataField="fGlassOnly" HeaderText="fGlassOnly" SortExpression="Glass" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="fSimOnly" HeaderText="fSimOnly" SortExpression="SimOnly" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlDSOrphanMakes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT COUNT(ac.idaircraft) AS NumberAircraft, man.manufacturer, m.*
        FROM models m
        LEFT JOIN aircraft ac ON m.idmodel=ac.idmodel
        INNER JOIN manufacturers man ON m.idmanufacturer=man.idmanufacturer
        GROUP BY m.idmodel
        HAVING NumberAircraft=0"
                DeleteCommand="DELETE FROM models WHERE idmodel=?idmodel"
                    >
                    <DeleteParameters>
                        <asp:Parameter Type="Int32" Direction="Input" Name="idmodel" />
                    </DeleteParameters>
            </asp:SqlDataSource>    
            <br />
            <h2>Review Type Designations</h2>
            <asp:Button ID="btnRefreshReview" runat="server" Text="Refresh" OnClick="btnRefreshReview_Click" />
            <asp:GridView ID="gvReviewTypes" runat="server" DataKeyNames="idmodel" AutoGenerateColumns="false" AutoGenerateEditButton="True" CssClass="stickyHeaderTable">
                <Columns>
                    <asp:HyperLinkField HeaderText="ModelID" Target="_blank" DataNavigateUrlFields="idmodel" DataTextField="idmodel" DataNavigateUrlFormatString="~/Member/EditMake.aspx?id={0}" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="idmodel" HeaderText="modelid" Visible="false" ReadOnly="true" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="catclass" HeaderText="catclass" ReadOnly="true" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="manufacturer" ReadOnly="true" HeaderText="Manufacturer" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="model" ReadOnly="true" HeaderText="Model Name" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="typename" HeaderText="Type name" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlDSReviewTypes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="select m.idmodel, man.manufacturer, m.model, m.typename, cc.catclass
from models m
inner join manufacturers man on m.idmanufacturer=man.idmanufacturer
inner join categoryclass cc on m.idcategoryclass=cc.idcatclass
where m.typename &lt;&gt; ''
order by cc.idcatclass ASC, man.manufacturer asc, m.model asc, m.typename asc;"
                UpdateCommand="UPDATE Models SET typename=?typename WHERE idmodel=?idmodel">
                <UpdateParameters>
                        <asp:Parameter Type="Int32" Direction="Input" Name="idmodel" />
                        <asp:Parameter Type="String" Direction="Input" Name="typename" />
                </UpdateParameters>
            </asp:SqlDataSource>  
            <h2>Models that are potential dupes:</h2><asp:SqlDataSource ID="SqlDataSourceDupeModels" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" >
            </asp:SqlDataSource>
            <div>
                <p><asp:CheckBox ID="ckExcludeSims" runat="server" Text="Exclude Sims (i.e., real aircraft only)" AutoPostBack="true" OnCheckedChanged="ckExcludeSims_CheckedChanged" /></p>
                <p>
                    Keep <asp:DropDownList ID="cmbModelToMergeInto" runat="server" DataSourceID="SqlDataSourceDupeModels"
                        DataTextField="DisplayName" DataValueField="idmodel">
                    </asp:DropDownList>
                    And kill <asp:DropDownList ID="cmbModelToDelete" runat="server" DataSourceID="SqlDataSourceDupeModels"
                        DataTextField="DisplayName" DataValueField="idmodel">
                    </asp:DropDownList>
                    (will be deleted) <asp:Button ID="btnPreview" runat="server" OnClick="btnPreview_Click" Text="Preview"
                        ValidationGroup="PreviewDupes" />
                    <asp:CustomValidator ID="CustomValidator1" runat="server" ValidationGroup="PreviewDupes"
                        ErrorMessage="These don't appear to be duplicates" OnServerValidate="CustomValidator1_ServerValidate"></asp:CustomValidator></p>
                    <div><asp:Label ID="lblPreview" runat="server" style="white-space:pre-line" /></div>
                <asp:Panel ID="pnlPreview" runat="server" Visible="false">
                    <asp:GridView ID="gvAirplanesToRemap" runat="server" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
                        <Columns>
                            <asp:BoundField DataField="AircraftID" HeaderText="Aircraft ID" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                            <asp:BoundField DataField="ModelID" HeaderText="Model ID" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                            <asp:BoundField DataField="TailNumber" HeaderText="Tail Number" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                            <asp:BoundField DataField="DisplayTailnumberWithModel" HeaderText="(Full Tail)" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                            <asp:TemplateField HeaderText="InstanceType" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered">
                                <ItemTemplate>
                                    <%# AircraftInstance.ShortNameForInstanceType((AircraftInstanceTypes) Eval("InstanceTypeID")) %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="Version" HeaderText="Version" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                        </Columns>
                    </asp:GridView>
                    <asp:Button ID="btnDeleteDupeMake" runat="server" Text="Delete Duplicate Make" OnClick="btnDeleteDupeMake_Click"
                        ValidationGroup="PreviewDupes" />
                    <br />
                </asp:Panel>
            </div>
            <div>&nbsp;</div>
            <asp:GridView ID="gvDupeModels" DataSourceID="SqlDataSourceDupeModels" runat="server" EnableModelValidation="True" AutoGenerateColumns="false" CssClass="stickyHeaderTable">
                <Columns>
                    <asp:HyperLinkField DataNavigateUrlFields="idmodel" DataNavigateUrlFormatString="~/Member/EditMake.aspx?id={0}" HeaderText="Model" DataTextField="idmodel" DataTextFormatString="{0}" Target="_blank"  HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:HyperLinkField DataNavigateUrlFields="idmodel" DataNavigateUrlFormatString="~/Member/Aircraft.aspx?a=1&m={0}" Text="View Aircraft" Target="_blank" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="Manufacturer" HeaderText="Manufacturer" SortExpression="Manufacturer" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="DisplayName" HeaderText="DisplayName" SortExpression="DisplayName" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="Family" HeaderText="Family" SortExpression="FamilyName" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:BoundField DataField="CatClass" HeaderText="CatClass" SortExpression="CatClass" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                    <asp:TemplateField HeaderText="Model Attributes"  HeaderStyle-CssClass="headerBase gvhDefault gvhCentered">
                        <ItemTemplate>
                            <%# Convert.ToBoolean(Eval("fComplex")) ? "Complex " : "" %>
                            <%# Convert.ToBoolean(Eval("fHighPerf")) ? "High Perf " : (Convert.ToBoolean(Eval("f200HP")) ? "200hp" : "") %>
                            <%# Convert.ToBoolean(Eval("fTailWheel")) ? "Tailwheel " : "" %>
                            <%# Convert.ToBoolean(Eval("fConstantProp")) ? "Constant Prop " : "" %>
                            <%# Convert.ToBoolean(Eval("fTurbine")) ? "Turbine " : "" %>
                            <%# Convert.ToBoolean(Eval("fRetract")) ? "Retract " : "" %>
                            <%# Convert.ToBoolean(Eval("fCowlFlaps")) ? "Flaps" : "" %>
                            <%# Eval("ArmyMissionDesignSeries").ToString().Length > 0 ? "AMS = " + Eval("ArmyMissionDesignSeries").ToString() : "" %>
                            <%# Convert.ToBoolean(Eval("fSimOnly")) ? "Sim Only " : "" %>
                            <%# Convert.ToBoolean(Eval("fGlassOnly")) ? "Glass Only " : "" %>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </asp:View>
        <asp:View ID="vwMainManufacturers" runat="server">
            <h2>Existing Manufacturers</h2><asp:UpdatePanel runat="server" ID="updatePanelManufacturers">
                <ContentTemplate>
                    <asp:Panel ID="Panel1" runat="server" DefaultButton="btnAdd">
                        Add a new manufacturer: <asp:TextBox ID="txtNewManufacturer" runat="server"></asp:TextBox><asp:Button ID="btnAdd" runat="server" OnClick="btnManAdd_Click" Text="Add" />
                    </asp:Panel>
                    <br />
                    <asp:GridView ID="gvManufacturers" EnableModelValidation="True" runat="server" OnRowDeleting="ManRowDeleting" OnRowUpdating="ManRowUpdating"
                        AllowSorting="True" AutoGenerateEditButton="True" AutoGenerateDeleteButton="True" OnRowDataBound="gvManufacturers_RowDataBound" 
                        DataSourceID="sqlDSManufacturers" DataKeyNames="idManufacturer" BorderStyle="None" CssClass="stickyHeaderTable" 
                        CellPadding="3" AutoGenerateColumns="False">
                            <Columns>
                                <asp:BoundField DataField="idManufacturer" HeaderText="idManufacturer" SortExpression="idManufacturer" ReadOnly="true" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                                <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" SortExpression="manufacturer" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                                <asp:TemplateField HeaderText="Restriction" SortExpression="DefaultSim">
                                    <ItemTemplate>
                                        <%# ((AllowedAircraftTypes) ((UInt32)Eval("DefaultSim"))).ToString() %>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:RadioButtonList ID="rblDefaultSim" runat="server">
                                            <asp:ListItem Text="No Restrictions" Value="0"></asp:ListItem>
                                            <asp:ListItem Text="Sim Only" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Sim or Generic, but not real" Value="2"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </EditItemTemplate>
                                    <HeaderStyle CssClass="headerBase gvhDefault gvhCentered" />
                                </asp:TemplateField>
                                <asp:BoundField DataField="Number of Models" ReadOnly="True" HeaderText="Number of models" SortExpression="Number of Models" HeaderStyle-CssClass="headerBase gvhDefault gvhCentered" />
                            </Columns>
                            <SelectedRowStyle BackColor="#E0E0E0" />
                    </asp:GridView>
                    <asp:SqlDataSource ID="sqlDSManufacturers" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                        SelectCommand="SELECT man.*, COUNT(m.idmodel) AS 'Number of Models'
            FROM manufacturers man
            LEFT JOIN models m ON m.idmanufacturer=man.idmanufacturer
            GROUP BY man.idmanufacturer
            ORDER BY manufacturer ASC"
                        UpdateCommand="UPDATE manufacturers SET Manufacturer=?manufacturer, DefaultSim=?DefaultSim WHERE idManufacturer=?idManufacturer"
                        DeleteCommand="DELETE FROM manufacturers WHERE idManufacturer=?idManufacturer"
                        >
                        <UpdateParameters>
                            <asp:Parameter Name="manufacturer" Type="String" Size="50"  Direction="Input" />
                            <asp:Parameter Name="DefaultSim" Type="Int32" Direction="InputOutput" />
                            <asp:Parameter Name="idManufacturer" Type="Int32" Direction="Input" />
                        </UpdateParameters>
                        <DeleteParameters>
                            <asp:Parameter Name="idManufacturer" Type="Int32" Direction="Input" />
                        </DeleteParameters>
                    </asp:SqlDataSource>
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="vwMainMisc" runat="server">
        </asp:View>
    </asp:MultiView>
</asp:Content>