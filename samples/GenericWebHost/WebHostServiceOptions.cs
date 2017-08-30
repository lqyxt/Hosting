using System;
using Microsoft.AspNetCore.Builder;

namespace GenericWebHost
{
    public class WebHostServiceOptions
    {
        public Action<IApplicationBuilder> ConfigureApp { get; internal set; }
    }
}