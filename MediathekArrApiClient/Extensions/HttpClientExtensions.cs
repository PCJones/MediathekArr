using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scrutor;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Add DI capabilities for Scrutor to work with HttpClient
/// </summary>
/// <remarks>https://github.com/khellang/Scrutor/issues/180</remarks>
public static class HttpClientExtensions
{
    /// <summary>
    /// Add as Named <see cref="HttpClient"/>
    /// </summary>
    /// <param name="selector"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IServiceTypeSelector AsHttpClient(this IServiceTypeSelector selector, string name = "")
    {
        var strategy = new NamedHttpClientRegistrationStrategy(name);
        return selector.UsingRegistrationStrategy(strategy);
    }

    /// <summary>
    /// Add as <see cref="HttpClient"/>
    /// </summary>
    /// <param name="selector"></param>
    /// <returns></returns>
    public static IServiceTypeSelector AsHttpClient(this IServiceTypeSelector selector)
    {
        return selector.UsingRegistrationStrategy(HttpClientRegistrationStrategy.Instance);
    }

    /// <summary>
    /// <see cref="RegistrationStrategy"/> for Named <see cref="HttpClient"/>
    /// </summary>
    private class NamedHttpClientRegistrationStrategy(string httpClientName) : RegistrationStrategy
    {
        public string HttpClientName { get; } = httpClientName;
        public static RegistrationStrategy Instance { get; private set; }
        public static RegistrationStrategy GetInstance(string httpClientName)
        {
            Instance ??= new NamedHttpClientRegistrationStrategy(httpClientName);
            return Instance;
        }

        public override void Apply(IServiceCollection services, ServiceDescriptor descriptor)
        {
            Type TInterface = descriptor.ServiceType;
            Type TClass = descriptor.ImplementationType!;
            Type TImplementingClass = typeof(HttpClientFactoryServiceCollectionExtensions);


            /* Get all methods named "AddHttpClient" */
            var methods = TImplementingClass.GetMethods()
                .Where(m => m.Name == "AddHttpClient" && m.IsGenericMethodDefinition)
                .ToArray();

            /* Then get the one that matches the amount of generic arguments we want to provide, 
             * in our case 2: Interface + Implementation AddHttpClient<TInterface,TImplementation>
             * Can be modified to add more parameters, but minimum is two: AddHttpClient(services, name)
             */
            var method = methods.FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                var genericArguments = m.GetGenericArguments();
                return
                    parameters.Length == 2 && genericArguments.Length == 2 &&
                    parameters[0].ParameterType == typeof(IServiceCollection) && parameters[1].ParameterType == typeof(string)
                    ;
            });

            var genericMethod = method.MakeGenericMethod(TInterface, TClass);
            genericMethod.Invoke(services, [services, HttpClientName]);
        }
    }

    /// <summary>
    /// <see cref="RegistrationStrategy"/> for <see cref="HttpClient"/>
    /// </summary>
    private class HttpClientRegistrationStrategy : RegistrationStrategy
    {
        public static readonly RegistrationStrategy Instance = new HttpClientRegistrationStrategy();

        public override void Apply(IServiceCollection services, ServiceDescriptor descriptor)
        {
            Type TInterface = descriptor.ServiceType;
            Type TClass = descriptor.ImplementationType!;
            Type TImplementingClass = typeof(HttpClientFactoryServiceCollectionExtensions);


            /* Get all methods named "AddHttpClient" */
            var methods = TImplementingClass.GetMethods()
                .Where(m => m.Name == "AddHttpClient" && m.IsGenericMethodDefinition)
                .ToArray();

            /* Then get the one that matches the amount of generic arguments we want to provide, 
             * in our case 2: Interface + Implementation AddHttpClient<TInterface,TImplementation>
             * Can be modified to add more parameters, but minimum is one AddHttpClient(services)
             */
            var method = methods.FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                var genericArguments = m.GetGenericArguments();
                return parameters.Length == 1 && genericArguments.Length == 2 && parameters[0].ParameterType == typeof(IServiceCollection);
            });

            var genericMethod = method.MakeGenericMethod(TInterface, TClass);
            genericMethod.Invoke(services, [services]);
        }
    }
}