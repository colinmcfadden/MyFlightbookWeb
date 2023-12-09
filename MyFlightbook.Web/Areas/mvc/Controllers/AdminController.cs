﻿using MyFlightbook.Achievements;
using MyFlightbook.Charting;
using MyFlightbook.Histogram;
using MyFlightbook.Instruction;
using MyFlightbook.Web.Admin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;


/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminController : AdminControllerBase
    {
        #region Admin Web Services
        #region Misc - Props
        [HttpPost]
        [Authorize]
        public ActionResult InvalidProps()
        {
            return SafeOp(ProfileRoles.maskCanSupport, () =>
            {
                ViewBag.emptyProps = CustomFlightProperty.ADMINEmptyProps();
                return PartialView("_miscInvalidProps");
            });
        }

        [HttpPost]
        [Authorize]
        public string DeleteEmptyProp(int propid)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                CustomFlightProperty cfp = new CustomFlightProperty() { PropID = propid };
                cfp.DeleteProperty();
                return string.Empty;
            });
        }
        #endregion

        #region Misc - Signatures
        private const string szSessKeyInvalidSigProgress = "sessSignedflightsAutoFixed";

        [HttpPost]
        [Authorize]
        public ActionResult UpdateInvalidSigs()
        {
            return SafeOp(ProfileRoles.maskCanSupport, () => {
                if (Session[szSessKeyInvalidSigProgress] == null)
                    Session[szSessKeyInvalidSigProgress] = new { offset = 0, lstToFix = new List<LogbookEntryBase>(), lstAutoFix = new List<LogbookEntryBase>(), progress = string.Empty, additionalFlights = 0 };

                dynamic state = Session[szSessKeyInvalidSigProgress];

                int cFlights = LogbookEntryBase.AdminGetProblemSignedFlights(state.offset, state.lstToFix, state.lstAutoFix);
                dynamic newState = new
                {
                    additionalFlights = cFlights,
                    offset = state.offset + cFlights,
                    lstToFix = state.lstToFix,
                    lstAutoFix = state.lstAutoFix,
                    progress = String.Format(CultureInfo.CurrentCulture, "Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", state.offset, state.lstToFix.Count, state.lstAutoFix.Count)
                };
                Session[szSessKeyInvalidSigProgress] = newState;
                return (ActionResult) Json(newState);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult InvalidSigsResult()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () => {
                dynamic state = Session[szSessKeyInvalidSigProgress];
                Session[szSessKeyInvalidSigProgress] = null;
                ViewBag.lstToFix = state.lstToFix;
                ViewBag.lstAutoFix = state.lstAutoFix;
                ViewBag.progress = String.Format(CultureInfo.CurrentCulture, "Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", state.offset, state.lstToFix.Count, state.lstAutoFix.Count);
                return PartialView("_invalidSigs");
            });
        }
        #endregion

        #region Misc - Nightly run
        [HttpPost]
        [Authorize]
        public string KickOffNightlyRun()
        {
            return SafeOp(ProfileRoles.maskSiteAdminOnly, () =>
            {
                string szURL = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, VirtualPathUtility.ToAbsolute("~/public/TotalsAndcurrencyEmail.aspx"));
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    byte[] rgdata = wc.DownloadData(szURL);
                    string szContent = Encoding.UTF8.GetString(rgdata);
                    if (!szContent.Contains("-- SuccessToken --"))
                        throw new InvalidOperationException();
                    return string.Empty;
                }
            });
        }
        #endregion

        #region Misc - Cache
        [HttpPost]
        [Authorize]
        public string FlushCache()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return String.Format(CultureInfo.CurrentCulture, "Cache flushed, {0:#,##0} items removed.", util.FlushCache());
            });
        }
        #endregion

        #region Property management
        [HttpPost]
        [Authorize]
        public ActionResult UpdateProperty(int idPropType, string title, string shortTitle, string sortKey, string formatString, string description, uint flags)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                CustomPropertyType cptOrig = CustomPropertyType.GetCustomPropertyType(idPropType);
                CustomPropertyType cpt = new CustomPropertyType();
                util.CopyObject(cptOrig, cpt);  // make our modifications on a copy to avoid mucking up live objects in case fcommit fails.
                cpt.Title = title;
                cpt.ShortTitle = shortTitle;
                cpt.SortKey = sortKey;
                cpt.FormatString = formatString;
                cpt.Description = description;
                cpt.Flags = flags;
                cpt.FCommit();
                return Json(cpt);
            });
        }
        #endregion

        #region Achievements
        [Authorize]
        [HttpPost]
        public string InvalidateBadgeCache()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                MyFlightbook.Profile.InvalidateAllAchievements();
                return "Achievements invalidated";
            });
        }
        #endregion

        #region Stats
        #endregion
        [Authorize]
        [HttpPost]
        public string TrimOldTokensAndAuths()
        {
            return SafeOp(ProfileRoles.maskCanReport, () =>
            {
                return AdminStats.TrimOldTokensAndAuths();
            });
        }

        [Authorize]
        [HttpPost]
        public string TrimOldOAuth()
        {
            return SafeOp(ProfileRoles.maskCanReport, () =>
            {
                AdminStats.TrimOldTokensAndAuths();
                return string.Empty;
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> FlightsPerUser(string dateRange)
        {
            return await SafeOp(ProfileRoles.maskCanReport, async () =>
            {
                IEnumerable<FlightsPerUserStats> lstFlightsPerUser = null;
                DateTime? creationDate = null;
                if (!String.IsNullOrEmpty(dateRange))
                    creationDate = DateTime.Now.AddMonths(-Convert.ToInt32(dateRange, CultureInfo.InvariantCulture));

                await Task.Run(() => { lstFlightsPerUser = FlightsPerUserStats.Refresh(creationDate); });

                NumericBucketmanager bmFlightsPerUser = new NumericBucketmanager() { BucketForZero = true, BucketWidth = 100, BucketSelectorName = "FlightCount" };
                HistogramManager hmFlightsPerUser = new HistogramManager()
                {
                    SourceData = lstFlightsPerUser,
                    SupportedBucketManagers = new BucketManager[] { bmFlightsPerUser },
                    Values = new HistogramableValue[] { new HistogramableValue("Range", "Flights", HistogramValueTypes.Integer) }
                };

                GoogleChartData flightsPerUserChart = new GoogleChartData
                {
                    Title = "Flights/user",
                    XDataType = GoogleColumnDataType.@string,
                    YDataType = GoogleColumnDataType.number,
                    XLabel = "Flights/User",
                    YLabel = "Users - All",
                    SlantAngle = 90,
                    Width = 1000,
                    Height = 500,
                    ChartType = GoogleChartType.ColumnChart,
                    ContainerID = "flightsPerUserDiv",
                    TickSpacing = (uint)((lstFlightsPerUser.Count() < 20) ? 1 : (lstFlightsPerUser.Count() < 100 ? 5 : 10))
                };

                bmFlightsPerUser.ScanData(hmFlightsPerUser);

                using (DataTable dt = bmFlightsPerUser.ToDataTable(hmFlightsPerUser))
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        flightsPerUserChart.XVals.Add((string)dr["DisplayName"]);
                        flightsPerUserChart.YVals.Add((int)Convert.ToDouble(dr["Flights"], CultureInfo.InvariantCulture));
                    }
                }
                ViewBag.flightsPerUserChart = flightsPerUserChart;
                return PartialView("_userActivity");
            });
        }
        #endregion

        #region Full page endpoints

        [HttpGet]
        public async Task<ActionResult> Stats(bool fForEmail = false)
        {
            // Allow local requests, unless you are authenticated AND can report AND not requesting the full page
            bool fIsLocalAndForEmail = Request.IsLocal && fForEmail;
            if (!fIsLocalAndForEmail)
                CheckAuth(ProfileRoles.maskCanReport);

            AdminStats astats = new AdminStats();
            if (await astats.Refresh(!fForEmail))
            {
                ViewBag.stats = astats;
                ViewBag.emailOnly = fForEmail;

                GoogleChartData newUserChart = new GoogleChartData()
                {
                    ChartType = GoogleChartType.LineChart,
                    XDataType = GoogleColumnDataType.date,
                    YDataType = GoogleColumnDataType.number,
                    Y2DataType = GoogleColumnDataType.number,
                    UseMonthYearDate = true,
                    Title = "Number of Users",
                    LegendType = GoogleLegendType.bottom,
                    TickSpacing = 1,
                    SlantAngle = 0,
                    XLabel = "Year/Month",
                    YLabel = "New Users",
                    Y2Label = "Cumulative Users",
                    Width = 800,
                    Height = 400,
                    ContainerID = "newUserDiv"
                };
                foreach (NewUserStats nus in astats.NewUserStatsMonthly)
                {
                    newUserChart.XVals.Add(nus.DisplayPeriod);
                    newUserChart.YVals.Add(nus.NewUsers);
                    newUserChart.Y2Vals.Add(nus.RunningTotal);
                }
                ViewBag.newUserChart = newUserChart;

                if (!fForEmail)
                {
                    GoogleChartData userActivityChart = new GoogleChartData()
                    {
                        LegendType = GoogleLegendType.right,
                        UseMonthYearDate = true,
                        XDataType = GoogleColumnDataType.date,
                        Title = "User Activity",
                        XLabel = "Date of Last Activity",
                        YLabel = "Users",
                        SlantAngle = 90,
                        ChartType = GoogleChartType.LineChart,
                        Width = 800,
                        Height = 400,
                        ContainerID = "userActivityDiv"
                    };
                    foreach (UserActivityStats uas in astats.UserActivity)
                    {
                        userActivityChart.XVals.Add(uas.Date);
                        userActivityChart.YVals.Add(uas.Count);
                    }
                    ViewBag.activityChart = userActivityChart;

                    GoogleChartData flightsByDateChart = new GoogleChartData()
                    {
                        LegendType = GoogleLegendType.bottom,
                        Title = "Flights recorded / month",
                        XDataType = GoogleColumnDataType.@string,
                        YDataType = GoogleColumnDataType.number,
                        UseMonthYearDate = true,
                        Y2DataType = GoogleColumnDataType.number,
                        XLabel = "Flights/Month",
                        TickSpacing = 36,
                        YLabel = "Flights",
                        Y2Label = "Running Total",
                        SlantAngle = 90,
                        ChartType = GoogleChartType.LineChart,
                        Width = 1000,
                        Height = 500,
                        ContainerID = "flightsByDateDiv"
                    };
                    YearMonthBucketManager bmFlights = new YearMonthBucketManager() { BucketSelectorName = "DateRange" };

                    HistogramManager hmFlightsByDate = new HistogramManager()
                    {
                        SourceData = astats.FlightsByDate,
                        SupportedBucketManagers = new BucketManager[] { bmFlights },
                        Values = new HistogramableValue[] { new HistogramableValue("DateRange", "Flights", HistogramValueTypes.Time) }
                    };
                    bmFlights.ScanData(hmFlightsByDate);

                    using (DataTable dt = bmFlights.ToDataTable(hmFlightsByDate))
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            flightsByDateChart.XVals.Add((string)dr["DisplayName"]);
                            flightsByDateChart.YVals.Add((int)Convert.ToDouble(dr["Flights"], CultureInfo.InvariantCulture));
                            flightsByDateChart.Y2Vals.Add((int)Convert.ToDouble(dr["Flights Running Total"], CultureInfo.InvariantCulture));
                        }
                    }
                    ViewBag.flightsByDateChart = flightsByDateChart;
                }
                return View("stats");
            }
            else
                return new EmptyResult();
        }

        [Authorize]
        [HttpPost]
        public ActionResult UpdateFAQ(int idFaq, string Category, string Question, string Answer)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                FAQItem fi = new FAQItem()
                {
                    Category = Category,
                    Question = Question,
                    Answer = Answer,
                    idFAQ = idFaq
                };
                    fi.Commit();
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpGet]
        public ActionResult FAQ()
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            ViewBag.faqs = FAQItem.AllFAQItems;
            return View("adminFAQ");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Endorsements(int id, string FARRef, string BodyTemplate, string Title)
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            EndorsementType et = new EndorsementType()
            {
                ID = id,
                FARReference = FARRef,
                BodyTemplate = BodyTemplate,
                Title = Title
            };
            et.FCommit();
            ViewBag.templates = EndorsementType.LoadTemplates();
            return View("adminEndorsements");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Endorsements()
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            ViewBag.templates = EndorsementType.LoadTemplates();
            return View("adminEndorsements");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Achievements(int id, string Name, string AirportsRaw, string overlay, bool? fBinary, int bronze = 0, int silver = 0, int gold = 0, int platinum = 0)
        {
            if (Name == null)
                throw new ArgumentNullException(nameof(Name));
            if (AirportsRaw == null)
                throw new ArgumentNullException(nameof(AirportsRaw));
            CheckAuth(ProfileRoles.maskCanManageData);

            AirportListBadgeData b = new AirportListBadgeData()
            {
                ID = (Badge.BadgeID)id,
                Name = Name,
                AirportsRaw = AirportsRaw,
                OverlayName = overlay,
                BinaryOnly = fBinary ?? false
            };
            b.Levels[0] = bronze;
            b.Levels[1] = silver;
            b.Levels[2] = gold;
            b.Levels[3] = platinum;
            b.Commit();
            return Redirect("Achievements");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Achievements()
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            ViewBag.airportBadges = AirportListBadge.BadgeData;
            return View("adminAchievements");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Properties(string txtCustomPropTitle, string txtCustomPropFormat, string txtCustomPropDesc, uint propType, uint propFlags)
        {
            CheckAuth(ProfileRoles.maskCanManageData);  // check for ability to do any admin
            CustomPropertyType cpt = new CustomPropertyType()
            {
                Title = txtCustomPropTitle,
                FormatString = txtCustomPropFormat,
                Description = txtCustomPropDesc,
                Type = (CFPPropertyType)propType,
                Flags = propFlags
            };
            cpt.FCommit();
            ViewBag.propList = CustomPropertyType.GetCustomPropertyTypes();
            return View("adminProps");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Properties()
        {
            CheckAuth(ProfileRoles.maskCanManageData);  // check for ability to do any admin
            ViewBag.propList = CustomPropertyType.GetCustomPropertyTypes();
            return View("adminProps");
        }

        [Authorize]
        public ActionResult Misc()
        {
            CheckAuth(ProfileRoles.maskCanManageData);

            Dictionary<string, int> d = new Dictionary<string, int>();
            foreach (System.Collections.DictionaryEntry entry in HttpRuntime.Cache)
            {
                string szClass = entry.Value.GetType().ToString();
                d[szClass] = d.TryGetValue(szClass, out int value) ? ++value : 1;
            }

            ViewBag.cacheSummary = d;
            ViewBag.memStats = String.Format(CultureInfo.CurrentCulture, "Cache has {0:#,##0} items", HttpRuntime.Cache.Count);
            return View("adminMisc");
        }

        [Authorize]
        // GET: mvc/Admin
        public ActionResult Index()
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            return View();
        }
        #endregion
    }
}