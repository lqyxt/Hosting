// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting
{
    public static class HostingHostBuilderExtensions
    {
        /// <summary>
        /// Specify the environment to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="environment">The environment to host the application in.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseEnvironment(this IHostBuilder hostBuilder, string environment)
        {
            return hostBuilder.ConfigureHostConfiguration(configBuilder =>
            {
                configBuilder.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>(HostDefaults.EnvironmentKey,
                        environment  ?? throw new ArgumentNullException(nameof(environment)))
                });
            });
        }

        /// <summary>
        /// Specify the content root directory to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="contentRoot">Path to root directory of the application.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseContentRoot(this IHostBuilder hostBuilder, string contentRoot)
        {
            return hostBuilder.ConfigureHostConfiguration(configBuilder =>
            {
                configBuilder.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>(HostDefaults.ContentRootKey,
                        contentRoot  ?? throw new ArgumentNullException(nameof(contentRoot)))
                });
            });
        }
    }
}
