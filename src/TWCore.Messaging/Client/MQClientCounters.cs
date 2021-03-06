﻿/*
Copyright 2015-2018 Daniel Adrian Redondo Suarez

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TWCore.Diagnostics.Status;
// ReSharper disable NotAccessedField.Local
// ReSharper disable InconsistentNaming

namespace TWCore.Messaging.Client
{
	/// <summary>
	/// Message queue client counters
	/// </summary>
	[StatusName("Counters")]
	public class MQClientCounters
	{
		private Timer _timerTen;
		private Timer _timerThirty;
		private Timer _timerHour;

		#region Properties
		/// <summary>
		/// Number of messages Sent
		/// </summary>
		public long MessagesSent { get; private set; }
		/// <summary>
		/// Number of messages sent in the last ten minutes
		/// </summary>
		public long LastTenMinutesMessagesSent { get; private set; }
		/// <summary>
		/// Number of messages sent in the last thirty minutes
		/// </summary>
		public long LastThirtyMinutesMessagesSent { get; private set; }
		/// <summary>
		/// Number of messages sent in the last hour
		/// </summary>
		public long LastHourMessagesSent { get; private set; }

		/// <summary>
		/// Number of messages received
		/// </summary>
		public long MessagesReceived { get; private set; }
		/// <summary>
		/// Number of messages received in the last ten minutes
		/// </summary>
		public long LastTenMinutesMessagesReceived { get; private set; }
		/// <summary>
		/// Number of messages received in the last thirty minutes
		/// </summary>
		public long LastThirtyMinutesMessagesReceived { get; private set; }
		/// <summary>
		/// Number of messages received in the last hour
		/// </summary>
		public long LastHourMessagesReceived { get; private set; }

		/// <summary>
		/// Total network time
		/// </summary>
		public double TotalNetworkTime { get; private set; }
		/// <summary>
		/// Total receiving time
		/// </summary>
		public double TotalReceivingTime { get; private set; }
		#endregion

		#region .ctor
		/// <summary>
		/// Message queue server counters
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MQClientCounters()
		{
			_timerTen = new Timer(state =>
			{
				LastTenMinutesMessagesSent = 0;
				LastTenMinutesMessagesReceived = 0;
			}, this, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

			_timerThirty = new Timer(state =>
			{
				LastThirtyMinutesMessagesSent = 0;
				LastThirtyMinutesMessagesReceived = 0;
			}, this, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

			_timerHour = new Timer(state =>
			{
				LastHourMessagesSent = 0;
				LastHourMessagesReceived = 0;
			}, this, TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(60));

			Core.Status.Attach(collection =>
			{
				collection.Add("Number of messages sent",
					new StatusItemValueItem("Last 10 Minutes", LastTenMinutesMessagesSent, true),
					new StatusItemValueItem("Last 30 Minutes", LastThirtyMinutesMessagesSent, true),
					new StatusItemValueItem("Last Hour", LastHourMessagesSent, true),
					new StatusItemValueItem("Total", MessagesSent, true));

				collection.Add("Number of messages received",
					new StatusItemValueItem("Last 10 Minutes", LastTenMinutesMessagesReceived, true),
					new StatusItemValueItem("Last 30 Minutes", LastThirtyMinutesMessagesReceived, true),
					new StatusItemValueItem("Last Hour", LastHourMessagesReceived, true),
					new StatusItemValueItem("Total", MessagesReceived, true));

				collection.Add("Total Time",
					new StatusItemValueItem("Network Time (ms)", TimeSpan.FromMilliseconds(TotalNetworkTime), true),
					new StatusItemValueItem("Receiving Time (ms)", TimeSpan.FromMilliseconds(TotalReceivingTime), true));
			});
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Increments the total network time
		/// </summary>
		/// <param name="increment">Increment value</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IncrementTotalNetworkTime(TimeSpan increment)
		{
			TotalNetworkTime += increment.TotalMilliseconds;
		}
		/// <summary>
		/// Increments the receiving time
		/// </summary>
		/// <param name="increment">Increment value</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IncrementReceivingTime(TimeSpan increment)
		{
			TotalReceivingTime += increment.TotalMilliseconds;
		}
		/// <summary>
		/// Increments the messages sent
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IncrementMessagesSent()
		{
			MessagesSent++;
			LastTenMinutesMessagesSent++;
			LastThirtyMinutesMessagesSent++;
			LastHourMessagesSent++;
		}
		/// <summary>
		/// Increment the message received
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IncrementMessagesReceived()
		{
			MessagesReceived++;
			LastTenMinutesMessagesReceived++;
			LastThirtyMinutesMessagesReceived++;
			LastHourMessagesReceived++;
		}
		#endregion
	}
}
