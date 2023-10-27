﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2007-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    public static class CustomCurrencyTimespanExtensions
    {
        #region extension methods for CustomCurrencyTimespanTypeTimespanType
        /// <summary>
        /// For fixed-range timespan ranges, returns the month (1 = Jan) for alignment
        /// </summary>
        /// <param name="tst"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static int AlignmentMonth(this TimespanType tst)
        {
            switch (tst)
            {
                default:
                    throw new ArgumentException("Unknown duration for TimeSpanType: {0}", tst.ToString());
                case TimespanType.CalendarMonths:
                case TimespanType.Days:
                case TimespanType.SlidingMonths:
                    throw new ArgumentException("CalendarMonths, sliding months, and days do not have an alignment date");
                case TimespanType.ThreeMonthJan:
                case TimespanType.FourMonthJan:
                case TimespanType.SixMonthJan:
                case TimespanType.TwelveMonthJan:
                    return 1;
                case TimespanType.ThreeMonthFeb:
                case TimespanType.FourMonthFeb:
                case TimespanType.SixMonthFeb:
                case TimespanType.TwelveMonthFeb:
                    return 2;
                case TimespanType.ThreeMonthMar:
                case TimespanType.FourMonthMar:
                case TimespanType.SixMonthMar:
                case TimespanType.TwelveMonthMar:
                    return 3;
                case TimespanType.FourMonthApr:
                case TimespanType.SixMonthApr:
                case TimespanType.TwelveMonthApr:
                    return 4;
                case TimespanType.SixMonthMay:
                case TimespanType.TwelveMonthMay:
                    return 5;
                case TimespanType.SixMonthJun:
                case TimespanType.TwelveMonthJun:
                    return 6;
                case TimespanType.TwelveMonthJul:
                    return 7;
                case TimespanType.TwelveMonthAug:
                    return 8;
                case TimespanType.TwelveMonthSep:
                    return 9;
                case TimespanType.TwelveMonthOct:
                    return 10;
                case TimespanType.TwelveMonthNov:
                    return 11;
                case TimespanType.TwelveMonthDec:
                    return 12;
            }
        }

        /// <summary>
        /// Returns the number of months duration for the specified timespantype
        /// </summary>
        /// <param name="tst"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static int Duration(this TimespanType tst)
        {
            switch (tst)
            {
                default:
                    throw new ArgumentException("Unknown duration for TimeSpanType: {0}", tst.ToString());
                case TimespanType.SlidingMonths:
                    throw new ArgumentException("Sliding months do not have a duration of whole months (ironically)");
                case TimespanType.Days:
                    throw new ArgumentException("Days do not have a duration of months");
                case TimespanType.CalendarMonths:
                    return 1;
                case TimespanType.ThreeMonthJan:
                case TimespanType.ThreeMonthFeb:
                case TimespanType.ThreeMonthMar:
                    return 3;
                case TimespanType.FourMonthJan:
                case TimespanType.FourMonthFeb:
                case TimespanType.FourMonthMar:
                case TimespanType.FourMonthApr:
                    return 4;
                case TimespanType.SixMonthJan:
                case TimespanType.SixMonthFeb:
                case TimespanType.SixMonthMar:
                case TimespanType.SixMonthApr:
                case TimespanType.SixMonthMay:
                case TimespanType.SixMonthJun:
                    return 6;
                case TimespanType.TwelveMonthJan:
                case TimespanType.TwelveMonthFeb:
                case TimespanType.TwelveMonthMar:
                case TimespanType.TwelveMonthApr:
                case TimespanType.TwelveMonthMay:
                case TimespanType.TwelveMonthJun:
                case TimespanType.TwelveMonthJul:
                case TimespanType.TwelveMonthAug:
                case TimespanType.TwelveMonthSep:
                case TimespanType.TwelveMonthOct:
                case TimespanType.TwelveMonthNov:
                case TimespanType.TwelveMonthDec:
                    return 12;
            }
        }

        /// <summary>
        /// Human readable string for the timespan type
        /// </summary>
        /// <param name="tst"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static string DisplayString(this TimespanType tst)
        {
            bool isAligned = tst.IsAligned();
            int alignMonth = isAligned ? tst.AlignmentMonth() : 1;
            int duration = isAligned ? tst.Duration() : 1;
            DateTime dt1 = new DateTime(DateTime.Now.Year, alignMonth, 1);
            DateTime dt2 = dt1.AddMonths(duration);
            DateTime dt3 = dt2.AddMonths(duration);
            DateTime dt4 = dt3.AddMonths(duration);
            switch (tst)
            {
                default:
                    throw new ArgumentException("Unknown duration for TimeSpanType: {0}", tst.ToString());
                case TimespanType.CalendarMonths:
                    return Resources.Currency.CustomCurrencyMonths;
                case TimespanType.Days:
                    return Resources.Currency.CustomCurrencyDays;
                case TimespanType.SlidingMonths:
                    return Resources.Currency.CustomCurrencySlidingMonths;
                case TimespanType.ThreeMonthJan:
                case TimespanType.ThreeMonthFeb:
                case TimespanType.ThreeMonthMar:
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrency3Month, dt1.ToString("MMM", CultureInfo.CurrentCulture), dt2.ToString("MMM", CultureInfo.CurrentCulture), dt3.ToString("MMM", CultureInfo.CurrentCulture), dt4.ToString("MMM", CultureInfo.CurrentCulture));
                case TimespanType.FourMonthJan:
                case TimespanType.FourMonthFeb:
                case TimespanType.FourMonthMar:
                case TimespanType.FourMonthApr:
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrency4Month, dt1.ToString("MMM", CultureInfo.CurrentCulture), dt2.ToString("MMM", CultureInfo.CurrentCulture), dt3.ToString("MMM", CultureInfo.CurrentCulture));
                case TimespanType.SixMonthJan:
                case TimespanType.SixMonthFeb:
                case TimespanType.SixMonthMar:
                case TimespanType.SixMonthApr:
                case TimespanType.SixMonthMay:
                case TimespanType.SixMonthJun:
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrency6Month, dt1.ToString("MMM", CultureInfo.CurrentCulture), dt2.ToString("MMM", CultureInfo.CurrentCulture));
                case TimespanType.TwelveMonthJan:
                case TimespanType.TwelveMonthFeb:
                case TimespanType.TwelveMonthMar:
                case TimespanType.TwelveMonthApr:
                case TimespanType.TwelveMonthMay:
                case TimespanType.TwelveMonthJun:
                case TimespanType.TwelveMonthJul:
                case TimespanType.TwelveMonthAug:
                case TimespanType.TwelveMonthSep:
                case TimespanType.TwelveMonthOct:
                case TimespanType.TwelveMonthNov:
                case TimespanType.TwelveMonthDec:
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrency12Month, dt1.ToString("MMM", CultureInfo.CurrentCulture));
            }
        }

        /// <summary>
        /// True if this is an aligned bucket (vs. normal currency computations which look back from today)
        /// </summary>
        /// <param name="tst"></param>
        /// <returns></returns>
        public static bool IsAligned(this TimespanType tst)
        {
            return tst != TimespanType.Days && tst != TimespanType.CalendarMonths && tst != TimespanType.SlidingMonths;
        }
        #endregion
    }

    /// <summary>
    /// Local class to pair a singular/plural name with an event type.
    /// </summary>
    public static class CustomCurrencyEventTypeExtensions
    {
        #region extension methods for CustomCurrencyEvenType
        private static Dictionary<CustomCurrency.CustomCurrencyEventType, string[]> s_eventTypeLabels;

        private static string[] LabelsForEventType(CustomCurrency.CustomCurrencyEventType ccet)
        {
            if (s_eventTypeLabels == null)
            {
                s_eventTypeLabels = new Dictionary<CustomCurrency.CustomCurrencyEventType, string[]>()
                    {
                        { CustomCurrency.CustomCurrencyEventType.Flights, new string[] { Resources.Currency.CustomCurrencyEventFlight, Resources.Currency.CustomCurrencyEventFlights } },
                        { CustomCurrency.CustomCurrencyEventType.Landings, new string[] { Resources.Currency.CustomCurrencyEventLanding, Resources.Currency.CustomCurrencyEventLandings } },
                        { CustomCurrency.CustomCurrencyEventType.TakeoffsAny, new string[] {Resources.Currency.CustomCurrencyEventTakeoff, Resources.Currency.CustomCurrencyEventTakeoffs} },
                        { CustomCurrency.CustomCurrencyEventType.Hours, new string[] { Resources.Currency.CustomCurrencyEventHour, Resources.Currency.CustomCurrencyEventHours } },
                        { CustomCurrency.CustomCurrencyEventType.IFRHours, new string[] { Resources.Currency.CustomCurrencyEventInstrumentHour, Resources.Currency.CustomCurrencyEventInstrumentHours } },
                        { CustomCurrency.CustomCurrencyEventType.IFRApproaches, new string[] { Resources.Currency.CustomCurrencyEventApproach, Resources.Currency.CustomCurrencyEventApproaches } },
                        { CustomCurrency.CustomCurrencyEventType.BaseCheck, new string[] { Resources.Currency.CustomCurrencyEventBaseCheck, Resources.Currency.CustomCurrencyEventBaseChecks } },
                        { CustomCurrency.CustomCurrencyEventType.UASLaunch, new string[] { Resources.Currency.CustomCurrencyEventLaunch, Resources.Currency.CustomCurrencyEventLaunches } },
                        { CustomCurrency.CustomCurrencyEventType.UASRecovery, new string[] { Resources.Currency.CustomCurrencyEventRecovery, Resources.Currency.CustomCurrencyEventRecoveries } },
                        { CustomCurrency.CustomCurrencyEventType.NightLandings, new string[] { Resources.Currency.CustomCurrencyEventNightLanding, Resources.Currency.CustomCurrencyEventNightLandings } },
                        { CustomCurrency.CustomCurrencyEventType.NightTakeoffs, new string[] { Resources.Currency.CustomCurrencyEventNightTakeoff, Resources.Currency.CustomCurrencyEventNightTakeoffs } },
                        { CustomCurrency.CustomCurrencyEventType.PICLandings, new string[] { Resources.Currency.CustomCurrencyEventLandingPIC, Resources.Currency.CustomCurrencyEventLandingsPIC } },
                        { CustomCurrency.CustomCurrencyEventType.PICNightLandings, new string[] { Resources.Currency.CustomCurrencyEventNightLandingPIC, Resources.Currency.CustomCurrencyEventNightLandingsPIC } },
                        { CustomCurrency.CustomCurrencyEventType.TotalHours, new string[] { Resources.Currency.CustomCurrencyEventTotalHour, Resources.Currency.CustomCurrencyEventTotalHours } },
                        { CustomCurrency.CustomCurrencyEventType.GroundSimHours, new string[] { Resources.Currency.CustomCurrencyEventSimulatorHour, Resources.Currency.CustomCurrencyEventSimulatorHours } },
                        { CustomCurrency.CustomCurrencyEventType.BackseatHours, new string[] { Resources.Currency.CustomCurrencyEventBackSeatHour, Resources.Currency.CustomCurrencyEventBackSeatHours } },
                        { CustomCurrency.CustomCurrencyEventType.FrontSeatHours, new string[] { Resources.Currency.CustomCurrencyEventFrontSeatHour, Resources.Currency.CustomCurrencyEventFrontSeatHours } },
                        { CustomCurrency.CustomCurrencyEventType.HoistOperations, new string[] { Resources.Currency.CustomCurrencyHoistOperation, Resources.Currency.CustomCurrencyHoistOperations } },
                        { CustomCurrency.CustomCurrencyEventType.NVHours, new string[] { Resources.Currency.CustomCurrencyEventNVHour, Resources.Currency.CustomCurrencyEventNVHours } },
                        { CustomCurrency.CustomCurrencyEventType.NVGoggles, new string[] { Resources.Currency.CustomCurrencyEventNVGHour, Resources.Currency.CustomCurrencyEventNVGHours } },
                        { CustomCurrency.CustomCurrencyEventType.NVFLIR, new string[] { Resources.Currency.CustomCurrencyEventNVSHour, Resources.Currency.CustomCurrencyEventNVSHours } },
                        { CustomCurrency.CustomCurrencyEventType.LandingsHighAltitude, new string[] { Resources.Currency.CustomCurrencyEventHighAltitudeLanding, Resources.Currency.CustomCurrencyEventHighAltitudeLandings } },
                        { CustomCurrency.CustomCurrencyEventType.HoursDual, new string[] { Resources.Currency.CustomCurrencyEventDualHour, Resources.Currency.CustomCurrencyEventDualHours } },
                        { CustomCurrency.CustomCurrencyEventType.InstructionGiven, new string[] {Resources.Currency.CustomCurrencyEventCFIHour, Resources.Currency.CustomCurrencyEventCFIHours} },
                        { CustomCurrency.CustomCurrencyEventType.NightFlight, new string[] { Resources.Currency.CustomCurrencyEventNightHour, Resources.Currency.CustomCurrencyEventNightHours } },
                        { CustomCurrency.CustomCurrencyEventType.CAP5Checkride, new string[] { Resources.Currency.CustomCurrencyEventCap5Checkride, Resources.Currency.CustomCurrencyEventCap5Checkrides } },
                        { CustomCurrency.CustomCurrencyEventType.CAP91Checkride, new string[] { Resources.Currency.CustomCurrencyEventCap91Checkride, Resources.Currency.CustomCurrencyEventCap91Checkrides } },
                        { CustomCurrency.CustomCurrencyEventType.FMSApproaches, new string[] { Resources.Currency.CustomCurrencyEventFMSApproach, Resources.Currency.CustomCurrencyEventFMSApproaches } },
                        { CustomCurrency.CustomCurrencyEventType.NightTouchAndGo, new string[] {Resources.Currency.CustomCurrencyEventNightTouchAndGo, Resources.Currency.CustomCurrencyEventNightTouchAndGos} },
                        { CustomCurrency.CustomCurrencyEventType.GliderTow, new string[] {Resources.Currency.CustomCurrencyEventGliderTow, Resources.Currency.CustomCurrencyEventGliderTows } },
                        { CustomCurrency.CustomCurrencyEventType.IPC, new string[] {Resources.Currency.CustomCurrencyEventIPC, Resources.Currency.CustomCurrencyEventIPC } },
                        { CustomCurrency.CustomCurrencyEventType.FlightReview, new string[] {Resources.Currency.CustomCurrencyEventFlightReview, Resources.Currency.CustomCurrencyEventFlightReview } },
                        { CustomCurrency.CustomCurrencyEventType.NightLandingAny, new string[] {Resources.Currency.CustomCurrencyEventNightLandingAny, Resources.Currency.CustomCurrencyEventNightLandingsAny } },
                        { CustomCurrency.CustomCurrencyEventType.RestTime, new string[] {Resources.Currency.CustomCurrencyEventRestHour, Resources.Currency.CustomCurrencyEventRestHours } },
                        { CustomCurrency.CustomCurrencyEventType.DutyTime, new string[] { Resources.Currency.CustomCurrencyEventDutyHour, Resources.Currency.CustomCurrencyEventDutyHours } },
                        { CustomCurrency.CustomCurrencyEventType.FlightDutyTime, new string[] {Resources.Currency.CustomCurrencyEventFlightDutyHour, Resources.Currency.CustomCurrencyEventFlightDutyHours } },
                        { CustomCurrency.CustomCurrencyEventType.SpecialAuthorizationApproach, new string[] { Resources.Currency.CustomCurrencyEventSAApproach, Resources.Currency.CustomCurrencyEventSAApproaches } },
                        { CustomCurrency.CustomCurrencyEventType.EnhancedVisionApproach, new string[] { Resources.Currency.CustomCurrencyEventEVApproach, Resources.Currency.CustomCurrencyEventEVApproaches } },
                        { CustomCurrency.CustomCurrencyEventType.NVUnaidedTime, new string[] { Resources.Currency.CustomCurrencyEventNVUnaidedHour , Resources.Currency.CustomCurrencyEventNVUnaidedHours} }
                    };
            }

            return s_eventTypeLabels.ContainsKey(ccet) ? s_eventTypeLabels[ccet] : null;
        }

        /// <summary>
        /// Description for the event type when there is one event.  E.g., "1 flight"
        /// </summary>
        /// <param name="ccet"></param>
        /// <returns></returns>
        public static string SingularName(this CustomCurrency.CustomCurrencyEventType ccet)
        {
            string[] rgsz = LabelsForEventType(ccet);
            return (rgsz == null || rgsz.Length < 1) ? ccet.ToString() : rgsz[0];
        }

        /// <summary>
        /// Description for the event type when there are multiple events.  E.g., "2 flights"
        /// </summary>
        /// <param name="ccet"></param>
        /// <returns></returns>
        public static string PluralName(this CustomCurrency.CustomCurrencyEventType ccet)
        {
            string[] rgsz = LabelsForEventType(ccet);
            return (rgsz == null || rgsz.Length < 2) ? ccet.ToString() : rgsz[1];
        }

        public static bool IsIntegerOnly(this CustomCurrency.CustomCurrencyEventType ccet)
        {
            switch (ccet)
            {
                case CustomCurrency.CustomCurrencyEventType.Flights:
                case CustomCurrency.CustomCurrencyEventType.Landings:
                case CustomCurrency.CustomCurrencyEventType.TakeoffsAny:
                case CustomCurrency.CustomCurrencyEventType.IFRApproaches:
                case CustomCurrency.CustomCurrencyEventType.BaseCheck:
                case CustomCurrency.CustomCurrencyEventType.UASLaunch:
                case CustomCurrency.CustomCurrencyEventType.UASRecovery:
                case CustomCurrency.CustomCurrencyEventType.NightLandings:
                case CustomCurrency.CustomCurrencyEventType.NightTouchAndGo:
                case CustomCurrency.CustomCurrencyEventType.NightLandingAny:
                case CustomCurrency.CustomCurrencyEventType.NightTakeoffs:
                case CustomCurrency.CustomCurrencyEventType.PICLandings:
                case CustomCurrency.CustomCurrencyEventType.PICNightLandings:
                case CustomCurrency.CustomCurrencyEventType.HoistOperations:
                case CustomCurrency.CustomCurrencyEventType.LandingsHighAltitude:
                case CustomCurrency.CustomCurrencyEventType.CAP5Checkride:
                case CustomCurrency.CustomCurrencyEventType.CAP91Checkride:
                case CustomCurrency.CustomCurrencyEventType.FMSApproaches:
                case CustomCurrency.CustomCurrencyEventType.GliderTow:
                case CustomCurrency.CustomCurrencyEventType.IPC:
                case CustomCurrency.CustomCurrencyEventType.FlightReview:
                case CustomCurrency.CustomCurrencyEventType.SpecialAuthorizationApproach:
                case CustomCurrency.CustomCurrencyEventType.EnhancedVisionApproach:
                    return true;
                    /*
                case CustomCurrency.CustomCurrencyEventType.TotalHours:
                case CustomCurrency.CustomCurrencyEventType.Hours:
                case CustomCurrency.CustomCurrencyEventType.IFRHours:
                case CustomCurrency.CustomCurrencyEventType.GroundSimHours:
                case CustomCurrency.CustomCurrencyEventType.BackseatHours:
                case CustomCurrency.CustomCurrencyEventType.FrontSeatHours:
                case CustomCurrency.CustomCurrencyEventType.NVHours:
                case CustomCurrency.CustomCurrencyEventType.NVGoggles:
                case CustomCurrency.CustomCurrencyEventType.NVFLIR:
                case CustomCurrency.CustomCurrencyEventType.NightFlight:
                case CustomCurrency.CustomCurrencyEventType.HoursDual:
                case CustomCurrency.CustomCurrencyEventType.RestTime:
                case CustomCurrency.CustomCurrencyEventType.DutyTime:
                case CustomCurrency.CustomCurrencyEventType.FlightDutyTime:
                */
                default:
                    return false;
            }
        }

        /// <summary>
        /// Is this custom currency type something that depends on duty time calculations?
        /// </summary>
        /// <param name="ccet"></param>
        /// <returns></returns>
        public static bool IsDutyTimeEvent(this CustomCurrency.CustomCurrencyEventType ccet)
        {
            switch (ccet)
            {
                case CustomCurrency.CustomCurrencyEventType.DutyTime:
                case CustomCurrency.CustomCurrencyEventType.FlightDutyTime:
                case CustomCurrency.CustomCurrencyEventType.RestTime:
                    return true;
            }
            return false;
        }
        #endregion
    }

    /// <summary>
    /// Currencies that are defined by the user.
    /// </summary>
    public class CustomCurrency : FlightCurrency
    {
        /// <summary>
        /// Indicates the type of event we are looking for
        /// </summary>
        public enum CustomCurrencyEventType
        {
            // NOTE: DO NOT REORDER THESE, THEY ARE PERSISTED IN THE DATABASE
            Flights = 0,
            Landings = 1,
            Hours = 2,
            IFRHours = 3,
            IFRApproaches = 4,
            BaseCheck = 5,
            UASLaunch = 6,
            UASRecovery = 7,
            NightLandings = 8,
            NightTakeoffs = 9,
            PICLandings = 10,
            PICNightLandings = 11,
            TotalHours = 12,
            GroundSimHours = 13,
            BackseatHours = 14,
            FrontSeatHours = 15,
            HoistOperations = 16,
            NVHours = 17,
            NVGoggles = 18,
            NVFLIR = 19,
            LandingsHighAltitude = 20,
            NightFlight = 21,
            CAP5Checkride = 22,
            CAP91Checkride = 23,
            FMSApproaches = 24,
            HoursDual = 25,
            NightTouchAndGo = 26,
            GliderTow = 27,
            IPC = 28,
            FlightReview = 29,
            NightLandingAny = 30,
            RestTime = 31,
            DutyTime = 32,
            FlightDutyTime = 33,
            TakeoffsAny = 34,
            SpecialAuthorizationApproach = 35,
            EnhancedVisionApproach = 36,
            InstructionGiven = 37,
            NVUnaidedTime = 38
        };

        public enum CurrencyRefType { Aircraft = 0, Models = 1, Properties = 2 };

        /// <summary>
        /// Is this a required minimum number of events, or a "do not exceed?"
        /// </summary>
        public enum LimitType { Minimum, Maximum };

        private const int ID_UNKNOWN = -1;

        readonly string m_szTailNumbers, m_szModelNames;
        string m_szCatClass;
        readonly string m_szPropertyNames;

        #region Initialization
        public CustomCurrency()
        {
            ModelsRestriction = new List<int>();
            AircraftRestriction = new List<int>();
            ID = ID_UNKNOWN;
            m_szTailNumbers = m_szModelNames = m_szCatClass = ErrorString = UserName = CategoryRestriction = string.Empty;
            CurrencyTimespanType = TimespanType.Days;
            AlignedStartDate = null;
            CurrencyLimitType = LimitType.Minimum;
        }

        private CustomCurrency(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            ID = Convert.ToInt32(dr["idCurrency"], CultureInfo.InvariantCulture);
            UserName = dr["username"].ToString();
            DisplayName = dr["name"].ToString();
            Discrepancy = RequiredEvents = Convert.ToDecimal(dr["minEvents"], CultureInfo.InvariantCulture);
            CurrencyLimitType = (LimitType)Convert.ToInt32(dr["limitType"], CultureInfo.InvariantCulture);

            CurrencyTimespanType = (TimespanType) Convert.ToInt32(dr["timespanType"], CultureInfo.InvariantCulture);

            if (CurrencyTimespanType.IsAligned())
            {
                int mNow = DateTime.Now.Month;
                int mAlign = CurrencyTimespanType.AlignmentMonth();
                int d = CurrencyTimespanType.Duration();
                int y = DateTime.Now.Year;

                if (mNow < mAlign)
                {
                    mNow += 12;
                    y--;
                }
                AlignedStartDate = new DateTime(y, mNow - ((mNow - mAlign) % d), 1);

                // Set the expiration span in days
                ExpirationSpan = AlignedStartDate.Value.AddMonths(d).Subtract(AlignedStartDate.Value).Days;
            }
            else
                ExpirationSpan = Convert.ToInt32(dr["timespan"], CultureInfo.InvariantCulture);

            EventType = (CustomCurrencyEventType)Convert.ToInt32(dr["eventType"], CultureInfo.InvariantCulture);
            CategoryRestriction = dr["categoryRestriction"].ToString();
            CatClassRestriction = (CategoryClass.CatClassID)Convert.ToInt32(dr["catClassRestriction"], CultureInfo.InvariantCulture);
            AirportRestriction = dr["airportRestriction"].ToString();
            TextRestriction = dr["textRestriction"].ToString();

            ModelsRestriction = new List<int>(dr["ModelsRestriction"].ToString().ToInts());
            AircraftRestriction = new List<int>(dr["AircraftRestriction"].ToString().ToInts());
            PropertyRestriction = new List<int>(dr["PropertyRestriction"].ToString().ToInts());

            // convenience properties - not persisted
            m_szTailNumbers = dr["AircraftDisplay"].ToString();
            m_szModelNames = dr["ModelsDisplay"].ToString();
            m_szCatClass = dr["CatClassDisplay"].ToString();
            m_szPropertyNames = dr["PropertyDisplay"].ToString();
            IsActive = Convert.ToInt32(dr["isinactive"], CultureInfo.InvariantCulture) == 0;
        }
        #endregion
        
        #region Properties
        /// <summary>
        /// Last error
        /// </summary>
        public string ErrorString { get; set; }

        /// <summary>
        /// ID for the currency
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Username of the owner
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Type of event which contributes currency
        /// </summary>
        public CustomCurrencyEventType EventType { get; set; }

        /// <summary>
        /// Name of the category to which this applies, null or empty for any
        /// </summary>
        public string CategoryRestriction { get; set; }

        /// <summary>
        /// ID of the catclass to which this applies, 0 for any
        /// </summary>
        public CategoryClass.CatClassID CatClassRestriction { get; set; }

        /// <summary>
        /// Airport which must be visited
        /// </summary>
        public string AirportRestriction { get; set; }

        /// <summary>
        /// Text contained in the comments field or any text properties
        /// </summary>
        public string TextRestriction { get; set; }

        /// <summary>
        /// ID's of properties that must be present
        /// </summary>
        public IEnumerable<int> PropertyRestriction { get; set; }

        override public string DiscrepancyString
        {
            get
            {
                if (CurrencyLimitType == LimitType.Maximum)
                    return string.Empty;    // no discrepancy to show
                if (CurrencyTimespanType.IsAligned())
                    return CurrentState == CurrencyState.OK ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAlignedDueDate, AlignedDueDate.Value.ToShortDateString());
                else
                    return IsCurrent() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy.ToString(EventType.IsIntegerOnly() ? "0" : "#,#.0#", CultureInfo.CurrentCulture), EventTypeLabel(this.Discrepancy, EventType));
            }
        }

        /// <summary>
        /// If this is an aligned property (e.g., 6 months April aligned), this is the start date based on today's date
        /// </summary>
        public DateTime? AlignedStartDate { get; set; }

        public DateTime? AlignedDueDate
        {
            get
            {
                return (AlignedStartDate.HasValue) ? AlignedStartDate.Value.AddMonths(CurrencyTimespanType.Duration()).AddDays(-1) : (DateTime?)null;
            }
        }

        /// <summary>
        /// Specify models that contribute to currency
        /// </summary>
        public IEnumerable<int> ModelsRestriction { get; set; }

        /// <summary>
        /// Specify aircraft (plural) that contribute to currency
        /// </summary>
        public IEnumerable<int> AircraftRestriction { get; set; }

        /// <summary>
        /// By default, we are looking for a MINIMUM number of qualifying events within a particular time frame
        /// If we set the Limit to Maximum, then we are looking for a MAXIMUM number of events.  E.g., no more than 10 flights in a 15 day period.
        /// This turns the currency on its head.  It basically inverts: you start off current and then lose currency, and you
        /// have a date when you've filled up and THEN you have an expiration date
        /// </summary>
        public LimitType CurrencyLimitType { get; set; }

        /// <summary>
        /// Local state dictionary for additional data as needed.
        /// </summary>
        protected Dictionary<string, object> AdditionalState { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Is the currency active for use?
        /// </summary>
        public bool IsActive { get; set; } = true;
        #endregion

        #region specific required state objects
        protected const string szStateKeyDutyPeriodExaminer = "dutyPeriod";

        protected DutyPeriodExaminer _DutyPeriodExaminer
        {
            get
            {
                if (!AdditionalState.ContainsKey(szStateKeyDutyPeriodExaminer))
                    AdditionalState[szStateKeyDutyPeriodExaminer] = new DutyPeriodExaminer(Profile.GetUser(UserName).UsesFAR117DutyTimeAllFlights);
                return (DutyPeriodExaminer)AdditionalState[szStateKeyDutyPeriodExaminer];
            }
        }

        #endregion

        /// <summary>
        /// Retrieves custom currencies for the specified user
        /// </summary>
        /// <param name="szUser">User name</param>
        /// <param name="fActiveOnly">If true, only returns active properties</param>
        /// <returns></returns>
        /// <exception cref="MyFlightbookException"></exception>
        static public IEnumerable<CustomCurrency> CustomCurrenciesForUser(string szUser, bool fActiveOnly = false)
        {
            List<CustomCurrency> lst = new List<CustomCurrency>();
            DBHelper dbh = new DBHelper(ConfigurationManager.AppSettings["CustomCurrencyForUserQuery"]);
            if (!dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("uname", szUser); },
                (dr) => { lst.Add(new CustomCurrency(dr)); }))
                throw new MyFlightbookException("Exception in CustomCurrenciesForUser - setup: " + dbh.LastError);

            if (fActiveOnly)
                lst.RemoveAll(cc => !cc.IsActive);

            return lst;
        }

        #region Overrides to support aligned currencies
        public override CurrencyState CurrentState
        {
            get
            {
                CurrencyState cs;

                if (CurrencyTimespanType.IsAligned())
                    cs = NumEvents >= RequiredEvents ? CurrencyState.OK : CurrencyState.NotCurrent;
                else if (EventType.IsDutyTimeEvent())
                {
                    double ratio = (double) (NumEvents / RequiredEvents);
                    if (CurrencyLimitType == LimitType.Minimum)
                        return (ratio > 1.15) ? CurrencyState.OK : (ratio >= 1.0 ? CurrencyState.GettingClose : CurrencyState.NotCurrent);
                    else
                        return (ratio > 1.0) ? CurrencyState.NotCurrent : (ratio > 0.85 ? CurrencyState.GettingClose : CurrencyState.OK);
                }
                else
                    cs = base.CurrentState;

                // If the limit is set as a maximum number of events, then invert it.
                if (CurrencyLimitType == LimitType.Maximum)
                {
                    switch (cs)
                    {
                        case CurrencyState.GettingClose:
                        case CurrencyState.OK:
                            cs = CurrencyState.NotCurrent;
                            break;
                        case CurrencyState.NotCurrent:
                            cs = (Discrepancy < 3) ? CurrencyState.GettingClose : CurrencyState.OK;
                            break;
                        default:
                            break;
                    }
                }
                return cs;
            }
        }

        public override string StatusDisplay
        {
            get
            {
                // short-circuit dutytime events; these are simply amounts relative to a limit
                if (EventType.IsDutyTimeEvent())
                    return String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyDutyPeriodStatus, NumEvents, RequiredEvents, EventTypeLabel(RequiredEvents, EventType));

                switch (CurrencyLimitType)
                {
                    case LimitType.Minimum:
                        if (CurrencyTimespanType.IsAligned())
                            return CurrentState == CurrencyState.OK ?
                                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAlignedStatusCompleted, RequiredEvents, EventTypeLabel(RequiredEvents, EventType), AlignedDueDate.Value.ToShortDateString()) :
                                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAlignedStatusIncomplete, NumEvents, RequiredEvents, EventTypeLabel(RequiredEvents, EventType));
                        else
                            return base.StatusDisplay;
                    case LimitType.Maximum:
                        return CurrentState == CurrencyState.NotCurrent ?
                            String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyStatusLimitExceeded, (CurrencyTimespanType.IsAligned() ? AlignedDueDate.Value : ExpirationDate).ToShortDateString()) :
                            String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyStatusLimitOK, Discrepancy, RequiredEvents, EventTypeLabel(RequiredEvents, EventType));
                    default:
                        return Resources.Currency.FormatNeverCurrent;
                }
            }
        }

        public override bool HasBeenCurrent
        {
            get
            {
                return CurrencyTimespanType.IsAligned() || CurrencyLimitType == LimitType.Maximum || EventType.IsDutyTimeEvent() || base.HasBeenCurrent;
            }
        }

        public bool IsNew { get { return ID == ID_UNKNOWN; } }
        #endregion

        public Boolean FIsValid()
        {
            ErrorString = string.Empty;
            if (String.IsNullOrEmpty(DisplayName))
                ErrorString = Resources.Currency.errCustomCurrencyNoName;
            if (ExpirationSpan <= 0 && !CurrencyTimespanType.IsAligned())
                ErrorString = Resources.Currency.errCusotmCurrencyBadTimeSpan;
            if (RequiredEvents <= 0)
                ErrorString = Resources.Currency.errCustomCurrencyBadRequiredEvents;
            if (RequiredEvents > 2000)
                ErrorString = Resources.Currency.errCustomCurrencyInvalidEventCount;
            if (DisplayName.Length > 45)
                ErrorString = Resources.Currency.errCustomCurrencyNameTooLong;

            return String.IsNullOrEmpty(ErrorString);
        }

        public Boolean FCommit()
        {
            bool fIsNew = IsNew;

            if (!FIsValid())
                return false;

            string szSet = @"SET 
username=?uname, name=?name, minEvents=?minEvents, limitType=?limit, timespan=?timespan, timespantype=?timespanType, eventType=?eventType, 
categoryRestriction=?categoryRestriction, catClassRestriction=?catClassRestriction, airportRestriction=?airportRestriction, textRestriction=?textRestriction, isinactive=?inactive";

            string szQ = String.Format(CultureInfo.InvariantCulture, "REPLACE INTO customcurrency {0}{1}", szSet, fIsNew ? string.Empty : ", idCurrency=?id");

            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(szQ,
                (comm) =>
                {
                    comm.Parameters.AddWithValue("id", ID);
                    comm.Parameters.AddWithValue("uname", UserName);
                    comm.Parameters.AddWithValue("name", DisplayName.LimitTo(44));
                    comm.Parameters.AddWithValue("minEvents", RequiredEvents);
                    comm.Parameters.AddWithValue("limit", (int)CurrencyLimitType);
                    comm.Parameters.AddWithValue("timespan", ExpirationSpan);
                    comm.Parameters.AddWithValue("timespantype", (int)CurrencyTimespanType);
                    comm.Parameters.AddWithValue("eventType", (int)EventType);
                    comm.Parameters.AddWithValue("categoryRestriction", String.IsNullOrEmpty(CategoryRestriction) ? string.Empty : CategoryRestriction);
                    comm.Parameters.AddWithValue("catClassrestriction", (int)CatClassRestriction);
                    comm.Parameters.AddWithValue("airportRestriction", AirportRestriction.LimitTo(45));
                    comm.Parameters.AddWithValue("textRestriction", TextRestriction.LimitTo(254));
                    comm.Parameters.AddWithValue("inactive", !IsActive);
                });

            string szErr = (dbh.LastError.Length > 0) ? szErr = "Error saving customcurrency: " + szQ + "\r\n" + dbh.LastError : "";

            if (szErr.Length > 0)
                throw new MyFlightbookException(szErr);

            if (fIsNew)
                ID = dbh.LastInsertedRowId;

            // Always re-write the models/aircraft restrictions.
            string szDeleteExisting = String.Format(CultureInfo.InvariantCulture, "DELETE FROM custcurrencyref WHERE idCurrency={0}", ID);
            dbh.DoNonQuery(szDeleteExisting,
                (comm) => { });

            string szInsert = "INSERT INTO custcurrencyref SET idCurrency={0}, value={1}, type={2}";
            // Write out the models
            foreach (int idModel in ModelsRestriction)
            {
                dbh.CommandText = String.Format(CultureInfo.InvariantCulture, szInsert, ID, idModel, (int)CurrencyRefType.Models);
                dbh.DoNonQuery();
            }

            // Write out the aircraft
            foreach (int idAircraft in AircraftRestriction)
            {
                dbh.CommandText = String.Format(CultureInfo.InvariantCulture, szInsert, ID, idAircraft, (int)CurrencyRefType.Aircraft);
                dbh.DoNonQuery();
            }

            foreach (int idProp in PropertyRestriction)
            {
                dbh.CommandText = String.Format(CultureInfo.InvariantCulture, szInsert, ID, idProp, (int)CurrencyRefType.Properties);
                dbh.DoNonQuery();
            }

            return (szErr.Length == 0);
        }

        public void FDelete()
        {
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery(String.Format(CultureInfo.InvariantCulture, "DELETE FROM customcurrency WHERE idCurrency={0}", ID), (comm) => { });
            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException("Error deleting customcurrency: " + dbh.LastError);
        }

        public static string EventTypeLabel(Decimal count, CustomCurrencyEventType ccet)
        {
            return (count == 1) ? ccet.SingularName() : ccet.PluralName();
        }

        override public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string szLimit = (CurrencyLimitType == LimitType.Minimum) ? Resources.Currency.CustomCurrencyMinEventsPrompt : Resources.Currency.CustomCurrencyMaxEventsPrompt;
            string szMinEvents = String.Format(CultureInfo.CurrentCulture, EventType.IsIntegerOnly() ? "{0:0}" : "{0:#,#.0#}", RequiredEvents);
            string szBase = CurrencyTimespanType.IsAligned() ?
                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyDescriptionAligned, szLimit, szMinEvents, EventTypeLabel(RequiredEvents, EventType), CurrencyTimespanType.DisplayString()) :
                String.Format(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyDescription, szLimit, szMinEvents, EventTypeLabel(RequiredEvents, EventType), ExpirationSpan, CurrencyTimespanType.DisplayString());

            if (ModelsRestriction.Any())
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyModel, m_szModelNames);

            if (!String.IsNullOrEmpty(CategoryRestriction))
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyCategory, CategoryRestriction);

            if (CatClassRestriction > 0)
            {
                if (String.IsNullOrEmpty(m_szCatClass))
                    m_szCatClass = CategoryClass.CategoryClassFromID(CatClassRestriction).CatClass;
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyCatClass, m_szCatClass);
            }
            if (AircraftRestriction.Any())
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAircraft, m_szTailNumbers);

            if (!String.IsNullOrWhiteSpace(AirportRestriction))
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyAirports, AirportRestriction);

            if (!String.IsNullOrWhiteSpace(TextRestriction))
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyText, TextRestriction);

            if (PropertyRestriction != null && PropertyRestriction.Any())
                sb.AppendFormat(CultureInfo.CurrentCulture, Resources.Currency.CustomCurrencyProperties, m_szPropertyNames);

            string szRestrict = sb.ToString();
            if (szRestrict.EndsWith(";", StringComparison.Ordinal))
                szRestrict = szRestrict.Substring(0, szRestrict.Length - 1);
            szRestrict = szRestrict.Replace(";", "\r\n");

            return String.Format(CultureInfo.InvariantCulture, "{0}\r\n{1}", szBase, szRestrict);
        }

        public string DisplayString
        {
            get { return ToString(); }
        }

        #region Query Helpers
        private void UpdateQueryDate(FlightQuery fq)
        {
            switch (CurrencyTimespanType)
            {
                case TimespanType.CalendarMonths:
                    fq.DateMin = DateTime.Now.AddCalendarMonths(-ExpirationSpan);
                    break;
                case TimespanType.SlidingMonths:
                    fq.DateMin = DateTime.Now.AddMonths(-ExpirationSpan);
                    break;
                case TimespanType.Days:
                    fq.DateMin = DateTime.Now.AddDays(-ExpirationSpan);
                    break;
                default:
                    fq.DateMin = AlignedStartDate.Value;
                    break;
            }
        }

        private void UpdateQueryCatClass(FlightQuery fq)
        {

            if (CatClassRestriction > 0)
                fq.AddCatClass(CategoryClass.CategoryClassFromID(CatClassRestriction));
            if (!String.IsNullOrEmpty(CategoryRestriction))
            {
                foreach (CategoryClass cc in CategoryClass.CategoryClasses())
                    if (cc.Category.CompareOrdinalIgnoreCase(CategoryRestriction) == 0)
                        fq.AddCatClass(cc);
            }
        }
        #endregion

        /// <summary>
        /// Generates a new flightquery object representing flights that match this custom currency
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public override FlightQuery Query
        {
            get
            {
                FlightQuery fq = new FlightQuery(this.UserName)
                {
                    DateRange = FlightQuery.DateRanges.Custom,
                    DateMax = DateTime.Now.AddDays(1)
                };
                UpdateQueryDate(fq);

                UpdateQueryCatClass(fq);

                foreach (int idmodel in ModelsRestriction)
                    fq.AddModelById(idmodel);

                IEnumerable<Aircraft> rgac = new UserAircraft(UserName).GetAircraftForUser();
                foreach (Aircraft ac in rgac)
                {
                    if (AircraftRestriction.Contains(ac.AircraftID)) 
                        fq.AircraftList.Add(ac);
                }

                if (!String.IsNullOrEmpty(AirportRestriction))
                    fq.AddAirports(Airports.AirportList.NormalizeAirportList(AirportRestriction));

                if (!String.IsNullOrEmpty(TextRestriction))
                    fq.GeneralText = TextRestriction;

                CustomPropertyType.KnownProperties prop = CustomPropertyType.KnownProperties.IDPropInvalid;
                List<CustomPropertyType> lstprops = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes());
                fq.PropertiesConjunction = GroupConjunction.All;
                switch (EventType)
                {
                    case CustomCurrencyEventType.BackseatHours:
                        prop = CustomPropertyType.KnownProperties.IDPropBackSeatTime;
                        break;
                    case CustomCurrencyEventType.BaseCheck:
                        fq.AddPropertyTypes(lstprops.FindAll(cpt => cpt.IsBaseCheck));
                        break;
                    case CustomCurrencyEventType.Flights:
                        break;
                    case CustomCurrencyEventType.FrontSeatHours:
                        prop = CustomPropertyType.KnownProperties.IDPropFrontSeatTime;
                        break;
                    case CustomCurrencyEventType.GroundSimHours:
                        fq.HasGroundSim = true;
                        break;
                    case CustomCurrencyEventType.HoistOperations:
                        prop = CustomPropertyType.KnownProperties.IDPropHoistOperations;
                        break;
                    case CustomCurrencyEventType.Hours:
                        fq.HasPIC = true;
                        break;
                    case CustomCurrencyEventType.IFRApproaches:
                        fq.HasApproaches = true;
                        break;
                    case CustomCurrencyEventType.IFRHours:
                        fq.HasAnyInstrument = true;
                        break;
                    case CustomCurrencyEventType.Landings:
                    case CustomCurrencyEventType.PICLandings:
                        fq.HasLandings = true;
                        if (EventType == CustomCurrencyEventType.PICLandings)
                            fq.HasPIC = true;
                        break;
                    case CustomCurrencyEventType.TakeoffsAny:
                        prop = CustomPropertyType.KnownProperties.IDPropTakeoffAny;
                        break;
                    case CustomCurrencyEventType.LandingsHighAltitude:
                        prop = CustomPropertyType.KnownProperties.IDPropHighAltitudeLandings;
                        break;
                    case CustomCurrencyEventType.NightFlight:
                        fq.HasNight = true;
                        break;
                    case CustomCurrencyEventType.NightLandings:
                        fq.HasNightLandings = true;
                        break;
                    case CustomCurrencyEventType.NightTouchAndGo:
                        prop = CustomPropertyType.KnownProperties.IDPropNightTouchAndGo;
                        break;
                    case CustomCurrencyEventType.NightTakeoffs:
                        fq.AddPropertyTypes(lstprops.FindAll(cpt => cpt.IsNightTakeOff));
                        break;
                    case CustomCurrencyEventType.NVFLIR:
                        prop = CustomPropertyType.KnownProperties.IDPropNVFLIRTime;
                        break;
                    case CustomCurrencyEventType.NVGoggles:
                        prop = CustomPropertyType.KnownProperties.IDPropNVGoggleTime;
                        break;
                    case CustomCurrencyEventType.NVHours:
                        fq.AddPropertyTypes(lstprops.FindAll(cpt => cpt.IsNightVisionTime));
                        break;
                    case CustomCurrencyEventType.NVUnaidedTime:
                        prop = CustomPropertyType.KnownProperties.IDPropNVUnaided;
                        break;
                    case CustomCurrencyEventType.PICNightLandings:
                        fq.HasNightLandings = true;
                        fq.HasPIC = true;
                        break;
                    case CustomCurrencyEventType.TotalHours:
                        fq.HasTotalTime = true;
                        break;
                    case CustomCurrencyEventType.UASLaunch:
                        fq.AddPropertyTypes(lstprops.FindAll(cpt => cpt.IsUASLaunch));
                        break;
                    case CustomCurrencyEventType.UASRecovery:
                        fq.AddPropertyTypes(lstprops.FindAll(cpt => cpt.IsUASRecovery));
                        break;
                    case CustomCurrencyEventType.CAP5Checkride:
                        prop = CustomPropertyType.KnownProperties.IDPropCAP5Checkride;
                        break;
                    case CustomCurrencyEventType.CAP91Checkride:
                        prop = CustomPropertyType.KnownProperties.IDPropCAP91Checkride;
                        break;
                    case CustomCurrencyEventType.FMSApproaches:
                        prop = CustomPropertyType.KnownProperties.IDPropFMSApproaches;
                        break;
                    case CustomCurrencyEventType.GliderTow:
                        prop = CustomPropertyType.KnownProperties.IDPropGliderTow;
                        break;
                    case CustomCurrencyEventType.HoursDual:
                        fq.HasDual = true;
                        break;
                    case CustomCurrencyEventType.InstructionGiven:
                        fq.HasCFI = true;
                        break;
                    case CustomCurrencyEventType.FlightReview:
                        fq.AddPropertyTypes(lstprops.FindAll(cpt => cpt.IsBFR));
                        break;
                    case CustomCurrencyEventType.IPC:
                        fq.AddPropertyTypes(lstprops.FindAll(cpt => cpt.IsIPC));
                        break;
                    case CustomCurrencyEventType.NightLandingAny:
                        fq.HasNight = true;
                        fq.HasLandings = true;
                        break;
                    case CustomCurrencyEventType.FlightDutyTime:
                    case CustomCurrencyEventType.DutyTime:
                    case CustomCurrencyEventType.RestTime:
                        break;
                    case CustomCurrencyEventType.SpecialAuthorizationApproach:
                        prop = CustomPropertyType.KnownProperties.IDPropSpecialAuthorizationApproach;
                        break;
                    case CustomCurrencyEventType.EnhancedVisionApproach:
                        prop = CustomPropertyType.KnownProperties.IDPropEnhancedVisionApproach;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown event type: " + EventType.ToString() + " in ToQuery()");
                }
                if (prop != CustomPropertyType.KnownProperties.IDPropInvalid)
                    fq.AddPropertyTypes(lstprops.FindAll(cpt => cpt.PropTypeID == (int)prop));

                if (PropertyRestriction != null && PropertyRestriction.Any())
                {
                    // Merge additional properties with any "intrinsic" properties from the type of custom currency above.
                    foreach (int i in PropertyRestriction)
                        if (fq.PropertyTypes.FirstOrDefault(cpt => i == (int) cpt.PropTypeID) == null)
                            fq.PropertyTypes.Add(CustomPropertyType.GetCustomPropertyType(i));
                }

                return fq;
            }
        }

        /// <summary>
        /// Returns the query representing flights that match this custom currency, compressed and URL encoded.
        /// </summary>
        /// <returns>The newly generated flightquery.  Don't forget to refresh it!</returns>
        public string FlightQueryJSON
        {
            get { return System.Web.HttpUtility.UrlEncode(Query.ToBase64CompressedJSONString()); }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private bool CheckMatchesCriteria(ExaminerFlightRow cfr)
        {
            // quickly see if we can ignore this.
            int idmodel = cfr.idModel;
            if (ModelsRestriction != null && ModelsRestriction.Any() && !ModelsRestriction.Any(model => model == idmodel))
                return false;
            if (!String.IsNullOrEmpty(CategoryRestriction) && cfr.szCategory.CompareOrdinalIgnoreCase(CategoryRestriction) != 0)
                return false;
            if (CatClassRestriction > 0 && cfr.idCatClassOverride != CatClassRestriction)
                return false;
            int idAircraft = Convert.ToInt32(cfr.idAircraft);
            if (AircraftRestriction != null && AircraftRestriction.Any() && !AircraftRestriction.Any(idac => idac == idAircraft))
                return false;
            if (AlignedStartDate.HasValue && AlignedStartDate.Value.CompareTo(cfr.dtFlight) > 0)
                return false;
            if (PropertyRestriction != null && PropertyRestriction.Any())
            {
                if (!cfr.FlightProps.Any())  // short circuit, plus empty set can be superset of other sets.
                    return false;

                HashSet<int> hsProps = new HashSet<int>(PropertyRestriction);
                HashSet<int> hsFlight = new HashSet<int>();
                foreach (CustomFlightProperty cfp in cfr.FlightProps)
                    hsFlight.Add(cfp.PropTypeID);

                if (!hsFlight.IsSupersetOf(hsProps))
                    return false;
            }
            if (!String.IsNullOrWhiteSpace(TextRestriction))
            {
                string szUpper = TextRestriction.ToUpper(CultureInfo.CurrentCulture);
                if (cfr.Comments.ToUpper(CultureInfo.CurrentCulture).Contains(szUpper))
                    return true;
                foreach (CustomFlightProperty cfp in cfr.FlightProps)
                    if (cfp.PropertyType.Type == CFPPropertyType.cfpString && cfp.TextValue.ToUpper(CultureInfo.CurrentCulture).Contains(szUpper))
                        return true;
                Aircraft ac = new UserAircraft(UserName).GetUserAircraftByID(cfr.idAircraft);
                if (ac != null && ac.PrivateNotes.ToUpper(CultureInfo.CurrentCulture).Contains(szUpper))
                    return true;
                return false;
            }
            if (!String.IsNullOrWhiteSpace(AirportRestriction))
            {
                string[] szAirports = Airports.AirportList.NormalizeAirportList(AirportRestriction.ToUpper(CultureInfo.CurrentCulture));
                string szRouteUpper = cfr.Route.ToUpper(CultureInfo.CurrentCulture);
                foreach (string szAirport in szAirports)
                    if (!szRouteUpper.Contains(szAirport))
                        return false;
            }
            return true;
        }

        /// <summary>
        /// Looks at the events in the flight for custom currency computations.
        /// </summary>
        /// <param name="cfr">The flight row to examine</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (!CheckMatchesCriteria(cfr))
                return;
            // don't look at anything older than necessary if we're a *maximum* currency
            // since we start out current and lose it over time, so "has been current" is always true.
            if (CurrencyLimitType == LimitType.Maximum && cfr.dtFlight.CompareTo(EarliestDate) < 0)
                return;

            CustomFlightProperty cfp = null;    //useful for checking properties

            // OK if we're here then the currency applies.
            switch (EventType)
            {
                case CustomCurrencyEventType.Flights:
                    AddRecentFlightEvents(cfr.dtFlight, 1);
                    break;
                case CustomCurrencyEventType.Hours:
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToDecimal(cfr.PIC));
                    break;
                case CustomCurrencyEventType.TotalHours:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.Total);
                    break;
                case CustomCurrencyEventType.GroundSimHours:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.GroundSim);
                    break;
                case CustomCurrencyEventType.FrontSeatHours:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropFrontSeatTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.BackseatHours:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropBackSeatTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.HoistOperations:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropHoistOperations)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.Landings:
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToInt32(cfr.cLandingsThisFlight));
                    break;
                case CustomCurrencyEventType.TakeoffsAny:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropTakeoffAny)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.PICLandings:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.PIC > 0 ? cfr.cLandingsThisFlight : 0);
                    break;
                case CustomCurrencyEventType.PICNightLandings:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.PIC > 0 ? cfr.cFullStopNightLandings : 0);
                    break;
                case CustomCurrencyEventType.IFRHours:
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToDecimal(cfr.IMCSim));
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToDecimal(cfr.IMC));
                    break;
                case CustomCurrencyEventType.IFRApproaches:
                    AddRecentFlightEvents(cfr.dtFlight, Convert.ToInt32(cfr.cApproaches));
                    break;
                case CustomCurrencyEventType.HoursDual:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.Dual);
                    break;
                case CustomCurrencyEventType.InstructionGiven:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.CFI);
                    break;
                case CustomCurrencyEventType.BaseCheck:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsBaseCheck)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.BoolValue ? 1 : 0);
                    });
                    break;
                case CustomCurrencyEventType.UASLaunch:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsUASLaunch)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.IntValue);
                    });
                    break;
                case CustomCurrencyEventType.UASRecovery:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsUASRecovery)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.IntValue);
                    });
                    break;
                case CustomCurrencyEventType.NightLandings:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.cFullStopNightLandings);
                    break;
                case CustomCurrencyEventType.NightLandingAny:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.cFullStopNightLandings + cfr.FlightProps.TotalCountForPredicate(fp => fp.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropNightTouchAndGo));
                    break;
                case CustomCurrencyEventType.NightTakeoffs:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsNightTakeOff)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.IntValue);
                    });
                    break;
                case CustomCurrencyEventType.NightTouchAndGo:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.NightFlight:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.Night);
                    break;
                case CustomCurrencyEventType.NVHours:
                    cfr.FlightProps.ForEachEvent((pfe) =>
                    {
                        if (pfe.PropertyType.IsNightVisionTime)
                            AddRecentFlightEvents(cfr.dtFlight, pfe.DecValue);
                    });
                    break;
                case CustomCurrencyEventType.NVGoggles:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropNVGoggleTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.NVFLIR:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropNVFLIRTime)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.NVUnaidedTime:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropNVUnaided)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.DecValue);
                    break;
                case CustomCurrencyEventType.LandingsHighAltitude:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropHighAltitudeLandings)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.CAP5Checkride:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropCAP5Checkride)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.BoolValue ? 1 : 0);
                    break;
                case CustomCurrencyEventType.CAP91Checkride:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropCAP91Checkride)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.BoolValue ? 1 : 0);
                    break;
                case CustomCurrencyEventType.FMSApproaches:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropFMSApproaches)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.GliderTow:
                    if ((cfp = cfr.FlightProps.GetEventWithTypeID(CustomPropertyType.KnownProperties.IDPropGliderTow)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, cfp.IntValue);
                    break;
                case CustomCurrencyEventType.FlightReview:
                    if ((cfp = cfr.FlightProps.FindEvent(fp => fp.PropertyType.IsBFR)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, 1);
                    break;
                case CustomCurrencyEventType.IPC:
                    if ((cfp = cfr.FlightProps.FindEvent(fp => fp.PropertyType.IsIPC)) != null)
                        AddRecentFlightEvents(cfr.dtFlight, 1);
                    break;
                case CustomCurrencyEventType.DutyTime:
                case CustomCurrencyEventType.FlightDutyTime:
                case CustomCurrencyEventType.RestTime:
                    _DutyPeriodExaminer.ExamineFlight(cfr);
                    break;
                case CustomCurrencyEventType.EnhancedVisionApproach:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropEnhancedVisionApproach));
                    break;
                case CustomCurrencyEventType.SpecialAuthorizationApproach:
                    AddRecentFlightEvents(cfr.dtFlight, cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropSpecialAuthorizationApproach));
                    break;
            }
        }

        public override void Finalize(decimal totalTime, decimal picTime)
        {
            base.Finalize(totalTime, picTime);

            if (EventType.IsDutyTimeEvent())
            {
                TimeSpan ts = DateTime.UtcNow.Subtract(EarliestDate);
                if (!CurrencyTimespanType.IsAligned())
                    ts = new TimeSpan(ts.Days, 0, 0, 0);

                switch (EventType)
                {
                    case CustomCurrencyEventType.DutyTime:
                        _DutyPeriodExaminer.Finalize(totalTime, picTime);
                        AddRecentFlightEvents(DateTime.Now, EffectiveDutyPeriod.DutyTimeSince(ts, _DutyPeriodExaminer.EffectiveDutyPeriods));
                        break;
                    case CustomCurrencyEventType.FlightDutyTime:
                        _DutyPeriodExaminer.Finalize(totalTime, picTime);
                        AddRecentFlightEvents(DateTime.Now, EffectiveDutyPeriod.FlightDutyTimeSince(ts, _DutyPeriodExaminer.EffectiveDutyPeriods));
                        break;
                    case CustomCurrencyEventType.RestTime:
                        AddRecentFlightEvents(DateTime.Now, ((decimal)ts.TotalHours) - EffectiveDutyPeriod.DutyTimeSince(ts, _DutyPeriodExaminer.EffectiveDutyPeriods));
                        _DutyPeriodExaminer.Finalize(totalTime, picTime);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public class CustomCurrencyEventArgs : EventArgs
    {
        public CustomCurrency Currency { get; set; }

        public CustomCurrencyEventArgs() : base()
        {
            Currency = null;
        }

        public CustomCurrencyEventArgs(CustomCurrency cc) : base()
        {
            Currency = cc;
        }
    }
}