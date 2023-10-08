﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2019-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights.CloudAhoy
{
    #region components of a cloudahoy flight
    [Serializable]
    public enum CloudAhoyManeuvers
    {
        unknown,
        chandelle,
        land,
        lineupAndWait,
        missedApproach,
        oscarPattern,
        slowFlight,
        spiral,
        stall,
        stopAndGo,
        sTurns,
        takeoff,
        touchAndGo,
        trafficPattern,
        turbulence,
        turn360,
        glide,
        tow,
        thermalling,
        autoRotate,
        hover
    }

    public enum CloudAhoyRoles
    {
        None, Pilot, Copilot, Student, Instructor, Passenger, SafetyPilot, Examiner
    }

    [Serializable]
    public class CloudAhoyAircraftDescriptor
    {
        public string make { get; set; }
        public string model { get; set; }
        public string registration { get; set; }

        public CloudAhoyAircraftDescriptor()
        {
            make = model = registration = string.Empty;
        }
    }

    [Serializable]
    public class CloudAhoyAirportDescriptor
    {
        public string icao { get; set; }
        public string name { get; set; }

        public CloudAhoyAirportDescriptor()
        {
            icao = name = string.Empty;
        }
    }

    [Serializable]
    public class CloudAhoyCrewDescriptor
    {
        public int PIC { get; set; }
        public int checkride { get; set; }
        public int solo { get; set; }
        public int currentUser { get; set; }
        public string role { get; set; }
        public string name { get; set; }

        public CloudAhoyCrewDescriptor()
        {
            role = name = string.Empty;
        }
    }

    [Serializable]
    public class CloudAhoyManeuverDescriptor
    {
        public string maneuverId { get; set; }
        
        [JsonProperty("url")]
        public string link { get; set; }
        public string code { get; set; }
        public string label { get; set; }

        public CloudAhoyManeuverDescriptor()
        {
            maneuverId = link = code = label = string.Empty;
        }
    }
    #endregion

    /// <summary>
    /// A flight as represented in CloudAhoy
    /// </summary>
    public class CloudAhoyFlight : ExternalFormat
    {
        #region Properties
        public CloudAhoyAircraftDescriptor aircraft { get; set; }
        public Collection<CloudAhoyAirportDescriptor> airports { get; private set; }
        public string cfiaScore { get; set; }
        public Collection<CloudAhoyCrewDescriptor> crew { get; private set; }
        public long duration { get; set; }
        public string flightId { get; set; }
        public Collection<CloudAhoyManeuverDescriptor> maneuvers { get; private set; }
        public string remarks { get; set; }
        public long time { get; set; }
        
        [JsonProperty("url")]
        public string link { get; set; }
        public string userRole { get; set; }

        [JsonProperty("thumbnailUrl")]
        public string thumbnailLink { get; set; }

        private Dictionary<CustomPropertyType.KnownProperties, CustomFlightProperty> DictProps { get; set; }

        public string UserName { get; set; }
        #endregion

        #region Constructors
        public CloudAhoyFlight()
        {
            aircraft = new CloudAhoyAircraftDescriptor();
            airports = new Collection<CloudAhoyAirportDescriptor>();
            crew = new Collection<CloudAhoyCrewDescriptor>();
            maneuvers = new Collection<CloudAhoyManeuverDescriptor>();
            cfiaScore = flightId = remarks = link = userRole = string.Empty;

            DictProps = new Dictionary<CustomPropertyType.KnownProperties, CustomFlightProperty>();

            UserName = null;
        }

        public CloudAhoyFlight(string szUser) : this()
        {
            UserName = szUser;
        }
        #endregion

        #region Conversion to pending flight
        private void PopulateCrewInfo(LogbookEntry le)
        {
            if (crew == null)
                return;
            foreach (CloudAhoyCrewDescriptor cd in crew)
            {
                if (cd.currentUser != 0)
                {
                    if (cd.PIC != 0)
                        le.PIC = le.TotalFlightTime;

                    if (Enum.TryParse<CloudAhoyRoles>(cd.role.Replace(" ", string.Empty), out CloudAhoyRoles role))
                    {
                        switch (role)
                        {
                            case CloudAhoyRoles.Instructor:
                                le.CFI = le.TotalFlightTime;
                                break;
                            case CloudAhoyRoles.Student:
                                le.Dual = le.TotalFlightTime;
                                break;
                            case CloudAhoyRoles.SafetyPilot:
                                DictProps[CustomPropertyType.KnownProperties.IDPropSafetyPilotTime] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSafetyPilotTime, le.TotalFlightTime);
                                break;
                            case CloudAhoyRoles.Copilot:
                                le.SIC = le.TotalFlightTime;
                                break;
                            default:
                                break;
                        }
                    }

                    if (cd.solo != 0)
                        DictProps[CustomPropertyType.KnownProperties.IDPropSolo] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropSolo, le.TotalFlightTime);
                }
            }
        }

        protected static string MarkdownLink(string szLabel, string szurl)
        {
            bool fHasLabel = !String.IsNullOrEmpty(szLabel);
            bool fHasLink = !String.IsNullOrEmpty(szurl);

            if (fHasLabel && fHasLink)
                return String.Format(CultureInfo.CurrentCulture, "[{0}]({1})", szLabel, szurl);
            if (fHasLabel)
                return szLabel;
            if (fHasLink)
                return szurl;
            return string.Empty;
        }

        private void PopulateManeuvers(LogbookEntry le, List<string> lst)
        {
            if (maneuvers == null)
                return;

            foreach (CloudAhoyManeuverDescriptor md in maneuvers)
            {
                if (Enum.TryParse(md.code, out CloudAhoyManeuvers maneuver))
                {
                    switch (maneuver)
                    {
                        case CloudAhoyManeuvers.land:
                        case CloudAhoyManeuvers.stopAndGo:
                            le.Landings++;
                            le.FullStopLandings++;
                            break;
                        case CloudAhoyManeuvers.touchAndGo:
                            le.Landings++;
                            break;
                        case CloudAhoyManeuvers.missedApproach:
                            le.Approaches++;
                            break;
                        case CloudAhoyManeuvers.slowFlight:
                            DictProps[CustomPropertyType.KnownProperties.IDPropManeuverSlowFlight] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropManeuverSlowFlight, true);
                            break;
                        case CloudAhoyManeuvers.chandelle:
                            DictProps[CustomPropertyType.KnownProperties.IDPropManeuverChandelle] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropManeuverChandelle, true);
                            break;
                        case CloudAhoyManeuvers.sTurns:
                            DictProps[CustomPropertyType.KnownProperties.IDPropManeuverSTurns] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropManeuverSTurns, true);
                            break;
                        case CloudAhoyManeuvers.stall:
                            DictProps[CustomPropertyType.KnownProperties.IDPropPowerOffStall] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropPowerOffStall, true);
                            break;
                        case CloudAhoyManeuvers.autoRotate:
                            DictProps[CustomPropertyType.KnownProperties.IDPropAutoRotate] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropAutoRotate, true);
                            break;
                        case CloudAhoyManeuvers.hover:
                            DictProps[CustomPropertyType.KnownProperties.IDPropHover] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropHover, true);
                            break;
                        case CloudAhoyManeuvers.tow:
                            if (!DictProps.ContainsKey(CustomPropertyType.KnownProperties.IDPropGliderTow))
                                DictProps[CustomPropertyType.KnownProperties.IDPropGliderTow] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropGliderTow, 1);
                            else
                                DictProps[CustomPropertyType.KnownProperties.IDPropGliderTow].IntValue++;
                            break;
                        default:
                            break;
                    }

                    lst.Add(MarkdownLink(md.label, md.link));
                }
            }
        }

        public override LogbookEntry ToLogbookEntry()
        {
            StringBuilder sb = new StringBuilder();
            if (airports != null)
                foreach (CloudAhoyAirportDescriptor ap in airports)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0} ", ap.icao);
            if (aircraft == null)
                aircraft = new CloudAhoyAircraftDescriptor();

            DateTime dtStart = DateTimeOffset.FromUnixTimeSeconds(time).DateTime;

            DictProps.Clear();

            List<string> lstText = new List<string>() { MarkdownLink(remarks, link) };

            PendingFlight le = new PendingFlight()
            {
                FlightID = LogbookEntry.idFlightNew,
                TailNumDisplay = aircraft.registration,
                ModelDisplay = aircraft.model,
                Route = sb.ToString().Trim(),
                TotalFlightTime = duration / 3600.0M,
                EngineStart = dtStart,
                EngineEnd = dtStart.AddSeconds(duration),
                Date = dtStart.Date
            };

            PopulateCrewInfo(le);
            PopulateManeuvers(le, lstText);

            le.Comment = String.Join(" ", lstText);

            if (!string.IsNullOrEmpty(flightId))
                le.PendingID = flightId;

            if (!String.IsNullOrEmpty(UserName))
            {
                le.User = UserName;
                Aircraft ac = BestGuessAircraftID(UserName, le.TailNumDisplay);
                if (ac != null)
                {
                    le.AircraftID = ac.AircraftID;
                    if (ac.IsAnonymous)
                        DictProps[CustomPropertyType.KnownProperties.IDPropAircraftRegistration] = CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropAircraftRegistration, le.TailNumDisplay);
                }
            }

            le.CustomProperties.SetItems(DictProps.Values);

            return le;
        }
        #endregion
    }
}