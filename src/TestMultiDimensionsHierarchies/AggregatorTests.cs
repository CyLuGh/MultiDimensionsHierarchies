﻿using FluentAssertions;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using System;
using System.Linq;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class AggregatorTests
{
    internal Seq<Skeleton<int>> GetLeavesSample( params string[] dimensions )
        => dimensions.Select( d => SkeletonTests.GetDimension( d ) )
            .Combine().Where( s => s.IsLeaf() )
            .Select( s =>
            {
                int value = 0;
                foreach ( var bone in s.Bones )
                {
                    foreach ( var part in bone.Label.Split( '.' ) )
                        if ( int.TryParse( part , out int v ) )
                            value += v;
                }
                return new Skeleton<int>( value , s );
            } )
            .ToSeq();

    internal Seq<Skeleton> GetTargets( params string[] dimensions )
        => dimensions.Select( d => SkeletonTests.GetDimension( d ) )
            .Combine().Where( s => !s.IsLeaf() )
            .ToSeq();

    internal int GetExpectedResult( int expectedSingle , int dimensionCount , int itemsCount )
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
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
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

    //[Fact]
    //public void TestTargeted2DimensionsSimplifiedData()
    //{
    //    var dimensions = new[] { "Dim A" , "Dim B" };
    //    var skeletons = GetLeavesSample( dimensions )
    //        .Where( b => b.Bones.Find( o => o.DimensionName.Equals( "Dim A" ) && o.Label.Equals( "2.1" ) ).IsSome );
    //    var targets = GetTargets( dimensions );
    //    targets = Seq.create(
    //        targets.Find( "1" , "2" ) , targets.Find( "2" , "2" )
    //        ).Somes();
    //    var result = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets );

    //    result.Status.Should().Be( AggregationStatus.OK );
    //    var r2 = result.Results.Find( "2" , "2" );
    //    r2.IsSome.Should().BeTrue();
    //    r2.IfSome( r =>
    //    {
    //        r.Value.IsSome.Should().BeTrue();
    //        r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 3 , dimensions.Length , 4 ) ) );
    //    } );
    //    result.Results.Length.Should().Be( targets.Length );
    //}
}