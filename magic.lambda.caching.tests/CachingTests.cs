/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System;
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
            var lambda = Common.Evaluate(@"
cache.set:foo
   expiration:5
   value:howdy world
cache.get:foo");
            Assert.Equal("howdy world", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheSetGetExpressionValue()
        {
            var lambda = Common.Evaluate(@"
.val:int:5
cache.set:foo
   value:x:@.val
cache.get:foo");
            Assert.Equal(5, lambda.Children.Skip(2).First().Value);
        }

        [Fact]
        public void CacheSetNullifyGet()
        {
            var lambda = Common.Evaluate(@"
cache.set:foo
   expiration:5
   value:howdy world
cache.set:foo
cache.get:foo");
            Assert.Null(lambda.Children.Skip(2).First().Value);
        }

        [Fact]
        public void CacheSetGetConfig()
        {
            var lambda = Common.Evaluate(@"
cache.set:foo
   value:howdy world
cache.get:foo");
            Assert.Equal("howdy world", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheSetGetNoConfig()
        {
            var lambda = Common.Evaluate(@"
cache.set:foo
   value:howdy world
cache.get:foo", false);
            Assert.Equal("howdy world", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheSetExplicitExpiration()
        {
            var lambda = Common.Evaluate(@"
cache.set:foo
   expiration:5
   value:howdy world
cache.get:foo");
            Assert.Equal("howdy world", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheSetNullKey()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"cache.set
   value:howdy world"));
        }

        [Fact]
        public void CacheTryGet()
        {
            var lambda = Common.Evaluate(@"
cache.try-get:foo
   .lambda
      return:Howdy World
cache.get:foo");
            Assert.Equal("Howdy World", lambda.Children.First().Value);
            Assert.Equal("Howdy World", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheTryGetNoConfig()
        {
            var lambda = Common.Evaluate(@"
cache.try-get:foo
   .lambda
      return:Howdy World
cache.get:foo", false);
            Assert.Equal("Howdy World", lambda.Children.First().Value);
            Assert.Equal("Howdy World", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheTryGetExplicitExpiration()
        {
            var lambda = Common.Evaluate(@"
cache.try-get:foo
   expiration:5
   .lambda
      return:Howdy World
cache.get:foo");
            Assert.Equal("Howdy World", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheTryGetNullKey()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"
cache.try-get
   .lambda
      return:Howdy World"));
        }

        [Fact]
        public void CacheTryGetNullLambda()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"cache.try-get:foo"));
        }

        [Fact]
        public void CacheSetClearGet()
        {
            var lambda = Common.Evaluate(@"
cache.set:foo
   value:howdy world
cache.clear
cache.get:foo");
            Assert.Null(lambda.Children.Skip(2).First().Value);
        }

        [Fact]
        public void CacheSetCount()
        {
            var lambda = Common.Evaluate(@"
cache.set:foo
   value:howdy world
cache.count");
            Assert.Equal(1, lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public void CacheSetList()
        {
            var lambda = Common.Evaluate(@"
cache.set:foo
   value:howdy world
cache.list");
            Assert.Single(lambda.Children.Skip(1).First().Children);
            Assert.Equal("foo", lambda.Children.Skip(1).First().Children.First().Children.First(x => x.Name == "key").Value);
            Assert.Equal("howdy world", lambda.Children.Skip(1).First().Children.First().Children.First(x => x.Name == "value").Value);
        }

        [Fact]
        public void CacheTryGetNode()
        {
            var lambda = Common.Evaluate(@"
cache.try-get:foo
   .lambda
      return
         foo:bar
cache.get:foo");
            Assert.Equal(typeof(Node), lambda.Children.Skip(1).First().Value.GetType());
            Assert.Equal("", lambda.Children.Skip(1).First().GetEx<Node>().Name);
            Assert.Null(lambda.Children.Skip(1).First().GetEx<Node>().Value);
            Assert.Equal("foo", lambda.Children.Skip(1).First().GetEx<Node>().Children.First().Name);
            Assert.Equal("bar", lambda.Children.Skip(1).First().GetEx<Node>().Children.First().Value);
        }

        [Fact]
        public async Task CacheTryGetAsync()
        {
            var lambda = await Common.EvaluateAsync(@"
cache.try-get:foo
   .lambda
      return:Howdy World
cache.get:foo");
            Assert.Equal("Howdy World", lambda.Children.First().Value);
            Assert.Equal("Howdy World", lambda.Children.Skip(1).First().Value);
        }

        [Fact]
        public async Task CacheTryGetNodeAsync()
        {
            var lambda = await Common.EvaluateAsync(@"
cache.try-get:foo
   .lambda
      return
         foo:bar
cache.get:foo");
            Assert.Equal(typeof(Node), lambda.Children.Skip(1).First().Value.GetType());
            Assert.Equal("", lambda.Children.Skip(1).First().GetEx<Node>().Name);
            Assert.Null(lambda.Children.Skip(1).First().GetEx<Node>().Value);
            Assert.Equal("foo", lambda.Children.Skip(1).First().GetEx<Node>().Children.First().Name);
            Assert.Equal("bar", lambda.Children.Skip(1).First().GetEx<Node>().Children.First().Value);
        }
    }
}
