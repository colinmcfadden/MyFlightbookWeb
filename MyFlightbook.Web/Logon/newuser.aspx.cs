using System;
using System.Web;
using System.Web.Security;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.LogonPages
{
    public partial class newuser : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void CreatingUser(object sender, LoginCancelEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            TextBox txtEmail = (TextBox)CreateUserWizardStep1.ContentTemplateContainer.FindControl("Email");
            TextBox txtUserName = (TextBox)CreateUserWizardStep1.ContentTemplateContainer.FindControl("UserName");

            if (txtEmail == null)
                throw new MyFlightbookException("Email control is null");
            if (txtUserName == null)
                throw new MyFlightbookException("UserName control is null");

            string szUser = Membership.GetUserNameByEmail(txtEmail.Text);
            if (szUser.Length > 0)
            {
                Panel pnlCollision = (Panel)CreateUserWizardStep1.ContentTemplateContainer.FindControl("pnlEmailCollision") ?? throw new MyFlightbookException("Cannot find error panel for email collision");
                pnlCollision.Visible = true;
                e.Cancel = true; // collision with existing email address
                return;
            }

            // now find a unique username to propose
            txtUserName.Text = HttpUtility.HtmlEncode(UserEntity.UserNameForEmail(txtEmail.Text));
        }

        /// <summary>
        /// User has been created - (a) sign them in, and (b) update their profile with the first/last name (if any) that they provided
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void UserCreated(object sender, EventArgs e)
        {
            string szUser = ((TextBox)CreateUserWizardStep1.ContentTemplateContainer.FindControl("UserName")).Text;
            FormsAuthentication.SetAuthCookie(szUser, false);

            // we send email from here rather than from the createuserwizard because for some reason createuserwizard isn't sending it.
            ProfileAdmin.FinalizeUser(szUser, ((TextBox)CreateUserWizardStep1.ContentTemplateContainer.FindControl("txtFirst")).Text, ((TextBox)CreateUserWizardStep1.ContentTemplateContainer.FindControl("txtLast")).Text);

            Response.Cookies[MFBConstants.keyNewUser].Value = true.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Redirect to the next page, but only if it is relative (for security)
            string szURLNext = util.GetStringParam(Request, "ReturnUrl");
            Response.Redirect(!String.IsNullOrEmpty(szURLNext) && Uri.IsWellFormedUriString(szURLNext, UriKind.Relative) ? szURLNext : "~/mvc/flights");
        }
    }
}