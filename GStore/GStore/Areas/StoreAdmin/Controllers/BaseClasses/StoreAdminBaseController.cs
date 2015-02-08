﻿using GStore.Areas.StoreAdmin.ViewModels;
using GStore.Controllers.BaseClass;
using GStore.Data;
using GStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GStore.Areas.StoreAdmin.Controllers.BaseClasses
{
	[Identity.AuthorizeGStoreAction(Identity.GStoreAction.Admin_StoreAdminArea)]
	public class StoreAdminBaseController : GStore.Controllers.BaseClass.BaseController
	{
		public StoreAdminBaseController(IGstoreDb dbContext): base(dbContext)
		{
			this._logActionsAsPageViews = false;
			this._throwErrorIfUserProfileNotFound = true;
			this._useInactiveStoreFrontAsActive = false;
			this._useInactiveStoreFrontConfigAsActive = true;
		}

		public StoreAdminBaseController()
		{
			this._logActionsAsPageViews = false;
			this._throwErrorIfUserProfileNotFound = true;
			this._useInactiveStoreFrontAsActive = false;
			this._useInactiveStoreFrontConfigAsActive = true;
		}

		protected override string ThemeFolderName
		{
			get
			{
				return CurrentStoreFrontConfigOrAny.AdminTheme.FolderName;
			}
		}

		public GStore.Areas.StoreAdmin.ViewModels.StoreAdminViewModel StoreAdminViewModel
		{
			get
			{
				return new GStore.Areas.StoreAdmin.ViewModels.StoreAdminViewModel(CurrentStoreFrontConfigOrAny, CurrentUserProfileOrThrow);
			}
		}


	}
}