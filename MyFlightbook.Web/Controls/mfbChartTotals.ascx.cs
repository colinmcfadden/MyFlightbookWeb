﻿using MyFlightbook.Histogram;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Charting
{
    public partial class mfbChartTotals : UserControl
    {
        public HistogramManager HistogramManager { get; set; }

        protected BucketManager BucketManager
        {
            get { return HistogramManager.SupportedBucketManagers.FirstOrDefault(bm => bm.DisplayName.CompareOrdinal(cmbGrouping.SelectedValue) == 0); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                cmbGrouping.SelectedValue = value.DisplayName;
            }
        }

        protected HistogramableValue SelectedFieldToGraph
        {
            get { return HistogramManager.Values.FirstOrDefault(hv => hv.DataField.CompareOrdinal(cmbFieldToView.SelectedValue) == 0); }
        }

        public bool CanDownload
        {
            get { return lnkDownloadCSV.Visible; }
            set { lnkDownloadCSV.Visible = value; }
        }

        protected bool UseHHMM { get; set; }

        protected void SetUpSelectors()
        {
            if (HistogramManager != null && (cmbFieldToView.Items.Count == 0 || cmbGrouping.Items.Count == 0))
            {
                cmbFieldToView.DataSource = HistogramManager.Values;
                cmbFieldToView.DataBind();
                cmbFieldToView.SelectedIndex = 0;

                cmbGrouping.DataSource = HistogramManager.SupportedBucketManagers;
                cmbGrouping.DataBind();
                cmbGrouping.SelectedIndex = 0;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // verify that we have a valid user (should never be a problem)
                if (!Page.User.Identity.IsAuthenticated)
                    return;

                SetUpSelectors();
            }

            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            UseHHMM = pf.UsesHHMM;

            if (Visible)
                Refresh();
        }

        protected string FormatBucketForMonthlyData(MonthsOfYearData moy, int month)
        {
            if (moy == null)
                throw new ArgumentNullException(nameof(moy));
            Bucket b = moy.ValueForMonth(month);

            if (b == null)
                return string.Empty;

            string szTitle = BucketManager.FormatForType(b.Values[SelectedFieldToGraph.DataField], SelectedFieldToGraph.DataType, UseHHMM, false);

            return String.IsNullOrEmpty(b.HRef) ? szTitle : String.Format(CultureInfo.InvariantCulture, "<a href=\"{0}\" target=\"_blank\">{1}</a>", VirtualPathUtility.ToAbsolute(b.HRef), szTitle);
        }

        /// <summary>
        /// Updates the chart based on the computed data (stored in RawData)
        /// </summary>
        protected void RefreshChartAndTable(IEnumerable<Bucket> buckets)
        {
            if (buckets == null)
                throw new ArgumentNullException(nameof(buckets));

            HistogramableValue hv = SelectedFieldToGraph;

            int count = 0;
            double average = 0;

            bool fHHMM = Profile.GetUser(Page.User.Identity.Name).UsesHHMM;

            gcTrends.ChartData.Clear();
            foreach (Bucket b in buckets)
            {
                gcTrends.ChartData.XVals.Add(gcTrends.XDataType == GoogleColumnDataType.@string ? b.DisplayName : b.OrdinalValue);
                gcTrends.ChartData.YVals.Add(b.Values[hv.DataField]);
                if (!b.ExcludeFromAverage)
                {
                    average += b.Values[hv.DataField];
                    count++;
                }

                if (b.HasRunningTotals)
                    gcTrends.ChartData.Y2Vals.Add(b.RunningTotals[hv.DataField]);

                string RankAndPercent = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ChartTotalsRankAndPercentOfTotals, b.Ranks[hv.DataField], buckets.Count(), b.PercentOfTotal[hv.DataField]);
                // Add a tooltip for the item.
                gcTrends.ChartData.Tooltips.Add(String.Format(CultureInfo.CurrentCulture, "<div class='ttip'><div class='dataVal'>{0}</div><div>{1}: <span class='dataVal'>{2}</span></div><div>{3}</div></div>",
                    HttpUtility.HtmlEncode(b.DisplayName),
                    HttpUtility.HtmlEncode(hv.DataName),
                    HttpUtility.HtmlEncode(BucketManager.FormatForType(b.Values[hv.DataField], hv.DataType, fHHMM)),
                    HttpUtility.HtmlEncode(RankAndPercent)));
            }

            if (gcTrends.ChartData.ShowAverage = (ckIncludeAverage.Checked && count > 0))
                gcTrends.ChartData.AverageValue = average / count;

            string szLabel = "{0}";
            {
                switch (hv.DataType)
                {
                    case HistogramValueTypes.Integer:
                        szLabel = Resources.LocalizedText.ChartTotalsNumOfX;
                        break;
                    case HistogramValueTypes.Time:
                        szLabel = Resources.LocalizedText.ChartTotalsHoursOfX;
                        break;
                    case HistogramValueTypes.Decimal:
                    case HistogramValueTypes.Currency:
                        szLabel = Resources.LocalizedText.ChartTotalsAmountOfX;
                        break;
                }
            }
            gcTrends.ChartData.YLabel = String.Format(CultureInfo.CurrentCulture, szLabel, hv.DataName);
            gcTrends.ChartData.Y2Label = Resources.LocalizedText.ChartRunningTotal;

            gcTrends.ChartData.ClickHandlerJS = BucketManager.ChartJScript;

            pnlChart.Visible = true;
        }

        protected void cmbFieldToview_SelectedIndexChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        /// <summary>
        /// Recomputes the data from the datasource and refreshes it
        /// </summary>
        public void Refresh()
        {
            // In case Page_Load has not been called, make sure combo boxes are populated.
            SetUpSelectors();
            if (String.IsNullOrEmpty(cmbGrouping.SelectedValue))
                cmbGrouping.SelectedIndex = 0;

            if (HistogramManager == null)
                throw new InvalidOperationException("Null HistogramManager");

            BucketManager bm = BucketManager;

            bm.ScanData(HistogramManager);

            // check for daily with less than a year
            if (bm is DailyBucketManager dbm && dbm.MaxDate.CompareTo(dbm.MinDate) > 0 && dbm.MaxDate.Subtract(dbm.MinDate).TotalDays > 365)
            {
                BucketManager = bm = new WeeklyBucketManager();
                bm.ScanData(HistogramManager);
            }

            if (bm is DateBucketManager datebm)
            {
                gcTrends.ChartData.XDatePattern = datebm.DateFormat;
                gcTrends.ChartData.XDataType = GoogleColumnDataType.date;
            }
            else
            {
                gcTrends.ChartData.XDatePattern = "{0}";
                gcTrends.XDataType = GoogleColumnDataType.@string;
            }

            using (DataTable dt = bm.ToDataTable(HistogramManager))
            {
                gvRawData.Columns.Clear();
                if (String.IsNullOrEmpty(bm.BaseHRef))
                    gvRawData.Columns.Add(new BoundField() { DataField = BucketManager.ColumnNameDisplayName, HeaderText = bm.DisplayName });
                else
                    gvRawData.Columns.Add(new HyperLinkField() { DataTextField = BucketManager.ColumnNameDisplayName, DataNavigateUrlFormatString = "{0}", DataNavigateUrlFields = new string[] { BucketManager.ColumnNameHRef }, HeaderText = bm.DisplayName, Target = "_blank" });

                foreach (DataColumn dc in dt.Columns)
                {
                    if (dc.ColumnName.CompareCurrentCultureIgnoreCase(BucketManager.ColumnNameHRef) == 0 || dc.ColumnName.CompareOrdinal(BucketManager.ColumnNameDisplayName) == 0)
                        continue;
                    gvRawData.Columns.Add(new BoundField() { HeaderText = dc.ColumnName, DataField = dc.ColumnName });
                }
                gvRawData.DataSource = dt;
                gvRawData.DataBind();
            }

            RefreshChartAndTable(bm.Buckets);

            if (bm is YearMonthBucketManager ybm && ybm.Buckets.Any())
            {
                gvYearly.Visible = true;
                gvYearly.DataSource = ybm.ToYearlySummary();
                gvYearly.DataBind();

                // Set the column headers so that they're localized
                for (int i = 0; i < 12; i++)
                    gvYearly.HeaderRow.Cells[i + 1].Text = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[i];
            }
            else
            {
                gvYearly.DataSource = Array.Empty<YearMonthBucketManager>();
                gvYearly.DataBind();
                gvYearly.Visible = false;
            }
        }

        protected void cmbGrouping_SelectedIndexChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        protected void lnkDownloadCSV_Click(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "text/csv";
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}-{3}", Branding.CurrentBrand.AppName, Resources.LocalizedText.DownloadFlyingStatsFilename, Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
            string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", RegexUtility.UnSafeFileChars.Replace(szFilename, string.Empty));
            Response.AddHeader("Content-Disposition", szDisposition);
            gvRawData.ToCSV(Response.OutputStream);
            Response.End();
        }

        protected void ckIncludeAverage_CheckedChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        protected void gvRawData_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            foreach (TableCell c in e.Row.Cells)
                c.CssClass = "PaddedCell";
        }
    }
}