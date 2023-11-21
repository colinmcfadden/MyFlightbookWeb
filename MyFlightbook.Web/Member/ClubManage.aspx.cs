﻿using MyFlightbook.Instruction;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Clubs
{
    public partial class ClubManage : Page
    {
        /// <summary>
        /// The current club being viewed.  Actually delegates to the ViewClub control, since this saves it in its viewstate.
        /// </summary>
        protected Club CurrentClub
        {
            get { return ViewClub1.ActiveClub; }
            set { ViewClub1.ActiveClub = value; }
        }

        protected bool IsManager
        {
            get
            {
                ClubMember cm = CurrentClub.GetMember(Page.User.Identity.Name);
                return cm != null && cm.IsManager;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.actMyClubs;

            try
            {
                if (Request.PathInfo.Length > 0 && Request.PathInfo.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsPostBack)
                    {
                        CurrentClub = Club.ClubWithID(Convert.ToInt32(Request.PathInfo.Substring(1), CultureInfo.InvariantCulture));
                        if (CurrentClub == null)
                            throw new MyFlightbookException(Resources.Club.errNoSuchClub);

                        Master.Title = CurrentClub.Name;
                        lblClubHeader.Text = CurrentClub.Name;

                        ClubMember cm = CurrentClub.GetMember(Page.User.Identity.Name);

                        if (!IsManager)
                            throw new MyFlightbookException(Resources.Club.errNotAuthorizedToManage);

                        gvMembers.DataSource = CurrentClub.Members;
                        gvMembers.DataBind();
                        vcEdit.ShowDelete = (cm.RoleInClub == ClubMember.ClubMemberRole.Owner);

                        cmbClubAircraft.DataSource = CurrentClub.MemberAircraft;
                        cmbClubAircraft.DataBind();
                        cmbClubMembers.DataSource = CurrentClub.Members;
                        cmbClubMembers.DataBind();

                        dateStart.Date = (Request.Cookies[szCookieLastStart] != null && DateTime.TryParse(Request.Cookies[szCookieLastStart].Value, out DateTime dtStart)) ? dtStart : CurrentClub.CreationDate;
                        dateEnd.Date = (Request.Cookies[szCookieLastEnd] != null && DateTime.TryParse(Request.Cookies[szCookieLastEnd].Value, out DateTime dtEnd)) ? dtEnd : DateTime.Now;
                        dateEnd.DefaultDate = DateTime.Now;
                        RefreshAircraft();

                        lblManageheader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.LabelManageThisClub, CurrentClub.Name);
                        lnkReturnToClub.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/ClubDetails.aspx/{0}", CurrentClub.ID);

                        vcEdit.ActiveClub = CurrentClub;    // do this at the end so that all of the above (like ShowDelete) are captured
                    }
                }
                else
                    throw new MyFlightbookException(Resources.Club.errNoClubSpecified);
            }
            catch (MyFlightbookException ex)
            {
                lblErr.Text = ex.Message;
                lnkReturnToClub.Visible = accClub.Visible = false;
            }
        }

        protected void vcEdit_ClubChanged(object sender, EventArgs e)
        {
            CurrentClub = vcEdit.ActiveClub;
        }
        protected void vcEdit_ClubChangeCanceled(object sender, EventArgs e)
        {
            vcEdit.ActiveClub = CurrentClub;
        }

        protected void btnDeleteClub_Click(object sender, EventArgs e)
        {
            CurrentClub.FDelete();
            Response.Redirect("~/Default.aspx");
        }

        #region aircraft management
        protected void refreshAdminAircraft(int editIndex)
        {
            gvAircraft.EditIndex = editIndex;
            ClubAircraft.RefreshClubAircraftTimes(CurrentClub.ID, CurrentClub.MemberAircraft);  // update the "max" times
            gvAircraft.DataSource = CurrentClub.MemberAircraft;
            gvAircraft.DataBind();
        }

        protected void gvAircraft_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            refreshAdminAircraft(-1);
        }

        protected void gvAircraft_RowEditing(object sender, GridViewEditEventArgs e)
        {
            if (e != null)
                refreshAdminAircraft(e.NewEditIndex);
        }

        protected void btnAddAircraft_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(cmbAircraftToAdd.SelectedValue))
                return;

            ClubAircraft ca = new ClubAircraft()
            {
                AircraftID = Convert.ToInt32(cmbAircraftToAdd.SelectedValue, CultureInfo.InvariantCulture),
                ClubDescription = txtDescription.Text,
                ClubID = CurrentClub.ID
            };
            if (!ca.FSaveToClub())
                lblManageAircraftError.Text = ca.LastError;
            else
            {
                txtDescription.Text = string.Empty;
                CurrentClub.InvalidateMemberAircraft(); // force a reload
                RefreshAircraft();
            }
        }

        protected void RefreshAircraft()
        {
            UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
            List<Aircraft> lst = new List<Aircraft>(ua.GetAircraftForUser());
            lst.RemoveAll(ac => ac.IsAnonymous || CurrentClub.MemberAircraft.FirstOrDefault(ca => ca.AircraftID == ac.AircraftID) != null); // remove all anonymous aircraft, or aircraft that are already in the list
            cmbAircraftToAdd.DataSource = lst;
            cmbAircraftToAdd.DataBind();

            gvAircraft.DataSource = CurrentClub.MemberAircraft;
            gvAircraft.DataBind();
        }

        protected void gvAircraft_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            ClubAircraft ca = CurrentClub.MemberAircraft.FirstOrDefault(ac => ac.AircraftID == Convert.ToInt32(e.Keys[0], CultureInfo.InvariantCulture));
            if (ca != null)
            {
                if (sender == null)
                    throw new ArgumentNullException(nameof(sender));
                if (e == null)
                    throw new ArgumentNullException(nameof(e));

                GridViewRow row = ((GridView)sender).Rows[e.RowIndex];
                ca.ClubDescription = ((Controls_mfbHtmlEdit)row.FindControl("txtDescription")).Text;
                ca.HighWater = ((Controls_mfbDecimalEdit)row.FindControl("decEditTime")).Value;

                if (ca.FSaveToClub())
                {
                    gvAircraft.EditIndex = -1;
                    RefreshAircraft();
                }
                else
                    lblManageAircraftError.Text = ca.LastError;
            }
        }

        protected void gvAircraft_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if ((e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
            {
                ClubAircraft ca = (ClubAircraft)e.Row.DataItem;
                Controls_mfbDecimalEdit decHighWater = (Controls_mfbDecimalEdit)e.Row.FindControl("decEditTime");
                decHighWater.Value = ca.HighWater;
                Label lnkHobbs = (Label)e.Row.FindControl("lnkCopyHobbs");
                Label lnkTach = (Label)e.Row.FindControl("lnkCopyTach");
                e.Row.FindControl("pnlHighHobbs").Visible = ca.HighestRecordedHobbs > 0;
                e.Row.FindControl("pnlHighTach").Visible = ca.HighestRecordedTach > 0;
                lnkHobbs.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubAircraftHighestHobbs, ca.HighestRecordedHobbs);
                lnkTach.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubAircraftHighestTach, ca.HighestRecordedTach);
                ((System.Web.UI.WebControls.Image)e.Row.FindControl("imgXFillHobbs")).Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:document.getElementById('{0}').value = '{1}';", decHighWater.EditBox.ClientID, ca.HighestRecordedHobbs.ToString(CultureInfo.CurrentCulture));
                ((System.Web.UI.WebControls.Image)e.Row.FindControl("imgXFillTach")).Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:document.getElementById('{0}').value = '{1}';", decHighWater.EditBox.ClientID, ca.HighestRecordedTach.ToString(CultureInfo.CurrentCulture));
            }
        }

        protected void gvAircraft_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.CommandName.CompareOrdinalIgnoreCase("_Delete") == 0)
            {
                ClubAircraft ca = CurrentClub.MemberAircraft.FirstOrDefault(ac => ac.AircraftID == Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture));
                if (ca != null)
                {
                    if (!ca.FDeleteFromClub())
                        lblManageAircraftError.Text = ca.LastError;
                    else
                    {
                        CurrentClub.InvalidateMemberAircraft();
                        RefreshAircraft();
                    }
                }
            }
        }
        #endregion

        #region Member Management
        protected void gvMembers_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e != null && e.Row.RowType == DataControlRowType.DataRow && (e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
            {
                ClubMember cm = (ClubMember)e.Row.DataItem;
                ((RadioButtonList)e.Row.FindControl("rblRole")).SelectedValue = cm.RoleInClub.ToString();
                ((CheckBox)e.Row.FindControl("ckMaintenanceOfficer")).Checked = cm.IsMaintanenceOfficer;
                ((CheckBox)e.Row.FindControl("ckTreasurer")).Checked = cm.IsTreasurer;
                ((CheckBox)e.Row.FindControl("ckInsuranceOfficer")).Checked = cm.IsInsuranceOfficer;
                ((CheckBox)e.Row.FindControl("ckInactive")).Checked = cm.IsInactive;

                ((TextBox)e.Row.FindControl("txtOffice")).Text = cm.ClubOffice ?? string.Empty;
            }
        }
        protected void gvMembers_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            ClubMember cm = CurrentClub.Members.FirstOrDefault(pf => pf.UserName.CompareOrdinalIgnoreCase(e.Keys[0].ToString()) == 0);
            if (cm != null)
            {
                GridViewRow gvr = gvMembers.Rows[e.RowIndex];
                RadioButtonList rbl = (RadioButtonList)gvr.FindControl("rblRole");
                ClubMember.ClubMemberRole requestedRole = (ClubMember.ClubMemberRole)Enum.Parse(typeof(ClubMember.ClubMemberRole), rbl.SelectedValue);
                cm.IsMaintanenceOfficer = ((CheckBox)gvr.FindControl("ckMaintenanceOfficer")).Checked;
                cm.IsTreasurer = ((CheckBox)gvr.FindControl("ckTreasurer")).Checked;
                cm.IsInsuranceOfficer = ((CheckBox)gvr.FindControl("ckInsuranceOfficer")).Checked;
                cm.IsInactive = ((CheckBox)gvr.FindControl("ckInactive")).Checked;
                cm.ClubOffice = ((TextBox)gvr.FindControl("txtOffice")).Text.Trim();

                bool fResult = true;
                try
                {
                    if (requestedRole == ClubMember.ClubMemberRole.Owner) // that's fine, but we need to un-make any other creators
                    {
                        ClubMember cmOldOwner = CurrentClub.Members.FirstOrDefault(pf => pf.RoleInClub == ClubMember.ClubMemberRole.Owner);
                        if (cmOldOwner != null) //should never happen!
                        {
                            cmOldOwner.RoleInClub = ClubMember.ClubMemberRole.Admin;
                            if (!cmOldOwner.FCommitClubMembership())
                                throw new MyFlightbookException(cmOldOwner.LastError);
                        }
                    }
                    else if (cm.RoleInClub == ClubMember.ClubMemberRole.Owner)    // if we're not requesting creator role, but this person currently is creator, then we are demoting - that's a no-no
                        throw new MyFlightbookException(Resources.Club.errCantDemoteOwner);

                    cm.RoleInClub = requestedRole;
                    if (!cm.FCommitClubMembership())
                        throw new MyFlightbookException(cm.LastError);
                }
                catch (MyFlightbookException ex)
                {
                    lblManageMemberError.Text = ex.Message;
                    fResult = false;
                }

                if (fResult)
                    refreshAdminMembers(-1);
            }
        }

        protected void refreshAdminMembers(int editRow)
        {
            gvMembers.EditIndex = editRow;
            gvMembers.DataSource = CurrentClub.Members;
            gvMembers.DataBind();
        }

        protected void gvMembers_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            refreshAdminMembers(-1);
        }

        protected void gvMembers_RowEditing(object sender, GridViewEditEventArgs e)
        {
            if (e != null)
                refreshAdminMembers(e.NewEditIndex);
        }

        protected void gvMembers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.CommandName.CompareOrdinalIgnoreCase("_Delete") == 0)
            {
                ClubMember cm = CurrentClub.Members.FirstOrDefault(pf => pf.UserName.CompareOrdinalIgnoreCase(e.CommandArgument.ToString()) == 0);
                if (cm != null)
                {
                    if (cm.RoleInClub == ClubMember.ClubMemberRole.Owner)
                        lblManageMemberError.Text = Resources.Club.errCannotDeleteOwner;
                    else
                    {
                        if (!cm.FDeleteClubMembership())
                            lblManageMemberError.Text = cm.LastError;
                        else
                        {
                            CurrentClub.InvalidateMembers();
                            gvMembers.DataSource = CurrentClub.Members;
                            gvMembers.DataBind();
                        }
                    }
                }
            }
        }

        protected void btnAddMember_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            if (CurrentClub.Status == Club.ClubStatus.Inactive)
            {
                lblErr.Text = Branding.ReBrand(Resources.Club.errClubInactive);
                return;
            }
            if (CurrentClub.Status == Club.ClubStatus.Expired)
            {
                lblErr.Text = Branding.ReBrand(Resources.Club.errClubPromoExpired);
                return;
            }

            try
            {
                new CFIStudentMapRequest(Page.User.Identity.Name, txtMemberEmail.Text, CFIStudentMapRequest.RoleType.RoleInviteJoinClub, CurrentClub).Send();
                lblAddMemberSuccess.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileRequestHasBeenSent, System.Web.HttpUtility.HtmlEncode(txtMemberEmail.Text));
                lblAddMemberSuccess.CssClass = "success";
                txtMemberEmail.Text = "";
            }
            catch (MyFlightbookException ex)
            {
                lblAddMemberSuccess.Text = ex.Message;
                lblAddMemberSuccess.CssClass = "error";
            }
        }
        #endregion

        #region Reporting
        const string szCookieLastStart = "clubFlyingLastStart";
        const string szCookieLastEnd = "clubFlyingLastEnd";

        protected void btnUpdate_Click(object sender, EventArgs e)
        {
            FlyingReport.Refresh(CurrentClub.ID, dateStart.Date, dateEnd.Date, cmbClubMembers.SelectedValue, Convert.ToInt32(cmbClubAircraft.SelectedValue, CultureInfo.InvariantCulture));
            btnDownload.Visible = true;

            Response.Cookies[szCookieLastStart].Value = dateStart.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Response.Cookies[szCookieLastEnd].Value = dateEnd.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Response.Cookies[szCookieLastStart].Expires = Response.Cookies[szCookieLastEnd].Expires = DateTime.Now.AddYears(5);
        }
        protected void btnDownload_Click(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "text/csv";
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, System.Text.RegularExpressions.Regex.Replace(CurrentClub.Name, "[^0-9a-zA-Z-]", string.Empty), DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
            string szDisposition = String.Format(CultureInfo.InvariantCulture, "inline;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", string.Empty));
            Response.AddHeader("Content-Disposition", szDisposition);
            FlyingReport.ToStream(Response.OutputStream);
            Response.End();
        }

        protected void lnkViewKML_Click(object sender, EventArgs e)
        {
            DataSourceType dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
            Response.Clear();
            Response.ContentType = dst.Mimetype;
            Response.AddHeader("Content-Disposition", String.Format(CultureInfo.CurrentCulture, "attachment;filename={0}-AllFlights.{1}", Branding.CurrentBrand.AppName, dst.DefaultExtension));

            FlyingReport.WriteKMLToStream(Response.OutputStream, CurrentClub.ID, dateStart.Date, dateEnd.Date, cmbClubMembers.SelectedValue, Convert.ToInt32(cmbClubAircraft.SelectedValue, CultureInfo.InvariantCulture));

            Response.End();
        }

        protected void btnUpdateMaintenance_Click(object sender, EventArgs e)
        {
            MaintenanceReport.Refresh(CurrentClub.ID);
            btnDownloadMaintenance.Visible = true;
        }

        protected void btnDownloadMaintenance_Click(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "text/csv";
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-Maintenance-{2}", Branding.CurrentBrand.AppName, System.Text.RegularExpressions.Regex.Replace(CurrentClub.Name, "[^0-9a-zA-Z-]", string.Empty), DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
            string szDisposition = String.Format(CultureInfo.InvariantCulture, "inline;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", string.Empty));
            Response.AddHeader("Content-Disposition", szDisposition);
            MaintenanceReport.ToStream(Response.OutputStream);
            Response.End();
        }
        #endregion

        protected void btnInsuranceReport_Click(object sender, EventArgs e)
        {
            InsuranceReport.Refresh(CurrentClub.ID, Convert.ToInt32(cmbMonthsInsurance.SelectedValue, CultureInfo.InvariantCulture));
        }
    }
}