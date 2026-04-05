using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Web.Tests;

public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;

    public FakeHttpClientFactory(MockHttpMessageHandler mockHttpMessageHandler)
    {
        _mockHttpMessageHandler = mockHttpMessageHandler;
    }

    public HttpClient CreateClient(string name)
    {
        var httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("https://localhost:5001")
        };
        return httpClient;
    }
}