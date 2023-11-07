﻿using MyFlightbook.SponsoredAds;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2016-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_SponsoredAd : System.Web.UI.UserControl
{
    public int SponsoredAdID
    {
        get { return String.IsNullOrEmpty(hdnAdID.Value) ? -1 : Convert.ToInt32(hdnAdID.Value, CultureInfo.InvariantCulture); }
        set 
        {
            hdnAdID.Value = value.ToString(CultureInfo.InvariantCulture); 
            SpecifiedAd = SponsoredAd.GetAd(value) ?? new SponsoredAd();
            InitForAd();
        }
    }

    protected SponsoredAd SpecifiedAd {get; set; }

    protected void InitForAd()
    {
        imgAd.ImageUrl = SpecifiedAd.ImagePath;
        lnkAd.NavigateUrl = "~/mvc/internal/AdTracker/" + SponsoredAdID.ToString(CultureInfo.InvariantCulture);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            /*
            // For now at least, only show the ad if we've turned it on.
            const string szCookieKey = "cookieAdState";
            bool fShowAd = false;

            int adState = util.GetIntParam(Request, "showsponsoredad", -1);
            if (adState == 1)
            {
                Response.Cookies[szCookieKey].Value = "yes";
                fShowAd = true;
            }
            else if (adState == 0)
            {
                Response.Cookies[szCookieKey].Value = "";
                Response.Cookies[szCookieKey].Expires = DateTime.Now.AddDays(-5);
                fShowAd = false;
            }
            else
                fShowAd = (Request.Cookies[szCookieKey] != null && Request.Cookies[szCookieKey].Value == "yes");

            Visible = fShowAd && Visible;
             * */
        }
    }

    protected void Page_PreRender(object sender, EventArgs e)
    {
        if (SpecifiedAd != null && Visible)
            SpecifiedAd.AddImpression();
    }
}