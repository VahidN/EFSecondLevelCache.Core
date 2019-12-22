using Xunit;

namespace EFSecondLevelCache.Core.Tests
{
    internal sealed class TestConstants
    {
        public static readonly string Empty = "";
        public static readonly string FooBar = "foobar";
        public static readonly string LoremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.  Ut ornare aliquam mauris, at volutpat massa.  Phasellus pulvinar purus eu venenatis commodo.";
    }

    public class XxHashTests
    {
        [Fact]
        public void TestEmptyXxHashReturnsCorrectValue()
        {
            var hash = XxHashUnsafe.ComputeHash(TestConstants.Empty);
            Assert.Equal((uint)0x02cc5d05, hash);
        }

        [Fact]
        public void TestFooBarXxHashReturnsCorrectValue()
        {
            var hash = XxHashUnsafe.ComputeHash(TestConstants.FooBar);
            Assert.Equal((uint)2348340516, hash);
        }

        [Fact]
        public void TestLoremIpsumXxHashReturnsCorrectValue()
        {
            var hash = XxHashUnsafe.ComputeHash(TestConstants.LoremIpsum);
            Assert.Equal((uint)4046722717, hash);
        }
    }
}