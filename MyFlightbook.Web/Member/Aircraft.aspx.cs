using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class MyAircraft : Page
    {
        private int idModel = -1;

        protected bool IsAdminMode { get; set; }

        protected AircraftGroup.GroupMode GroupingMode
        {
            get
            {
                return (AircraftGroup.GroupMode)Enum.Parse(typeof(AircraftGroup.GroupMode), cmbAircraftGrouping.SelectedValue);
            }
        }

        protected static string FormatOptionalDate(DateTime dt)
        {
            return dt.HasValue() ? dt.ToShortDateString() : string.Empty;
        }

        protected IEnumerable<Aircraft> SourceAircraft { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.Master.SelectedTab = tabID.actMyAircraft;
            this.Title = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.AircraftTitle, Branding.CurrentBrand.AppName);

            idModel = util.GetIntParam(Request, "m", -1);
            IsAdminMode = (idModel > 0) && (util.GetIntParam(Request, "a", 0) != 0) && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData;

            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            if (!IsPostBack)
            {
                bool fClearCache = (util.GetIntParam(Request, "flush", 0) != 0);
                if (fClearCache)
                    ua.InvalidateCache();
            }
            lblAdminMode.Visible = IsAdminMode;
            lnkDownloadCSV.Visible = !IsAdminMode;

            RefreshAircraftList();

            Refresh();
        }

        protected void RefreshAircraftList()
        {
            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            UserAircraft.AircraftRestriction r = IsAdminMode ? UserAircraft.AircraftRestriction.AllMakeModel : UserAircraft.AircraftRestriction.UserAircraft;
            SourceAircraft = ua.GetAircraftForUser(r, idModel);
        }

        /// <summary>
        /// Populates stats, if needed.
        /// </summary>
        protected void PopulateStats()
        {
            if (String.IsNullOrEmpty(hdnStatsFetched.Value))
            {
                hdnStatsFetched.Value = "yes";
                RefreshAircraftList();
                AircraftStats.PopulateStatsForAircraft(SourceAircraft, Page.User.Identity.Name);
            }
        }

        protected void Refresh(bool fForceStats = false)
        {
            if (fForceStats)
                hdnStatsFetched.Value = string.Empty;

            if (GroupingMode == AircraftGroup.GroupMode.Recency)
                PopulateStats();

            lblNumAircraft.Text = SourceAircraft.Any() ? String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.MyAircraftAircraftCount, SourceAircraft.Count()) : Resources.LocalizedText.MyAircraftNoAircraft;
            rptAircraftGroups.DataSource = AircraftGroup.AssignToGroups(SourceAircraft, IsAdminMode ? AircraftGroup.GroupMode.All : GroupingMode);
            rptAircraftGroups.DataBind();
        }

        protected void cmbAircraftGrouping_SelectedIndexChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        protected void rptAircraftGroups_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            AircraftControls.AircraftList aircraftList = (AircraftControls.AircraftList)e.Item.FindControl("AircraftList");
            aircraftList.IsAdminMode = IsAdminMode;
            aircraftList.AircraftSource = ((AircraftGroup)e.Item.DataItem).MatchingAircraft;
        }

        protected void AircraftList_AircraftDeleted(object sender, CommandEventArgs e)
        {
            RefreshAircraftList();
            Refresh(true);
        }

        protected static string ValueString(object o, decimal offSet = 0.0M)
        {
            if (o is DateTime dt)
            {
                if (dt != null && dt.HasValue())
                    return dt.ToShortDateString();
            }
            else if (o is decimal d)
            {
                if (d > 0)
                    return (d + offSet).ToString("#,##0.0#", CultureInfo.CurrentCulture);
            }
            return string.Empty;
        }

        protected void lnkDownloadCSV_Click(object sender, EventArgs e)
        {
            PopulateStats();
            gvAircraftToDownload.DataSource = SourceAircraft;
            gvAircraftToDownload.DataBind();
            Response.Clear();
            Response.ContentType = "text/csv";
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            string szFilename = String.Format(CultureInfo.InvariantCulture, "Aircraft-{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
            string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", RegexUtility.UnSafeFileChars.Replace(szFilename, string.Empty));
            Response.AddHeader("Content-Disposition", szDisposition);
            gvAircraftToDownload.ToCSV(Response.OutputStream);
            Response.End();
        }

        protected void btnMigrate_Click(object sender, EventArgs e)
        {
            if (Int32.TryParse(cmbMigr.SelectedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idTarget) && idTarget > 0 &&
                Int32.TryParse(hdnMigSrc.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idSrc) && idSrc > 0)
            {
                UserAircraft ua = new UserAircraft(User.Identity.Name);
                Aircraft acSrc = ua.GetUserAircraftByID(idSrc);
                Aircraft acTarg = ua.GetUserAircraftByID(idTarget);
                if (acSrc != null && acTarg != null)
                {
                    Aircraft.AdminMigrateFlights(User.Identity.Name, acSrc, acTarg);
                    if (ckDelAfterMigr.Checked)
                    {
                        ua.FDeleteAircraftforUser(acSrc.AircraftID);
                        RefreshAircraftList();
                        Refresh(true);
                    }
                } 
            }
            pnlMigrate.Visible = false;
        }

        protected void AircraftList_MigrateAircraft(object sender, AircraftEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            hdnMigSrc.Value = e.AircraftID.ToString(CultureInfo.InvariantCulture);

            lblMigrate.Text = String.Format(CultureInfo.CurrentCulture, Resources.Aircraft.editAircraftMigratePrompt, e.Aircraft.DisplayTailnumber);

            // Show all other aircraft, removing the source one.
            List<Aircraft> lst = new List<Aircraft>(new UserAircraft(Page.User.Identity.Name).GetAircraftForUser());
            lst.RemoveAll(ac => ac.AircraftID == e.AircraftID);

            cmbMigr.Items.Clear();
            cmbMigr.DataSource = lst;
            cmbMigr.DataBind();
            pnlMigrate.Visible = true;  // jquery will show this in a dialog
        }
    }
}