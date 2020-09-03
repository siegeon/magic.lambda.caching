/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Linq;
using System.Threading.Tasks;
using Xunit;
using magic.node;
using magic.node.extensions;

namespace magic.lambda.caching.tests
{
    public class CachingTests
    {
        [Fact]
        public void CacheSetGet()
        {
            var lambda = Common.Evaluate(@"cache.set:foo
   expiration:5
   expiration-type:sliding
   value:howdy world
cache.get:foo");
            Assert.Equal("howdy world", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheSetGetConfig()
        {
            var lambda = Common.Evaluate(@"cache.set:foo
   value:howdy world
cache.get:foo");
            Assert.Equal("howdy world", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheTryGet()
        {
            var lambda = Common.Evaluate(@"cache.try-get:foo
   .lambda
      return:Howdy World
cache.get:foo");
            Assert.Equal("Howdy World", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public async Task CacheTryGetAsync()
        {
            var lambda = await Common.EvaluateAsync(@"wait.cache.try-get:foo
   .lambda
      return:Howdy World
cache.get:foo");
            Assert.Equal("Howdy World", lambda.Children.First().Value);
            Assert.Equal("Howdy World", lambda.Children.Skip(1).First().Value);
        }
    }
}
