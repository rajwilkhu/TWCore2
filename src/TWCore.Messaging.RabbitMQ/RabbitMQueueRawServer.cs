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
using System.Threading.Tasks;
using RabbitMQ.Client;
using TWCore.Messaging.Configuration;
using TWCore.Messaging.RawServer;
using TWCore.Threading;

namespace TWCore.Messaging.RabbitMQ
{
	/// <inheritdoc />
	/// <summary>
	/// RabbitMQ Server Implementation
	/// </summary>
	public class RabbitMQueueRawServer : MQueueRawServerBase
	{
		private readonly NonBlocking.ConcurrentDictionary<string, RabbitMQueue> _rQueue = new NonBlocking.ConcurrentDictionary<string, RabbitMQueue>();
		private byte _priority;
		private byte _deliveryMode;
		private string _expiration;
		private string _label;

		/// <inheritdoc />
		/// <summary>
		/// On client initialization
		/// </summary>
		protected override void OnInit()
		{
			var senderOptions = Config.ResponseOptions.ServerSenderOptions;
			if (senderOptions == null)
				throw new ArgumentNullException("ServerSenderOptions");
			_priority = (byte)(senderOptions.MessagePriority == MQMessagePriority.High ? 9 :
							senderOptions.MessagePriority == MQMessagePriority.Low ? 1 : 5);
			_expiration = (senderOptions.MessageExpirationInSec * 1000).ToString();
			_deliveryMode = (byte)(senderOptions.Recoverable ? 2 : 1);
			_label = senderOptions.Label;
		}

		/// <inheritdoc />
		/// <summary>
		/// On Create all server listeners
		/// </summary>
		/// <param name="connection">Queue server listener</param>
		/// <param name="responseServer">true if the server is going to act as a response server</param>
		/// <returns>IMQueueServerListener</returns>
		protected override IMQueueRawServerListener OnCreateQueueServerListener(MQConnection connection, bool responseServer = false)
			=> new RabbitMQueueRawServerListener(connection, this, responseServer);

		/// <inheritdoc />
		/// <summary>
		/// On Send message data
		/// </summary>
		/// <param name="message">Response message instance</param>
		/// <param name="e">RawRequest received event args</param>
		protected override Task<int> OnSendAsync(SubArray<byte> message, RawRequestReceivedEventArgs e)
		{
			var queues = e.ResponseQueues;
			queues.Add(new MQConnection
			{
				Route = e.Sender.Route,
				Parameters = e.Sender.Parameters
			});

			var crId = e.CorrelationId.ToString();
			var replyTo = e.Metadata["ReplyTo"];

			var response = true;
			foreach (var queue in queues)
			{
				try
				{
					var rabbitQueue = _rQueue.GetOrAdd(queue.Route, q =>
					{
						var rq = new RabbitMQueue(queue);
						rq.EnsureConnection();
						return rq;
					});
					if (!rabbitQueue.EnsureConnection()) continue;
					rabbitQueue.EnsureExchange();
					var props = rabbitQueue.Channel.CreateBasicProperties();
					props.CorrelationId = crId;
					props.Priority = _priority;
					props.Expiration = _expiration;
					props.AppId = Core.ApplicationName;
					props.ContentType = SenderSerializer.MimeTypes[0];
					props.DeliveryMode = _deliveryMode;
					props.Type = _label;
					if (!string.IsNullOrEmpty(replyTo))
					{
						if (string.IsNullOrEmpty(queue.Name))
						{
							Core.Log.LibVerbose("Sending {0} bytes to the Queue '{1}' with CorrelationId={2}", message.Count, rabbitQueue.Route + "/" + replyTo, crId);
							rabbitQueue.Channel.BasicPublish(rabbitQueue.ExchangeName ?? string.Empty, replyTo, props, (byte[])message);
						}
						else if (queue.Name.StartsWith(replyTo, StringComparison.Ordinal))
						{
							Core.Log.LibVerbose("Sending {0} bytes to the Queue '{1}' with CorrelationId={2}", message.Count, rabbitQueue.Route + "/" + queue.Name + "_" + replyTo, crId);
							rabbitQueue.Channel.BasicPublish(rabbitQueue.ExchangeName ?? string.Empty, queue.Name + "_" + replyTo, props, (byte[])message);
						}
					}
					else
					{
						Core.Log.LibVerbose("Sending {0} bytes to the Queue '{1}' with CorrelationId={2}", message.Count, rabbitQueue.Route + "/" + queue.Name, crId);
						rabbitQueue.Channel.BasicPublish(rabbitQueue.ExchangeName ?? string.Empty, queue.Name, props, (byte[])message);
					}
				}
				catch (Exception ex)
				{
					response = false;
					Core.Log.Write(ex);
				}
			}
		    return response ? Task.FromResult(message.Count) : TaskHelper.CompleteValueMinus1;
		}

		/// <inheritdoc />
		/// <summary>
		/// On Dispose
		/// </summary>
		protected override void OnDispose()
		{
			foreach (var queue in _rQueue.Values)
				queue.Close();
			_rQueue.Clear();
		}
	}
}