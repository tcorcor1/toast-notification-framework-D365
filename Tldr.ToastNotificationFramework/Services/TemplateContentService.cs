using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Tldr.ToastNotificationFramework
{
	internal class TemplateContentService
	{
		private ExecutionContext _context;
		private IOrganizationService _service;
		private Entity _toastNotification;
		private string _targetLogicalName;
		private Guid _targetId;
		private Entity _targetEtn;

		public List<TemplateContentItem> TemplateContentItems { get; set; }

		public TemplateContentService (Entity toastNotification, ExecutionContext context)
		{
			_context = context;
			_service = context.Service;
			_toastNotification = toastNotification;
			_targetLogicalName = (string)toastNotification.Attributes["yyz_sdksteptargetentity"];
			_targetId = context.Target.Id;
			_targetEtn = _service.Retrieve(_targetLogicalName, _targetId, new ColumnSet(true));
		}

		public void ProcessTemplateContentItems ()
		{
			TemplateContentItems.ForEach((item) =>
			{
				var dynamicsSchemaName = Regex.Replace(item.TemplateValue, "{{|}}", "");

				var hasDynamicsValue = _targetEtn.Attributes.TryGetValue(dynamicsSchemaName, out object value);

				if (hasDynamicsValue)
				{
					var attributeType = value.GetType().Name;

					switch (attributeType)
					{
						case "OptionSetValue":
							item.DynamicsValue = _targetEtn.FormattedValues[dynamicsSchemaName].ToString();
							break;

						case "EntityReference":
							item.DynamicsValue = _targetEtn.FormattedValues[dynamicsSchemaName].ToString();
							break;

						case "Decimal":
							item.DynamicsValue = _targetEtn.FormattedValues[dynamicsSchemaName].ToString();
							break;

						case "Money":
							item.DynamicsValue = _targetEtn.FormattedValues[$"{dynamicsSchemaName}_base"];
							break;

						default:
							item.DynamicsValue = value.ToString();
							break;
					}
				}
				else
				{
					item.DynamicsValue = item.TemplateValue;
				}
			});
		}

		public IEnumerable<RecipientItem> GetRecipientItems ()
		{
			var queryHelper = new QueryService(_service);

			var toastNotificationRecipientCollection = queryHelper.GetToastNotificationRecipients(_toastNotification.Id, new ColumnSet(new string[] { "statecode", "yyz_toastnotificationrecipienttypecode", "yyz_toastnotificationrecipient" }));

			var recipientList = new List<RecipientItem>();

			foreach (var recipientEtn in toastNotificationRecipientCollection.Entities)
			{
				var recipientTypeCode = recipientEtn.GetAttributeValue<OptionSetValue>("yyz_toastnotificationrecipienttypecode").Value;

				if (recipientTypeCode == (int)ToastNotificationRecipientTypeCode.EMAIL)
				{
					var systemUser = queryHelper.GetSystemUserByEmail(recipientEtn.GetAttributeValue<string>("yyz_toastnotificationrecipient"));

					if (systemUser == null) continue;

					var recipientItem = new RecipientItem((Guid)systemUser.Attributes["systemuserid"], recipientEtn.GetAttributeValue<string>("yyz_toastnotificationrecipient"));

					recipientList.Add(recipientItem);
				}
				else if (recipientTypeCode == (int)ToastNotificationRecipientTypeCode.LOOKUP)
				{
					var hasSystemUserId = _targetEtn.Attributes.TryGetValue((string)recipientEtn.Attributes["yyz_toastnotificationrecipient"], out object systemUserEntityReference);

					if (hasSystemUserId)
					{
						var systemUser = queryHelper.GetSystemUserById(((EntityReference)systemUserEntityReference).Id);

						var recipientItem = new RecipientItem((Guid)systemUser.Attributes["systemuserid"], (string)systemUser.Attributes["internalemailaddress"]);

						recipientList.Add(recipientItem);
					}
				}
				else if (recipientTypeCode == (int)ToastNotificationRecipientTypeCode.RELATEDENTITYLOOKUP)
				{
					try
					{
						var recipientPointer = (string)recipientEtn.Attributes["yyz_toastnotificationrecipient"];
						var recipientPointerItems = recipientPointer.Split('.');

						var hasRelatedEtnAttr = _context.PostImage.Attributes.TryGetValue(recipientPointerItems[0], out object relatedEtnValue);

						if (hasRelatedEtnAttr)
						{
							var targetEtnLogicalName = ((EntityReference)relatedEtnValue).LogicalName; //incident

							var relEtnRequest = new RetrieveEntityRequest()
							{
								LogicalName = targetEtnLogicalName,
								EntityFilters = EntityFilters.Entity
							};

							var relEtnResponse = (RetrieveEntityResponse)_service.Execute(relEtnRequest);
							var relEtnPrimaryIdAttributeName = relEtnResponse.EntityMetadata.PrimaryIdAttribute; //incidentid

							var connectedFromPrimaryAttr = recipientPointerItems[0];
							var relEtnAttributeName = recipientPointerItems[1];

							var toastTargetEtn = _context.Target.LogicalName;

							var fetchXml = string.Format(@"
							<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' aggregate='true'>
							  <entity name='{0}'>
								<filter type='and'>
								  <condition attribute='{3}' operator='eq' uitype='{1}' value='{5}' />
								</filter>
								<link-entity name='{1}' from='{2}' to='{3}' link-type='inner' alias='alias_{3}'>
								  <attribute alias='recipientid' groupby='true' name='{4}' />
								</link-entity>
							  </entity>
							</fetch>", toastTargetEtn, targetEtnLogicalName, relEtnPrimaryIdAttributeName, connectedFromPrimaryAttr, relEtnAttributeName, ((EntityReference)relatedEtnValue).Id);

							var relRecipientRes = _service.RetrieveMultiple(new FetchExpression(fetchXml));

							var systemUserId = (((AliasedValue)relRecipientRes.Entities.First().Attributes["recipientid"]).Value as EntityReference).Id;

							var systemUser = queryHelper.GetSystemUserById(systemUserId);

							var recipientItem = new RecipientItem((Guid)systemUser.Attributes["systemuserid"], (string)systemUser.Attributes["internalemailaddress"]);

							recipientList.Add(recipientItem);
						}
					}
					catch (Exception e)
					{
						_context.TracingService.Trace($"Error accessing related recipient.\nError message: {e.Message}\nToast Notification: {_toastNotification.Attributes["yyz_name"]}");
					}
				}
				else if (recipientTypeCode == (int)ToastNotificationRecipientTypeCode.TEAM)
				{
					var teamId = (string)recipientEtn.Attributes["yyz_toastnotificationrecipient"];

					var fetchXml = string.Format(@"
					<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
					  <entity name='systemuser'>
						<attribute name='systemuserid' />
						<attribute name='internalemailaddress' />
						<link-entity name='teammembership' from='systemuserid' to='systemuserid' intersect='true'>
						  <link-entity name='team' from='teamid' to='teamid' alias='alias_teamid'>
							<filter>
							  <condition attribute='teamid' operator='eq' value='{0}' />
							</filter>
						  </link-entity>
						</link-entity>
					  </entity>
					</fetch>
					", teamId);

					var teamMemberRes = _service.RetrieveMultiple(new FetchExpression(fetchXml));

					foreach (var user in teamMemberRes.Entities)
					{
						var recipientItem = new RecipientItem((Guid)user.Attributes["systemuserid"], (string)user.Attributes["internalemailaddress"]);

						recipientList.Add(recipientItem);
					}
				}
			}

			return recipientList.Distinct();
		}

		public IEnumerable<Guid> GetRecipientIds ()
		{
			var queryService = new QueryService(_service);

			var toastNotificationRecipientCollection = queryService.GetToastNotificationRecipients(_toastNotification.Id, new ColumnSet(new string[] { "statecode", "yyz_toastnotificationrecipienttypecode", "yyz_toastnotificationrecipient" }));

			var recipientList = new List<Guid>();

			foreach (var recipientEtn in toastNotificationRecipientCollection.Entities)
			{
				if (recipientEtn.GetAttributeValue<OptionSetValue>("yyz_toastnotificationrecipienttypecode").Value == (int)ToastNotificationRecipientTypeCode.EMAIL)
				{
					var systemUser = queryService.GetSystemUserByEmail(recipientEtn.GetAttributeValue<string>("yyz_toastnotificationrecipient"));

					if (systemUser == null) continue;

					recipientList.Add((Guid)systemUser.Attributes["systemuserid"]);
				}
				else if (recipientEtn.GetAttributeValue<OptionSetValue>("yyz_toastnotificationrecipienttypecode").Value == (int)ToastNotificationRecipientTypeCode.LOOKUP)
				{
					var hasSystemUserId = _targetEtn.Attributes.TryGetValue((string)recipientEtn.Attributes["yyz_toastnotificationrecipient"], out object systemUserEntityReference);

					if (hasSystemUserId)
					{
						recipientList.Add(((EntityReference)systemUserEntityReference).Id);
					}
				}
			}

			return recipientList.Distinct();
		}

		public Entity GetUrlAction ()
		{
			return new Entity()
			{
				Attributes =
					{
						["actions"] = new EntityCollection ()
						{
							Entities =
							{
								new Entity ()
								{
									Attributes =
									{
										["title"] = "View record",
										["data"] = new Entity ()
										{
											Attributes =
											{
												["type"] = "url",
												["navigationTarget"] = "newWindow",
												["url"] = $"?pagetype=entityrecord&etn={_toastNotification["yyz_sdksteptargetentity"]}&id={_targetId}"
											}
										}
									}
								}
							}
						}
					}
			};
		}
	}
}