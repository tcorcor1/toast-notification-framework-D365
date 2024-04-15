﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

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
				var targetEntityId = context.Target.Id;
				var toastMessageTargetEntity = (string)toastNotification.Attributes["yyz_sdksteptargetentity"];
				var hasToastNotificationTriggerFilter = toastNotification.Attributes.TryGetValue("yyz_sdksteptriggerfilter", out object sdkStepTriggerFilter);

				if (hasToastNotificationTriggerFilter)
				{
					var etnRequest = new RetrieveEntityRequest()
					{
						LogicalName = toastMessageTargetEntity,
						EntityFilters = EntityFilters.Entity
					};

					var etnResponse = (RetrieveEntityResponse)context.Service.Execute(etnRequest);

					var targetPrimaryIdAttributeName = etnResponse.EntityMetadata.PrimaryIdAttribute;

					var fetchXml = string.Format(
					@"<fetch>
                      <entity name='{0}'>
                        <all-attributes />
                        <filter type='and'>
                          <condition attribute='{1}' operator='eq'  value='{2}' />
                        </filter>
						{3}
                      </entity>
                    </fetch>", toastMessageTargetEntity, targetPrimaryIdAttributeName, targetEntityId, sdkStepTriggerFilter);

					var filterEtnCollection = context.Service.RetrieveMultiple(new FetchExpression(fetchXml));

					if (filterEtnCollection.Entities.Count == 0)
						return;
				}

				var toastMessageBody = (string)toastNotification["yyz_toastnotificationbody"];

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
							["IconType"] = (OptionSetValue)toastNotification["yyz_toastnotificationiconcode"],
							["Priority"] = (OptionSetValue)toastNotification["yyz_toastnotificationprioritycode"],
							["ToastType"] = new OptionSetValue((int)ToastNotificationBehavior.TIMED),
							["Expiry"] = 2592000,
							["OverrideContent"] = new Entity()
							{
								Attributes =
								{
									["body"] = toastMessageBody,
									["title"] = toastNotification["yyz_toastnotificationtitle"]
								}
							}
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