﻿using MyFlightbook.Airports;
using MyFlightbook.Image;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    /// <summary>
    /// Base class to reduce class coupling in Member_FlightDetail
    /// </summary>
    public partial class FlightDetailBase : Page
    {
        protected const double DataCropRange = 800.0;

        protected enum DownloadFormat { Original, CSV, KML, GPX };

        protected enum DetailsTab { Flight, Aircraft, Chart, Data, Download }

        #region some properties that don't rely on page controls
        private const string szKeyVSRestriction = "viewstateRestrictionKey";
        protected FlightQuery Restriction
        {
            get { return (FlightQuery)ViewState[szKeyVSRestriction]; }
            set { ViewState[szKeyVSRestriction] = value; }
        }

        protected string LinkForFlight(int idFlight)
        {
            return String.Format(CultureInfo.InvariantCulture, "~/Member/FlightDetail.aspx/{0}{1}", idFlight, Restriction == null || Restriction.IsDefault ? string.Empty : "?fq=" + Restriction.ToBase64CompressedJSONString());
        }

        private Profile m_pfUser;

        protected Profile Viewer
        {
            get { return m_pfUser ?? (m_pfUser = Profile.GetUser(Page.User.Identity.Name)); }
        }

        /// <summary>
        /// Distance to display for the flight
        /// </summary>
        protected string DistanceDisplay { get; set; }

        /// <summary>
        /// Indicates that the parsed flight data has lat/lon information.
        /// </summary>
        protected bool HasLatLong { get; set; }
        #endregion

        #region Mapping
        private const string szkeyVSAirportListResult = "szkeyVSALR";
        protected ListsFromRoutesResults RoutesList(string szRoute)
        {
            if (szRoute == null)
                throw new ArgumentNullException(nameof(szRoute));

            ListsFromRoutesResults lfrr = (ListsFromRoutesResults)ViewState[szkeyVSAirportListResult];
            if (lfrr == null)
                ViewState[szkeyVSAirportListResult] = lfrr = AirportList.ListsFromRoutes(szRoute);
            return lfrr;
        }

        /// <summary>
        /// Sets up the specified map manager
        /// </summary>
        /// <param name="mapMgr"></param>
        /// <param name="szRoute"></param>
        /// <param name="PathLatLongArrayID"></param>
        /// <param name="fd">The flight data containing the map.</param>
        /// <returns>True if the data has latlong info and more than one row of data (i.e., is dynamic and can be downloaded</returns>
        protected bool SetUpMapManager(FlightData fd, Controls_mfbGoogleMapMgr mapMgr, string szRoute, string PathLatLongArrayID)
        {
            if (mapMgr == null)
                throw new ArgumentNullException(nameof(mapMgr));
            if (szRoute == null)
                throw new ArgumentNullException(szRoute);
            if (fd == null)
                throw new ArgumentNullException(nameof(fd));

            mapMgr.Map.Airports = RoutesList(szRoute).Result;
            mapMgr.ShowMarkers = true;
            mapMgr.Map.Options.PathVarName = PathLatLongArrayID;
            mapMgr.Map.Path = fd.GetPath();
            bool result = fd.HasLatLongInfo && fd.Data.Rows.Count > 1;
            mapMgr.Mode = result ? Mapping.GMap_Mode.Dynamic : Mapping.GMap_Mode.Static;
            return result;
        }
        #endregion

        #region Data types
        protected static string FormatNameForTelemetry(LogbookEntryCore led)
        {
            if (led == null)
                throw new ArgumentNullException(nameof(led));

            return DataSourceType.DataSourceTypeFromFileType(led.Telemetry.TelemetryType).DisplayName;
        }

        protected static bool CanSpecifyUnitsForTelemetry(LogbookEntryCore led)
        {
            if (led == null)
                throw new ArgumentNullException(nameof(led));
            switch (led.Telemetry.TelemetryType)
            {
                case DataSourceType.FileType.GPX:
                case DataSourceType.FileType.KML:
                case DataSourceType.FileType.NMEA:
                case DataSourceType.FileType.IGC:
                    return false;
                default:
                    return true;
            }
        }

        protected static double TryParse(string sz, double defValue)
        {
            if (!double.TryParse(sz, NumberStyles.Float | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double val))
                val = defValue;

            return val;
        }
        #endregion

        #region databinding various stuff in the form
        protected static void BindImages(Control c, LogbookEntry le, Controls_mfbGoogleMapMgr mapMgr)
        {
            if (!(c is Controls_mfbImageList mfbilFlight))
                throw new ArgumentNullException(nameof(c));
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            if (mapMgr == null)
                throw new ArgumentNullException(nameof(mapMgr));

            mfbilFlight.Key = le.FlightID.ToString(CultureInfo.InvariantCulture);
            mfbilFlight.Refresh();
            mapMgr.Map.Images = mfbilFlight.Images.ImageArray;
        }

        protected static void BindVideoEntries(Control c, LogbookEntry le)
        {
            if (!(c is Controls_mfbVideoEntry ve))
                throw new ArgumentNullException(nameof(c));
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            ve.Videos.Clear();
            foreach (VideoRef vr in le.Videos)
                ve.Videos.Add(vr);
        }

        protected void BindAirportServices(Control c, Controls_mfbGoogleMapMgr mapMgr, string szRoute)
        {
            if (!(c is Controls_mfbAirportServices aptsvc))
                throw new ArgumentNullException(nameof(c));
            if (mapMgr == null)
                throw new ArgumentNullException(nameof(mapMgr));
            if (szRoute == null)
                throw new ArgumentNullException(nameof(szRoute));

            aptsvc.GoogleMapID = mapMgr.MapID;
            aptsvc.AddZoomLink = (mapMgr.Mode == MyFlightbook.Mapping.GMap_Mode.Dynamic);
            aptsvc.SetAirports(RoutesList(szRoute).MasterList.GetNormalizedAirports());
        }

        protected static void BindSignature(Control c, LogbookEntryDisplay le)
        {
            if (!(c is Controls_mfbSignature sig))
                throw new ArgumentNullException(nameof(c));
            sig.Flight = le ?? throw new ArgumentNullException(nameof(le));
        }

        protected static void BindBadges(Profile pf, int idFlight, Control c)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));
            if (!(c is Repeater rptBadges))
                throw new ArgumentNullException(nameof(c));

            IEnumerable<MyFlightbook.Achievements.Badge> cached = pf.CachedBadges;
            List<MyFlightbook.Achievements.Badge> badges = (cached == null ? null : new List<MyFlightbook.Achievements.Badge>(cached));
            if (badges != null)
            {
                rptBadges.DataSource = MyFlightbook.Achievements.BadgeSet.BadgeSetsFromBadges(badges.FindAll(b => b.IDFlightEarned == idFlight));
                rptBadges.DataBind();
            }
        }
        #endregion

        #region Download
        protected void DownloadData(LogbookEntry le, FlightData m_fd, int altUnits, int speedUnits, int format)
        {
            if (m_fd == null)
                throw new ArgumentNullException(nameof(m_fd));
            if (le == null)
                throw new ArgumentNullException(nameof(le));

            m_fd.AltitudeUnits = (FlightData.AltitudeUnitTypes)altUnits;
            m_fd.SpeedUnits = (FlightData.SpeedUnitTypes)speedUnits;
            m_fd.FlightID = le.FlightID;

            DataSourceType dst = null;
            Action writeData = null;

            switch ((DownloadFormat)format)
            {
                case DownloadFormat.Original:
                    string szData = le.FlightData;
                    dst = DataSourceType.BestGuessTypeFromText(szData);
                    writeData = () => { Response.Write(szData); };
                    break;
                case DownloadFormat.CSV:
                    dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.CSV);
                    writeData = () => { MyFlightbook.CSV.CsvWriter.WriteToStream(Response.Output, m_fd.Data, true, true); };
                    break;
                case DownloadFormat.KML:
                    dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
                    writeData = () => { m_fd.WriteKMLData(Response.OutputStream); };
                    break;
                case DownloadFormat.GPX:
                    dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.GPX);
                    writeData = () => { m_fd.WriteGPXData(Response.OutputStream); };
                    break;
            }

            if (dst != null && writeData != null)
            {
                Response.Clear();
                Response.ContentType = dst.Mimetype;
                Response.AddHeader("Content-Disposition", String.Format(CultureInfo.CurrentCulture, "attachment;filename=FlightData{0}.{1}", m_fd.FlightID, dst.DefaultExtension));
                writeData();
                Response.End();
            }
        }
        #endregion

        #region CloudAhoy
        protected static async System.Threading.Tasks.Task<bool> PushToCloudahoy(string szUser, LogbookEntry le, FlightData fd, int altUnits, int speedUnits, bool fSandbox)
        {
            if (fd == null)
                throw new ArgumentNullException(nameof(fd));

            fd.AltitudeUnits = (FlightData.AltitudeUnitTypes)altUnits;
            fd.SpeedUnits = (FlightData.SpeedUnitTypes)speedUnits;

            return await MyFlightbook.OAuth.CloudAhoy.CloudAhoyClient.PushCloudAhoyFlight(szUser, le, fd, fSandbox).ConfigureAwait(false);
        }
        #endregion

        protected static void BindAircraftImages(Control c)
        {
            if (!(c is Controls_mfbHoverImageList hil))
                throw new ArgumentNullException(nameof(c));
            hil.Refresh();
        }

        /// <summary>
        /// Checks to see if the viewer is an instructor of the owner of the flight; throws an exception if not.
        /// </summary>
        /// <param name="szFlightOwner">The owner of the flight</param>
        protected static void CheckCanViewFlight(string szFlightOwner, string szViewer)
        {
            if (szFlightOwner == null)
                throw new ArgumentNullException(nameof(szFlightOwner));
            if (szViewer == null)
                throw new ArgumentNullException(nameof(szViewer));

            MyFlightbook.Instruction.CFIStudentMap sm = new MyFlightbook.Instruction.CFIStudentMap(szViewer);
            InstructorStudent student = MyFlightbook.Instruction.CFIStudentMap.GetInstructorStudent(sm.Students, szFlightOwner);
            if (student == null || !student.CanViewLogbook)
                throw new MyFlightbookException(Resources.SignOff.ViewStudentLogbookUnauthorized);
        }

        #region Page Initialization
        protected void InitPassedRestriction()
        {
            string szFQParam = util.GetStringParam(Request, "fq");
            if (!String.IsNullOrEmpty(szFQParam))
            {
                try
                {
                    Restriction = FlightQuery.FromBase64CompressedJSON(szFQParam);
                    Restriction.Refresh();
                }
                catch (Exception ex) when (ex is ArgumentNullException || ex is FormatException || ex is Newtonsoft.Json.JsonSerializationException || ex is Newtonsoft.Json.JsonException) { }
            }
        }

        protected int InitRequestedFlightID()
        {
            return (Request.PathInfo.Length > 0 && Int32.TryParse(Request.PathInfo.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int idflight)) ? idflight : util.GetIntParam(Request, "id", LogbookEntryCore.idFlightNone);
        }
        #endregion
    }

    public partial class FlightDetail : FlightDetailBase { 
        const string PathLatLongArrayID = "rgll";

        #region properties
        protected DetailsTab SelectedTab
        {
            get { return Enum.TryParse(hdnSelectedTab.Value, out DetailsTab result) ? result : DetailsTab.Flight; }
            set { hdnSelectedTab.Value = value.ToString(); }
        }

        protected string SelectedTabProxy
        {
            get
            {
                switch (SelectedTab)
                {
                    default:
                    case DetailsTab.Flight:
                        return "apcFlight";
                    case DetailsTab.Aircraft:
                        return "apcAircraft";
                    case DetailsTab.Chart:
                        return apcChart.ClientID;
                    case DetailsTab.Data:
                        return apcRaw.ClientID;
                    case DetailsTab.Download:
                        return apcDownload.ClientID;
                }
            }
        }

        protected string EditPath(int idFlight)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}/{1}", TargetPage, idFlight);
        }

        /// <summary>
        /// The URL to which edited flights should go
        /// </summary>
        public string TargetPage { get; set; } = "~/Member/LogbookNew.aspx";

        protected int CurrentFlightID
        {
            get { return String.IsNullOrEmpty(hdnFlightID.Value) ? 0 : Convert.ToInt32(hdnFlightID.Value, CultureInfo.CurrentCulture); }
            set { hdnFlightID.Value = value.ToString(CultureInfo.CurrentCulture); }
        }

        private string KeyCacheFlight
        {
            get { return "CurFlight" + CurrentFlightID.ToString(CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Returns the current flight.
        /// </summary>
        protected LogbookEntryDisplay CurrentFlight
        {
            get
            {
                if (ViewState[KeyCacheFlight] == null)
                    ViewState[KeyCacheFlight] = LoadFlight(CurrentFlightID);
                return (LogbookEntryDisplay)ViewState[KeyCacheFlight];
            }
            set { ViewState[KeyCacheFlight] = value; }
        }

        /// <summary>
        /// Returns the current flight data.
        /// </summary>
        protected TelemetryDataTable DataTableForFlightData(FlightData fd)
        {
            if (fd == null)
                return null;

            LogbookEntryDisplay led = CurrentFlight;

            if (fd.NeedsComputing)
            {
                if (!fd.ParseFlightData(led) && (lblErr.Text = fd.ErrorString.Replace("\r\n", "<br />")).Length > 0)
                    pnlErrors.Visible = true;
            }
            HasLatLong = fd.HasLatLongInfo; // remember this status for when we don't have the flight data object directly.
            return fd.Data;
        }
        #endregion

        #region Flight selection
        protected void lnkPreviousFlight_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(hdnPrevID.Value))
                Response.Redirect(LinkForFlight(Convert.ToInt32(hdnPrevID.Value, CultureInfo.InvariantCulture)));
        }

        protected void lnkNextFlight_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(hdnNextID.Value))
                Response.Redirect(LinkForFlight(Convert.ToInt32(hdnNextID.Value, CultureInfo.InvariantCulture)));
        }

        protected void UpdateNextPrevious()
        {
            // Before we go, set the next/previous links
            List<int> lstFlights = new List<int>(LogbookEntry.FlightIDsForUser(CurrentFlight.User, Restriction));
            int index = lstFlights.IndexOf(CurrentFlightID);

            if (lnkNextFlight.Visible = (index > 0))
                hdnNextID.Value = lstFlights[index - 1].ToString(CultureInfo.InvariantCulture);

            if (lnkPreviousFlight.Visible = (index < lstFlights.Count - 1))
                hdnPrevID.Value = lstFlights[index + 1].ToString(CultureInfo.InvariantCulture);
        }

        private LogbookEntryDisplay LoadFlight(int idFlight)
        {
            bool fIsAdmin = (util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanSupport);

            // Check to see if the requested flight belongs to the current user, or if they're authorized.
            // It's an extra database hit (or more, if viewing a student flight), but will let us figure out next/previous
            string szFlightOwner = LogbookEntry.OwnerForFlight(idFlight);
            if (String.IsNullOrEmpty(szFlightOwner))
                throw new MyFlightbookException(Resources.LogbookEntry.errNoSuchFlight);

            // Check that you own the flight, or are admin.  If not either of these, check to see if you are authorized
            if (String.Compare(szFlightOwner, Page.User.Identity.Name, StringComparison.OrdinalIgnoreCase) != 0 && !fIsAdmin)
            {
                // check for authorized by student
                CheckCanViewFlight(szFlightOwner, Page.User.Identity.Name);

                // At this point, we're viewing a student's flight.  Change the return link.
                mvReturn.SetActiveView(vwReturnStudent);
                lnkReturnStudent.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/StudentLogbook.aspx?student={0}", szFlightOwner);
                lnkReturnStudent.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.ReturnToStudent, MyFlightbook.Profile.GetUser(szFlightOwner).UserFullName);
                popmenu.Visible = false;
            }

            // If we're here, we're authorized
            LogbookEntryDisplay led = new LogbookEntryDisplay(idFlight, szFlightOwner, LogbookEntry.LoadTelemetryOption.LoadAll);
            if (!led.HasFlightData)
                led.FlightData = string.Empty;

            if (String.IsNullOrEmpty(led.FlightData))
                apcChart.Visible = apcDownload.Visible = apcRaw.Visible = false;

            lblFlightDate.Text = led.Date.ToShortDateString();
            lblFlightAircraft.Text = HttpUtility.HtmlEncode(led.TailNumDisplay) ?? string.Empty;
            lblCatClass.Text = String.Format(CultureInfo.CurrentCulture, "({0})", HttpUtility.HtmlEncode(led.CatClassDisplay));
            lblCatClass.CssClass = led.IsOverridden ? "ExceptionData" : string.Empty;
            mfbTTCatClass.Visible = led.IsOverridden;
            litDesc.Text = led.CommentWithReplacedApproaches;
            lblRoute.Text = HttpUtility.HtmlEncode(led.Route.ToUpper(CultureInfo.CurrentCulture));

            Page.Title = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.FlightDetailsTitle, led.Date);

            return led;
        }

        #region Query management
        protected void UpdateRestriction()
        {
            if (Restriction != null && !Restriction.IsDefault)
            {
                pnlFilter.Visible = true;
                mfbQueryDescriptor1.DataSource = Restriction;
                mfbQueryDescriptor1.DataBind();
            }
            else
                pnlFilter.Visible = false;
            UpdateNextPrevious();
        }

        protected void mfbQueryDescriptor1_QueryUpdated(object sender, FilterItemClickedEventArgs fic)
        {
            if (fic == null)
                throw new ArgumentNullException(nameof(fic));
            Restriction.ClearRestriction(fic.FilterItem);
            UpdateRestriction();
        }
        #endregion
        #endregion

        protected bool DoDirectDownload()
        {
            // for debugging, have a download option that skips all the rest
            if (util.GetIntParam(Request, "d", 0) != 0 && !String.IsNullOrEmpty(CurrentFlight.FlightData))
            {
                Response.DownloadToFile(CurrentFlight.FlightData, "application/octet-stream", String.Format(CultureInfo.InvariantCulture, "Data{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-"), "csv");
                return true;
            }
            return false;
        }

        protected void SetUpMaps(FlightData fd)
        {
            // Set up any maps.
            cmbFormat.Items[(int)DownloadFormat.KML].Enabled = cmbFormat.Items[(int)DownloadFormat.GPX].Enabled = pnlMapControls.Visible = SetUpMapManager(fd, mfbGoogleMapManager1, CurrentFlight.Route, PathLatLongArrayID);
            lnkZoomToFit.NavigateUrl = mfbGoogleMapManager1.ZoomToFitScript;
        }

        protected void SetUpDownload(LogbookEntryBase led)
        {
            if (led == null)
                throw new ArgumentNullException(nameof(led));
            if (Viewer.CloudAhoyToken == null || Viewer.CloudAhoyToken.AccessToken == null)
                lnkSendCloudAhoy.Visible = false;

            lblOriginalFormat.Text = FormatNameForTelemetry(led);

            // allow selection of units if units are not implicitly known.
            cmbAltUnits.Enabled = cmbSpeedUnits.Enabled = CanSpecifyUnitsForTelemetry(led);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.tabLogbook;
            Page.ClientScript.RegisterClientScriptInclude("accordionExtender", ResolveClientUrl("~/public/Scripts/accordionproxy.js"));

            using (FlightData fd = new FlightData())
            {
                if (!IsPostBack)
                {
                    SelectedTab = Enum.TryParse(util.GetStringParam(Request, "tabID"), out DetailsTab dtRequested) ? dtRequested : DetailsTab.Flight;

                    try
                    {
                        CurrentFlightID = InitRequestedFlightID();
                        if (CurrentFlightID == LogbookEntryCore.idFlightNone)
                            throw new MyFlightbookException("No valid ID passed");

                        InitPassedRestriction();

                        LogbookEntryDisplay led = CurrentFlight = LoadFlight(CurrentFlightID);
                        UpdateRestriction();

                        SetUpDownload(led);

                        // shouldn't happen but sometimes does: GetUserAircraftByID returns null.  Not quite sure why.
                        Aircraft ac = (new UserAircraft(led.User).GetUserAircraftByID(led.AircraftID)) ?? new Aircraft(led.AircraftID);
                        fmvAircraft.DataSource = new Aircraft[] { ac };
                        fmvAircraft.DataBind();

                        if (String.IsNullOrEmpty(CurrentFlight.FlightData) && SelectedTab != DetailsTab.Aircraft && SelectedTab != DetailsTab.Flight)
                            SelectedTab = DetailsTab.Flight;
                    }
                    catch (MyFlightbookException ex)
                    {
                        lblPageErr.Text = ex.Message;
                        mfbGoogleMapManager1.Visible = pnlMap.Visible = pnlAccordionMenuContainer.Visible = pnlFlightDesc.Visible = false;
                        return;
                    }

                    if (DoDirectDownload())
                        return;
                }

                if (Restriction != null && !Restriction.IsDefault)
                    mfbFlightContextMenu.EditTargetFormatString = mfbFlightContextMenu.EditTargetFormatString + "?fq=" + Restriction.ToBase64CompressedJSONString();
                mfbFlightContextMenu.Flight = CurrentFlight;

                if (fd.NeedsComputing)
                {
                    if (!fd.ParseFlightData(CurrentFlight) && (lblErr.Text = fd.ErrorString.Replace("\r\n", "<br />")).Length > 0)
                        pnlErrors.Visible = true;
                }
                HasLatLong = fd.HasLatLongInfo; // remember this status for when we don't have the flight data object directly.                cmbAltUnits.SelectedValue = ((int)fd.AltitudeUnits).ToString(CultureInfo.InvariantCulture);
                cmbSpeedUnits.SelectedValue = ((int)fd.SpeedUnits).ToString(CultureInfo.InvariantCulture);
                if (!fd.HasDateTime)
                    lnkSendCloudAhoy.Visible = false;

                SetUpMaps(fd);

                if (!IsPostBack)
                {
                    DistanceDisplay = CurrentFlight.GetPathDistanceDescription(fd.ComputePathDistance());
                    // Bind details - this will bind everything else.
                    CurrentFlight.UseHHMM = Viewer.UsesHHMM;    // make sure we capture hhmm correctly.
                    fmvLE.DataSource = new LogbookEntryDisplay[] { CurrentFlight };
                    fmvLE.DataBind();
                }
            }
        }

        // No need to do anything - the postback will clear events from the map.
        protected void lnkClearEvents_Click(object sender, EventArgs e) { }

        protected void btnDownload_Click(object sender, EventArgs e)
        {
            using (FlightData fd = new FlightData() { FlightID = CurrentFlightID })
            {
                fd.ParseFlightData(CurrentFlight);
                DownloadData(CurrentFlight, fd, Convert.ToInt32(cmbAltUnits.SelectedValue, CultureInfo.InvariantCulture), Convert.ToInt32(cmbSpeedUnits.SelectedValue, CultureInfo.InvariantCulture), cmbFormat.SelectedIndex);
            }
        }

        protected async void lnkSendCloudAhoy_Click(object sender, EventArgs e)
        {
            using (FlightData fd = new FlightData() { FlightID = CurrentFlightID })
            {
                try
                {
                    fd.ParseFlightData(CurrentFlight);
                    pnlCloudAhoySuccess.Visible = await PushToCloudahoy(Page.User.Identity.Name, CurrentFlight, fd, Convert.ToInt32(cmbAltUnits.SelectedValue, CultureInfo.InvariantCulture), Convert.ToInt32(cmbSpeedUnits.SelectedValue, CultureInfo.InvariantCulture), !Branding.CurrentBrand.MatchesHost(Request.Url.Host)).ConfigureAwait(false);
                }
                catch (MyFlightbookException ex)
                {
                    lblCloudAhoyErr.Text = ex.Message;
                }
            }
        }

        #region Aircraft/Flight formview
        protected void fmvLE_DataBound(object sender, EventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            if (!(sender is FormView fv))
                throw new InvalidCastException("Sender is not a formview!");

            LogbookEntryDisplay le = (LogbookEntryDisplay)fv.DataItem;
            BindImages(fv.FindControl("mfbilFlight"), le, mfbGoogleMapManager1);

            BindVideoEntries(fv.FindControl("mfbVideoEntry1"), le);

            BindAirportServices(fv.FindControl("mfbAirportServices1"), mfbGoogleMapManager1, CurrentFlight.Route);

            BindSignature(fv.FindControl("mfbSignature"), le);

            BindBadges(Viewer, le.FlightID, fv.FindControl("rptBadges"));

            ((Label)fv.FindControl("lblDistanceForFlight")).Text = DistanceDisplay;

            ((Controls_METAR) fv.FindControl("METARDisplay")).Route = CurrentFlight.Route;
        }

        protected void fmvAircraft_DataBound(object sender, EventArgs e)
        {

            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            FormView fv = sender as FormView;
            BindAircraftImages(fv.FindControl("mfbHoverImageList"));
        }
        #endregion

        protected void mfbFlightContextMenu_DeleteFlight(object sender, LogbookEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            LogbookEntryBase.FDeleteEntry(e.FlightID, Page.User.Identity.Name);
            Response.Redirect(TargetPage);
        }
    }
}