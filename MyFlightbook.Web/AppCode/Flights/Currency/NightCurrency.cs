﻿using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// NightCurrency class per 61.57
    /// </summary>
    public class NightCurrency : CompositeFlightCurrency
    {
        // 61.57(b) parameters and sub-currencies
        const int RequiredLandings = 3;
        const int RequiredTakeoffs = 3;
        const int TimeSpan = 90;

        public FlightCurrency NightTakeoffCurrency { get; set; }

        // 61.57(e) options
        const int MinTime6157e = 1500;                     // 61.57(e)(4)(i)(A) and (ii)(A)
        const int MinRecentTimeInType = 15;                // 61.57(e)(4)(i)(C) and (ii)(C) - 15 hours in type in the preceding 90 days
        const int MinRecencyInType = 90;
        const int MinLandings6157eAirplane = 3;
        const int Duration6157eAirplane = 6;
        const int MinLandings6157eSim = 6;
        const int MinTakeoffs6157eSim = 6;
        const int Duration6157eSim = 12;

        // 61.57(e) sub-currencies
        private readonly FlightCurrency m_fc6157ei = new FlightCurrency(MinLandings6157eAirplane, Duration6157eAirplane, true, "Landings per 61.57(e)(4)(i)");
        private readonly FlightCurrency m_fc6157eiTakeoffs = new FlightCurrency(MinLandings6157eAirplane, Duration6157eAirplane, true, "Takeoffs per 61.57(e)(4)(i)");
        private readonly FlightCurrency m_fc6157eii = new FlightCurrency(MinLandings6157eSim, Duration6157eSim, true, "Landings per 61.57(e)(4)(ii)");
        private readonly FlightCurrency m_fc6157eiiTakeoffs = new FlightCurrency(MinTakeoffs6157eSim, 12, true, "Takeoffs per 61.57(e)(4)(ii)");
        private readonly FlightCurrency m_fc6157eTotalTime = new FlightCurrency(MinTime6157e, 12, true, "Total time");
        private readonly FlightCurrency m_fc6157TimeInType = new FlightCurrency(MinRecentTimeInType, MinRecencyInType, false, "Recent time in type");
        private readonly PassengerCurrency m_fc6157Passenger = new PassengerCurrency("61.57(e)(4)(i)(B)", false); // 61.57(e)(4)(i)(B) and (ii)(B) - regular passenger currency must have been met too.  This is a US regulation, so day or night counts.

        public string TypeDesignator { get; set; }

        protected bool AllowTouchAndGo { get; set; }

        public NightCurrency(string szName, bool fAllowTouchAndGo) : base(RequiredLandings, TimeSpan, false, szName)
        {
            NightTakeoffCurrency = new FlightCurrency(RequiredTakeoffs, TimeSpan, false, szName);
            AllowTouchAndGo = fAllowTouchAndGo;

            Query = new FlightQuery()
            {
                DateRange = FlightQuery.DateRanges.Trailing90,
                HasNightLandings = true,
                PropertiesConjunction = GroupConjunction.None
            };
            Query.PropertyTypes.Add(CustomPropertyType.GetCustomPropertyType((int)CustomPropertyType.KnownProperties.IDPropPilotMonitoring));
        }

        public NightCurrency(string szName, string szType, bool fAllowTouchAndGo) : this(szName, fAllowTouchAndGo)
        {
            TypeDesignator = szType;
        }

        protected override void ComputeComposite()
        {
            // Compute both loose (ignores takeoffs) and strict (requires takeoffs) night currencies.
            // Discrepancy can't be counted on after AND/OR so we set that to the one we wish to expose
            FlightCurrency fc6157b = this;  // just for clarity that the "this" object does the basic 61.57(b) implementation.
            FlightCurrency fc6157e4i = m_fc6157eTotalTime.AND(m_fc6157Passenger).AND(m_fc6157TimeInType).AND(m_fc6157ei);
            FlightCurrency fc6157e4ii = m_fc6157eTotalTime.AND(m_fc6157Passenger).AND(m_fc6157TimeInType).AND(m_fc6157eii);
            FlightCurrency fcLoose = fc6157b.OR(fc6157e4i.OR(fc6157e4ii));
            fcLoose.Discrepancy = fc6157b.Discrepancy;

            FlightCurrency fc6157bStrict = fc6157b.AND(NightTakeoffCurrency);
            FlightCurrency fc6157e4iStrict = fc6157e4i.AND(m_fc6157eiTakeoffs);
            FlightCurrency fc6157e4iiStrict = fc6157e4ii.AND(m_fc6157eiiTakeoffs);
            FlightCurrency fcStrict = fc6157bStrict.OR(fc6157e4iStrict).OR(fc6157e4iiStrict);

            // Loose rules for purposes of determining state
            CompositeCurrencyState = fcLoose.CurrentState;
            CompositeExpiration = fcLoose.ExpirationDate;
            CompositeDiscrepancy = fcLoose.DiscrepancyString;

            // determine the correct discrepancy string to show
            // if we've EVER met the strict definition, then use that - indicates we've logged at least some takeoffs.
            if (fcStrict.HasBeenCurrent)
            {
                CompositeExpiration = fcStrict.ExpirationDate;
                CompositeCurrencyState = fcStrict.CurrentState;
                CompositeDiscrepancy = fcStrict.CurrentState == CurrencyState.NotCurrent ? 
                    String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, Math.Max(NightTakeoffCurrency.Discrepancy, fc6157b.Discrepancy), NightTakeoffCurrency.Discrepancy > fc6157b.Discrepancy ? Resources.Currency.NightTakeoffs : (fc6157b.Discrepancy > 1 ? Resources.Totals.Landings : Resources.Totals.Landing)) : 
                    string.Empty;
            }
            // else if we met the loose definition but not strict - meaning takeoffs were not found; Give a reminder about required takeoffs
            else if (CompositeCurrencyState.IsCurrent())
            {
                CompositeDiscrepancy = NightTakeoffCurrency.Discrepancy >= NightTakeoffCurrency.RequiredEvents
                    ? Resources.Currency.NightTakeoffReminder
                    : String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplateNight, NightTakeoffCurrency.Discrepancy, (NightTakeoffCurrency.Discrepancy > 1) ? Resources.Currency.Takeoffs : Resources.Currency.Takeoff);
            }
            // else we aren't current at all - use the full discrepancy template using 61.57(b).  DON'T CALL DISCREPANCY STRING because that causes an infinite recursion.
            else
                CompositeDiscrepancy = String.Format(CultureInfo.CurrentCulture, Resources.Currency.DiscrepancyTemplate, this.Discrepancy, (this.Discrepancy > 1) ? Resources.Totals.Landings : Resources.Totals.Landing);
        }

        public override void Finalize(decimal totalTime, decimal picTime)
        {
            base.Finalize(totalTime, picTime);
            m_fc6157eTotalTime.AddRecentFlightEvents(DateTime.Now, totalTime);
        }

        private enum NightCurrencyOptions { FAR6157bOnly, FAR6157eAirplane, FAR6157eSim }

        /// <summary>
        /// Adds night-time takeoff(s) to the currency.  This is mostly informative - we don't currently require these to be logged
        /// </summary>
        /// <param name="dt">The date of the takeoff(s)</param>
        /// <param name="cEvents">The number of takeoffs</param>
        /// <param name="nco">Indicates whether 61.57(e) applies, and if so, whether it is sim or real aircraft</param>
        private void AddNighttimeTakeOffEvent(DateTime dt, decimal cEvents, NightCurrencyOptions nco)
        {
            NightTakeoffCurrency.AddRecentFlightEvents(dt, cEvents);
            if (nco == NightCurrencyOptions.FAR6157eAirplane)
                m_fc6157eiTakeoffs.AddRecentFlightEvents(dt, cEvents);
            else if (nco == NightCurrencyOptions.FAR6157eSim)
                m_fc6157eiiTakeoffs.AddRecentFlightEvents(dt, cEvents);
        }

        /// <summary>
        /// Adds night-time landing(s) to the currency.
        /// </summary>
        /// <param name="dt">The date of the landing(s)</param>
        /// <param name="cEvents">The number of landings</param>
        /// <param name="nco">Indicates whether 61.57(e) applies, and if so, whether it is sim or real aircraft</param>
        private void AddNighttimeLandingEvent(DateTime dt, decimal cEvents, NightCurrencyOptions nco)
        {
            AddRecentFlightEvents(dt, cEvents);
            if (nco == NightCurrencyOptions.FAR6157eAirplane)
                m_fc6157ei.AddRecentFlightEvents(dt, cEvents);
            else if (nco == NightCurrencyOptions.FAR6157eSim)
                m_fc6157eii.AddRecentFlightEvents(dt, cEvents);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            base.ExamineFlight(cfr);

            if (!cfr.fIsCertifiedLanding)
                return;

            bool fIsAppropriateTurbine = CategoryClass.IsAirplane(cfr.idCatClassOverride) && cfr.turbineLevel.IsTurbine() && !cfr.fIsCertifiedSinglePilot && !String.IsNullOrEmpty(TypeDesignator);

            // 61.57(e)(4)(i/ii)(A) - 1500 hrs - comes into play after finalize

            // 61.57(e)(4)(i/ii)(B) - Meets 61.57(a), computed below

            // 61.57(e)(4)(i/ii)(C) - 15 hours in *this* type in the last 90 days.  Only if in an actual aircraft, since it doesn't seem to allow sim time.
            // Do this first because we'll exclude others if you were pilot monitoring
            if (cfr.fIsRealAircraft && fIsAppropriateTurbine && cfr.szType.CompareCurrentCultureIgnoreCase(TypeDesignator ?? string.Empty) == 0)
                m_fc6157TimeInType.AddRecentFlightEvents(cfr.dtFlight, cfr.Total);

            if (!cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDPropPilotMonitoring))
            {
                // we need to subtract out monitored landings
                int cMonitoredLandings = cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropMonitoredNightLandings);
                int cMonitoredTakeoffs = cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropMonitoredNightTakeoffs);
                int cNightLandings = cfr.cFullStopNightLandings + (AllowTouchAndGo ? cfr.FlightProps.IntValueForProperty(CustomPropertyType.KnownProperties.IDPropNightTouchAndGo) : 0);

                // 61.57(e)(4)(i/ii)(B) - passenger currency in this type
                m_fc6157Passenger.ExamineFlight(cfr);

                // 61.57(e) only applies if turbine and type rated.  Everything else must be in certified landing, or turbine airplane, or not type rated, or not in the type for this aircraft
                NightCurrencyOptions nco = fIsAppropriateTurbine ? (cfr.fIsRealAircraft ? NightCurrencyOptions.FAR6157eAirplane : NightCurrencyOptions.FAR6157eSim) : NightCurrencyOptions.FAR6157bOnly;

                // 61.57(b), 61.57(e)(4)(i/ii)(D) - Night takeoffs/landings
                if (cNightLandings > 0)
                    AddNighttimeLandingEvent(cfr.dtFlight, Math.Max(cNightLandings - cMonitoredLandings, 0), nco);

                // Night-time take-offs are also technically required for night currency
                int cNightTakeoffs = cfr.FlightProps.TotalCountForPredicate(cfp => cfp.PropertyType.IsNightTakeOff);
                if (cNightTakeoffs > 0)
                    AddNighttimeTakeOffEvent(cfr.dtFlight, Math.Max(cNightTakeoffs - cMonitoredTakeoffs, 0), nco);
            }
        }
    }
}
