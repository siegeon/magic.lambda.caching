/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Linq;
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
            System.Console.WriteLine(lambda.ToHyperlambda());
            Assert.Equal("howdy world", lambda.Children.Skip(1).First().Value);
        }
    }
}
