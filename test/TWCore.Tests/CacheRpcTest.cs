using System;
using System.Threading.Tasks;
using TWCore.Cache;
using TWCore.Cache.Client;
using TWCore.Cache.Storages;
using TWCore.Cache.Storages.IO;
using TWCore.Net.RPC.Client.Transports.Default;
using TWCore.Net.RPC.Client.Transports.TW;
using TWCore.Net.RPC.Server.Transports;
using TWCore.Net.RPC.Server.Transports.Default;
using TWCore.Net.RPC.Server.Transports.TW;
using TWCore.Serialization;
using TWCore.Serialization.WSerializer;
using TWCore.Services;
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UnusedMember.Global

namespace TWCore.Tests
{
    /// <inheritdoc />
    public class CacheRpcTest : ContainerParameterServiceAsync
    {
        private static ISerializer GlobalSerializer = new WBinarySerializer();

        public CacheRpcTest() : base("cacherpcTest", "Cache Test") { }
        protected override async Task OnHandlerAsync(ParameterHandlerInfo info)
        {
            Core.Log.Warning("Starting CACHE TEST");

            var cacheService = new TestCacheService();
            cacheService.OnStart(null);

			using (var cachePool = new CacheClientPool { Serializer = GlobalSerializer })
            {
				var cacheClient = await CacheClientProxy.GetClientAsync(new DefaultTransportClient("127.0.0.1", 20051, 3, GlobalSerializer)).ConfigureAwait(false);
                cachePool.Add("localhost:20051", cacheClient, StorageItemMode.ReadAndWrite);

	            try
	            {
	                for (var i = 0; i < 15; i++)
			            cachePool.GetKeys();

		            Console.ReadLine();

	                for (var i = 0; i < 100; i++)
		            {
			            var key = "test-" + i;
			            cachePool.Get(key);
			            cachePool.Set(key, "bla bla bla bla bla");
		            }
		            Console.ReadLine();
		            for (var i = 0; i < 100; i++)
		            {
			            var key = "test-" + i;
			            cachePool.Get(key);
			            cachePool.Set(key, "bla bla bla bla bla");
		            }
	            }
	            catch (Exception ex)
	            {
		            Core.Log.Write(ex);
	            }
            }
        }

        /// <inheritdoc />
        private class TestCacheService : CacheService
        {
            protected override StorageManager GetManager()
            {
				var fileSto = new FileStorage("./cache_data")
				{
					NumberOfSubFolders = 10
				};
				var lruSto = new LRU2QStorage(10000);
                var stoManager = new StorageManager();
                stoManager.Push(fileSto);
                stoManager.Push(lruSto);
                return stoManager;
            }
            protected override ITransportServer[] GetTransports()
            {
				return new ITransportServer[] { new DefaultTransportServer(20051, GlobalSerializer) };
            }
        }
    }
}