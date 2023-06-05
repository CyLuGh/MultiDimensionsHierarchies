using FluentAssertions;
using LanguageExt;
using LanguageExt.UnitTesting;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using SampleGenerator;
using System;
using System.Linq;
using Xunit;

namespace TestMultiDimensionsHierarchies
{
    public class SkeletonsAccumulatorTests
    {
        [Fact]
        public void TestBottomTop1Dimension()
        {
            var dimensions = new[] { "Dim A" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var result = Aggregator.DetailedAggregate( skeletons.ToArray() , data => data.Sum( t => t.value ) );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" );

            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
                r.Count.Should().Be( 4 );
            } );
        }

        [Fact]
        public void TestBottomTop2Dimensions()
        {
            var dimensions = new[] { "Dim A" , "Dim B" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var result = Aggregator.DetailedAggregate( skeletons.ToArray() , data => data.Sum( t => t.value ) );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" );
            r2.ShouldBeSome( r => r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) ) );

            result.Results.Where( s => s.Key.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , 2 ) );
            result.Results.Where( s => s.Key.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );

            var cplx = dimensions.Select( d => SkeletonTests.GetDimension( d ) ).Complexity();
            result.Results.LongCount().Should().Be( cplx );
        }

        [Fact]
        public void TestBottomTop5Dimensions()
        {
            var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var result = Aggregator.DetailedAggregate( skeletons.ToArray() , data => data.Sum( t => t.value ) );

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
        public void TestTopDown1Dimension()
        {
            var dimensions = new[] { "Dim A" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var targets = AggregatorTests.GetTargets( dimensions );
            var result = Aggregator.DetailedAggregate( skeletons.ToArray() , targets , data => data.Sum( t => t.value ) , items => items.Sum() , false );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" );
            r2.ShouldBeSome( r => r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) ) );
            result.Results.Where( s => s.Key.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
            result.Results.Length.Should().Be( targets.Length );
        }

        [Fact]
        public void TestTopDown5DimensionsSubSelect()
        {
            var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var targets = AggregatorTests.GetTargets( dimensions );
            targets = Seq.create(
                targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
                ).Somes();
            var result = Aggregator.DetailedAggregate( skeletons.ToArray() , targets , data => data.Sum( t => t.value ) , items => items.Sum() , false );

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
        public void TestTopDownSimplify5DimensionsSubSelect()
        {
            var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var targets = AggregatorTests.GetTargets( dimensions );
            targets = Seq.create(
                targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
                ).Somes();
            var result = Aggregator.DetailedAggregate(
             skeletons.ToArray() ,
             targets ,
             data => data.Sum( t => t.value ) ,
             items => items.Sum() ,
             true ,
             Array.Empty<string>()
            );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
                r.Count.Should().Be( (int) Math.Pow( 4 , 1 ) );
            } );
            result.Results.Length.Should().Be( targets.Length );
        }

        [Fact]
        public void TestTopDownSimplifyPreserve5DimensionsSubSelect()
        {
            var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var targets = AggregatorTests.GetTargets( dimensions );
            targets = Seq.create(
                targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
                ).Somes();
            var result = Aggregator.DetailedAggregate(
                skeletons.ToArray() ,
                targets ,
                data => data.Sum( t => t.value ) ,
                items => items.Sum() ,
                true ,
                new[] { "Dim B" }
            );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
                r.Count.Should().Be( (int) Math.Pow( 4 , 2 ) );
            } );
            result.Results.Length.Should().Be( targets.Length );
        }

        [Fact]
        public void TestTopDownSimplifyPreserveGroup5DimensionsSubSelect()
        {
            var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
            var skeletons = AggregatorTests.GetLeavesSample( dimensions );
            var targets = AggregatorTests.GetTargets( dimensions );
            targets = Seq.create(
                targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
                ).Somes();
            var result = Aggregator.DetailedAggregate( skeletons.ToArray() ,
                targets ,
                data => data.Sum( t => t.value ) ,
                ds => ds.Sum() ,
                true ,
                new[] { "Dim B" , "Dim C" }
            );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should().Be( AggregatorTests.GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
                r.Count.Should().Be( (int) Math.Pow( 4 , 3 ) );
            } );
            result.Results.Length.Should().Be( targets.Length );
        }

        [Fact]
        public void TestBottomTopWeight1Dimension()
        {
            var dimensions = new[] { "Dim A" };
            var skeletons = AggregatorTests.GetLeavesWeightSample( dimensions );
            var result = Aggregator.DetailedAggregate( skeletons , data => data.Sum( t => t.value * t.weight ) );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should()
                    .Be( 23 ) );
            } );
        }

        [Fact]
        public void TestTopDownWeight1Dimension()
        {
            var dimensions = new[] { "Dim A" };
            var skeletons = AggregatorTests.GetLeavesWeightSample( dimensions );
            var targets = AggregatorTests.GetWeightTargets( dimensions ).FindAll( "2" );

            var result = Aggregator.DetailedAggregate( skeletons , targets , data => data.Sum( t => t.value * t.weight ) , ds => ds.Sum() , false );

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
        public void TestBottomTopWeight2Dimensions()
        {
            var dimensions = new[] { "Dim A" , "Dim B" };
            var skeletons = AggregatorTests.GetLeavesWeightSample( dimensions );
            var result = Aggregator.DetailedAggregate( skeletons , data => data.Sum( t => t.value * t.weight ) );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should()
                    .Be( 207 ) );
            } );
        }

        [Fact]
        public void TestTopDownWeight2Dimensions()
        {
            var dimensions = new[] { "Dim A" , "Dim B" };
            var skeletons = AggregatorTests.GetLeavesWeightSample( dimensions );
            var targets = AggregatorTests.GetWeightTargets( dimensions ).FindAll( "2" , "2" );
            var result = Aggregator.DetailedAggregate( skeletons , targets , data => data.Sum( t => t.value * t.weight ) , ds => ds.Sum() , false );

            result.Status.Should().Be( AggregationStatus.OK );
            var r2 = result.Results.Find( "2" , "2" );
            r2.ShouldBeSome( r =>
            {
                r.Value.ShouldBeSome( v => v.Should()
                    .Be( 207 ) );
            } );
        }

        [Fact]
        public void TestStripDimension()
        {
            var generator = new Generator( 1000 , 3 );
            var targets = generator.GenerateTargets( 100 );

            var allDim = DimensionFactory.BuildWithParentLink( "Test" ,
                new[] { ("A", "ALL") , ("B", "ALL") , ("C", "ALL") , ("D", "ALL") } ,
                t => t.Item1 ,
                t => t.Item2 );
            var allDimLeaves = allDim.Leaves().ToArr();
            var allTargets = targets.Select( s => s.Add( allDim.Frame.First() ) );

            var data = generator.Skeletons.Select( ( s , i ) => s.Add( allDimLeaves[i % allDimLeaves.Length] ) ).ToSeq();

            var allResults = Aggregator.DetailedAggregate( data , allTargets , data => data.Sum( t => t.value ) , ds => ds.Sum() , true , new[] { "Test" } )
                    .Results.Map( s => s.Except( "Test" ) );

            var subDim1 = DimensionFactory.BuildWithParentLink( "Test" ,
                new[] { ("A", "ALL") , ("B", "ALL") } ,
                t => t.Item1 ,
                t => t.Item2 );
            var subDim1Leaves = subDim1.Leaves().ToArr();
            var subDim1Targets = targets.Select( s => s.Add( allDim.Frame.First() ) );

            var subData1 = generator.Skeletons.Select( ( s , i ) => (s, i) )
                .Where( t => t.i % allDimLeaves.Length <= 1 )
                .Select( t => t.s.Add( subDim1Leaves[t.i % allDimLeaves.Length] ) ).ToSeq();

            var subDim1Results = Aggregator.DetailedAggregate( subData1 , subDim1Targets , data => data.Sum( t => t.value ) , ds => ds.Sum() , true , new[] { "Test" } )
                    .Results.Map( s => s.Except( "Test" ) );

            var subDim2 = DimensionFactory.BuildWithParentLink( "Test" ,
                new[] { ("C", "ALL") , ("D", "ALL") } ,
                t => t.Item1 ,
                t => t.Item2 );
            var subDim2Leaves = subDim2.Leaves().ToArr();
            var subDim2Targets = targets.Select( s => s.Add( allDim.Frame.First() ) );

            var subData2 = generator.Skeletons.Select( ( s , i ) => (s, i) )
                .Where( t => ( t.i % allDimLeaves.Length ) - 2 <= 1 && ( t.i % allDimLeaves.Length ) - 2 >= 0 )
                .Select( t => t.s.Add( subDim2Leaves[( t.i % allDimLeaves.Length ) - 2] ) ).ToSeq();

            var subDim2Results = Aggregator.DetailedAggregate( subData2 , subDim2Targets , data => data.Sum( t => t.value ) , ds => ds.Sum() , true , new[] { "Test" } )
                    .Results.Map( s => s.Except( "Test" ) );

            var merged = subDim1Results.Concat( subDim2Results )
                .GroupBy( x => x.Key )
                .Select( g => new SkeletonsAccumulator<int>( g.Key , g.SelectMany( x => x.Components ) , g.First().Aggregator ) )
                .ToArr();

            allResults.Length.Should().Be( merged.Length );

            foreach ( var t in targets )
            {
                var check = from a in allResults.Find( x => x.Key.Equals( t ) )
                            from m in merged.Find( x => x.Key.Equals( t ) )
                            select (a.Value, m.Value);

                check.ShouldBeSome( t =>
                {
                    var (va, vm) = t;
                    var test = from a in va
                               from m in vm
                               select a - m;

                    test.ShouldBeSome( i => i.Should().Be( 0 ) );
                } );
            }
        }
    }
}