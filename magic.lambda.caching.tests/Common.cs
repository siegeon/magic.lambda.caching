/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using magic.node;
using magic.node.contracts;
using magic.signals.services;
using magic.signals.contracts;
using magic.lambda.caching.services;
using magic.lambda.caching.contracts;
using magic.node.extensions.hyperlambda;

namespace magic.lambda.caching.tests
{
    public static class Common
    {
        private class RootResolver : IRootResolver
        {
            public string DynamicFiles => AppDomain.CurrentDomain.BaseDirectory;
            public string RootFolder => AppDomain.CurrentDomain.BaseDirectory;

            public string AbsolutePath(string path)
            {
                return DynamicFiles + path.TrimStart(new char[] { '/', '\\' });
            }

            public string RelativePath(string path)
            {
                return path.Substring(DynamicFiles.Length - 1);
            }
        }

        static public Node Evaluate(string hl, bool config = true)
        {
            var signaler = Initialize(config);
            var lambda = HyperlambdaParser.Parse(hl);
            signaler.Signal("eval", lambda);
            return lambda;
        }

        static async public Task<Node> EvaluateAsync(string hl)
        {
            var signaler = Initialize();
            var lambda = HyperlambdaParser.Parse(hl);
            await signaler.SignalAsync("eval", lambda);
            return lambda;
        }

        public static ISignaler Initialize(bool config = true)
        {
            var services = new ServiceCollection();
            var mockConfiguration = new Mock<IMagicConfiguration>();
            mockConfiguration
                .SetupGet(x => x[It.Is<string>(x => x == "magic:caching:expiration")])
                .Returns(() => config ? "5" : null);
            services.AddTransient((svc) => mockConfiguration.Object);
            services.AddTransient<ISignaler, Signaler>();
            services.AddSingleton<IMagicCache, MagicMemoryCache>();
            services.AddTransient<IRootResolver, RootResolver>();
            var types = new SignalsProvider(InstantiateAllTypes<ISlot>(services));
            services.AddTransient<ISignalsProvider>((svc) => types);
            var provider = services.BuildServiceProvider();
            return provider.GetService<ISignaler>();
        }

        #region [ -- Private helper methods -- ]

        static IEnumerable<Type> InstantiateAllTypes<T>(ServiceCollection services) where T : class
        {
            var type = typeof(T);
            var result = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic && !x.FullName.StartsWith("Microsoft", StringComparison.InvariantCulture))
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach (var idx in result)
            {
                services.AddTransient(idx);
            }
            return result;
        }

        #endregion
    }
}
