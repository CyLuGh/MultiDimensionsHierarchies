using FluentAssertions;
using LanguageExt;
using LanguageExt.UnitTesting;
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
    public class SkeletonsAccumulatorTests
    {
        [Fact]
        public void TestHeuristic1Dimension()
        {
            var dimensions = new[] { "Dim A" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var result = Aggregator.DetailedAggregate( Method.BottomTop , skeletons.ToArray() , data => data.Sum( t => t.value ) );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" );

            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
                r.Count.Should().Be( 4 );
            } );
        }

        [Fact]
        public void TestHeuristic2Dimensions()
        {
            var dimensions = new[] { "Dim A" , "Dim B" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var result = Aggregator.DetailedAggregate( Method.BottomTop , skeletons.ToArray() , data => data.Sum( t => t.value ) );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
            } );

            result.Results.Where( s => s.Key.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , 2 ) );
            result.Results.Where( s => s.Key.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );

            var cplx = dimensions.Select( d => SkeletonTests.GetDimension( d ) ).Complexity();
            result.Results.LongCount().Should().Be( cplx );
        }

        [Fact]
        public void TestHeuristic5Dimensions()
        {
            var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var result = Aggregator.DetailedAggregate( Method.BottomTop , skeletons.ToArray() , data => data.Sum( t => t.value ) );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
                r.Count.Should().Be( (int) Math.Pow( 4 , 5 ) );
            } );
            result.Results.Where( s => s.Key.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );

            var cplx = dimensions.Select( d => SkeletonTests.GetDimension( d ) ).Complexity();
            result.Results.LongCount().Should().Be( cplx );
        }

        [Fact]
        public void TestTargeted1Dimension()
        {
            var dimensions = new[] { "Dim A" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var targets = AggregatorTests.GetTargets( dimensions );
            var result = Aggregator.DetailedAggregate( Method.TopDown , skeletons.ToArray() , data => data.Sum( t => t.value ) , targets );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
            } );
            result.Results.Where( s => s.Key.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
            result.Results.Length.Should().Be( targets.Length );
        }

        [Fact]
        public void TestTargeted5DimensionsSubSelect()
        {
            var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var targets = AggregatorTests.GetTargets( dimensions );
            targets = Seq.create(
                targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
                ).Somes();
            var result = Aggregator.DetailedAggregate( Method.TopDown , skeletons.ToArray() , data => data.Sum( t => t.value ) , targets );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
                r.Count.Should().Be( (int) Math.Pow( 4 , 5 ) );
            } );
            result.Results.Length.Should().Be( targets.Length );
        }

        [Fact]
        public void TestHeuristicWeight1Dimension()
        {
            var dimensions = new[] { "Dim A" };
            var skeletons = AggregatorTests.GetLeavesWeightSample( dimensions );
            var result = Aggregator.DetailedAggregate( Method.BottomTop , skeletons , data => data.Sum( t => t.value * t.weight ) );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should()
                    .Be( 23 ) );
            } );
        }

        [Fact]
        public void TestTargetedWeight1Dimension()
        {
            var dimensions = new[] { "Dim A" };
            var skeletons = AggregatorTests.GetLeavesWeightSample( dimensions );
            var targets = AggregatorTests.GetWeightTargets( dimensions ).FindAll( "2" );

            var result = Aggregator.DetailedAggregate( Method.TopDown , skeletons , data => data.Sum( t => t.value * t.weight ) , targets );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.IsSome.Should().BeTrue();
                r.Value.IfSome( v => v.Should()
                    .Be( 23 ) );
            } );
        }

        [Fact]
        public void TestHeuristicWeight2Dimensions()
        {
            var dimensions = new[] { "Dim A" , "Dim B" };
            var skeletons = AggregatorTests.GetLeavesWeightSample( dimensions );
            var result = Aggregator.DetailedAggregate( Method.BottomTop , skeletons , data => data.Sum( t => t.value * t.weight ) );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should()
                    .Be( 207 ) );
            } );
        }

        [Fact]
        public void TestTargetedWeight2Dimensions()
        {
            var dimensions = new[] { "Dim A" , "Dim B" };
            var skeletons = AggregatorTests.GetLeavesWeightSample( dimensions );
            var targets = AggregatorTests.GetWeightTargets( dimensions ).FindAll( "2" , "2" );
            var result = Aggregator.DetailedAggregate( Method.TopDown , skeletons , data => data.Sum( t => t.value * t.weight ) , targets );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should()
                    .Be( 207 ) );
            } );
        }
    }
}