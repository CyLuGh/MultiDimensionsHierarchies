using FluentAssertions;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using Xunit;

namespace TestMultiDimensionsHierarchies
{
    public class MethodTests
    {
        [Fact]
        public void TestFindMethod()
        {
            var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );

            var method = Aggregator.FindBestMethod( skeletons.ToSeq() , LanguageExt.HashSet<Skeleton>.Empty );

            method.Should().Be( Method.BottomTopGroupCached );
        }
    }
}