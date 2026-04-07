using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace VideoAnonymizer.Contracts.Extensions
{
    public static class HostEnvironmentExtensions
    {
        public const string ENVIRONMENT_TEST = "Test";
        public static bool IsTest(this IHostEnvironment environment)
        {
            return environment.IsEnvironment(ENVIRONMENT_TEST);
        }
    }
}
