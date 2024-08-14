﻿using MyFlightbook.Currency;
using MyFlightbook.Telemetry;
using MyFlightbook.Templates;
using MyFlightbook.Web.Sharing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class PrefsController : AdminControllerBase
    {
        #region Web Services
        #region autofill
        [HttpPost]
        [Authorize]
        public ActionResult SetAutofillOptions(AutoFillOptions afo)
        {
            return SafeOp(() =>
            {
                if (afo == null)
                    throw new ArgumentNullException(nameof(afo));
                if (!(new HashSet<int>(AutoFillOptions.DefaultSpeeds).Contains((int) afo.TakeOffSpeed)))
                    afo.TakeOffSpeed = AutoFillOptions.DefaultTakeoffSpeed;
                afo.LandingSpeed = AutoFillOptions.BestLandingSpeedForTakeoffSpeed((int) afo.TakeOffSpeed);
                afo.IgnoreErrors = true;
                afo.SaveForUser(User.Identity.Name);
                return new EmptyResult();
            });
        }
        #endregion

        #region Properties and Templates
        [HttpPost]
        [Authorize]
        public ActionResult EditPropBlockList(int id, bool fAllow)
        {
            return SafeOp(() =>
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                if (fAllow)
                {
                    if (pf.BlocklistedProperties.Contains(id))
                    {
                        pf.BlocklistedProperties.RemoveAll(idPropType => id == idPropType);
                        pf.FCommit();
                        // refresh the cache
                        CustomPropertyType.GetCustomPropertyTypes(User.Identity.Name, true);
                    }
                }
                else if (!pf.BlocklistedProperties.Contains(id))
                {
                    pf.BlocklistedProperties.Add(id);
                    pf.FCommit();
                    // refresh the cache
                    CustomPropertyType.GetCustomPropertyTypes(User.Identity.Name, true);
                }
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult PropTemplateEditor(int idTemplate, string containerID, bool fCopy)
        {
            return SafeOp(() =>
            {
                ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                ViewBag.idTemplate = idTemplate;
                ViewBag.containerID = containerID;
                ViewBag.fCopy = fCopy;
                return PartialView("_prefEditPropTemplate");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult CommitPropTemplate()
        {
            return SafeOp(() =>
            {
                UserPropertyTemplate pt = new UserPropertyTemplate()
                {
                    ID = Convert.ToInt32(Request["propTemplateID"], CultureInfo.InvariantCulture),
                    Name = Request["propTemplateName"],
                    Group = (PropertyTemplateGroup)Enum.Parse(typeof(PropertyTemplateGroup), Request["propTemplateCategory"]),
                    Owner = User.Identity.Name,
                    OriginalOwner = Request["propTemplateOriginalOwner"],
                    Description = Request["propTemplateDescription"],
                    IsDefault = Convert.ToBoolean(Request["propTemplateDefault"], CultureInfo.InvariantCulture),
                    IsPublic = Convert.ToBoolean(Request["propTemplatePublic"], CultureInfo.InvariantCulture)
                };
                IEnumerable<int> propIDs = Request["propTemplateIncludedIDs"].ToInts();
                foreach (int propTypeID in propIDs)
                    pt.PropertyTypes.Add(propTypeID);
                pt.Commit();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult AddPublicTemplate(int idTemplate)
        {
            return SafeOp(() =>
            {
                UserPropertyTemplate pt = new UserPropertyTemplate(idTemplate);
                IEnumerable<PropertyTemplate> currentTemplates = UserPropertyTemplate.TemplatesForUser(User.Identity.Name);
                PersistablePropertyTemplate pptNew = pt.CopyPublicTemplate(User.Identity.Name);
                // Override the existing one if it exists with the same name.
                PropertyTemplate ptMatch = currentTemplates.FirstOrDefault(ptUser => ptUser.Group == pt.Group && ptUser.Name.CompareCurrentCultureIgnoreCase(pt.Name) == 0);
                if (ptMatch != null)
                    pptNew.ID = ptMatch.ID;
                pptNew.Commit();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SetTemplateFlags(int idTemplate, bool fPublic, bool fDefault)
        {
            return SafeOp(() =>
            {
                UserPropertyTemplate pt = new UserPropertyTemplate(idTemplate)
                {
                    IsDefault = fDefault,
                    IsPublic = fPublic
                };
                pt.Commit();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePropTemplate(int idTemplate)
        {
            return SafeOp(() =>
            {
                UserPropertyTemplate pt = new UserPropertyTemplate(idTemplate);
                pt.DeleteForUser(User.Identity.Name);
                return new EmptyResult();
            });
        }
        #endregion

        #region Deadlines
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDeadline(int idDeadline)
        {
            return SafeOp(() =>
            {
                DeadlineCurrency dc = DeadlineCurrency.DeadlineForUser(User.Identity.Name, idDeadline) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Deadline {0} doesn't exist for user {1}", idDeadline, User.Identity.Name));
                dc.FDelete();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateDeadline(int idDeadline)
        {
            return SafeOp(() =>
            {
                DeadlineCurrency dc = DeadlineCurrency.DeadlineForUser(User.Identity.Name, idDeadline) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Deadline {0} doesn't exist for user {1}", idDeadline, User.Identity.Name));
                if (dc.AircraftHours > 0)
                    dc.AircraftHours = dc.NewHoursBasedOnHours(decimal.Parse(Request["deadlineNewHours"], CultureInfo.CurrentCulture));
                else
                    dc.Expiration = dc.NewDueDateBasedOnDate(DateTime.Parse(Request["deadlineNewDate"], CultureInfo.CurrentCulture));
                if (!dc.IsValid() || !dc.FCommit())
                    throw new InvalidOperationException(dc.ErrorString);
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeadlineList()
        {
            return SafeOp(() => { return PartialView("_prefDeadlineList"); });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeadlineEditor(int idDeadline, bool fShared = false, int idAircraft = Aircraft.idAircraftUnknown)
        {
            ViewBag.idDeadline = idDeadline;
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            ViewBag.fShared = fShared;
            ViewBag.idAircraft = idAircraft;
            return PartialView("_prefDeadlineEdit");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitDeadline()
        {
            return SafeOp(() =>
            {
                bool fUseHours = !String.IsNullOrEmpty(Request["deadlineUsesHours"]);
                DeadlineCurrency.RegenUnit ru = Request["deadlineRegenType"].CompareCurrentCultureIgnoreCase("None") == 0 ?
                    DeadlineCurrency.RegenUnit.None : (fUseHours ? DeadlineCurrency.RegenUnit.Hours : (DeadlineCurrency.RegenUnit)Enum.Parse(typeof(DeadlineCurrency.RegenUnit), Request["deadlineRegenRange"]));
                bool fCreateShared = bool.Parse(Request["deadlineShared"]);
                DeadlineCurrency dc = new DeadlineCurrency()
                {
                    ID = Convert.ToInt32(Request["idDeadline"], CultureInfo.InvariantCulture),
                    Username = fCreateShared ? null : User.Identity.Name,
                    Name = Request["deadlineName"],
                    Expiration = fUseHours ? DateTime.MinValue : Convert.ToDateTime(Request["deadlineNewDate"], CultureInfo.CurrentCulture),
                    AircraftHours = fUseHours ? Convert.ToDecimal(Request["deadlineNewHours"], CultureInfo.InvariantCulture) : 0,
                    AircraftID = int.TryParse(Request["deadlineAircraftID"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int acID) ? acID : Aircraft.idAircraftUnknown,
                    RegenType = ru,
                    RegenSpan = ru == DeadlineCurrency.RegenUnit.None ? 0 : Convert.ToInt32(Request["deadlineRegenInterval"], CultureInfo.InvariantCulture),
                };
                if (!dc.IsValid() || !dc.FCommit())
                    throw new InvalidOperationException(dc.ErrorString);
                return new EmptyResult();
            });
        }
        #endregion

        #region Custom Currency
        [HttpPost]
        [Authorize]
        public ActionResult CustCurrencyEditor(int idCustCurrency)
        {
            return SafeOp(() =>
            {
                ViewBag.idCustCurrency = idCustCurrency;
                ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                return PartialView("_prefCustCurrencyEdit");
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult CustCurrencyList()
        {
            return SafeOp(() =>
            {
                return PartialView("_prefCustCurrencyList");
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCustCurrency(int idCustCurrency)
        {
            return SafeOp(() =>
            {
                CustomCurrency cc = CustomCurrency.CustomCurrencyForUser(User.Identity.Name, idCustCurrency) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "No such currency {0} for use {1}", idCustCurrency, User.Identity.Name));
                cc.FDelete();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitCustCurrency()
        {
            return SafeOp(() =>
            {
                CustomCurrency cc = new CustomCurrency
                {
                    ID = Convert.ToInt32(Request["custCurrencyID"], CultureInfo.InvariantCulture),
                    UserName = User.Identity.Name,
                    DisplayName = Request["custCurrencyName"],
                    RequiredEvents = Convert.ToDecimal(Request["custCurrencyMinEvents"], CultureInfo.InvariantCulture),
                    EventType = (CustomCurrency.CustomCurrencyEventType)Enum.Parse(typeof(CustomCurrency.CustomCurrencyEventType), Request["custCurrencyEventType"]),
                    ExpirationSpan = Convert.ToInt32(Request["custCurrencyTimeFrame"], CultureInfo.InvariantCulture),
                    CurrencyTimespanType = (TimespanType)Enum.Parse(typeof(TimespanType), Request["custCurrencyMonthsDays"]),
                    ModelsRestriction = Request["custCurrencyModels"].ToInts(),
                    AircraftRestriction = Request["custCurrencyAircraft"].ToInts(),
                    CategoryRestriction = Request["custCurrencyCategory"],
                    AirportRestriction = Request["custCurrencyAirport"],
                    TextRestriction = Request["custCurrencyText"],
                    PropertyRestriction = Request["custCurrencyProps"].ToInts()
                };
                if (Enum.TryParse(Request["custCurrencyLimitType"], out CustomCurrency.LimitType lt))
                    cc.CurrencyLimitType = lt;
                cc.CatClassRestriction = Enum.TryParse(Request["custCurrencyCatClass"], out CategoryClass.CatClassID ccid) ? ccid : 0;

                if (!cc.FCommit())
                    throw new InvalidOperationException(cc.ErrorString);

                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult SetCustCurrencyActive(int idCustCurrency, bool fActive)
        {
            return SafeOp(() =>
            {
                CustomCurrency cc = CustomCurrency.CustomCurrencyForUser(User.Identity.Name, idCustCurrency) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "No such currency {0} for use {1}", idCustCurrency, User.Identity.Name));
                cc.IsActive = fActive;
                if (!cc.FCommit())
                    throw new InvalidOperationException(cc.ErrorString);
                return new EmptyResult();
            });
        }
        #endregion

        #region Share keys
        [HttpPost]
        [Authorize]
        public ActionResult UpdateShareKey(string idShareKey, bool fFlights, bool fTotals, bool fCurrency, bool fAchievements, bool fAirports)
        {
            return SafeOp(() =>
            {
                ShareKey sk = ShareKey.ShareKeyWithID(idShareKey) ?? throw new InvalidOperationException("Unknown key: " + idShareKey);
                if (sk.Username.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException("User " + User.Identity.Name + " does not own this share link!");

                sk.CanViewFlights = fFlights;
                sk.CanViewTotals = fTotals;
                sk.CanViewCurrency = fCurrency;
                sk.CanViewAchievements = fAchievements;
                sk.CanViewVisitedAirports = fAirports;
                sk.FCommit();
                return new EmptyResult();
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeleteShareKey(string id)
        {
            return SafeOp(() =>
            {
                ShareKey sk = ShareKey.ShareKeyWithID(id) ?? throw new InvalidOperationException("Unknown key: " + id);
                if (sk.Username.CompareOrdinal(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException();
                sk.FDelete();
                return new EmptyResult();
            });
        }
        #endregion
        #endregion

        #region child actions
        #region Autofill options
        [ChildActionOnly]
        public ActionResult AutoFillOptionsEditor(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            ViewBag.afo = AutoFillOptions.DefaultOptionsForUser(szUser);
            return PartialView("_autofillOptions");
        }
        #endregion
        #endregion

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CreateShareKey()
        {
            ShareKey sk = new ShareKey(User.Identity.Name)
            {
                Name = Request["prefShareLinkName"],
                CanViewFlights = Request["prefShareLinkFlights"] != null,
                CanViewTotals = Request["prefShareLinkTotals"] != null,
                CanViewCurrency = Request["prefShareLinkCurrency"] != null,
                CanViewAchievements = Request["prefShareLinkAchievements"] != null,
                CanViewVisitedAirports = Request["prefShareLinkAirports"] != null,
            };
            try
            {
                sk.FCommit();
                return RedirectToAction("Index", new { pane = "social" });
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return RedirectToAction("Index", new { pane = "social", shareKeyErr = ex.Message });
            }
        }

        // GET: mvc/Prefs
        [Authorize]
        public ActionResult Index()
        {
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            return View("mainPrefs");
        }

        [Authorize]
        public ActionResult BrowseTemplates()
        {
            ViewBag.pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            return View("browseTemplates");
        }
    }
}