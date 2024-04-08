using System;

namespace Tldr.ToastNotificationFramework
{
	public class TemplateContentItem
	{
		public TemplateContentItem (string value)
		{
			TemplateValue = value;
		}

		public string TemplateValue { get; set; }
		public string DynamicsValue { get; set; }
	}
}