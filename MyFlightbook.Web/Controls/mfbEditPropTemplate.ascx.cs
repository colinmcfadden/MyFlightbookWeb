﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Templates.Controls
{
    public partial class Controls_mfbEditPropTemplate : UserControl
    {
        public event EventHandler<PropertyTemplateEventArgs> TemplateCreated;

        #region Properties
        private const string szvsActiveTemplate = "vsActiveTemplate";
        public UserPropertyTemplate ActiveTemplate
        {
            get { return (UserPropertyTemplate)ViewState[szvsActiveTemplate]; }
            set { ViewState[szvsActiveTemplate] = value; }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            Page.ClientScript.RegisterClientScriptInclude("ListDrag", ResolveClientUrl("~/Public/Scripts/listdrag.js?v=6"));
            Page.ClientScript.RegisterClientScriptInclude("filterDropdown", ResolveClientUrl("~/Public/Scripts/DropDownFilter.js?v=3"));
            searchProps.TextBoxControl.Attributes["onkeyup"] = String.Format(CultureInfo.InvariantCulture, "FilterProps(this, '{0}', '{1}', '{2}')", divAvailableProps.ClientID, lblFilteredLabel.ClientID, Resources.LogbookEntry.PropertiesFound);
            divAvailableProps.Attributes["ondrop"] = String.Format(CultureInfo.InvariantCulture, "javascript:lstDropTemplate.moveProp(event, 'cptT', this, '{0}', '{1}')", hdnUsedProps.ClientID, hdnAvailableProps.ClientID);
            divCurrentProps.Attributes["ondrop"] = String.Format(CultureInfo.InvariantCulture, "javascript:lstDropTemplate.moveProp(event, 'cptT', this, '{0}', '{1}')", hdnAvailableProps.ClientID, hdnUsedProps.ClientID);

            if (!IsPostBack)
            {
                if (ActiveTemplate == null)
                    ActiveTemplate = new UserPropertyTemplate() { Owner = Page.User.Identity.Name, OriginalOwner = string.Empty };

                PropertyTemplateGroup[] rgGroups = (PropertyTemplateGroup[])Enum.GetValues(typeof(PropertyTemplateGroup));

                foreach (PropertyTemplateGroup ptg in rgGroups)
                    if (ptg != PropertyTemplateGroup.Automatic)
                        cmbCategories.Items.Add(new ListItem(PropertyTemplate.NameForGroup(ptg), ptg.ToString()));
                cmbCategories.SelectedIndex = 0;

                ToForm();

                locTemplateDescription1.Text = Branding.ReBrand(Resources.LogbookEntry.TemplateDescription);
                locTemplateDescription2.Text = Branding.ReBrand(Resources.LogbookEntry.TemplateDescription2);
            }
            else
            {
                if (ActiveTemplate == null)
                    throw new InvalidOperationException("Active Template is null - how? ");
                ActiveTemplate.PropertyTypes.Clear();
                int[] props = (String.IsNullOrEmpty(hdnUsedProps.Value) ? Array.Empty<int>() : JsonConvert.DeserializeObject<int[]>(hdnUsedProps.Value)) ?? throw new InvalidOperationException("props is null");
                foreach (int propid in props)
                    ActiveTemplate.PropertyTypes.Add(propid);
                UpdateLists();
            }
        }

        protected void UpdateLists()
        {
            UserPropertyTemplate pt = ActiveTemplate;
            List<CustomPropertyType> lstAll = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes());

            List<CustomPropertyType> lstIncluded = lstAll.FindAll(cpt => pt.ContainsProperty(cpt));
            IEnumerable<int> includedProps = lstIncluded.Select(cpt => cpt.PropTypeID);
            rptTemplateProps.DataSource = lstIncluded;
            rptTemplateProps.DataBind();

            lstAll.RemoveAll(cpt => pt.ContainsProperty(cpt));
            IEnumerable<int> availableProps = lstAll.Select(cpt => cpt.PropTypeID);
            rptAvailableProps.DataSource = lstAll;
            rptAvailableProps.DataBind();

            hdnAvailableProps.Value = JsonConvert.SerializeObject(availableProps);
            hdnUsedProps.Value = JsonConvert.SerializeObject(includedProps);
        }

        protected void ToForm()
        {
            searchProps.SearchText = string.Empty;  // clear the filter before updating the lists.
            UpdateLists();

            UserPropertyTemplate pt = ActiveTemplate;
            txtTemplateName.Text = pt.Name;
            txtDescription.Text = pt.Description;
            cmbCategories.SelectedValue = pt.Group.ToString();

            List<TemplateCollection> rgtc = new List<TemplateCollection>(TemplateCollection.GroupTemplates(UserPropertyTemplate.TemplatesForUser(Page.User.Identity.Name)));
            rgtc.RemoveAll(tc => tc.Group == PropertyTemplateGroup.Automatic);
            rptTemplateGroups.DataSource = rgtc;
            rptTemplateGroups.DataBind();
            btnCancelEditTemplate.Visible = ActiveTemplate.ID > 0;

            mvOwnedTemplates.Visible = ActiveTemplate.ID <= 0;

            mvOwnedTemplates.SetActiveView(rgtc.Count == 0 ? vwNoTemplates : vwTemplates);
        }

        protected void btnSaveTemplate_Click(object sender, EventArgs e)
        {
            Page.Validate("vgPropTemplate");
            if (!Page.IsValid)
                return;

            ActiveTemplate.Name = txtTemplateName.Text;
            ActiveTemplate.Description = txtDescription.Text;
            ActiveTemplate.Group = (PropertyTemplateGroup)Enum.Parse(typeof(PropertyTemplateGroup), cmbCategories.SelectedValue);
            ActiveTemplate.Owner = Page.User.Identity.Name;
            try
            {
                ActiveTemplate.Commit();
                TemplateCreated?.Invoke(this, new PropertyTemplateEventArgs(ActiveTemplate));

                // Clear for the next
                ActiveTemplate = new UserPropertyTemplate();
                ToForm();
                cpeNewTemplate.ClientState = "true";
            }
            catch (MyFlightbookValidationException ex)
            {
                lblErr.Text = ex.Message;
            }
        }

        protected void btnCancelEditTemplate_Click(object sender, EventArgs e)
        {
            ActiveTemplate = new UserPropertyTemplate();
            ToForm();
            cpeNewTemplate.ClientState = "true";
        }


        protected static UserPropertyTemplate Target(Control c)
        {
            if (c == null)
                throw new ArgumentNullException(nameof(c));
            HiddenField h = (HiddenField)c.NamingContainer.FindControl("hdnID");
            if (!Int32.TryParse(h.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ptid))
                throw new FormatException(String.Format(CultureInfo.InvariantCulture, "Invalid property template ID '{0}'", h.Value));

            UserPropertyTemplate pt = new UserPropertyTemplate(ptid);
            return pt;
        }

        protected void ckIsPublic_CheckedChanged(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            CheckBox ck = (CheckBox)sender;
            UserPropertyTemplate pt = Target(ck);

            if (ck.Checked)
            {
                List<UserPropertyTemplate> lst = new List<UserPropertyTemplate>(UserPropertyTemplate.PublicTemplates());
                if (lst.Find(ptPublic => ptPublic.Name.CompareCurrentCultureIgnoreCase(pt.Name) == 0 && ptPublic.Owner.CompareCurrentCultureIgnoreCase(pt.Owner) != 0) != null)
                {
                    ((Label)ck.NamingContainer.FindControl("lblPublicErr")).Text = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.TemplateDuplicateSharedName, System.Web.HttpUtility.HtmlEncode(pt.Name));
                    ck.Checked = false;
                    return;
                }
            }

            pt.IsPublic = ck.Checked;
            pt.Commit();
        }

        protected void imgbtnEdit_Click(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            ActiveTemplate = Target((sender as Control).NamingContainer);
            ToForm();
            cpeNewTemplate.Collapsed = false;
            cpeNewTemplate.ClientState = "false";
        }

        protected void imgDelete_Click(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            PersistablePropertyTemplate pt = Target((sender as Control).NamingContainer);

            // Remove this from any aircraft templates that it may be associated with
            UserAircraft ua = new UserAircraft(pt.Owner);
            IEnumerable<Aircraft> rgac = ua.GetAircraftForUser();
            foreach (Aircraft ac in rgac)
            {
                if (ac.DefaultTemplates.Contains(pt.ID))
                {
                    ac.DefaultTemplates.Remove(pt.ID);
                    ua.FAddAircraftForUser(ac);
                }
            }

            pt.Delete();

            ToForm();
        }

        protected void imgCopy_Click(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            UserPropertyTemplate pt = Target((sender as Control).NamingContainer);
            if (pt.Name.Length > 35)
                pt.Name = pt.Name.Substring(0, 35) + "…";
            pt.Name = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.TemplateCopyTemplate, pt.Name);
            pt.ID = (int)KnownTemplateIDs.ID_NEW;
            pt.Commit();

            // And set up to edit this.
            ActiveTemplate = pt;
            cpeNewTemplate.Collapsed = false;
            cpeNewTemplate.ClientState = "false";

            ToForm();
        }

        protected void ckDefault_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox ck = sender as CheckBox;
            UserPropertyTemplate pt = Target(ck);
            pt.IsDefault = ck.Checked;
            pt.Commit();
            ToForm();
        }

        protected bool MatchesFilter(string text)
        {
            if (String.IsNullOrEmpty(text))
                return true;

            text = text.ToUpper(CultureInfo.CurrentCulture);
            string[] words = RegexUtility.WhiteSpace.Split(searchProps.SearchText.ToUpper(CultureInfo.CurrentCulture));

            foreach (string word in words)
            {
                if (String.IsNullOrWhiteSpace(word))
                    continue;
                if (!text.Contains(word))
                    return false;
            }

            return true;
        }

        protected string StyleForTitle(string text)
        {
            return MatchesFilter(text) ? string.Empty : "display: none;";
        }

        protected void searchProps_SearchClicked(object sender, EventArgs e)
        {
            UpdateLists();
        }

        protected void rptTemplates_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            Controls_popmenu pop = (Controls_popmenu)e.Item.FindControl("popmenu");
            PersistablePropertyTemplate pt = (PersistablePropertyTemplate)e.Item.DataItem;
            pop.FindControl("imgbtnEdit").Visible = pop.FindControl("lnkEditTemplate").Visible = pt.IsMutable;
            ((AjaxControlToolkit.ConfirmButtonExtender)(pop.FindControl("confirmDeleteTemplate"))).ConfirmText = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.TemplateDeleteConfirm, pt.Name);
        }
    }
}