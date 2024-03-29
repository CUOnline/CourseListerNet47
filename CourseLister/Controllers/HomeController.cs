﻿
using Canvas.Clients;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Canvas.Clients.Models.Enums;
using Canvas.Clients.Models;
using Microsoft.Owin.Security;
using CourseLister.Models;
using System.IO;

namespace CourseLister.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var authenticateResult = await HttpContext.GetOwinContext().Authentication.AuthenticateAsync("ExternalCookie");
            if (authenticateResult == null)
            {
                return RedirectToAction("ExternalLogin");
            }

            if (!(await Authorized(ConfigurationManager.AppSettings["CanvasAccountId"])))
            {
                // return unauthorized view
                var model = new HomeViewModel()
                {
                    Authorized = false
                };
                return View(model);
            }
            else
            {
                var model = new HomeViewModel()
                {
                    Authorized = true
                };

                return View(model);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Index(HomeViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var client = new AccountsClient();
                IEnumerable<Course> courses = null;
                try
                {
                    courses = await client.GetAllCourses(viewModel.CanvasAccountId, viewModel.Query);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(viewModel.CanvasAccountId), ex);
                    return View(viewModel);
                }

                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                foreach (var course in courses.Where(x => x.AccountId == viewModel.CanvasAccountId))
                {
                    writer.Write($"{course.Id}, {course.CourseCode}, {course.SisCourseId}, {course.Name}\n");
                }
                writer.Flush();
                stream.Position = 0;

                return new FileStreamResult(stream, "text/csv")
                {
                    FileDownloadName = "Export.csv"
                };
            }

            return View(viewModel);
        }

        #region LoginHelper
        [AllowAnonymous]
        public ActionResult ExternalLogin(string provider)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult("Canvas", Url.Action("ExternalLoginCallback", "Home"));
        }

        [AllowAnonymous]
        public ActionResult ExternalLoginCallback()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogout(string provider)
        {
            var authenticationManager = HttpContext.GetOwinContext().Authentication;
            authenticationManager.SignOut("ExternalCookie");

            return RedirectToAction("LoggedOut");
        }

        public ActionResult LoggedOut()
        {
            return View();
        }

        private async Task<bool> Authorized(string accountId)
        {
            List<RoleNames> authorizedRoles = new List<RoleNames>()
            {
                RoleNames.AccountAdmin
            };

            var authenticateResult = await HttpContext.GetOwinContext().Authentication.AuthenticateAsync("ExternalCookie");
            if (authenticateResult != null)
            {
                ViewBag.authenticated = true;
                AccountsClient client = new AccountsClient();
                var userId = authenticateResult.Identity.Claims.Where(cl => cl.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;

                var roles = (await client.GetAccountRolesForUserAsync(accountId, userId)).Where(x => x.WorkflowState == WorkflowState.Active);

                if (roles.Select(x => x.Name).Intersect(authorizedRoles).Any())
                {
                    return true;
                }
                else
                {
                    var account = await client.Get<Account>(accountId);
                    ViewBag.error = $"You do not have the proper roles assigned to access information for {account.Name}";
                }
            }

            return false;
        }

        // Used for XSRF protection when adding external logns
        private const string XsrfKey = "XsrfId";

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        private async Task<string> GetCurrentUserEmail()
        {
            var authenticateResult = await HttpContext.GetOwinContext().Authentication.AuthenticateAsync("ExternalCookie");
            if (authenticateResult != null)
            {
                var emailClaim = authenticateResult.Identity.Claims.Where(cl => cl.Type == ClaimTypes.Email).FirstOrDefault();

                return emailClaim?.Value;
            }

            return string.Empty;
        }
        #endregion
    }
}