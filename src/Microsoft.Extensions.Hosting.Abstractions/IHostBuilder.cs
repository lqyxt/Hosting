// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    public interface IHostBuilder
    {
        IHost Build();

        IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate);

        IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate);

        IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate);

        IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate);

        IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory);

        IDictionary<object, object> Properties { get; }
    }
}
