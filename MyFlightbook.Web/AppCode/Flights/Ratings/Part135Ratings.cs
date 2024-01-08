﻿using MyFlightbook.Currency;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2013-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.RatingsProgress
{
    /// <summary>
    /// PArt 135 Milestones
    /// </summary>
    [Serializable]
    public class Part135Milestones : MilestoneGroup
    {
        public Part135Milestones()
        {
            GroupName = Resources.MilestoneProgress.RatingGroup135;
        }

        public override Collection<MilestoneProgress> Milestones
        {
            get
            {
                return new Collection<MilestoneProgress> {
                new Part135243b(),
                new Part135243c()
                };
            }
        }

        /// <summary>
        /// Determines the amount of part 135 cross-country time in this flight.
        /// Part 135 time is cross country if it goes from airport A to airport B
        /// ABCD-ABCD or ABC-ABC.  (If it's simply ABC or simply ABCD, then it fails the length test)
        /// If it is non-local, then we use MAX(total time, cross-country), otherwise we use the logged XC time.
        /// </summary>
        /// <param name="cfr">The flight</param>
        /// <returns>The amount of cross-country time to contribute</returns>
        public static decimal Part135CrossCountry(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                return 0.0M;

            // short route is always local - just record the XC time reported.
            return (RegexUtility.LocalFlight.IsMatch(cfr.Route ?? string.Empty)) ? cfr.XC : Math.Max(cfr.XC, cfr.Total);
        }
    }

    #region Part 135 Ratings
    /// <summary>
    /// Part 135.243b - PIC (VFR)
    /// </summary>
    [Serializable]
    public class Part135243b : MilestoneProgress
    {
        protected MilestoneItem miMinTimeAsPilot { get; set; }
        protected MilestoneItem miMinXCTime { get; set; }
        protected MilestoneItem miMinXCNightTime { get; set; }

        protected const decimal minTime = 500.0M;
        protected const decimal minXCTime = 100.0M;
        protected const decimal minXCNightTime = 25.0M;

        public Part135243b() : base()
        {
            RatingSought = RatingType.Part135PIC;
            BaseFAR = "135.243(b)(2)";
            FARLink = "https://www.law.cornell.edu/cfr/text/14/135.243";
            string szFAR = ResolvedFAR(string.Empty);
            Title = Resources.MilestoneProgress.Title135243PIC;
            GeneralDisclaimer = Branding.ReBrand(Resources.MilestoneProgress.Part135PICDisclaimer);
            miMinTimeAsPilot = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part135PICMinTime, minTime), szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minTime);
            miMinXCTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part135PICXCMinTime, minXCTime), szFAR, Resources.MilestoneProgress.Part135XCNote, MilestoneItem.MilestoneType.Time, minXCTime);
            miMinXCNightTime = new MilestoneItem(Resources.MilestoneProgress.Part135PICNightXCMinTime, szFAR, Resources.MilestoneProgress.Part135XCNote, MilestoneItem.MilestoneType.Time, minXCNightTime);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            if (!cfr.fIsRealAircraft)
                return;

            decimal part135XC = Part135Milestones.Part135CrossCountry(cfr);

            miMinTimeAsPilot.AddEvent(cfr.Total);
            miMinXCTime.AddEvent(part135XC);
            miMinXCNightTime.AddEvent(Math.Min(part135XC, cfr.Night));
        }

        public override Collection<MilestoneItem> Milestones
        {
            get
            { return new Collection<MilestoneItem>() { miMinTimeAsPilot, miMinXCTime, miMinXCNightTime }; }
        }
    }

    /// <summary>
    /// Part 135.243(c) - PIC (IFR)
    /// </summary>
    [Serializable]
    public class Part135243c : MilestoneProgress
    {
        protected MilestoneItem miMinTimeAsPilot { get; set; }
        protected MilestoneItem miMinXCTime { get; set; }
        protected MilestoneItem miMinNightTime { get; set; }
        protected MilestoneItem miMinIFRTime { get; set; }
        protected MilestoneItem miMinIFRAircraftTime { get; set; }

        protected const decimal minTime = 1200.0M;
        protected const decimal minXCTime = 500.0M;
        protected const decimal minNightTime = 100.0M;
        protected const decimal minIFRTime = 75.0M;
        protected const decimal minIFRAircraftTime = 50.0M;

        public Part135243c() : base()
        {
            RatingSought = RatingType.Part135PICIFR;
            BaseFAR = "135.243(c)(2)";
            FARLink = "https://www.law.cornell.edu/cfr/text/14/135.243";
            string szFAR = ResolvedFAR(string.Empty);
            Title = Resources.MilestoneProgress.Title135243PICIFR;
            GeneralDisclaimer = Branding.ReBrand(Resources.MilestoneProgress.Part135PICDisclaimer);
            miMinTimeAsPilot = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part135PICMinTime, minTime), szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minTime);
            miMinXCTime = new MilestoneItem(String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.Part135PICXCMinTime, minXCTime), szFAR, Resources.MilestoneProgress.Part135XCNote, MilestoneItem.MilestoneType.Time, minXCTime);
            miMinNightTime = new MilestoneItem(Resources.MilestoneProgress.Part135PICIFRNightTime, szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minNightTime);
            miMinIFRTime = new MilestoneItem(Resources.MilestoneProgress.Part135PICIFRTime, szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minIFRTime);
            miMinIFRAircraftTime = new MilestoneItem(Resources.MilestoneProgress.Part135PICIFRTimeInFlight, szFAR, string.Empty, MilestoneItem.MilestoneType.Time, minIFRAircraftTime);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            decimal IMCTime = cfr.IMC + cfr.IMCSim;
            if (cfr.fIsCertifiedIFR)
                miMinIFRTime.AddEvent(IMCTime);

            if (!cfr.fIsRealAircraft)
                return;

            decimal part135XC = Part135Milestones.Part135CrossCountry(cfr);

            miMinTimeAsPilot.AddEvent(cfr.Total);
            miMinXCTime.AddEvent(part135XC);
            miMinNightTime.AddEvent(cfr.Night);
            miMinIFRAircraftTime.AddEvent(IMCTime);
        }

        public override Collection<MilestoneItem> Milestones
        {
            get { return new Collection<MilestoneItem>() { miMinTimeAsPilot, miMinXCTime, miMinNightTime, miMinIFRTime, miMinIFRAircraftTime }; }
        }
    }
    #endregion
}