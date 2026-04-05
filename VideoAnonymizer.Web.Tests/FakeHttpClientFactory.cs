using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Web.Tests;

public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _httpClient;

    public FakeHttpClientFactory(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public HttpClient CreateClient(string name)
    {
        return _httpClient;
    }
}