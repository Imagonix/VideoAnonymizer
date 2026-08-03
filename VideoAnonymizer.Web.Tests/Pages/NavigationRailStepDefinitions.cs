using System.Net;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Reqnroll;
using RichardSzalay.MockHttp;
using VideoAnonymizer.Web.Components;
using VideoAnonymizer.Web.Pages;
using VideoAnonymizer.Web.Services;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;
using VideoAnonymizer.Web.Tests.FakeServices;
using VideoAnonymizer.Web.Tests.TestDoubles;

namespace VideoAnonymizer.Web.Tests.Pages;

[Binding]
public sealed class NavigationRailStepDefinitions
{
    private BunitContext _context = default!;
    private MockHttpMessageHandler _http = default!;
    private FakeJobHubClient _jobHub = default!;
    private IRenderedComponent<Home> _home = default!;
    private AppStateDto _appState = new();

    [BeforeScenario("navigation_rail")]
    public void SetUp()
    {
        _context = new BunitContext();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _http = new MockHttpMessageHandler();
        _jobHub = new FakeJobHubClient();
        _appState = new AppStateDto { ObjectDetectionApiRunning = true };

        _context.Services.AddMudServices();
        _context.Services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(_http));
        _context.Services.AddSingleton<IJobHubClient>(_jobHub);
        _context.Services.AddSingleton<IDownloadService>(new FakeDownloadService());
        _context.Render<MudPopoverProvider>();

        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.Videos}")
            .Respond("application/json", Json(new ApiResponse<List<VideoDto>>
            {
                IsSuccess = true,
                Payload = []
            }));
        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.AppState}")
            .Respond("application/json", Json(_appState));
    }

    [AfterScenario("navigation_rail")]
    public async Task TearDown()
    {
        _http.Dispose();
        await _context.DisposeAsync();
    }

    [Given("the home page is open")]
    public void GivenTheHomePageIsOpen()
    {
        RenderHome();
    }

    [Given("the home page is open with GPU runtime available")]
    public void GivenTheHomePageIsOpenWithGpuRuntimeAvailable()
    {
        _appState = new AppStateDto
        {
            ObjectDetectionApiRunning = true,
            CudaRuntime = new CudaRuntimeStateDto
            {
                CudaExecutionProviderActive = true,
                ActiveProviders = ["CUDAExecutionProvider"]
            }
        };
        RenderHome();
    }

    [Then("there is no standalone {string} title row")]
    public void ThenThereIsNoStandaloneTitleRow(string title)
    {
        _home.Markup.Should().NotContain(title);
        _home.FindAll("h1").Should().BeEmpty();
    }

    [Then("the navigation rail shows an Import icon action with an accessible name")]
    public void ThenTheNavigationRailShowsAnImportIconAction()
    {
        var import = _home.Find("[data-testid='nav-import']");
        import.GetAttribute("aria-label").Should().Be("Import");
        import.GetAttribute("title").Should().Be("Import");
        import.TextContent.Trim().Should().BeEmpty();
    }

    [Then("the navigation rail shows a Review and Export icon action with an accessible name")]
    public void ThenTheNavigationRailShowsAReviewAndExportIconAction()
    {
        var review = _home.Find("[data-testid='nav-review']");
        review.GetAttribute("aria-label").Should().Be("Review & Export");
        review.GetAttribute("title").Should().Be("Review & Export");
        review.TextContent.Trim().Should().BeEmpty();
    }

    [Then("the Import navigation action is the active action")]
    public void ThenTheImportNavigationActionIsTheActiveAction()
    {
        var import = _home.Find("[data-testid='nav-import']");
        import.GetAttribute("data-active").Should().Be("true");
        import.GetAttribute("aria-current").Should().Be("true");
    }

    [When("the reviewer activates the Review and Export navigation action")]
    public async Task WhenTheReviewerActivatesTheReviewAndExportNavigationAction()
    {
        await _home.InvokeAsync(() => _home.Find("[data-testid='nav-review']").Click());
        _home.Render();
    }

    [Then("the Review and Export navigation action is the active action")]
    public void ThenTheReviewAndExportNavigationActionIsTheActiveAction()
    {
        var review = _home.Find("[data-testid='nav-review']");
        review.GetAttribute("data-active").Should().Be("true");
        review.GetAttribute("aria-current").Should().Be("true");
    }

    [Then("the review view is shown")]
    public void ThenTheReviewViewIsShown()
    {
        _home.FindComponent<ReviewExportTab>().Should().NotBeNull();
    }

    [When("the reviewer switches between the Import and Review and Export views")]
    public async Task WhenTheReviewerSwitchesBetweenViews()
    {
        await _home.InvokeAsync(() => _home.Find("[data-testid='nav-review']").Click());
        _home.Render();
        await _home.InvokeAsync(() => _home.Find("[data-testid='nav-import']").Click());
        _home.Render();
        await _home.InvokeAsync(() => _home.Find("[data-testid='nav-review']").Click());
        _home.Render();
    }

    [Then("the content area keeps its fixed rail and does not shift position")]
    public void ThenTheContentAreaKeepsItsFixedRailAndDoesNotShiftPosition()
    {
        var shell = _home.Find("[data-testid='app-shell']");
        shell.GetAttribute("style").Should().Contain("flex-direction: row");

        var rail = _home.Find("[data-testid='nav-rail']");
        rail.GetAttribute("style").Should().Contain("flex: 0 0 auto");

        var content = _home.Find("[data-testid='content-area']");
        content.GetAttribute("style").Should().Contain("flex: 1");

        _home.FindAll("[data-testid='nav-rail']").Should().HaveCount(1);
        _home.FindAll("[data-testid='content-area']").Should().HaveCount(1);
    }

    [Then("the runtime mode indicator is placed at the bottom of the navigation rail")]
    public void ThenTheRuntimeModeIndicatorIsPlacedAtTheBottomOfTheNavigationRail()
    {
        _home.Find("[data-testid='nav-rail'] [data-testid='nav-rail-bottom'] [data-testid='runtime-mode-indicator']")
            .Should().NotBeNull();

        var indicator = _home.Find("[data-testid='runtime-mode-indicator']");
        indicator.TextContent.Trim().Should().Contain("GPU");
    }

    [Then("the runtime mode indicator remains visible at the bottom of the navigation rail")]
    public void ThenTheRuntimeModeIndicatorRemainsVisibleAtTheBottomOfTheNavigationRail()
    {
        _home.WaitForAssertion(() =>
        {
            _home.FindComponent<ReviewExportTab>().Should().NotBeNull();
            _home.Find("[data-testid='nav-rail'] [data-testid='nav-rail-bottom'] [data-testid='runtime-mode-indicator']")
                .Should().NotBeNull();
            _home.Find("[data-testid='runtime-mode-indicator']").TextContent.Trim().Should().Contain("GPU");
        }, TimeSpan.FromSeconds(5));
    }

    private void RenderHome()
    {
        _home = _context.Render<Home>(parameters => parameters
            .Add(p => p.InitialAppState, _appState));
        _home.WaitForAssertion(() =>
            _home.Find("[data-testid='nav-rail']").Should().NotBeNull());
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
