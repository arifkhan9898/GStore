﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using GStore.Models;
using GStore.Models.Extensions;
using GStore.Models.ViewModels;

namespace GStore.Controllers
{
	[Authorize]
	public class ProfileController : BaseClass.BaseController
	{
		public ProfileController()
		{
		}

		public ProfileController(AspNetIdentityUserManager userManager)
		{
			UserManager = userManager;
		}

		protected override string LayoutName
		{
			get
			{
				return CurrentStoreFrontOrThrow.ProfileLayoutName;
			}
		}

		private AspNetIdentityUserManager _userManager;
		public AspNetIdentityUserManager UserManager
		{
			get
			{
				return _userManager ?? HttpContext.GetOwinContext().GetUserManager<AspNetIdentityUserManager>();
			}
			private set
			{
				_userManager = value;
			}
		}

		//
		// GET: /Profile/Index
		public async Task<ActionResult> Index(ProfileMessageId? message)
		{
			ViewBag.StatusMessage =
				message == ProfileMessageId.ChangePasswordSuccess ? "Your password has been changed."
				: message == ProfileMessageId.SetPasswordSuccess ? "Your password has been set."
				: message == ProfileMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
				: message == ProfileMessageId.Error ? "An error has occurred."
				: message == ProfileMessageId.AddPhoneSuccess ? "Your phone number was added."
				: message == ProfileMessageId.RemovePhoneSuccess ? "Your phone number was removed."
				: "";


			UserProfile profile = CurrentUserProfileOrThrow;
			Identity.AspNetIdentityUser aspUser = profile.AspNetIdentityUser();

			var model = new ProfileViewModel
			{
				HasPassword = HasPassword(),
				PhoneNumber = await UserManager.GetPhoneNumberAsync(User.Identity.GetUserId()),
				TwoFactor = await UserManager.GetTwoFactorEnabledAsync(User.Identity.GetUserId()),
				Logins = await UserManager.GetLoginsAsync(User.Identity.GetUserId()),
				BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(User.Identity.GetUserId()),
				EmailConfirmed = aspUser.EmailConfirmed,
				AspNetIdentityUser = aspUser,
				UserProfile = profile
			};
			return View(model);
		}

		//
		// GET: /Profile/RemoveLogin
		public ActionResult RemoveLogin()
		{
			var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());
			ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
			return View(linkedAccounts);
		}

		//
		// POST: /Profile/RemoveLogin
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
		{
			ProfileMessageId? message;
			var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
			if (result.Succeeded)
			{
				var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
				if (user != null)
				{
					await SignInAsync(user, isPersistent: false);
				}
				message = ProfileMessageId.RemoveLoginSuccess;
			}
			else
			{
				message = ProfileMessageId.Error;
			}
			return RedirectToAction("Logins", new { Message = message });
		}

		//
		// GET: /Profile/AddPhoneNumber
		public ActionResult AddPhoneNumber()
		{
			return View();
		}

