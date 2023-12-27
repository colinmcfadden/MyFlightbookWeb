﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2007-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AllMakesController : Controller
    {
        private void SetCaching()
        {
            Response.Cache.SetExpires(DateTime.Now.AddDays(14));
            Response.Cache.SetCacheability(HttpCacheability.Public);
        }

        // GET: mvc/AllMakes
        public ActionResult Index(int idman, int idmodel)
        {
            // Different experience when signed in and when not signed in.
            if (User.Identity.IsAuthenticated && idman == 0)
                return Redirect("~/mvc/Aircraft/Makes");

            // No manufacturer specified - show all manufacturers 
            if (idman <= 0)
            {
                ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.Makes.AllManufacturersTitle, Branding.CurrentBrand.AppName);
                List<Manufacturer> lst = new List<Manufacturer>(Manufacturer.CachedManufacturers());
                lst.RemoveAll(man => man.AllowedTypes != AllowedAircraftTypes.Any);
                ViewBag.Manufacturers = lst;

                return View("manufacturerlist");
            }
            else if (idmodel <= 0)  // no model specified - show all models by this manufacturer
            {
                SetCaching();

                if (!MakeModel.ModelsByManufacturer().ContainsKey(idman))
                    throw new HttpException(404, "Not found");

                Manufacturer man = Manufacturer.CachedManufacturers().FirstOrDefault((m) => m.ManufacturerID == idman);
                ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.Makes.AllMakesTitle, Branding.CurrentBrand.AppName, man.ManufacturerName);
                SetCaching();
                ViewBag.Models = MakeModel.ModelsByManufacturer()[idman];
                return View("modellist");
            }
            else
            {
                SetCaching();

                // if we're here, we have both a manufacturer and a model, so show all of the aircraft
                MakeModel m = MakeModel.GetModel(idmodel);
                ViewBag.MakeModel = m;

                if (m == null)
                    throw new HttpException(404, "Not found");

                ViewBag.Title = String.Format(CultureInfo.CurrentCulture, Resources.Makes.AllAircraftForModel, Branding.CurrentBrand.AppName, m.DisplayName);

                List<Aircraft> lst = new List<Aircraft>();
                // UserAircraft.GetAircraftForUser is pretty heavyweight, especially for models witha  lot of aircraft like C-152.
                // We just don't need that much detail, since we're just binding images by ID and tailnumbers
                DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT idaircraft, tailnumber FROM aircraft WHERE idmodel=?modelid AND tailnumber NOT LIKE '{0}%' AND instanceType=1 ORDER BY tailnumber ASC", CountryCodePrefix.szAnonPrefix));
                dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("modelid", idmodel); },
                    (dr) =>
                    {
                        int idaircraft = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture);
                        string tailnumber = (string)dr["tailnumber"];
                        lst.Add(new Aircraft() { AircraftID = idaircraft, TailNumber = tailnumber });
                    });
                ViewBag.Aircraft = lst;
                return View("aircraftlist");
            }
        }
    }
}