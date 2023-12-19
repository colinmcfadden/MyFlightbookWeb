﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Instruction
{
    public partial class mfbEndorsementList : UserControl
    {
        public event EventHandler<EndorsementEventArgs> CopyEndorsement;

        #region Properties
        private const string szVSKeyStudent = "vsKeyStudent";
        private const string szVSKeyInstructor = "vsKeyInstructor";
        private const string szVSExcludeInstructor = "vsKeyExcludeInstructor";

        /// <summary>
        /// The name of the student to restrict on
        /// </summary>
        public string Student
        {
            get { return (string) ViewState[szVSKeyStudent]; }
            set { ViewState[szVSKeyStudent] = value; }
        }

        /// <summary>
        /// The name of the instructor to restrict on.
        /// </summary>
        public string Instructor
        {
            get { return (string)ViewState[szVSKeyInstructor]; }
            set { ViewState[szVSKeyInstructor] = value; }
        }

        /// <summary>
        /// True to exclude the specified instructor name when instructor is otherwise null (i.e., getting ALL student endorsements).  This allows retrieval of the "other" endorsements.
        /// </summary>
        public string ExcludeInstructor
        {
            get { return (string) ViewState[szVSExcludeInstructor]; }
            set { ViewState[szVSExcludeInstructor] = value;}
        }

        protected bool CanDelete(Endorsement e)
        {
            return (e != null && ShowDelete && e.StudentName.CompareCurrentCultureIgnoreCase(Page.User.Identity.Name) == 0);
        }

        protected bool CanCopy(Endorsement e)
        {
            return e != null && e.IsMemberEndorsement && e.StudentName.CompareCurrentCultureIgnoreCase(Page.User.Identity.Name) != 0;
        }

        protected SortDirection CurSortDirection
        {
            get { return (SortDirection)Enum.Parse(typeof(SortDirection), hdnCurSortDir.Value, true); }
            set { hdnCurSortDir.Value = value.ToString(); }
        }

        protected EndorsementSortKey CurSortKey
        {
            get { return (EndorsementSortKey)Enum.Parse(typeof(EndorsementSortKey), hdnCurSort.Value, true); }
            set { hdnCurSort.Value = value.ToString(); }
        }

        public bool ShowSort
        {
            get { return gvExistingEndorsements.ShowHeader; }
            set { gvExistingEndorsements.ShowHeader = value; }
        }

        public bool ShowDelete { get; set; } = true;
        #endregion

        /// <summary>
        /// Refreshes (databinds) the list of endorsements
        /// </summary>
        /// <returns># of endorsements bound</returns>
        public int RefreshEndorsements()
        {
            IEnumerable<Endorsement> rg = Endorsement.RemoveEndorsementsByInstructor(Endorsement.EndorsementsForUser(Student, Instructor, CurSortDirection, CurSortKey), ExcludeInstructor);

            gvExistingEndorsements.DataSource = rg;
            gvExistingEndorsements.DataBind();

            int cItems = rg.Count();

            gvExistingEndorsements.ShowHeader = cItems > 0;
            if (cItems > 0)
            {
                LinkButton lbSortDate = (LinkButton)gvExistingEndorsements.HeaderRow.FindControl("lnkSortDate");
                LinkButton lbSortTitle = (LinkButton)gvExistingEndorsements.HeaderRow.FindControl("lnkSortTitle");
                lbSortDate.CssClass = String.Format(CultureInfo.InvariantCulture, "headerBase{0}", CurSortKey == EndorsementSortKey.Date ? (CurSortDirection == SortDirection.Ascending ? " headerSortAsc" : " headerSortDesc") : string.Empty);
                lbSortTitle.CssClass = String.Format(CultureInfo.InvariantCulture, "headerBase{0}", CurSortKey == EndorsementSortKey.Title ? (CurSortDirection == SortDirection.Ascending ? " headerSortAsc" : " headerSortDesc") : string.Empty);
            }

            lnkDownload.Visible = !String.IsNullOrEmpty(Instructor) && cItems > 0;
            
            return cItems;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                lnkDownload.Visible = !String.IsNullOrEmpty(Instructor) && gvExistingEndorsements.DataSource != null && gvExistingEndorsements.Rows.Count > 0;
            }
        }
        protected void gvExistingEndorsements_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e != null && e.Row.RowType == DataControlRowType.DataRow)
            {
                Endorsement endorsement = (Endorsement)e.Row.DataItem;
                Controls_mfbEndorsement mfbEndorsement = (Controls_mfbEndorsement)e.Row.FindControl("mfbEndorsement1");
                mfbEndorsement.SetEndorsement(endorsement);
            }
        }
        protected void gvExistingEndorsements_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null || String.IsNullOrEmpty(e.CommandArgument.ToString()))
                return;

            int id = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);

            try
            {
                if (id <= 0)
                    throw new MyFlightbookException("Invalid endorsement ID to delete");

                if (e.CommandName.CompareOrdinalIgnoreCase("_DeleteExternal") == 0 && !String.IsNullOrEmpty(e.CommandArgument.ToString()))
                {
                    List<Endorsement> rgEndorsements = new List<Endorsement>(Endorsement.EndorsementsForUser(null, Page.User.Identity.Name));

                    Endorsement en = rgEndorsements.FirstOrDefault(en2 => en2.ID == id);
                    if (en == null)
                        throw new MyFlightbookException("ID of endorsement to delete is not found in owners endorsements");

                    if (en.StudentType == Endorsement.StudentTypes.Member)
                        throw new MyFlightbookException(Resources.SignOff.errCantDeleteMemberEndorsement);

                    en.FDelete();
                    RefreshEndorsements();
                }
                else if (e.CommandName.CompareOrdinalIgnoreCase("_DeleteOwned") == 0 && !String.IsNullOrEmpty(e.CommandArgument.ToString()))
                {
                    List<Endorsement> rgEndorsements = new List<Endorsement>(Endorsement.EndorsementsForUser(Page.User.Identity.Name, null));
                    Endorsement en = rgEndorsements.FirstOrDefault(en2 => en2.ID == id);

                    if (en == null)
                        throw new MyFlightbookException("Can't find endorsement with ID=" + id.ToString(CultureInfo.InvariantCulture));

                    if (en.StudentType == Endorsement.StudentTypes.External)
                        throw new MyFlightbookException("Can't delete external endorsement with ID=" + id.ToString(CultureInfo.InvariantCulture));

                    en.FDelete();
                    RefreshEndorsements();
                }
                else if (e.CommandName.CompareOrdinalIgnoreCase("_Copy") == 0 && !String.IsNullOrEmpty(e.CommandArgument.ToString()))
                {
                    Endorsement en = Endorsement.EndorsementWithID(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture));

                    if (en == null)
                        throw new MyFlightbookException("Can't find endorsement with ID=" + id.ToString(CultureInfo.InvariantCulture));

                    if (en.StudentType == Endorsement.StudentTypes.External)
                        throw new MyFlightbookException("Can't copy external endorsement with ID=" + id.ToString(CultureInfo.InvariantCulture));

                    CopyEndorsement?.Invoke(this, new EndorsementEventArgs(en));
                }
            }
            catch (MyFlightbookException ex)
            {
                lblErr.Text = ex.Message;
            }
        }

        protected void lnkDownload_Click(object sender, EventArgs e)
        {
            gvDownload.DataSource = Endorsement.EndorsementsForUser(Student, Instructor, CurSortDirection, CurSortKey);
            gvDownload.DataBind();
            Response.ContentType = "text/csv";
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            string szFilename = String.Format(CultureInfo.CurrentCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, Resources.SignOff.DownloadEndorsementsFilename, String.IsNullOrEmpty(Student) ? Resources.SignOff.DownloadEndorsementsAllStudents : MyFlightbook.Profile.GetUser(Student).UserFullName);
            string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", RegexUtility.UnSafeFileChars.Replace(szFilename, string.Empty));
            Response.AddHeader("Content-Disposition", szDisposition);
            gvDownload.ToCSV(Response.OutputStream);
            Response.End();
        }

        protected void lnkSortDate_Click(object sender, EventArgs e)
        {
            if (CurSortKey == EndorsementSortKey.Date)  // toggle direction
                CurSortDirection = (CurSortDirection == SortDirection.Descending) ? SortDirection.Ascending : SortDirection.Descending;
            else
            {
                CurSortKey = EndorsementSortKey.Date;
                CurSortDirection = SortDirection.Descending;
            }
            RefreshEndorsements();
        }

        protected void lnkSortTitle_Click(object sender, EventArgs e)
        {
            if (CurSortKey == EndorsementSortKey.Title) // toggle direction 
                CurSortDirection = (CurSortDirection == SortDirection.Descending) ? SortDirection.Ascending : SortDirection.Descending;
            else
            {
                CurSortKey = EndorsementSortKey.Title;
                CurSortDirection = SortDirection.Ascending;
            }
            RefreshEndorsements();
        }
    }
}