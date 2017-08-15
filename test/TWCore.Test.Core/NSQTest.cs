using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TWCore.Collections;
using TWCore.Messaging.Configuration;
using TWCore.Messaging.NSQ;
using TWCore.Security;
using TWCore.Serialization;
using TWCore.Services;

namespace TWCore.Tests
{
    public class NSQTest : ContainerParameterService
    {
        public NSQTest() : base("nsqtest", "NSQ Test") { }
        protected override void OnHandler(ParameterHandlerInfo info)
        {
            Core.Log.Warning("Starting NSQ Test");

            #region Set Config
            var mqConfig = new MQPairConfig
            {
                Name = "QueueTest",
                Types = new MQObjectTypes { ClientType = typeof(NSQueueClient), ServerType = typeof(NSQueueServer) },
                ClientQueues = new List<MQClientQueues>
                {
                    new MQClientQueues
                    {
                        EnvironmentName = "",
                        MachineName = "",
						SendQueues = new List<MQConnection> { new MQConnection("nsqd=localhost:4150;", "TEST_RQ", null) },
						RecvQueue = new MQConnection("nsqd=localhost:4150;", "TEST_RS", null)
                    }
                },
                ServerQueues = new List<MQServerQueues>
                {
                    new MQServerQueues
                    {
                        EnvironmentName = "",
                        MachineName = "",
						RecvQueues = new List<MQConnection> { new MQConnection("nsqd=localhost:4150;", "TEST_RQ", null) }
                    }
                },
                RequestOptions = new MQRequestOptions
                {
                    SerializerMimeType = SerializerManager.DefaultBinarySerializer.MimeTypes[0],
                    CompressorEncodingType = "gzip",
                    ClientSenderOptions = new MQClientSenderOptions
                    {
                        Label = "TEST REQUEST",
                        MessageExpirationInSec = 30,
                        MessagePriority = MQMessagePriority.Normal,
                        Recoverable = false
                    },
                    ServerReceiverOptions = new MQServerReceiverOptions
                    {
                        MaxSimultaneousMessagesPerQueue = 2000,
                        ProcessingWaitOnFinalizeInSec = 10,
                        SleepOnExceptionInSec = 1000
                    }
                },
                ResponseOptions = new MQResponseOptions
                {
                    SerializerMimeType = SerializerManager.DefaultBinarySerializer.MimeTypes[0],
                    CompressorEncodingType = "gzip",
                    ClientReceiverOptions = new MQClientReceiverOptions(60,
                        new KeyValue<string, string>("SingleResponseQueue", "true")
                    ),
                    ServerSenderOptions = new MQServerSenderOptions
                    {
                        Label = "TEST RESPONSE",
                        MessageExpirationInSec = 30,
                        MessagePriority = MQMessagePriority.Normal,
                        Recoverable = false
                    }
                }
            };
            #endregion

            JsonTextSerializerExtensions.Serializer.Indent = true;

            mqConfig.SerializeToXmlFile("nsqConfig.xml");
            mqConfig.SerializeToJsonFile("nsqConfig.json");

			Core.DebugMode = false;
			Core.Log.MaxLogLevel = Diagnostics.Log.LogLevel.InfoDetail;

            Core.Log.Warning("Starting with Normal Listener and Client");
            NormalTest(mqConfig);
            mqConfig.ResponseOptions.ClientReceiverOptions.Parameters["SingleResponseQueue"] = "true";
        }

        private static void NormalTest(MQPairConfig mqConfig)
        {
            using (var mqServer = mqConfig.GetServer())
            {
                mqServer.RequestReceived += (s, e) =>
                {
                    e.Response.Body = "Bienvenido!!!";
                };
                mqServer.StartListeners();

                using (var mqClient = mqConfig.GetClient())
                {
                    var totalQ = 1000;

                    #region Sync Mode
                    Core.Log.Warning("Sync Mode Test, using Unique Response Queue");
                    using (var w = Watch.Create($"Hello World Example in Sync Mode for {totalQ} times"))
                    {
                        for (var i = 0; i < totalQ; i++)
                        {
                            var response = mqClient.SendAndReceive<string>("Hola mundo");
                        }
                        Core.Log.InfoBasic("Total time: {0}", TimeSpan.FromMilliseconds(w.GlobalElapsedMilliseconds));
                        Core.Log.InfoBasic("Average time in ms: {0}. Press ENTER To Continue.", (w.GlobalElapsedMilliseconds / totalQ));
                    }
                    Console.ReadLine();
                    #endregion

                    #region Parallel Mode
                    Core.Log.Warning("Parallel Mode Test, using Unique Response Queue");
                    using (var w = Watch.Create($"Hello World Example in Parallel Mode for {totalQ} times"))
                    {
                        Parallel.For(0, totalQ, i =>
                        {
                            var response = mqClient.SendAndReceive<string>("Hola mundo");
                        });
                        Core.Log.InfoBasic("Total time: {0}", TimeSpan.FromMilliseconds(w.GlobalElapsedMilliseconds));
                        Core.Log.InfoBasic("Average time in ms: {0}. Press ENTER To Continue.", (w.GlobalElapsedMilliseconds / totalQ));
                    }
                    Console.ReadLine();
                    #endregion
                }

                mqConfig.ResponseOptions.ClientReceiverOptions.Parameters["SingleResponseQueue"] = "false";
                using (var mqClient = mqConfig.GetClient())
                {
                    var totalQ = 1000;

                    #region Sync Mode
                    Core.Log.Warning("Sync Mode Test, using Multiple Response Queue");
                    using (var w = Watch.Create($"Hello World Example in Sync Mode for {totalQ} times"))
                    {
                        for (var i = 0; i < totalQ; i++)
                        {
                            var response = mqClient.SendAndReceive<string>("Hola mundo");
                        }
                        Core.Log.InfoBasic("Total time: {0}", TimeSpan.FromMilliseconds(w.GlobalElapsedMilliseconds));
                        Core.Log.InfoBasic("Average time in ms: {0}. Press ENTER To Continue.", (w.GlobalElapsedMilliseconds / totalQ));
                    }
                    Console.ReadLine();
                    #endregion

                    #region Parallel Mode
                    Core.Log.Warning("Parallel Mode Test, using Multiple Response Queue");
                    using (var w = Watch.Create($"Hello World Example in Parallel Mode for {totalQ} times"))
                    {
                        Parallel.For(0, totalQ, i =>
                        {
                            var response = mqClient.SendAndReceive<string>("Hola mundo");
                        });
                        Core.Log.InfoBasic("Total time: {0}", TimeSpan.FromMilliseconds(w.GlobalElapsedMilliseconds));
                        Core.Log.InfoBasic("Average time in ms: {0}. Press ENTER To Continue.", (w.GlobalElapsedMilliseconds / totalQ));
                    }
                    Console.ReadLine();
                    #endregion
                }
            }
        }

    }
}