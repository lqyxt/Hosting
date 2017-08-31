// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Internal
{
    internal class Host : IHost
    {
        private ILogger<Host> _logger;
        private IHostLifetime _hostLifetime;
        private ApplicationLifetime _applicationLifetime;
        private IEnumerable<IHostedService> _hostedServices;

        internal Host(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            _applicationLifetime = Services.GetRequiredService<IApplicationLifetime>() as ApplicationLifetime;
            _logger = Services.GetRequiredService<ILogger<Host>>();
            _hostLifetime = Services.GetService<IHostLifetime>();
        }

        public IServiceProvider Services { get; }

        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.Starting();
            
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var delayStart = new TaskCompletionSource<object>();
            _hostLifetime?.OnStarted(obj => ((TaskCompletionSource<object>)obj).TrySetResult(null), delayStart);
            _hostLifetime?.OnStopping(obj => (obj as IApplicationLifetime)?.StopApplication(), _applicationLifetime);

            await delayStart.Task; // TODO: Cancelation

            _hostedServices = Services.GetService<IEnumerable<IHostedService>>();

            // TODO: Try/Catch block to stop started services? Or just rely on Dispose for that?
            foreach (var hostedService in _hostedServices)
            {
                // Fire IHostedService.Start
                await hostedService.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            // Fire IApplicationLifetime.Started
            _applicationLifetime?.NotifyStarted();

            _logger.Started();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.Stopping();

            if (_hostedServices == null)
            {
                // Not started. InvalidOperationException?
                return;
            }
            
            // Trigger IApplicationLifetime.ApplicationStopping
            _applicationLifetime?.StopApplication();

            // TODO: Default timeout?
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

            // Fire IApplicationLifetime.Stopped
            _applicationLifetime?.NotifyStopped();
            await _hostLifetime?.StopAsync();

            if (exceptions.Count > 0)
            {
                var ex = new AggregateException("One or more hosted services failed to stop.", exceptions);
                _logger.StoppedWithException(ex);
                throw ex;
            }

            _logger.Stopped();
        }

        public void Dispose()
        {
            (Services as IDisposable)?.Dispose();
        }
    }
}
