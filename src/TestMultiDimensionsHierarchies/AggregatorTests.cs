using FluentAssertions;
using LanguageExt;
using LanguageExt.UnitTesting;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using System;
using System.Linq;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class AggregatorTests
{
    internal static Seq<Skeleton<int>> GetLeavesSample( params string[] dimensions )
        => dimensions.Select( d => SkeletonTests.GetDimension( d ) )
            .Combine().Where( s => s.IsLeaf() )
            .Select( s =>
            {
                int value = 0;
                foreach ( var bone in s.Bones )
                {
                    foreach ( var part in bone.Label.Split( '.' ) )
                    {
                        if ( int.TryParse( part , out int v ) )
                            value += v;
                    }
                }
                return new Skeleton<int>( value , s );
            } )
            .ToSeq();

    internal static Seq<Skeleton<int>> GetSample( params string[] dimensions )
        => dimensions.Select( d => SkeletonTests.GetDimension( d ) )
            .Combine()
            .Select( s =>
            {
                int value = 0;
                foreach ( var bone in s.Bones )
                {
                    foreach ( var part in bone.Label.Split( '.' ) )
                    {
                        if ( int.TryParse( part , out int v ) )
                            value += v;
                    }
                }
                return new Skeleton<int>( value , s );
            } )
            .ToSeq();

    internal static Seq<Skeleton<double>> GetLeavesWeightSample( params string[] dimensions )
        => dimensions.Select( d => SkeletonTests.GetDimensionWithRedundantLabel( d ) )
            .Combine().Where( s => s.IsLeaf() )
            .Select( s =>
            {
                double value = 0;
                foreach ( var bone in s.Bones )
                {
                    foreach ( var part in bone.Label.Split( '.' ) )
                    {
                        if ( double.TryParse( part , out double v ) )
                            value += v != 0 ? v : 10d;
                    }
                }
                return new Skeleton<double>( value , s );
            } )
            .ToSeq();

    internal static Seq<Skeleton> GetTargets( params string[] dimensions )
        => dimensions.Select( d => SkeletonTests.GetDimension( d ) )
            .Combine().Where( s => !s.IsLeaf() )
            .ToSeq();

    internal static Seq<Skeleton> GetWeightTargets( params string[] dimensions )
        => dimensions.Select( d => SkeletonTests.GetDimensionWithRedundantLabel( d ) )
            .Combine().Where( s => !s.IsLeaf() )
            .ToSeq();

    internal static int GetExpectedResult( int expectedSingle , int dimensionCount , int itemsCount )
        => expectedSingle
        * dimensionCount // Because of how we assign values to leaves
        * (int) Math.Pow( itemsCount , dimensionCount - 1 );

    [Fact]
    public void TestHeuristic1Dimension()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesSample( dimensions );
        var result = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
    }

    [Fact]
    public void TestHeuristic1DimensionMultiLevelData()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetSample( dimensions );
        var result = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b );

        result.Status.Should().Be( AggregationStatus.OK );
        var r1 = result.Results.Find( "1" );
        r1.IsSome.Should().BeTrue();
        r1.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 26 , dimensions.Length , 8 ) ) );
        } );

        var r2 = result.Results.Find( "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 20 , dimensions.Length , 5 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
    }

    [Fact]
    public void TestHeuristic2Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesSample( dimensions );
        var result = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );

        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , 2 ) );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );

        var cplx = dimensions.Select( d => SkeletonTests.GetDimension( d ) ).Complexity();
        result.Results.LongCount().Should().Be( cplx );
    }

    [Fact]
    public void TestHeuristic2DimensionMultiLevelData()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetSample( dimensions );
        var result = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b );

        result.Status.Should().Be( AggregationStatus.OK );
        var r1 = result.Results.Find( "1" , "1" );
        r1.IsSome.Should().BeTrue();
        r1.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 26 , dimensions.Length , 8 ) ) );
        } );

        var r2 = result.Results.Find( "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 20 , dimensions.Length , 5 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
    }

    [Fact]
    public void TestHeuristic3Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" };
        var skeletons = GetLeavesSample( dimensions );
        var result = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
    }

    [Fact]
    public void TestHeuristic5Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        var skeletons = GetLeavesSample( dimensions );
        var result = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
        r2.ShouldBeSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );

        var cplx = dimensions.Select( d => SkeletonTests.GetDimension( d ) ).Complexity();
        result.Results.LongCount().Should().Be( cplx );
    }

    [Fact]
    public void CompateHeuristic5Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        var skeletons = GetLeavesSample( dimensions );

        var resultGroup = Aggregator.Aggregate( Method.HeuristicGroup , skeletons , ( a , b ) => a + b );
        var resultDict = Aggregator.Aggregate( Method.HeuristicDictionary , skeletons , ( a , b ) => a + b );

        resultGroup.Status.Should().Be( AggregationStatus.OK );
        var r2Grp = resultGroup.Results.Find( "2" , "2" , "2" , "2" , "2" );
        r2Grp.ShouldBeSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        resultGroup.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );

        var cplx = dimensions.Select( d => SkeletonTests.GetDimension( d ) ).Complexity();
        resultGroup.Results.LongCount().Should().Be( cplx );

        var r2Dct = resultDict.Results.Find( "2" , "2" , "2" , "2" , "2" );
        r2Dct.ShouldBeSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );

        resultGroup.Results.LongCount().Should().Be( resultDict.Results.LongCount() );
    }

    [Fact]
    public void TestHeuristic5DimensionsWithTargets()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        var skeletons = GetLeavesSample( dimensions );

        var targets = GetTargets( dimensions );
        targets = Seq.create(
            targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
            ).Somes();

        var result = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b , targets );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Length.Should().Be( targets.Length );
    }

    [Fact]
    public void TestTargeted1Dimension()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        var result = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
        result.Results.Length.Should().Be( targets.Length );
    }

    [Fact]
    public void TestTargeted1DimensionSubSelect()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        targets = Seq.create(
            targets.Find( "1" ) , targets.Find( "2" )
            ).Somes();
        var result = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Length.Should().Be( targets.Length );
    }

    [Fact]
    public void TestTargeted2Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        var result = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
        result.Results.Length.Should().Be( targets.Length );
    }

    [Fact]
    public void TestTargeted2DimensionsSubSelect()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        targets = Seq.create(
            targets.Find( "1" , "2" ) , targets.Find( "2" , "2" )
            ).Somes();
        var result = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Length.Should().Be( targets.Length );
    }

    [Fact]
    public void TestTargeted2SimplifyData()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        var result = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets );

        GetTargets( dimensions[1] ).Find( "1.2" ).IfSome( tgt =>
        {
            var targets2 = GetTargets( dimensions[0] ).Select( s => s.Add( tgt ) );
            var result2 = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets2 );

            var test = from r in result.Results.Find( "2" , "1.2" )
                       from r2 in result2.Results.Find( "2" , "1.2" )
                       select r.Value.Equals( r.Value );
            test.ShouldBeSome( t => t.Should().BeTrue() );
        } );
    }

    [Fact]
    public void TestTargeted5DimensionsSubSelect()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        targets = Seq.create(
            targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
            ).Somes();
        var result = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Length.Should().Be( targets.Length );
    }

    [Fact]
    public void TestHeuristicWeight1Dimension()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesWeightSample( dimensions );
        var result = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b ,
            weightEffect: ( t , w ) => t * w );

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
    public void TestTargetedWeight1Dimension()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesWeightSample( dimensions );
        var targets = GetWeightTargets( dimensions ).FindAll( "2" );

        var result = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b ,
            targets ,
            weightEffect: ( t , w ) => t * w );

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
        var skeletons = GetLeavesWeightSample( dimensions );
        var result = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b ,
            weightEffect: ( t , w ) => t * w );

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
        var skeletons = GetLeavesWeightSample( dimensions );
        var targets = GetWeightTargets( dimensions ).FindAll( "2" , "2" );

        var result = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b ,
            targets ,
            weightEffect: ( t , w ) => t * w );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" );
        r2.ShouldBeSome( r =>
        {
            r.Value.ShouldBeSome( v => v.Should()
                .Be( 207 ) );
        } );
    }
}