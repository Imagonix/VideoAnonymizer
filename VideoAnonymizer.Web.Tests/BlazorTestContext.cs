using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Reqnroll;
using RichardSzalay.MockHttp;
using static MudBlazor.Colors;

namespace VideoAnonymizer.Web.Tests
{
    public abstract class BlazorTestBase<TComponent> : BunitContext, IDisposable
        where TComponent : ComponentBase
    {
        protected readonly ScenarioContext _scenarioContext;

        protected IRenderedComponent<TComponent> ComponentUnderTest
        {
            get => _scenarioContext.Get<IRenderedComponent<TComponent>>(nameof(ComponentUnderTest));
            set => _scenarioContext.Set(value, nameof(ComponentUnderTest));
        }

        protected HubConnection HubConnection
        {
            get => _scenarioContext.Get<HubConnection>(nameof(HubConnection));
            set => _scenarioContext.Set(value, nameof(HubConnection));
        }

        protected MockHttpMessageHandler MockHttpMessageHandler
        {
            get => _scenarioContext.Get<MockHttpMessageHandler>(nameof(MockHttpMessageHandler));
            set => _scenarioContext.Set(value, nameof(MockHttpMessageHandler));
        }

        protected BlazorTestBase(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            MockHttpMessageHandler = new MockHttpMessageHandler();
            SetupServices();
        }

        private void SetupServices()
        {
            var httpClient = new HttpClient(MockHttpMessageHandler)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };
            Services.AddSingleton(httpClient);

            var factory = new WebApplicationFactory<Program>();
            SetupMockClient();
            SetupMockClientSignalR(factory);
            var client = factory.CreateClient();
            var hubUrl = new Uri(client.BaseAddress!, "/hubs/jobs");
            HubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler())
                .WithAutomaticReconnect()
                .Build();
            Services.AddSingleton(HubConnection);
        }

        protected virtual void SetupMockClient() { }

        private void SetupMockClientSignalR(WebApplicationFactory<Program> webAppFactory)
        {
            MockHttpMessageHandler.When("*")
            .With(req => req.RequestUri?.PathAndQuery.Contains("/hubs/") == true
                      || req.RequestUri?.PathAndQuery.Contains("negotiate") == true)
            .Respond(async (req) =>
            {
                using var realClient = webAppFactory.CreateClient();
                var realRequest = new HttpRequestMessage
                {
                    Method = req.Method,
                    RequestUri = req.RequestUri,
                    Content = req.Content,
                    Headers = { }
                };

                foreach (var header in req.Headers)
                    realRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

                var response = await realClient.SendAsync(realRequest);
                return response;
            });
        }

        [BeforeScenario]
        public virtual async Task Initialize()
        {
            ComponentUnderTest = Render<TComponent>();
            await HubConnection.StartAsync();
        }

        public new void Dispose()
        {
            MockHttpMessageHandler.Dispose();
            base.Dispose();
        }
    }
}