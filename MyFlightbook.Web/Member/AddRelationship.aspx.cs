﻿using MyFlightbook.Clubs;
using MyFlightbook.Instruction;
using System;
using System.Globalization;
using System.Net.Mail;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public partial class AddRelationship : System.Web.UI.Page
    {
        private CFIStudentMapRequest m_smr;
        private CFIStudentMap m_sm;

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.tabTraining;
            Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleProfile, Branding.CurrentBrand.AppName);

            string szReq = util.GetStringParam(Request, "req");

            try
            {
                if (szReq.Length == 0)
                    throw new MyFlightbookValidationException(Resources.LocalizedText.AddRelationshipErrInvalidRequest);

                m_smr = new CFIStudentMapRequest();
                m_smr.DecryptRequest(szReq);

                Profile pfRequestor = Profile.GetUser(m_smr.RequestingUser);
                if (!pfRequestor.IsValid())
                    throw new MyFlightbookValidationException(Resources.LocalizedText.AddRelationshipErrInvalidUser);

                Profile pfCurrent = Profile.GetUser(User.Identity.Name);
                if (pfCurrent.Email.CompareCurrentCultureIgnoreCase(m_smr.TargetUser) != 0 && !pfCurrent.IsVerifiedEmail(m_smr.TargetUser))
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipErrNotTargetUser, m_smr.TargetUser, pfCurrent.Email));

                m_sm = new CFIStudentMap(User.Identity.Name);

                switch (m_smr.Requestedrole)
                {
                    case CFIStudentMapRequest.RoleType.RoleStudent:
                        lblRequestDesc.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipRequestStudent, Branding.CurrentBrand.AppName, HttpUtility.HtmlEncode(pfRequestor.UserFullName));
                        if (m_sm.IsStudentOf(m_smr.RequestingUser))
                            throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipErrAlreadyStudent, pfRequestor.UserFullName));
                        break;
                    case CFIStudentMapRequest.RoleType.RoleCFI:
                        lblRequestDesc.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipRequestInstructor, Branding.CurrentBrand.AppName, HttpUtility.HtmlEncode(pfRequestor.UserFullName));
                        if (m_sm.IsInstructorOf(m_smr.RequestingUser))
                            throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipErrAlreadyInstructor, pfRequestor.UserFullName));
                        break;
                    case CFIStudentMapRequest.RoleType.RoleInviteJoinClub:
                        if (m_smr.ClubToJoin == null)
                            throw new MyFlightbookValidationException(Resources.Club.errNoClubInRequest);
                        if (m_smr.ClubToJoin.HasMember(pfCurrent.UserName))
                            throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.Club.errAlreadyMember, m_smr.ClubToJoin.Name));
                        lblRequestDesc.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberFromInvitation, m_smr.ClubToJoin.Name);
                        break;
                    case CFIStudentMapRequest.RoleType.RoleRequestJoinClub:
                        if (m_smr.ClubToJoin == null)
                            throw new MyFlightbookValidationException(Resources.Club.errNoClubInRequest);
                        if (m_smr.ClubToJoin.HasMember(pfRequestor.UserName))
                            throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.Club.errAlreadyAddedMember, pfRequestor.UserFullName, m_smr.ClubToJoin.Name));
                        lblRequestDesc.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberFromRequest, HttpUtility.HtmlEncode(pfRequestor.UserFullName), m_smr.ClubToJoin.Name);
                        break;
                }
            }
            catch (MyFlightbookValidationException ex)
            {
                pnlReviewRequest.Visible = false;
                lblError.Text = ex.Message;
                pnlConfirm.Visible = false;
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Default.aspx");
        }

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                m_sm.ExecuteRequest(m_smr);
                switch (m_smr.Requestedrole)
                {
                    case CFIStudentMapRequest.RoleType.RoleCFI:
                    case CFIStudentMapRequest.RoleType.RoleStudent:
                        Response.Redirect("~/mvc/training/" + (m_smr.Requestedrole == CFIStudentMapRequest.RoleType.RoleCFI ? "students" : "instructors"));
                        break;
                    case CFIStudentMapRequest.RoleType.RoleInviteJoinClub:
                        {
                            // Let the requestor know that the invitation has been accepted.
                            Profile pfTarget = Profile.GetUser(m_smr.TargetUser.Contains("@") ? System.Web.Security.Membership.GetUserNameByEmail(m_smr.TargetUser) : m_smr.TargetUser);
                            string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberInvitationAccepted, m_smr.ClubToJoin.Name);
                            string szBody = Branding.ReBrand(Resources.Club.ClubInvitationAccepted).Replace("<% ClubName %>", m_smr.ClubToJoin.Name).Replace("<% ClubInvitee %>", pfTarget.UserFullName);
                            foreach (ClubMember cm in ClubMember.AdminsForClub(m_smr.ClubToJoin.ID))
                                util.NotifyUser(szSubject, szBody.Replace("<% FullName %>", cm.UserFullName), new MailAddress(cm.Email, cm.UserFullName), false, false);
                            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/ClubDetails.aspx/{0}", m_smr.ClubToJoin.ID));
                        }
                        break;
                    case CFIStudentMapRequest.RoleType.RoleRequestJoinClub:
                        {
                            // Let the requestor know that the request has been approved.
                            Profile pfRequestor = Profile.GetUser(m_smr.RequestingUser);
                            string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberRequestAccepted, m_smr.ClubToJoin.Name);
                            string szBody = Branding.ReBrand(Resources.Club.ClubRequestAccepted).Replace("<% ClubName %>", m_smr.ClubToJoin.Name).Replace("<% FullName %>", pfRequestor.UserFullName);
                            util.NotifyUser(szSubject, szBody, new MailAddress(pfRequestor.Email, pfRequestor.UserFullName), false, false);
                            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "~/Member/ClubDetails.aspx/{0}", m_smr.ClubToJoin.ID));
                        }
                        break;
                }
            }
            catch (MyFlightbookValidationException ex)
            {
                pnlReviewRequest.Visible = false;
                lblError.Text = ex.Message;
                pnlConfirm.Visible = false;
            }
        }
    }
}