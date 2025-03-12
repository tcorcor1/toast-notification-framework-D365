﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tldr.ToastNotificationFramework
{
	public class AsyncHelpers
	{
		/// <summary>
		/// Helper class to run async methods within a sync process.
		/// </summary>
		private readonly TaskFactory _taskFactory = new
			TaskFactory(CancellationToken.None,
						TaskCreationOptions.None,
						TaskContinuationOptions.None,
						TaskScheduler.Default);

		/// <summary>
		/// Executes an async Task method which has a void return value synchronously
		/// USAGE: AsyncUtil.RunSync(() => AsyncMethod());
		/// </summary>
		/// <param name="task">Task method to execute</param>
		public void RunSync (Func<Task> task)
			=> _taskFactory
				.StartNew(task)
				.Unwrap()
				.GetAwaiter()
				.GetResult();

		/// <summary>
		/// Executes an async Task<T> method which has a T return type synchronously
		/// USAGE: T result = AsyncUtil.RunSync(() => AsyncMethod<T>());
		/// </summary>
		/// <typeparam name="TResult">Return Type</typeparam>
		/// <param name="task">Task<T> method to execute</param>
		/// <returns></returns>
		public TResult RunSync<TResult> (Func<Task<TResult>> task)
			=> _taskFactory
				.StartNew(task)
				.Unwrap()
				.GetAwaiter()
				.GetResult();
	}
}