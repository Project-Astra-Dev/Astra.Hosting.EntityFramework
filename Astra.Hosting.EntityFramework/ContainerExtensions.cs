using Astra.Hosting.Autofac;
using Autofac;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace Astra.Hosting.EntityFramework;

public static class ContainerExtensions
{
    public static ContainerBuilder AddDbContext<TContextInterface, TContextImplementation>(
            this ContainerBuilder builder, 
            [Optional] Action<DbContext>? optionsAction
        )
        where TContextImplementation : DbContext, TContextInterface
        where TContextInterface : notnull
    {
        return builder.AddTransient<TContextInterface, TContextImplementation>(options =>
        {
            options.Database.EnsureCreated();
            optionsAction?.Invoke(options);
        }, options => options.Dispose());
    }
}