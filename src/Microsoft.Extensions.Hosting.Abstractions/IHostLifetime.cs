// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    public interface IHostLifetime
    {
        void OnStarted(Action<object> callback, object state);

        void OnStopping(Action<object> callback, object state);

        Task StopAsync();
    }
}
