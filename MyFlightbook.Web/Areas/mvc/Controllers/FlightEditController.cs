﻿using MyFlightbook.CloudStorage;
using MyFlightbook.Image;
using MyFlightbook.Lint;
using MyFlightbook.Telemetry;
using MyFlightbook.Templates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class FlightEditController : FlightControllerBase
    {
        #region Web Services
        #region Commit
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CommitFlight()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                // Could already have an error just from data entry - don't bother trying to commit just yet.
                if (!String.IsNullOrEmpty(le.ErrorString))
                    throw new InvalidOperationException(le.ErrorString);

                if (!CommitFlight(le) || !String.IsNullOrEmpty(le.ErrorString))
                    throw new InvalidOperationException(le.ErrorString);

                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CommitFlightAsPending()
        {
            return SafeOp(() =>
            {
                PendingFlight pf = new PendingFlight(LogbookEntryFromForm());

                Aircraft ac = new Aircraft(pf.AircraftID);
                pf.TailNumDisplay = ac.DisplayTailnumber;
                pf.ModelDisplay = ac.ModelDescription;
                pf.Commit();
                return new EmptyResult();
            });
        }
        #endregion

        #region Check flights
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckFlight()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();
                // See if there are any actual errors, stick those at the top of the list.
                le.IsValid(); // will populate ErrorString.
                if (!le.IsNewFlight)
                    le.CFISignatureState = new LogbookEntry(le.FlightID, le.User).CFISignatureState;
                IEnumerable<FlightWithIssues> rgf = new FlightLint().CheckFlights(new LogbookEntryBase[] { le }, le.User, FlightLint.DefaultOptionsForLocale);
                return FlightIssues(rgf.FirstOrDefault()?.Issues);
            });
        }
        #endregion

        #region Telemetry
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AutoFillFlight()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                // Load from the DB if needed
                if (String.IsNullOrEmpty(le.FlightData) && !LogbookEntryCore.IsNewFlightID(le.FlightID))
                    le.FlightData = new LogbookEntry(le.FlightID, le.User, LogbookEntryCore.LoadTelemetryOption.LoadAll).FlightData;

                AutoFillOptions afOptions = AutoFillOptions.DefaultOptionsForUser(le.User);
                afOptions.TimeZoneOffset = Convert.ToInt32(Request["tzOffset"], CultureInfo.InvariantCulture);

                Profile pfTarget = MyFlightbook.Profile.GetUser(Request["szTargetUser"]);
                Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);

                using (FlightData fd = new FlightData())
                {
                    fd.AutoFill(le, afOptions);
                }

                return FlightEditorBody(pfTarget, pfViewer, le, le as PendingFlight);
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTelemetry()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                le.FlightData = null;
                // Load from the DB if needed
                if (!LogbookEntryCore.IsNewFlightID(le.FlightID) && le.FLoadFromDB(le.FlightID, User.Identity.Name))
                    le.FCommit(true);

                Profile pfTarget = MyFlightbook.Profile.GetUser(Request["szTargetUser"]);
                Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);

                return FlightEditorBody(pfTarget, pfViewer, le, le as PendingFlight);
            });
        }
        #endregion

        #region Images and videos
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteVideoRef()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();
                int idVideoToDelete = Convert.ToInt32(Request["flightVideoToDelete"], CultureInfo.InvariantCulture);
                VideoRef v = le.Videos.FirstOrDefault(vr => vr.ID == idVideoToDelete) ?? throw new InvalidOperationException("Video not found!");
                le.Videos.Remove(v);
                v.Delete();
                return EmbeddedVideos(le, true);
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddVideoRef()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                VideoRef vr = new VideoRef(le.FlightID, Request["flightNewVideoRef"], Request["flightNewVideoComment"]);
                if (!vr.IsValid)
                    throw new InvalidOperationException(vr.ErrorString);
                le.Videos.Add(vr);
                return EmbeddedVideos(le, Payments.EarnedGratuity.UserQualifies(User.Identity.Name, Payments.Gratuity.GratuityTypes.Videos));
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult UploadFlightImages(int szKey, bool fCanDoVideo)
        {
            return SafeOp(() =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");

                MFBPostedFile pf = new MFBPostedFile(Request.Files[0]);
                string szID = String.Format(CultureInfo.InvariantCulture, "{0}-pendingImage-{1}-{2}", MFBImageInfoBase.ImageClass.Flight.ToString(), (pf.FileName ?? string.Empty).Replace(".", "_"), pf.GetHashCode());
                MFBPendingImage pi = new MFBPendingImage(pf, szID);

                if (!LogbookEntryCore.ValidateFileType(MFBImageInfo.ImageTypeFromFile(pf), fCanDoVideo))
                    return Content(string.Empty);

                if (szKey > 0)
                    pi?.Commit(MFBImageInfoBase.ImageClass.Flight, szKey.ToString(CultureInfo.InvariantCulture));
                else if (pi?.IsValid ?? false)
                    Session[szID] = pi;

                return Content(pi.URLThumbnail.ToAbsolute());
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddGooglePhoto()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                string szKey = "googlePhoto_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);

                MFBPendingImage pi = GooglePhoto.AddToFlight(le, Convert.ToInt32(Request["gPhotoClickIndex"], CultureInfo.InvariantCulture), Request["gmrJSON"], szKey);

                if (le.IsNewFlight)
                    Session[szKey] = pi;
                else
                    pi?.Commit(MFBImageInfoBase.ImageClass.Flight, le.FlightID.ToString(CultureInfo.InvariantCulture));

                le.PopulateImages();

                Profile pfTarget = MyFlightbook.Profile.GetUser(Request["szTargetUser"]);
                Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);

                return FlightEditorBody(pfTarget, pfViewer, le, le as PendingFlight);
            });
        }
        #endregion

        #region Pending Flights
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult DeletePendingFlight(string pfID)
        {
            return SafeOp(() =>
            {
                PendingFlight pf = (PendingFlight.PendingFlightsForUser(User.Identity.Name)).FirstOrDefault(pf2 => pf2.PendingID.CompareOrdinal(pfID) == 0);
                if (pf == null || pf.User.CompareOrdinal(User.Identity.Name) != 0)
                    throw new MyFlightbookException(Resources.WebService.errFlightNotYours);
                pf.Delete();
                return new EmptyResult();
            });
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult DeleteAllPendingFlights()
        {
            return SafeOp(() =>
            {
                PendingFlight.DeletePendingFlightsForUser(User.Identity.Name);
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult PendingFlightsInRange(int offset, int pageSize, string sortField, SortDirection sortDirection)
        {
            return SafeOp(() =>
            { 
                return PendingFlightsTable(offset, pageSize, sortField, sortDirection);
            });
        }
        #endregion

        [Authorize]
        [HttpPost]
        public ActionResult FlightEditorForFlight(int idFlight, string targetUser = null, string nextFlightHref = null, string prevFlightHref = null, string onCancel = null, string onSave = null, int chk = 0)
        {
            return SafeOp(() =>
            {
                targetUser = targetUser ?? User.Identity.Name;
                LogbookEntry le = new LogbookEntry();
                if (!le.FLoadFromDB(idFlight, targetUser))
                    throw new UnauthorizedAccessException(le.ErrorString);
                return FlightEditor(targetUser, le, false, nextFlightHref, prevFlightHref, onCancel, onSave, chk);
            });
        }

        #region Misc
        /// <summary>
        /// Saves the current flight into the session state so that when we come back we can re-constitute it.  Does NOT work for a pending flight - only actual flights that aren't yet saved.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFlightToSession()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();
                if (le.IsNewFlight && !(le is PendingFlight))
                    Session[keySessionInProgress] = le;
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddApproachDesc()
        {
            return SafeOp(() =>
            {
                LogbookEntry le = LogbookEntryFromForm();

                int appchCount = util.GetIntParam(Request, "appchHelpCount", 0);
                // Form the string to append
                string szAppchDesc = new ApproachDescription(appchCount, Request["appchHelpType"], Request["appchHelpRwy"], Request["appchHelpApt"]).ToCanonicalString();

                if (Request["appchHelpAdd"] != null)
                    le.Approaches += appchCount;

                CustomFlightProperty cfp = le.CustomProperties.GetEventWithTypeIDOrNew(CustomPropertyType.KnownProperties.IDPropApproachName);
                cfp.TextValue = (cfp.TextValue + Resources.LocalizedText.LocalizedSpace + new ApproachDescription(appchCount, Request["appchHelpType"] + Request["appchHelpTypeSfx"], Request["appchHelpRwy"] + Request["appchHelpRwySfx"], Request["appchHelpApt"]).ToCanonicalString()).Trim();
                le.CustomProperties.Add(cfp);

                Profile pfTarget = MyFlightbook.Profile.GetUser(Request["szTargetUser"]);
                Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);

                return FlightEditorBody(pfTarget, pfViewer, le, le as PendingFlight);
            });
        }


        [Authorize]
        [HttpPost]
        public ActionResult UpdatePropset(string szTargetUser, string propTuples, bool fHHMM, int[] activeTemplateIDs, bool fStripDefault, int idFlight, string dtDefault, int idAircraft = Aircraft.idAircraftUnknown)
        {
            return SafeOp(() =>
            {
                // check that the current user can edit the target user's flights.
                if (!(CheckCanViewFlights(szTargetUser, User.Identity.Name)?.CanAddLogbook ?? true))
                    throw new UnauthorizedAccessException();

                TimeZoneInfo tz = MyFlightbook.Profile.GetUser(User.Identity.Name).PreferredTimeZone;

                IEnumerable<PropertyTemplate> userTemplates = UserPropertyTemplate.TemplatesForUser(szTargetUser, true);
                return PropSetEditor(szTargetUser, CustomFlightProperty.PropertiesFromJSONTuples(propTuples, idFlight, DateTime.Parse(dtDefault, CultureInfo.CurrentCulture, DateTimeStyles.None), CultureInfo.CurrentCulture), fHHMM, PropertyTemplate.TemplatesFromIDs(userTemplates, PropertyTemplate.RefreshForAicraft(szTargetUser, idAircraft, activeTemplateIDs ?? Array.Empty<int>())), tz, fStripDefault);
            });
        }
        #endregion
        #endregion

        #region utilities
        const string keyLastEndingHobbs = "LastHobbs";
        const string keyLastEndingTach = "LastTach";
        const string keyLastEntryDate = "LastEntryDate";
        const string keySessionInProgress = "InProgressFlight";

        /// <summary>
        /// Initializes a logbookentry from the form, checking that the viewer has SAVE permissions on the flight.
        /// All other errors/exceptions (besides authorization) are in the errorstring!
        /// </summary>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException">If the viewing user is not authorized to EDIT the flight, they are unauthorized</exception>
        private LogbookEntry LogbookEntryFromForm()
        {
            string pendingID = Request["idPending"];
            LogbookEntry le = String.IsNullOrEmpty(pendingID) ? new LogbookEntry() : new PendingFlight(pendingID);
            le.FlightID = util.GetIntParam(Request, "idFlight", LogbookEntryCore.idFlightNew);
            le.User = Request["szTargetUser"] ?? User.Identity.Name;
            le.ErrorString = string.Empty;  // clear this out.

            // Check that you can save - and if it's an instructor/student, further check that it's a new flight.
            CheckCanSaveFlight(le.User, le);

            try
            {
                // Core fields
                le.Date = DateTime.Parse(Request["flightDate"], CultureInfo.CurrentCulture).Date;

                le.AircraftID = util.GetIntParam(Request, "flightAircraft", 0);
                le.CatClassOverride = util.GetIntParam(Request, "flightCatClassOverride", 0);

                le.Route = Request["flightRoute"];
                le.Comment = Request["flightComments"];

                le.Approaches = util.GetIntParam(Request, "flightApproaches", 0);
                le.fHoldingProcedures = Request["flightHold"] != null;
                le.Landings = util.GetIntParam(Request, "flightLandings", 0);
                le.FullStopLandings = util.GetIntParam(Request, "flightFSDayLandings", 0);
                le.NightLandings = util.GetIntParam(Request, "flightFSNightLandings", 0);

                le.CrossCountry = (Request["flightXC"] ?? string.Empty).SafeParseDecimal();
                le.Nighttime = (Request["flightNight"] ?? string.Empty).SafeParseDecimal();
                le.SimulatedIFR = (Request["flightSimIMC"] ?? string.Empty).SafeParseDecimal();
                le.IMC = (Request["flightIMC"] ?? string.Empty).SafeParseDecimal();
                le.GroundSim = (Request["flightGroundSim"] ?? string.Empty).SafeParseDecimal();
                le.Dual = (Request["flightDual"] ?? string.Empty).SafeParseDecimal();
                le.CFI = (Request["flightCFI"] ?? string.Empty).SafeParseDecimal();
                le.SIC = (Request["flightSIC"] ?? string.Empty).SafeParseDecimal();
                le.PIC = (Request["flightPIC"] ?? string.Empty).SafeParseDecimal();
                le.TotalFlightTime = (Request["flightTotal"] ?? string.Empty).SafeParseDecimal();

                le.HobbsStart = Request["flightHobbsStart"].SafeParseDecimal();
                le.HobbsEnd = Request["flightHobbsEnd"].SafeParseDecimal();

                // Datetimes have been entered in the user's preferred timezone
                TimeZoneInfo tz = MyFlightbook.Profile.GetUser(User.Identity.Name).PreferredTimeZone;

                le.EngineStart = Request["flightEngineStart"].ParseUTCDateTime(le.Date, tz);
                le.EngineEnd = Request["flightEngineEnd"].ParseUTCDateTime(le.Date, tz);
                le.FlightStart = Request["flightFlightStart"].ParseUTCDateTime(le.Date, tz);
                le.FlightEnd = Request["flightFlightEnd"].ParseUTCDateTime(le.Date, tz);

                le.CustomProperties = new CustomPropertyCollection(CustomFlightProperty.PropertiesFromJSONTuples(Request["flightPropTuples"], le.FlightID, le.Date, CultureInfo.CurrentCulture), true);

                // Each of the custom properties that is a date-time has been expressed in user's preferred timezone; need to convert to UTC
                foreach (CustomFlightProperty cfp in le.CustomProperties)
                {
                    if (cfp.PropertyType.Type == CFPPropertyType.cfpDateTime && cfp.DateValue.HasValue())
                        cfp.DateValue = DateTime.SpecifyKind(cfp.DateValue, DateTimeKind.Local).ConvertFromTimezone(tz);
                }

                // If this is from a pending flight, its saved telemetry might be in the flightPendingTelemetry hidden field.
                string cachedFlightData = (Request["flightPendingTelemetry"] ?? string.Empty);
                if (!String.IsNullOrEmpty(cachedFlightData))
                    le.FlightData = Convert.FromBase64String(cachedFlightData).Uncompress();
                // But if new data is explicitly provided, overwrite that:
                if ((Request.Files["flightTelemetry"]?.ContentLength ?? 0) > 0)
                    le.FlightData = FlightData.ReadFromStream(Request.Files["flightTelemetry"]?.InputStream);

                IEnumerable<VideoRef> videoRefs = JsonConvert.DeserializeObject<VideoRef[]>(Request["flightVideosJSON"]);
                le.Videos.Clear();
                foreach (VideoRef vr in videoRefs)
                    le.Videos.Add(vr);

                le.PopulateImages();

                le.fIsPublic = Request["flightPublic"] != null;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                le.ErrorString = ex.Message;
            }
            return le;
        }

        private bool CommitFlight(LogbookEntry le)
        {
            if (le.IsValid())
            {
                // ensure that the aircraft is in their profile
                UserAircraft ua = new UserAircraft(le.User);
                if (ua[le.AircraftID] == null)
                    ua.FAddAircraftForUser(new Aircraft(le.AircraftID));

                // if a new flight and hobbs > 0, save it for the next flight
                bool fIsNew = le.IsNewFlight;
                if (fIsNew)
                {
                    Session[keyLastEndingHobbs] = le.HobbsEnd;
                    Session[keyLastEndingTach] = le.CustomProperties.DecimalValueForProperty(CustomPropertyType.KnownProperties.IDPropTachEnd);
                    Session[keyLastEntryDate] = le.Date; // new flight - save the date
                }

                try
                {
                    if (le.FCommit(le.HasFlightData))
                    {
                        AircraftUtility.LastTail = le.AircraftID;

                        if (fIsNew)
                        {
                            // this should now have a flight ID - save this so that we can scroll to it.
                            Session[MFBConstants.keySessLastNewFlight] = le.FlightID;

                            // process pending images, if this was a new flight
                            foreach (MFBPendingImage pendingImage in MFBPendingImage.PendingImagesInSession(Session))
                            {
                                pendingImage.Commit(MFBImageInfoBase.ImageClass.Flight, le.FlightID.ToString(CultureInfo.InvariantCulture));
                            }
                        }

                        // Badge computation may be wrong
                        MyFlightbook.Profile.GetUser(le.User).SetAchievementStatus(Achievements.Achievement.ComputeStatus.NeedsComputing);
                    }
                }
                catch (MyFlightbookException ex)
                {
                    le.ErrorString = !String.IsNullOrEmpty(le.ErrorString) ? le.ErrorString : ex?.InnerException.Message ?? ex.Message;
                }
            }

            return String.IsNullOrEmpty(le.ErrorString);
        }
        #endregion

        #region Child Views
        [ChildActionOnly]
        public ActionResult FlightIssues(IEnumerable<FlightIssue> flightIssues)
        {
            ViewBag.flightIssues = flightIssues ?? Array.Empty<FlightIssue>();
            return PartialView("_flightlint");
        }

        [ChildActionOnly]
        public ActionResult EmbeddedVideos(LogbookEntry le, bool fCanDoVideo)
        {
            ViewBag.le = le;
            ViewBag.fCanDoVideo = fCanDoVideo;
            return PartialView("_videoRefs");
        }

        /// <summary>
        /// Renders the "meat" of the flight editor - JUST the fields of the flight.  This allows updating of the main fields (e.g., for autofill) without doing a full-page postback
        /// </summary>
        /// <param name="le">The flight to edit</param>
        /// <param name="pf">If it's a pending flight, this is that flight as a pending flight</param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult FlightEditorBody(Profile pfTarget, Profile pfViewer, LogbookEntry le, PendingFlight pf)
        {
            if (le == null)
                throw new ArgumentNullException(nameof(le));
            if (pfTarget == null || pfViewer == null)
                throw new UnauthorizedAccessException();
            // Validate that this user has authority to add/edit the user's flight.
            bool fAddStudent = (CheckCanViewFlights(pfTarget?.UserName, User.Identity.Name)?.CanAddLogbook ?? false);
            ViewBag.fAddStudent = fAddStudent;

            ViewBag.activeTemplates = PropertyTemplate.DefaultPropTemplatesForUser(pfTarget.UserName, le.AircraftID, fAddStudent);
            ViewBag.le = le;
            ViewBag.pf = pf;
            ViewBag.pfTarget = pfTarget;
            ViewBag.pfViewer = pfViewer;
            ViewBag.rgAircraft = new UserAircraft(pfTarget.UserName).GetAircraftForUser();
            return PartialView("_editFlightBody");
        }

        [ChildActionOnly]
        public ActionResult FlightEditor(string targetUser, LogbookEntry le = null, bool fAsAdmin = false, string nextFlightHref = null, string prevFlightHref = null, string onCancel = null, string onSave = null, int chk = 0)
        {
            if (String.IsNullOrEmpty(targetUser))
                throw new UnauthorizedAccessException();
            le = le ?? new LogbookEntry() { User = targetUser };

            PendingFlight pf = le as PendingFlight;

            // If this is a new flight and we have an existing flight in progress, pick that one up instead.
            if (le.IsNewFlight && pf == null &&  Session[keySessionInProgress] != null)
                le = (LogbookEntry)Session[keySessionInProgress];

            CheckCanSaveFlight(targetUser, le);

            Session.Remove(keySessionInProgress);   // clear it regardless.

            // If no aircraft ID provided, try using the last tail, if present, otherwise take the first one from their aircraft list.
            if (le.AircraftID <= 0)
                le.AircraftID = AircraftUtility.LastTail;
            if (le.AircraftID <= 0)
                le.AircraftID = new UserAircraft(targetUser).GetAircraftForUser().FirstOrDefault()?.AircraftID ?? Aircraft.idAircraftUnknown;

            ViewBag.pf = pf;
            ViewBag.le = le;
            ViewBag.pfTarget = MyFlightbook.Profile.GetUser(targetUser);
            Profile pfViewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.pfViewer = pfViewer;
            ViewBag.asAdmin = fAsAdmin && pfViewer.CanSupport;
            ViewBag.nextFlightHref = nextFlightHref;
            ViewBag.prevFlightHref = prevFlightHref;
            ViewBag.fAdminMode = !String.IsNullOrEmpty(Request["a"]) && pfViewer.CanSupport;
            ViewBag.onCancel = onCancel;
            ViewBag.onSave = onSave;

            ViewBag.flightIssues = (chk != 0) ? (new FlightLint().CheckFlights(new LogbookEntryBase[] { le }, le.User, FlightLint.DefaultOptionsForLocale).FirstOrDefault()?.Issues ?? Array.Empty<FlightIssue>()) : null;

            if (le.IsNewFlight && pf == null)
            {
                // Set the starting hobbs - if a new flight - to the ending hobbs of the last flight, if present
                le.HobbsStart = ((decimal?)Session[keyLastEndingHobbs]) ?? 0.0M;
                le.CustomProperties.Add(CustomFlightProperty.PropertyWithValue(CustomPropertyType.KnownProperties.IDPropTachStart, ((decimal?)Session[keyLastEndingTach]) ?? 0.0M));

                // If the user has entered another flight this session, default to that date rather than today
                if (Session[keyLastEntryDate] != null)
                    le.Date = (DateTime)Session[keyLastEntryDate];

                // Now that we've used these values, clear them
                Session.Remove(keyLastEndingHobbs);
                Session.Remove(keyLastEndingTach);
                Session.Remove(keyLastEntryDate);
            }

            return PartialView("_editFlight");
        }

        /// <summary>
        /// Returns a property set editor for the given combination of user, aircraft, and selected templates
        /// </summary>
        /// <param name="szTargetUser">The user for whom the editing is being performed</param>
        /// <param name="rgfp">Any existing properties that may have values (i.e., that are in the flight) - can contain empty values</param>
        /// <param name="fHHMM">HHMM preference</param>
        /// <param name="fStripDefault">True to remove any properties that have default values</param>
        /// <param name="activeTemplateIDs">The set of currently active templates; if null, this gets initialized from aircraft and user's default or MRU</param>
        /// <param name="timeZone">The default time zone to use for datetime fields</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [ChildActionOnly]
        public ActionResult PropSetEditor(string szTargetUser, IEnumerable<CustomFlightProperty> rgfp, bool fHHMM, IEnumerable<PropertyTemplate> activeTemplates, TimeZoneInfo timeZone, bool fStripDefault = false)
        {
            if (rgfp == null)
                throw new ArgumentNullException(nameof(rgfp));
            if (string.IsNullOrEmpty(szTargetUser))
                throw new ArgumentNullException(nameof(szTargetUser));
            if (activeTemplates == null)
                throw new ArgumentNullException(nameof(activeTemplates));

            // create a merged template either the currently active templates, if any, otherwise the default templates for the user/aircraft
            HashSet<int> hsSelectedTemplates = new HashSet<int>();
            foreach (PropertyTemplate pt in activeTemplates)
                hsSelectedTemplates.Add(pt.ID);
            PropertyTemplate ptMerged = PropertyTemplate.MergedTemplate(activeTemplates);

            List<CustomFlightProperty> Properties = new List<CustomFlightProperty>(rgfp);
            List<CustomPropertyType> lstRemainingProps = new List<CustomPropertyType>();
            List<CustomFlightProperty> ActiveProperties = new List<CustomFlightProperty>();

            CustomPropertyType[] rgCptAll = CustomPropertyType.GetCustomPropertyTypes(szTargetUser);

            foreach (CustomPropertyType cpt in rgCptAll)
            {
                // see if this either has a value or is in one of the active templates.
                // if it doesn't have a value but is in a template, give it a value.
                CustomFlightProperty fp = Properties.Find(cfp => cfp.PropTypeID == cpt.PropTypeID);

                // To be included, it must be EITHER
                // a) in the merged set of templates OR
                // b) in the set of properties with a non-default value (fp != null && !fp.IsDefaultValue) OR
                // c) in the set of properties with a default value (fp != null && (!fStripDefault && fp.IsDefaultValue)
                bool fInclude = ptMerged.ContainsProperty(cpt.PropTypeID) || (fp != null && (!fStripDefault || !fp.IsDefaultValue));
                if (fp == null)
                    fp = new CustomFlightProperty(cpt);

                if (!fInclude)
                    lstRemainingProps.Add(cpt);
                else
                {
                    fp.SetPropertyType(cpt);    // make sure to pick up things like autocompletion
                    ActiveProperties.Add(fp);
                }
            }
            ActiveProperties.Sort((cfp1, cfp2) => { return cfp1.PropertyType.SortKey.CompareCurrentCultureIgnoreCase(cfp2.PropertyType.SortKey); });

            ViewBag.groupedTemplates = TemplateCollection.GroupTemplates(UserPropertyTemplate.TemplatesForUser(szTargetUser, true));
            ViewBag.propList = ActiveProperties;
            ViewBag.activeTemplateIDs = hsSelectedTemplates;
            ViewBag.remainingProps = lstRemainingProps;
            ViewBag.fHHMM = fHHMM;
            ViewBag.timeZone = timeZone;
            return PartialView("_propSetEdit");
        }

        [ChildActionOnly]
        public ActionResult PropEdit(CustomFlightProperty fp, bool fHHMM, TimeZoneInfo timeZone, bool fHidden = false)
        {
            ViewBag.fp = fp ?? throw new ArgumentNullException(nameof(fp));
            ViewBag.fHHMM = fHHMM;
            ViewBag.timeZone = timeZone;

            // Add any cross-fill:

            if (fp.PropertyType.Type == CFPPropertyType.cfpInteger)
            {
                if (fp.PropertyType.IsLanding || fp.PropertyType.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropGliderTow)
                    ViewBag.xfillDescriptor = new CrossFillDescriptor(Resources.LocalizedText.CrossfillPromptLandings, "getTotalFillFunc('fieldLandings')");
                else if (fp.PropertyType.IsApproach)
                    ViewBag.xfillDescriptor = new CrossFillDescriptor(Resources.LocalizedText.CrossfillPromptApproaches, "getTotalFillFunc('fieldApproaches')");
            }
            else if (fp.PropertyType.Type == CFPPropertyType.cfpDecimal)
            {
                if (fp.PropertyType.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTachStart)
                    ViewBag.xfillDescriptor = new CrossFillDescriptor(Resources.LogbookEntry.TachCrossfillTip, String.Format(CultureInfo.InvariantCulture, "getTachFill(currentlySelectedAircraft, '{0}')", "~/Member/Ajax.asmx".ToAbsolute()));
                else if (fp.PropertyType.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropTaxiTime)
                    ViewBag.xfillDescriptor = new CrossFillDescriptor(Resources.LogbookEntry.TaxiCrossFillTip, String.Format(CultureInfo.InvariantCulture, "getTaxiFill('{0}')", "~/Member/Ajax.asmx".ToAbsolute()));
                else if (fp.PropertyType.PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropAirborneTime)
                    ViewBag.XFillDescriptor = new CrossFillDescriptor(Resources.LogbookEntry.AirborneCrossFillTip, String.Format(CultureInfo.InvariantCulture, "getAirborneFill('{0}')", "~/Member/Ajax.asmx".ToAbsolute()));
                else if (!fp.PropertyType.IsBasicDecimal)
                    ViewBag.xfillDescriptor = new CrossFillDescriptor(Resources.LocalizedText.CrossfillPrompt, "getTotalFillFunc('fieldTotal')");
            }
            ViewBag.hideByDefault = fHidden;

            return PartialView("_propEdit");
        }

        [ChildActionOnly]
        public ActionResult CoreField(string id, string name, string value, string cssClass, string label, string xFillSource, bool fHHMM)
        {
            ViewBag.id = id;
            ViewBag.name = name;
            ViewBag.value = value;
            ViewBag.cssClass = cssClass;
            ViewBag.label = label;
            ViewBag.cfd = String.IsNullOrEmpty(xFillSource) ? null : new CrossFillDescriptor(Resources.LocalizedText.CrossfillPrompt, "getTotalFillFunc('fieldTotal')");
            ViewBag.fHHMM = fHHMM;
            return PartialView("_coreField");
        }

        [ChildActionOnly]
        public ActionResult PendingFlightsTable(int offset, int pageSize, string sortField, SortDirection sortDir)
        {
            List<PendingFlight> lst = new List<PendingFlight>(PendingFlight.PendingFlightsForUser(User.Identity.Name));
            lst.Sort((l1, l2) => { return LogbookEntry.CompareFlights(l1, l2, sortField, sortDir); });

            ViewBag.pendingFlights = lst.GetRange(offset, Math.Min(pageSize, lst.Count - offset));
            ViewBag.pageSize = pageSize;
            ViewBag.offset = offset;
            ViewBag.curPage = (offset / pageSize);
            ViewBag.numPages = (lst.Count / pageSize) + 1;
            ViewBag.viewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.sortField = sortField;
            ViewBag.sortDir = sortDir;
            return PartialView("_pendingFlightTable");
        }
        #endregion

        #region Visible Endpoints
        [Authorize]
        public ActionResult Pending(string id = null)
        {
            Profile viewer = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.viewer =viewer;
            List <PendingFlight> lst = new List<PendingFlight>(PendingFlight.PendingFlightsForUser(User.Identity.Name));
            ViewBag.pendingFlights = lst;
            if (!String.IsNullOrEmpty(id))
            {
                int i = lst.FindIndex(pf => pf.PendingID.CompareOrdinal(id) == 0);
                if (i >= 0)
                {
                    PendingFlight pf = lst[i];
                    // Pull in any pending images
                    pf.PopulateImages();

                    // since flights are in descending chronological order, the one that is earlier in the list is the "next" flight, the one that is later in the list is "previous"
                    if (i > 0)
                        ViewBag.nextFlightHref = Url.Action(this.ControllerContext.RouteData.Values["action"].ToString(), this.ControllerContext.RouteData.Values["controller"].ToString(), new { id = lst[i - 1].PendingID });
                    if (i < lst.Count - 1)
                        ViewBag.prevFlightHref = Url.Action(this.ControllerContext.RouteData.Values["action"].ToString(), this.ControllerContext.RouteData.Values["controller"].ToString(), new { id = lst[i + 1].PendingID });
                    ViewBag.pendingFlight = pf;
                    ViewBag.onCancel = "cancelEdit";
                }
                if (ViewBag.pendingFlight == null)
                    return RedirectToAction("PendingFlights", new { id = string.Empty });
            }
            ViewBag.onSave = "flightSaved";
            ViewBag.pageSize = FlightsPerPageForUser(viewer);
            return View("reviewPending");
        }

        // GET: mvc/FlightEdit
        public ActionResult Flight(string id, string fq = null, int clone = -1, int reverse = -1, string src = "", int a = 0)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            FlightQuery q = String.IsNullOrEmpty(fq) ? new FlightQuery(User.Identity.Name) : FlightQuery.FromBase64CompressedJSON(fq);
            if (q.UserName.CompareOrdinal(User.Identity.Name) != 0)
                throw new UnauthorizedAccessException("invalid query - incorrect username");

            LogbookEntry le = null;
            int idFlight = LogbookEntryCore.idFlightNone;

            // Three scenarios:
            // a) src= parameter provided and no id parameter provided - initialize from that
            // b) Otherwise, if a valid id is provided, initialize from that.
            // c) Otherwise, just create a new flight.
            if (!String.IsNullOrEmpty(src) && String.IsNullOrEmpty(id))
            {
                LogbookEntry leSrc = LogbookEntry.FromShareKey(src, User.Identity.Name);

                if (leSrc.FlightID == LogbookEntryCore.idFlightNone || !String.IsNullOrEmpty(leSrc.ErrorString))
                    throw new UnauthorizedAccessException("Invalid source key");

                le = leSrc.Clone(le);
                le.User = User.Identity.Name; // for safety.

                // clear out any role like PIC/SIC that likely doesn't carry over to the target pilot.
                le.CFI = le.Dual = le.PIC = le.SIC = 0.0M;

                if (le.AircraftID != Aircraft.idAircraftUnknown)
                {
                    // Add this aircraft to the user's profile if needed
                    UserAircraft ua = new UserAircraft(User.Identity.Name);
                    Aircraft ac = new Aircraft(le.AircraftID);
                    if (!ua.CheckAircraftForUser(ac))
                        ua.FAddAircraftForUser(ac);
                }
            } else if (int.TryParse(id, out idFlight) && idFlight > 0) {
                // force load it - we will check in the flight editor if we have permission to edit it
                le = new LogbookEntry(idFlight, User.Identity.Name, fForceLoad: true);

                if (clone != -1)
                {
                    le = le.Clone(null, reverse != -1);
                    le.CleanNewClone();
                }

                le.PopulateImages();
            }
            else
                le = new LogbookEntry() { User =  User.Identity.Name };
            
            ViewBag.returnURL = "~/mvc/flights".ToAbsolute() + SetUpNextPrevious(q, idFlight, pf);
            ViewBag.pf = pf;
            ViewBag.fAsAdmin = (a != 0 && pf.CanSupport);
            ViewBag.le = le;
            return View("editFlight");
        }

        public ActionResult Index()
        {
            return Flight(string.Empty);
        }
        #endregion
    }
}