using FluentAssertions;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            var method = Aggregator.FindBestMethod( skeletons.ToArray() , LanguageExt.HashSet<Skeleton>.Empty );

            method.Should().Be( Method.BottomTopGroupCached );
        }
    }
}