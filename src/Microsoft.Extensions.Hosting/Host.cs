// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting
{
    internal class Host : IHost
    {
        private ILogger<Host> _logger;
        private IEnumerable<IHostedService> _hostedServices;

        internal Host(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceProvider Services { get; }

        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger = Services.GetRequiredService<ILogger<Host>>();
            // _logger.Starting();
            
            // _applicationLifetime = _applicationServices.GetRequiredService<IApplicationLifetime>() as ApplicationLifetime;

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _hostedServices = Services.GetService<IEnumerable<IHostedService>>();

            // TODO: Catch exceptions and stop started services? Or just rely on Dispose for that?
            foreach (var hostedService in _hostedServices)
            {
                // Fire IHostedService.Start
                await hostedService.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            // Fire IApplicationLifetime.Started
            // _applicationLifetime?.NotifyStarted();

            // _logger.Started();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_hostedServices == null)
            {
                // Not started. InvalidOperationException?
                return;
            }

            IList<Exception> exceptions = new List<Exception>();
            foreach (var hostedService in _hostedServices.Reverse())
            {
                try
                {
                    await hostedService.StopAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException("One or more hosted services failed to stop.", exceptions);
            }
        }

        public void Dispose()
        {
            try
            {
                // TODO: Should we bother? or should IHostedService just be IDisposable?
                var cts = new CancellationTokenSource();
                cts.Cancel();
                StopAsync(cts.Token).GetAwaiter().GetResult();
            }
            finally
            {
                if (Services is IDisposable disposableServices)
                {
                    disposableServices.Dispose();
                }
            }
        }
    }
}
