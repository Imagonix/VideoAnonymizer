using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using RichardSzalay.MockHttp;
using VideoAnonymizer.Web.Services;
using VideoAnonymizer.Web.Tests.TestDoubles;

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

        protected MockHttpMessageHandler MockHttpMessageHandler
        {
            get => _scenarioContext.Get<MockHttpMessageHandler>(nameof(MockHttpMessageHandler));
            set => _scenarioContext.Set(value, nameof(MockHttpMessageHandler));
        }

        protected FakeJobHubClient JobHubClient
        {
            get => _scenarioContext.Get<FakeJobHubClient>(nameof(JobHubClient));
            set => _scenarioContext.Set(value, nameof(JobHubClient));
        }

        protected BlazorTestBase(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            MockHttpMessageHandler = new MockHttpMessageHandler();
            JobHubClient = new FakeJobHubClient();

            SetupServices();
        }

        private void SetupServices()
        {
            var httpClient = new HttpClient(MockHttpMessageHandler)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };

            Services.AddSingleton(httpClient);
            Services.AddSingleton<IJobHubClient>(JobHubClient);

            SetupMockClient();
        }

        protected virtual void SetupMockClient()
        {
        }

        [BeforeScenario]
        public virtual async Task Initialize()
        {
            ComponentUnderTest = Render<TComponent>();
            await Task.CompletedTask;
        }

        public new void Dispose()
        {
            MockHttpMessageHandler.Dispose();
            base.Dispose();
        }
    }
}