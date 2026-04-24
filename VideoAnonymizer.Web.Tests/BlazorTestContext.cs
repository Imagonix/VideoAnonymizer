using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
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

        protected BlazorTestBase(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        protected virtual void SetupServices() { }

        protected virtual async Task BeforeRender() { }

        [BeforeScenario]
        public virtual async Task Initialize()
        {
            SetupServices();
            Services.AddMudServices();
            await BeforeRender();
            ComponentUnderTest = Render<TComponent>();
            await Task.CompletedTask;
        }
    }
}