using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using System.Text.RegularExpressions;

namespace Tldr.ToastNotificationFramework
{
	internal class TemplateContentService
	{
		private IOrganizationService _service;
		private Entity _toastNotification;
		private string _targetLogicalName;
		private Guid _targetId;
		private Entity _targetEtn;

		public List<TemplateContentItem> TemplateContentItems { get; set; }

		public TemplateContentService (Entity toastNotification, ExecutionContext context)
		{
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
				var templateValuesDeconstructed = item.TemplateValue.Split('.');
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