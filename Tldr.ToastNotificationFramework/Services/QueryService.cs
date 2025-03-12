using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace Tldr.ToastNotificationFramework
{
	public class QueryService
	{
		private Dictionary<int, string> _sdkMessageTypes = new Dictionary<int, string>()
		{
			{ 214220000, "Create" },
			{ 214220001, "Update"}
		};

		private IOrganizationService _service { get; set; }

		public QueryService (IOrganizationService service)
		{
			_service = service;
		}

		public EntityCollection GetToastNotificationMessages (Guid sdkProcessingStepGuid)
		{
			var toastNotificationCollectionQuery = new QueryExpression("yyz_toastnotificationmessage")
			{
				ColumnSet = new ColumnSet(true)
			};

			var toastNotificationCollectionQueryFilter = new FilterExpression(LogicalOperator.And);
			toastNotificationCollectionQueryFilter.Conditions.AddRange(new[]
			{
				new ConditionExpression("yyz_sdkstepid", ConditionOperator.Equal, sdkProcessingStepGuid),
				new ConditionExpression("statecode", ConditionOperator.Equal, (int)ToastNotificationStateCode.ACTIVE)
			});
			toastNotificationCollectionQuery.Criteria.AddFilter(toastNotificationCollectionQueryFilter);

			return _service.RetrieveMultiple(toastNotificationCollectionQuery);
		}

		public EntityCollection GetToastNotificationRecipients (Guid toastNotificationId, ColumnSet columns)
		{
			var toastNotificationRecipientCollectionQuery = new QueryExpression("yyz_toastnotificationrecipient")
			{
				ColumnSet = columns
			};

			var toastNotificationRecipientCollectionQueryFilter = new FilterExpression(LogicalOperator.And);
			toastNotificationRecipientCollectionQueryFilter.Conditions.AddRange(new[]
			{
				new ConditionExpression("yyz_toastnotificationmessage", ConditionOperator.Equal, toastNotificationId),
				new ConditionExpression("statecode", ConditionOperator.Equal, (int)ToastNotificationStateCode.ACTIVE)
			});
			toastNotificationRecipientCollectionQuery.Criteria.AddFilter(toastNotificationRecipientCollectionQueryFilter);

			return _service.RetrieveMultiple(toastNotificationRecipientCollectionQuery);
		}

		public Entity GetSystemUserById (Guid id)
		{
			var systemUserCollectionQuery = new QueryExpression("systemuser")
			{
				ColumnSet = new ColumnSet(new string[] { "systemuserid", "internalemailaddress" })
			};

			var systemUserCollectionQueryFilter = new FilterExpression(LogicalOperator.And);
			systemUserCollectionQueryFilter.Conditions.AddRange(new[]
			{
				new ConditionExpression("systemuserid", ConditionOperator.Equal, id),
			});
			systemUserCollectionQuery.Criteria.AddFilter(systemUserCollectionQueryFilter);

			return _service.RetrieveMultiple(systemUserCollectionQuery).Entities.First();
		}

		public Entity GetSystemUserByEmail (string email)
		{
			var systemUserCollectionQuery = new QueryExpression("systemuser")
			{
				ColumnSet = new ColumnSet(new string[] { "systemuserid", "internalemailaddress" })
			};

			var systemUserCollectionQueryFilter = new FilterExpression(LogicalOperator.And);
			systemUserCollectionQueryFilter.Conditions.AddRange(new[]
			{
				new ConditionExpression("internalemailaddress", ConditionOperator.Equal, email),
			});
			systemUserCollectionQuery.Criteria.AddFilter(systemUserCollectionQueryFilter);

			return _service.RetrieveMultiple(systemUserCollectionQuery).Entities.First();
		}

		public Entity GetSdkMessage (int sdkMessageTypeCode)
		{
			var qry = new QueryExpression()
			{
				ColumnSet = new ColumnSet(new string[] { "name", "sdkmessageid" }),
				EntityName = "sdkmessage"
			};

			var filter = new FilterExpression(LogicalOperator.And);
			filter.AddCondition(new ConditionExpression("name", ConditionOperator.Equal, _sdkMessageTypes[sdkMessageTypeCode]));

			qry.Criteria.AddFilter(filter);

			var sdkMessageCollection = _service.RetrieveMultiple(qry);

			return sdkMessageCollection.Entities.FirstOrDefault();
		}

		public Entity GetSdkMessageFilter (string targetEntityName, Guid sdkMessageId)
		{
			var qry = new QueryExpression()
			{
				ColumnSet = new ColumnSet(new string[] { "primaryobjecttypecode", "sdkmessageid" }),
				EntityName = "sdkmessagefilter"
			};

			var filter = new FilterExpression(LogicalOperator.And);
			filter.AddCondition(new ConditionExpression("primaryobjecttypecode", ConditionOperator.Equal, targetEntityName));
			filter.AddCondition(new ConditionExpression("sdkmessageid", ConditionOperator.Equal, sdkMessageId));

			qry.Criteria.AddFilter(filter);

			var sdkMessageFilterCollection = _service.RetrieveMultiple(qry);

			return sdkMessageFilterCollection.Entities.FirstOrDefault();
		}
	}
}