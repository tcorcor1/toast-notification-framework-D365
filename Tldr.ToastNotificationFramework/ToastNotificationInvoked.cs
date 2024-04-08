using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;

namespace Tldr.ToastNotificationFramework
{
	public class ToastNotificationInvoked : PluginBase
	{
		public override void ExecuteAction (ExecutionContext context)
		{
			var queryService = new QueryService(context.Service);

			var toastNotificationMessageCollection = queryService.GetToastNotificationMessages(context.PluginContext.OwningExtension.Id);

			if (toastNotificationMessageCollection.Entities.Count() == 0) return;

			var regExHandlebars = new Regex("\\{{([^}]+)\\}}");

			foreach (var toastNotification in toastNotificationMessageCollection.Entities)
			{
				string toastMessageBody = (string)toastNotification["yyz_toastnotificationbody"];

				var TemplateContentService = new TemplateContentService(toastNotification, context);

				var regexMatches = regExHandlebars.Matches(toastMessageBody);

				TemplateContentService.TemplateContentItems = regExHandlebars.Matches(toastMessageBody).Cast<Match>()
					.GroupBy(match => match.Value)
					.Select(group => new TemplateContentItem(group.First().Value))
					.ToList();

				if (TemplateContentService.TemplateContentItems.Count() > 0)
				{
					TemplateContentService.ProcessTemplateContentItems();

					TemplateContentService.TemplateContentItems.ForEach((item) =>
					{
						toastMessageBody = Regex.Replace(toastMessageBody, item.TemplateValue, item.DynamicsValue);
					});
				};

				var recipientIdCollection = TemplateContentService.GetRecipientIds();

				foreach (var recipientId in recipientIdCollection)
				{
					var toastNotificationRequest = new OrganizationRequest()
					{
						RequestName = "SendAppNotification",
						Parameters = new ParameterCollection
						{
							["Title"] = toastNotification["yyz_toastnotificationtitle"],
							["Recipient"] = new EntityReference("systemuser", recipientId),
							["Body"] = toastMessageBody,
							["IconType"] = (OptionSetValue)toastNotification["yyz_toastnotificationiconcode"],
							["Priority"] = (OptionSetValue)toastNotification["yyz_toastnotificationprioritycode"],
							["ToastType"] = new OptionSetValue((int)ToastNotificationBehavior.TIMED),
							["Expiry"] = 2592000
						}
					};

					var hasUrlAction = toastNotification.TryGetAttributeValue("yyz_hasurlaction", out object urlActionValue);

					if (hasUrlAction && (bool)urlActionValue)
					{
						toastNotificationRequest.Parameters.Add("Actions", TemplateContentService.GetUrlAction());
					}

					OrganizationResponse toastNotificationResponse = context.Service.Execute(toastNotificationRequest);
				}
			}
		}
	}
}