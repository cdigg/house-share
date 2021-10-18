using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using HouseShare.Domain.Repositories.Abstract;
using WebMatrix.WebData;

namespace HouseShare.Util
{
    public class CurrentUser : IRequiresSessionState
    {
        private static IUserProfile _userProfileRepository;

        public CurrentUser(IUserProfile userProfileRepo)
        {
            _userProfileRepository = userProfileRepo;
        }
        //verify that user is logged in, if the session has expired and log the user out if needed
        public static bool IsLoggedIn()
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || Membership.GetUser() == null)
            {
                HttpContext.Current.Session.Abandon();
                FormsAuthentication.SignOut();
                return false;
            }
            else
                return true;
        }
        public static int UserId
        {
            get { return WebSecurity.CurrentUserId; }
        }

        public static string Username
        {
            get
            {
                if (Membership.GetUser() == null)
                {
                    HttpContext.Current.Session.Abandon();
                    FormsAuthentication.SignOut();
                    return "";
                }
                return Membership.GetUser().UserName;
            }
        }

        /// <summary>
        /// ID of current organization
        /// </summary>
        public static int HouseId
        {
            get
            {
                //get org id
                if (HttpContext.Current.Session["houseId"] == null)
                {
                    HttpContext.Current.Session["houseId"] = _userProfileRepository.Get.Single(x => x.UserId == UserId).House.Id;
                }
                return int.Parse(HttpContext.Current.Session["houseId"].ToString());
            }
        }

    }
}
