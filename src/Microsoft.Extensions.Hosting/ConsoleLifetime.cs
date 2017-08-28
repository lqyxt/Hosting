// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    public class ConsoleLifetime : IHostLifetime
    {
        public void OnStarted(Action<object> callback, object state)
        {
            // There's no event to wait on started
            callback(state);
        }

        public void OnStopping(Action<object> callback, object state)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                callback(state);
            };
        }

        public Task StopAsync()
        {
            // There's nothing to do here
            return Task.CompletedTask;
        }
    }
}
