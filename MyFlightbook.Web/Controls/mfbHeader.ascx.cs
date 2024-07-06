﻿using MyFlightbook.Schedule;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;

/******************************************************
 * 
 * Copyright (c) 2009-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Controls
{
    public partial class mfbHeader : UserControl
    {
        /// <summary>
        /// Filename for the xml source for the tabs.
        /// </summary>
        public String XmlSrc { get; set; } = "~/NavLinks.xml";

        public TabList TabList
        {
            get { return TabList.CurrentTabList(XmlSrc); }
        }

        /// <summary>
        /// Which tab is selected?
        /// </summary>
        public tabID SelectedTab { get; set; } = tabID.tabHome;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // fix up the appropriate app name
                lnkDownload.Text = Branding.ReBrand(Resources.LocalizedText.HeaderDownload);
                lnkDownloadIPhone.Text = Branding.ReBrand(Resources.LocalizedText.HeaderDownloadIOS);
                lnkDownloadAndroid.Text = Branding.ReBrand(Resources.LocalizedText.HeaderDownloadAndroid);
                lnkLogo.ImageUrl = Branding.CurrentBrand.LogoHRef;
                lnkDonate.Text = Branding.ReBrand(Resources.LocalizedText.DonateSolicitation);

                if (Request != null && Request.UserAgent != null)
                {
                    string s = Request.UserAgent.ToUpperInvariant();

                    if (s.Contains("IPAD") || s.Contains("IPHONE"))
                        mvXSell.SetActiveView(vwIOS);

                    if (s.Contains("DROID"))
                        mvXSell.SetActiveView(vwDroid);
                }

                mvCrossSellOrEvents.SetActiveView(vwMobileCrossSell);

                mvLoginStatus.SetActiveView(Page.User.Identity.IsAuthenticated ? vwSignedIn : vwNotSignedIn);
                mvWelcome.SetActiveView(Page.User.Identity.IsAuthenticated ? vwWelcomeAuth : vwWelcomeNotAuth);

                if (Page.User.Identity.IsAuthenticated)
                {
                    Refresh();
                    // see if we need to show an upcoming event; we repurpose a known GUID for this.  
                    // If it's in the database AND in the future, we show it.
                    // Since header is loaded on every page load, cache it, using a dummy expired one if there was none.
                    ScheduledEvent se = (ScheduledEvent)Cache["upcomingWebinar"];
                    if (se == null)
                    {
                        se = ScheduledEvent.AppointmentByID("00000000-fe32-5932-bef8-000000000001", TimeZoneInfo.Utc);
                        if (se == null)
                            se = new ScheduledEvent() { EndUtc = DateTime.Now.AddDays(-2) };
                        Cache.Add("upcomingWebinar", se, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), System.Web.Caching.CacheItemPriority.Default, null);
                    }
                    if (se != null && DateTime.UtcNow.CompareTo(se.EndUtc) < 0)
                    {
                        string[] rgLines = se.Body.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        litWebinar.Text = String.Format(CultureInfo.CurrentCulture, "Join \"{0}\" on {1}", (rgLines == null || rgLines.Length == 0) ? string.Empty : rgLines[0], se.LocalStart.ToShortDateString()).Linkify();
                        mvCrossSellOrEvents.SetActiveView(vwUpcomingEvent);
                        lblWebinarDetails.Text = se.Body.Linkify(true);
                    }
                }
            }

            plcMenuBar.Controls.Add(new LiteralControl(TabList.WriteTabsHtml(Request != null && Request.UserAgent != null && Request.UserAgent.ToUpper(CultureInfo.CurrentCulture).Contains("ANDROID"), Profile.GetUser(Page.User.Identity.Name).Role, SelectedTab)));
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/mvc/flights?s=" + HttpUtility.UrlEncode(mfbSearchbox.SearchText));
        }

        public void Refresh()
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);

            imgHdSht.Src = pf.HeadShotHRef;

            lblUser.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LoginStatusWelcome, HttpUtility.HtmlEncode(pf.PreferredGreeting));
            lblMemberSince.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberSinceShort, pf.CreationDate);
            if (pf.LastLogon.HasValue())
                lblLastLogin.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastLogonShort, pf.LastLogon);
            else
                lblLastLogin.Visible = false;

            lblLastActivity.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.MemberLastActivityShort, pf.LastActivity);
            itemLastActivity.Visible = pf.LastActivity.Date.CompareTo(pf.LastLogon.Date) != 0;
        }
    }
}