		//
		// POST: /Profile/AddPhoneNumber
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}
			// Generate the token and send it
			var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
			if (UserManager.SmsService != null)
			{

				StoreFront storeFront = CurrentStoreFrontOrNull;
				string messageBody = Models.Extensions.StoreFrontExtensions.AddPhoneNumberMessage(storeFront, code, Request.Url);

				var message = new IdentityMessage
				{
					Destination = model.Number,
					Body = messageBody
				};
				await UserManager.SmsService.SendAsync(message);
			}
			return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
		}

		//
		// POST: /Profile/EnableTwoFactorAuthentication
		[HttpPost]
		public async Task<ActionResult> EnableTwoFactorAuthentication()
		{
			await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
			var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
			if (user != null)
			{
				await SignInAsync(user, isPersistent: false);
			}
			return RedirectToAction("Index", "Profile");
		}

		//
		// POST: /Profile/DisableTwoFactorAuthentication
		[HttpPost]
		public async Task<ActionResult> DisableTwoFactorAuthentication()
		{
			await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
			var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
			if (user != null)
			{
				await SignInAsync(user, isPersistent: false);
			}
			return RedirectToAction("Index", "Profile");
		}

		//
		// GET: /Profile/VerifyPhoneNumber
		public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
		{
			var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
			// Send an SMS through the SMS provider to verify the phone number
			if (phoneNumber == null)
			{
				return HttpBadRequest("VerifyPhoneNumber phoneNumber = null");
			}
			return View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
		}

		//
		// POST: /Profile/VerifyPhoneNumber
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}
			var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
			if (result.Succeeded)
			{
				var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
				if (user != null)
				{
					await SignInAsync(user, isPersistent: false);
				}
				return RedirectToAction("Index", new { Message = ProfileMessageId.AddPhoneSuccess });
			}
			// If we got this far, something failed, redisplay form
			ModelState.AddModelError("", "Failed to verify phone");
			return View(model);
		}

		//
		// GET: /Profile/RemovePhoneNumber
		public async Task<ActionResult> RemovePhoneNumber()
		{
			var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
			if (!result.Succeeded)
			{
				return RedirectToAction("Index", new { Message = ProfileMessageId.Error });
			}
			var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
			if (user != null)
			{
				await SignInAsync(user, isPersistent: false);
			}
			return RedirectToAction("Index", new { Message = ProfileMessageId.RemovePhoneSuccess });
		}

		//
		// GET: /Profile/ChangePassword
		public ActionResult ChangePassword()
		{
			return View();
		}

		//
		// POST: /Profile/ChangePassword
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}
			var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
			if (result.Succeeded)
			{
				var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
				if (user != null)
				{
					await SignInAsync(user, isPersistent: false);
				}

				StoreFront storeFront = CurrentStoreFrontOrNull;
				if (storeFront == null)
				{
					AddUserMessage("Password updated.", "Your password has been changed successfully for all store fronts.", AppHtmlHelpers.UserMessageType.Info);
				}
				else
				{
					string baseUrl = Url.Action("Details", "Notifications", new { id = "" });
					GStoreDb.CreatePasswordChangedNotification(CurrentStoreFrontOrThrow, CurrentUserProfileOrThrow, Request.Url, baseUrl);
				}
				return RedirectToAction("Index", new { Message = ProfileMessageId.ChangePasswordSuccess });
			}
			AddErrors(result);
			return View(model);
		}

		//
		// GET: /Profile/SetPassword
		public ActionResult SetPassword()
		{
			return View();
		}

		//
		// POST: /Profile/SetPassword
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
		{
			if (ModelState.IsValid)
			{
				var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
				if (result.Succeeded)
				{
					var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
					if (user != null)
					{
						await SignInAsync(user, isPersistent: false);
					}
					return RedirectToAction("Index", new { Message = ProfileMessageId.SetPasswordSuccess });
				}
				AddErrors(result);
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// GET: /Profile/ProfileLogins
		public async Task<ActionResult> Logins(ProfileMessageId? message)
		{
			ViewBag.StatusMessage =
				message == ProfileMessageId.RemoveLoginSuccess ? "The external login was removed."
				: message == ProfileMessageId.Error ? "An error has occurred."
				: "";
			var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
			if (user == null)
			{
				throw new ApplicationException("Cannot find user name: " + User.Identity.Name);
			}
			var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
			var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
			ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
			return View(new ProfileLoginsViewModel
			{
				CurrentLogins = userLogins,
				OtherLogins = otherLogins
			});
		}

		//
		// POST: /Profile/LinkLogin
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult LinkLogin(string provider)
		{
			// Request a redirect to the external login provider to link a login for the current user
			return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Profile"), User.Identity.GetUserId());
		}

		//
		// GET: /Profile/LinkLoginCallback
		public async Task<ActionResult> LinkLoginCallback()
		{
			var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
			if (loginInfo == null)
			{
				return RedirectToAction("Logins", new { Message = ProfileMessageId.Error });
			}
			var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
			return result.Succeeded ? RedirectToAction("Logins") : RedirectToAction("Logins", new { Message = ProfileMessageId.Error });
		}

		[Authorize]
		public async Task<ActionResult> ConfirmEmail()
		{
			UserProfile profile = CurrentUserProfileOrThrow;
			string userId = profile.UserId;
			await this.SendEmailConfirmationCode(userId);
			return View(profile);
		}

		//
		// GET: /Profile/Settings
		public ActionResult Notifications()
		{
			UserProfile profile = CurrentUserProfileOrThrow;

			NotificationSettingsViewModel viewModel = new NotificationSettingsViewModel()
			{

				AllowUsersToSendSiteMessages = profile.AllowUsersToSendSiteMessages,
				Email = profile.Email,
				NotifyAllWhenLoggedOn = profile.NotifyAllWhenLoggedOn,
				NotifyOfSiteUpdatesToEmail = profile.NotifyOfSiteUpdatesToEmail,
				SendSiteMessagesToEmail = profile.SendSiteMessagesToEmail,
				SendSiteMessagesToSms = profile.SendSiteMessagesToSms,
				SubscribeToNewsletterEmail = profile.SubscribeToNewsletterEmail,
				UserProfile = profile
			};

			return View(viewModel);
		}

		//
		// POST: /Profile/Settings
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Notifications(NotificationSettingsViewModel model)
		{

			UserProfile profile = CurrentUserProfileOrThrow;

			profile.AllowUsersToSendSiteMessages = model.AllowUsersToSendSiteMessages;
			profile.NotifyAllWhenLoggedOn = model.NotifyAllWhenLoggedOn;
			profile.NotifyOfSiteUpdatesToEmail = model.NotifyOfSiteUpdatesToEmail;
			profile.SendSiteMessagesToEmail = model.SendSiteMessagesToEmail;
			profile.SubscribeToNewsletterEmail = model.SubscribeToNewsletterEmail;

			GStoreDb.SaveChanges();

			return RedirectToAction("Index");
		}

		#region Helpers
		// Used for XSRF protection when adding external logins
		private const string XsrfKey = "XsrfId";

		private async Task SendEmailConfirmationCode(string userId)
		{
			// Send an email with this link
			string code = await UserManager.GenerateEmailConfirmationTokenAsync(userId);

			var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = userId, code = code }, protocol: Request.Url.Scheme);

			StoreFront storeFront = CurrentStoreFrontOrNull;
			string subject = Models.Extensions.StoreFrontExtensions.EmailConfirmationCodeSubject(storeFront, callbackUrl, Request.Url);
			string messageHtml = Models.Extensions.StoreFrontExtensions.EmailConfirmationCodeMessageHtml(storeFront, callbackUrl, Request.Url);

			await UserManager.SendEmailAsync(userId, subject, messageHtml);
		}


		private IAuthenticationManager AuthenticationManager
		{
			get
			{
				return HttpContext.GetOwinContext().Authentication;
			}
		}

		private async Task SignInAsync(Identity.AspNetIdentityUser user, bool isPersistent)
		{
			AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie, DefaultAuthenticationTypes.TwoFactorCookie);
			AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = isPersistent }, await user.GenerateUserIdentityAsync(UserManager));
		}

		private void AddErrors(IdentityResult result)
		{
			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", error);
			}
		}

		private bool HasPassword()
		{
			var user = UserManager.FindById(User.Identity.GetUserId());
			if (user != null)
			{
				return user.PasswordHash != null;
			}
			return false;
		}

		private bool HasPhoneNumber()
		{
			var user = UserManager.FindById(User.Identity.GetUserId());
			if (user != null)
			{
				return user.PhoneNumber != null;
			}
			return false;
		}

		public enum ProfileMessageId
		{
			AddPhoneSuccess,
			ChangePasswordSuccess,
			SetTwoFactorSuccess,
			SetPasswordSuccess,
			RemoveLoginSuccess,
			RemovePhoneSuccess,
			Error
		}

		#endregion
	}
}