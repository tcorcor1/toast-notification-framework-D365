using System;

namespace Tldr.ToastNotificationFramework
{
	public class EnvironmentVariable
	{
		public string Name;
		public object Value;

		public EnvironmentVariable (string schemaName, object value)
		{
			Name = schemaName;
			Value = value;
		}
	}
}