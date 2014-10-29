﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using GStore;
using GStore.Models;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace GStore.AppHtmlHelpers
{
	public static class AppHtmlHelper
	{

		public static MvcHtmlString ActionSortLink<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string action)
		{

			ModelMetadata metaData = htmlHelper.MetaModel(expression);
			string displayName = htmlHelper.DisplayNameFor(expression).ToString();
			string modelPropertyName = metaData.PropertyName;

			return htmlHelper.ActionSortLink(displayName, action, modelPropertyName);

		}

		public static MvcHtmlString ActionSortLink<TModel, TValue>(this HtmlHelper<System.Collections.Generic.IEnumerable<TModel>> htmlHelper, Expression<Func<TModel, TValue>> expression, string action)
		{
			ModelMetadata metaData = htmlHelper.MetaModel(expression);
			string displayName = htmlHelper.DisplayNameFor(expression).ToString();
			string modelPropertyName = metaData.PropertyName;

			return htmlHelper.ActionSortLink(displayName, action, modelPropertyName);
		}

		public static MvcHtmlString ActionSortLink(this HtmlHelper helper, string linkText, string action, string sortField)
		{

			HttpRequestBase request = helper.ViewContext.HttpContext.Request;

			bool currentFieldIsSorted = false;
			bool linkWillSortUp = true;
			bool? currentSortUp = null;

			if (string.IsNullOrEmpty(request["sortBy"]) || request["sortBy"].ToLower() != sortField.ToLower())
			{
				//fresh sort
				//this field is not part of the sort
			}
			else
			{
				//this field is being sorted

				currentFieldIsSorted = true;
				//no ascending specified, ascending is implied
				if (string.IsNullOrEmpty(request["ascending"]) || request["ascending"].ToLower() == "true")
				{
					currentSortUp = true;
					//re-sorting, reverse order
					linkWillSortUp = false;
				}
				else
				{
					currentSortUp = false;
				}

			}

			string sortClue = string.Empty;
			if (currentFieldIsSorted)
			{
				if (currentSortUp.HasValue && currentSortUp.Value)
				{
					sortClue = " ↑";
				}
				else
				{
					sortClue = " ↓";
				}

			}

			MvcHtmlString returnValue = helper.ActionLink(linkText + (currentFieldIsSorted ? "**" : ""), action, new { sortBy = sortField, ascending = linkWillSortUp });
			if (!string.IsNullOrEmpty(sortClue))
			{
				returnValue = new MvcHtmlString(returnValue.ToHtmlString() + helper.Encode(sortClue));
			}

			return returnValue;
		}

		/// <summary>
		/// Renders alerts from viewbag and tempdata if any exist (messages to user)
		/// </summary>
		/// <returns></returns>
		public static MvcHtmlString RenderUserMessages<TModel>(this HtmlHelper<TModel> htmlHelper)
		{
			StringBuilder returnValue = new StringBuilder();

			List<UserMessage> messages = new List<UserMessage>();
			TempDataDictionary tempData = htmlHelper.ViewContext.TempData;
			if (!tempData.ContainsKey("UserMessages"))
			{
				return new MvcHtmlString("");
			}
			//get user messages ad remove them from the queue
			List<UserMessage> userMessages = (List<UserMessage>)tempData["UserMessages"];
			if (userMessages.Count == 0)
			{
				return new MvcHtmlString("");
			}

			returnValue.AppendLine("<script>");
			foreach (UserMessage userMessage in userMessages)
			{
				returnValue.AppendLine("AddAlert('" + userMessage.Title + "', '" + userMessage.Message + "', '" + userMessage.MessageType.ToString().ToLower() + "');");
			}
			returnValue.AppendLine("</script>");


			//AddBottomAlert();



			return new MvcHtmlString(returnValue.ToString());
		}

		public static MvcHtmlString RenderUserMessagesBottom<TModel>(this HtmlHelper<TModel> htmlHelper)
		{
			StringBuilder returnValue = new StringBuilder();

			List<UserMessage> messages = new List<UserMessage>();
			TempDataDictionary tempData = htmlHelper.ViewContext.TempData;
			if (!tempData.ContainsKey("UserMessagesBottom"))
			{
				return new MvcHtmlString("");
			}
			//get user messages ad remove them from the queue
			List<UserMessage> userMessagesBottom = (List<UserMessage>)tempData["UserMessagesBottom"];
			if (userMessagesBottom.Count == 0)
			{
				return new MvcHtmlString("");
			}

			returnValue.AppendLine("<script>");
			foreach (UserMessage userMessage in userMessagesBottom)
			{
				returnValue.AppendLine("AddBottomAlert('" + userMessage.Title + "', '" + userMessage.Message + "', '" + userMessage.MessageType.ToString().ToLower() + "');");
			}
			returnValue.AppendLine("</script>");

			return new MvcHtmlString(returnValue.ToString());
		}

		public static MvcHtmlString RenderAnnouncements<TModel>(this HtmlHelper<TModel> htmlHelper)
		{
			StringBuilder returnValue = new StringBuilder();

			List<UserMessage> messages = new List<UserMessage>();
			TempDataDictionary tempData = htmlHelper.ViewContext.TempData;
			if (!tempData.ContainsKey("Announcements"))
			{
				return new MvcHtmlString("");
			}
			//get user messages ad remove them from the queue
			List<string> announcements = (List<string>)tempData["Announcements"];
			if (announcements.Count == 0)
			{
				return new MvcHtmlString("");
			}

			returnValue.AppendLine("<script>");
			foreach (string value in announcements)
			{
				returnValue.AppendLine("AddAnnouncement('" + HttpUtility.JavaScriptStringEncode(value) + "');");
			}
			returnValue.AppendLine("</script>");

			return new MvcHtmlString(returnValue.ToString());
		}

		public static MvcHtmlString RenderGaEvents<TModel>(this HtmlHelper<TModel> htmlHelper)
		{
			StringBuilder returnValue = new StringBuilder();

			List<UserMessage> messages = new List<UserMessage>();
			TempDataDictionary tempData = htmlHelper.ViewContext.TempData;
			if (!tempData.ContainsKey("GaEvents"))
			{
				return new MvcHtmlString("");
			}
			//get user messages ad remove them from the queue
			List<GaEvent> gaEvents = (List<GaEvent>)tempData["GaEvents"];
			if (gaEvents.Count == 0)
			{
				return new MvcHtmlString("");
			}

			returnValue.AppendLine("<script>");
			returnValue.AppendLine("$(document).ready(function () {");

			foreach (GaEvent value in gaEvents)
			{
				string category = value.Category;
				string action = value.Action;
				string label = value.Label;

				returnValue.AppendLine("GaEvent("
					+ "'" + HttpUtility.JavaScriptStringEncode(category) + "'"
					+ ",'" + HttpUtility.JavaScriptStringEncode(action) + "'"
					+ ",'" + HttpUtility.JavaScriptStringEncode(label) + "'"
					+ ");");
			}
			returnValue.AppendLine("})");
			returnValue.AppendLine("</script>");



			return new MvcHtmlString(returnValue.ToString());
		}

		/// <summary>
		/// Writes a GaEvent script for an onclick event or anywhere a script is needed to track a google event
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <param name="htmlHelper"></param>
		/// <param name="category"></param>
		/// <param name="action"></param>
		/// <param name="label"></param>
		/// <returns></returns>
		public static MvcHtmlString GaEventScript<TModel>(this HtmlHelper<TModel> htmlHelper, string category, string action, string label)
		{
			string script = "GaEvent("
				+ "'" + HttpUtility.JavaScriptStringEncode(category) + "'"
				+ ",'" + HttpUtility.JavaScriptStringEncode(action) + "'"
				+ ",'" + HttpUtility.JavaScriptStringEncode(label) + "'"
				+ ");";

			return new MvcHtmlString(script);

		}

		public static MvcHtmlString RenderNotificationLink<TModel>(this HtmlHelper<TModel> htmlHelper, Models.NotificationLink link, int counter)
		{
			return new MvcHtmlString(link.NotificationLinkTag(counter));
		}


		public static MvcHtmlString DisplayTextMaxLines<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, int maxLines = 4, int lineLength=40)
		{
			return htmlHelper.DisplayFor(expression, "DisplayTextMaxLines", new { MaxLines = maxLines, LineLength = lineLength} );
		}


		/// <summary>
		/// Simple file dropdown helper, use overload for more options most are optional
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <param name="htmlHelper"></param>
		/// <param name="expression"></param>
		/// <param name="virtualPathDefault"></param>
		/// <returns></returns>
		public static MvcHtmlString FileHelperFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string virtualPathDefault)
		{
			return FileHelperFor(htmlHelper, expression, null, virtualPathDefault, string.Empty, null);
		}

		public static MvcHtmlString FileHelperFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes = null, string virtualPathDefault = "", string virtualPathFieldName = "", IQueryable<string> usedFileQuery = null, string fieldNameSuffix = "_FileHelper")
		{

			string virtualPath = virtualPathDefault;
			if (!string.IsNullOrEmpty(virtualPathFieldName))
			{
				MvcHtmlString virtualPathData = htmlHelper.Value(virtualPathFieldName);
				if (!string.IsNullOrEmpty(virtualPathData.ToString()))
				{
					virtualPath = virtualPathData.ToString();
				}
			}

			string fieldName = htmlHelper.MetaModel(expression).PropertyName;
			string currentValue = string.Empty;

			MvcHtmlString fieldValueData = htmlHelper.Value(fieldName);
			if (!string.IsNullOrEmpty(fieldValueData.ToString()))
			{
				currentValue = fieldValueData.ToString();
			}

			string filePath = htmlHelper.ViewContext.HttpContext.Server.MapPath(virtualPath);

			DirectoryInfo dir = new DirectoryInfo(filePath);
			List<FileInfo> fileInfos = new List<FileInfo>();
			if (!dir.Exists)
			{
				htmlHelper.ViewBag.Message = "Path not found: " + virtualPath;
			}
			else
			{
				fileInfos = dir.GetFiles("*.mp3").ToList();
			}

			string[] existingValues = null;
			if (usedFileQuery != null)
			{
				existingValues = usedFileQuery.ToArray();
			}

			Debug.Print(fileInfos.Count() + " existing files in " + HttpUtility.HtmlEncode(virtualPath));
			List<SelectListItem> selectList = new List<SelectListItem>();

			SelectListGroup group1Selected = new SelectListGroup();
			group1Selected.Name = "--Currently Selected File";
			SelectListGroup group2Unused = new SelectListGroup();
			group2Unused.Name = "--Files Available";
			SelectListGroup group3InUse = new SelectListGroup();
			group3InUse.Name = "--Files That are Already in Use";
			SelectListGroup group4Unknown = new SelectListGroup();
			group4Unknown.Name = "--Unknown status";

			foreach (FileInfo file in fileInfos)
			{
				string text = file.Name;
				string value = file.Name;
				bool isSelected = false;
				SelectListGroup group = group4Unknown;
				if (existingValues != null)
				{
					if (existingValues.Contains(value))
					{
						text += " [in use]";
						group = group3InUse;
					}
					else
					{
						text = " [Unused] " + text;
						group = group2Unused;
					}

				}

				if (file.Name.ToLower() == currentValue.ToLower())
				{
					isSelected = true;
					text = "[selected] " + text;
					group = group1Selected;
				}


				selectList.Add(new SelectListItem() { Text = text, Value = value, Selected = isSelected, Group = group });
			}

			selectList = selectList.OrderBy(sl => (sl.Group.Name)).ToList();

			return htmlHelper.DropDownList(fieldName + fieldNameSuffix, selectList, htmlAttributes: htmlAttributes);

		}

		/// <summary>
		/// Returns a set of meta data about a field in the model, use overload for enum models
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="htmlHelper"></param>
		/// <param name="expression"></param>
		/// <returns></returns>
		public static ModelMetadata MetaModel<TModel, TValue>(
							 this HtmlHelper<TModel> htmlHelper,
							 Expression<Func<TModel, TValue>> expression)
		{
			return ModelMetadata.FromLambdaExpression<TModel, TValue>(expression, htmlHelper.ViewData);
		}

		/// <summary>
		/// Returns a set of meta data about a field in the model, this is for enum models
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="htmlHelper"></param>
		/// <param name="expression"></param>
		/// <returns></returns>
		public static ModelMetadata MetaModel<TModel, TValue>(
							 this HtmlHelper<System.Collections.Generic.IEnumerable<TModel>> htmlHelper,
							 Expression<Func<TModel, TValue>> expression)
		{
			var name = ExpressionHelper.GetExpressionText(expression);
			name = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
			var metadata = ModelMetadataProviders.Current.GetMetadataForProperty(() => Activator.CreateInstance<TModel>(), typeof(TModel), name);
			return metadata;
		}

		public static void SendEmail(Client client, string toEmail, string toName, string subject, string textBody, string htmlBody, string urlHost)
		{
			if (!Properties.Settings.Default.AppEnableEmail)
			{
				return;
			}
			if (!client.UseSendGridEmail)
			{
				return;
			}

			string mailFromEmail = client.SendGridMailFromEmail;
			string mailFromName = client.SendGridMailFromName;

			System.Net.Mail.MailAddress fromAddress = new System.Net.Mail.MailAddress(mailFromEmail, mailFromName);
			System.Net.Mail.MailAddress[] toAddresses = { new System.Net.Mail.MailAddress(toEmail, toName) };

			textBody += "\n\n" + mailFromName
				+ "\n" + mailFromEmail
				+ "\n\nSent from http://" + urlHost;

			htmlBody += "<br/><hr/><br/>" + mailFromName
				+ "<br/>" + mailFromEmail
				+ "<br/><br/>Sent from " + urlHost;

			SendGrid.SendGridMessage myMessage = new SendGrid.SendGridMessage(fromAddress, toAddresses, subject, htmlBody, textBody);

			System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(
				client.SendGridMailAccount,
				client.SendGridMailPassword
					   );

			// Create a Web transport for sending email.
			SendGrid.Web transportWeb = new SendGrid.Web(credentials);
			transportWeb.Deliver(myMessage);
		}

		public static void SendSms(Client client, string toPhoneNumber, string textBody, string urlHost)
		{
			if (!Properties.Settings.Default.AppEnableSMS)
			{
				return;
			}
			if (!client.UseTwilioSms)
			{
				return;
			}


			string smsFromEmail = client.TwilioSmsFromEmail;
			string smsFromName = client.TwilioSmsFromName;

			textBody += "\n\n" + smsFromName
				+ "\n" + smsFromEmail
				+ "\n\nSent from http://" + urlHost;

			Twilio.TwilioRestClient Twilio = new Twilio.TwilioRestClient(
				client.TwilioSid,
				client.TwilioToken
		   );
			var result = Twilio.SendMessage(
				client.TwilioFromPhone,
				toPhoneNumber, 
				textBody
				);

			// Status is one of Queued, Sending, Sent, Failed or null if the number is not valid
			Trace.TraceInformation(result.Status);

		}



	}
}