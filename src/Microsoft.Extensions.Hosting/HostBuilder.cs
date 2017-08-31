// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting.Internal;

namespace Microsoft.Extensions.Hosting
{
    public class HostBuilder : IHostBuilder
    {
        private List<Action<IConfigurationBuilder>> _configureHostConfigActions = new List<Action<IConfigurationBuilder>>();
        private List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppConfigActions = new List<Action<HostBuilderContext, IConfigurationBuilder>>();
        private List<Action<HostBuilderContext, IServiceCollection>> _configureServicesActions = new List<Action<HostBuilderContext, IServiceCollection>>();
        private List<object> _configureContainerActions = new List<object>();
        private object _serviceProviderFactory = new DefaultServiceProviderFactory(); // TODO: Validate scopes?
        private bool _hostBuilt;
        private IConfiguration _hostConfiguration;
        private IConfiguration _appConfiguration;
        private HostBuilderContext _hostBuilderContext;
        private IHostingEnvironment _hostingEnvironment;
        private IServiceProvider _appServices;

        public IDictionary<object, object> Properties => new Dictionary<object, object>();

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _configureHostConfigActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureAppConfigActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            _serviceProviderFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _configureContainerActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _configureServicesActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHost Build()
        {
            if (_hostBuilt)
            {
                throw new InvalidOperationException("Build can only be called once.");
            }
            _hostBuilt = true;

            BuildHostConfiguration();
            CreateHostingEnvironment();
            CreateHostBuilderContext();
            BuildAppConfiguration();
            CreateServiceProvider();

            return new Host(_appServices);
        }

        private void BuildHostConfiguration()
        {
            var configBuilder = new ConfigurationBuilder();
            foreach (var buildAction in _configureHostConfigActions)
            {
                buildAction(configBuilder);
            }
            _hostConfiguration = configBuilder.Build();
        }

        private void CreateHostingEnvironment()
        {
            _hostingEnvironment = new HostingEnvironment()
            {
                ApplicationName = _hostConfiguration[HostDefaults.ApplicationKey],
                EnvironmentName = _hostConfiguration[HostDefaults.EnvironmentKey] ?? EnvironmentName.Production,
                ContentRootPath = _hostConfiguration[HostDefaults.ContentRootKey] ?? Directory.GetCurrentDirectory(),
            };
            _hostingEnvironment.ContentRootFileProvider = new PhysicalFileProvider(_hostingEnvironment.ContentRootPath);
        }

        private void CreateHostBuilderContext()
        {
            _hostBuilderContext = new HostBuilderContext(Properties)
            {
                HostingEnvironment = _hostingEnvironment,
                Configuration = _hostConfiguration
            };
        }

        private void BuildAppConfiguration()
        {
            // TODO: Should we chain in the hosting config provider by default, or let them do it manually?
            // Chained config: https://github.com/aspnet/Configuration/issues/630
            var configBuilder = new ConfigurationBuilder();
            foreach (var buildAction in _configureAppConfigActions)
            {
                buildAction(_hostBuilderContext, configBuilder);
            }
            _appConfiguration = configBuilder.Build();
            _hostBuilderContext.Configuration = _appConfiguration;
        }

        private void CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);
            services.AddSingleton(_hostBuilderContext);
            services.AddSingleton(_appConfiguration);
            services.AddSingleton<IApplicationLifetime, ApplicationLifetime>();
            services.AddOptions();
            services.AddLogging();
            
            foreach (var configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(_hostBuilderContext, services);
            }

            // We can't invoke the factory directly because we don't know the genric type at compile time.
            // TContainerBuilder IServiceProviderFactory<TContainerBuilder>.CreateBuilder(IServiceCollection)
            var createBuilderMethod = _serviceProviderFactory.GetType().GetMethod("CreateBuilder");
            var containerBuilder = createBuilderMethod.Invoke(_serviceProviderFactory, new[] { services });

            foreach (var containerAction in _configureContainerActions)
            {
                // TODO: verify that the TContainerBuilder matches the same type from IServiceProviderFactory<TContainerBuilder>.
                //       There's no compile time check for that because they're seperate methods.
                // Action<HostBuilderContext, TContainerBuilder>
                var invokeMethod = containerAction.GetType().GetMethod("Invoke");
                invokeMethod.Invoke(containerAction, new[] { _hostBuilderContext, containerBuilder });
            }

            // IServiceProvider IServiceProviderFactory<TContainerBuilder>.CreateServiceProvider(TContainerBuilder)
            var createServiceProviderMethod = _serviceProviderFactory.GetType().GetMethod("CreateServiceProvider");
            _appServices = (IServiceProvider)createServiceProviderMethod.Invoke(_serviceProviderFactory, new[] { containerBuilder });

            if (_appServices == null)
            {
                throw new InvalidOperationException($"The {_serviceProviderFactory.GetType().Name} returned a null IServiceProvider.");
            }
        }
    }
}
