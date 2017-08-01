using System.Threading.Tasks;
using TWCore.Net.HttpServer;
using TWCore.Services;

namespace TWCore.Tests
{
    public class HttpTest : ContainerParameterService
    {
        public HttpTest() : base("httpTest", "Http Test") { }
        protected override void OnHandler(ParameterHandlerInfo info)
        {
            Core.Log.Warning("Starting HTTP TEST");
            info.Service = new TaskService(async (token) =>
            {
                var server = new SimpleHttpServer()
                    .AddGetHandler("/", context =>
                    {
                        context.Response.WriteLine("Hola Mundo");
                    })
                    .AddHttpControllerRoutes<MyController>();
                await server.StartAsync(8085).ConfigureAwait(false);
                Core.Log.InfoBasic("Listening to port 8085");
                await Task.Run(() => token.WaitHandle.WaitOne(), token).ConfigureAwait(false);
                await server.StopAsync().ConfigureAwait(false);
            });
            info.ShouldEndExecution = false;
        }

        class MyController : HttpControllerBase
        {
            [HttpRoute(HttpMethod.GET, "/persona")]
            public SimplePerson GetPersona()
            {
                return new SimplePerson { Firstname = "Nombre", Lastname = "Apellido" };
            }
            [HttpRoute(HttpMethod.GET, "/hola/{name?}")]
            public string Hello(string name)
            {
                return string.IsNullOrEmpty(name) ? "Put a name to say hello" : $"Hello {name}!!!";
            }
        }
    }
}