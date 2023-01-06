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

    internal static Seq<Skeleton<int>> GetLeavesSample( params Dimension[] dimensions )
        => dimensions
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
    public void TestBottomTop1Dimension()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesSample( dimensions );
        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b );

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
    public void TestBottomTop1DimensionMultiLevelData()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetSample( dimensions );
        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b );

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
    public void TestBottomTop2Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesSample( dimensions );
        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b );

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
    public void TestBottomTop2DimensionsWithFilter()
    {
        var dimensions = new[] { "Dim A" , "Dim B" }.Select( d => SkeletonTests.GetDimension( d ) ).ToArray();
        var skeletons = GetLeavesSample( dimensions );

        var filters = dimensions[1].FindAll( "2" );

        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b , filters );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.IfSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );

        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , 2 ) / 2 );
    }

    [Fact]
    public void TestBottomTop2DimensionMultiLevelData()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetSample( dimensions );
        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b );

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
    public void TestBottomTop3Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" };
        var skeletons = GetLeavesSample( dimensions );
        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.ShouldBeSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.ShouldBeSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
    }

    [Fact]
    public void TestBottomTop3DimensionsWithoutCache()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" };
        var skeletons = GetLeavesSample( dimensions );
        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b , useCachedSkeletons: false );

        result.Status.Should().Be( AggregationStatus.OK );
        var r2 = result.Results.Find( "2" , "2" , "2" );
        r2.IsSome.Should().BeTrue();
        r2.ShouldBeSome( r =>
        {
            r.Value.IsSome.Should().BeTrue();
            r.Value.ShouldBeSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
        } );
        result.Results.Where( s => s.IsRoot() ).Count().Should().Be( (int) Math.Pow( 2 , dimensions.Length ) );
    }

    [Fact]
    public void TestBottomTop5Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        var skeletons = GetLeavesSample( dimensions );
        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b );

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
    public void CompareBottomTop5Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        var skeletons = GetLeavesSample( dimensions );

        var resultGroup = Aggregator.Aggregate( Method.BottomTopGroup , skeletons , ( a , b ) => a + b );
        var resultDict = Aggregator.Aggregate( Method.BottomTopDictionary , skeletons , ( a , b ) => a + b );

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
    public void TestBottomTop5DimensionsWithTargets()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        var skeletons = GetLeavesSample( dimensions );

        var targets = GetTargets( dimensions );
        targets = Seq.create(
            targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
            ).Somes();

        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b , targets );

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
    public void TestTopDown1Dimension()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        var result = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b , targets );

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
    public void TestTopDown1DimensionSubSelect()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        targets = Seq.create(
            targets.Find( "1" ) , targets.Find( "2" )
            ).Somes();
        var result = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b , targets );

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
    public void TestTopDown2Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        var result = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b , targets );

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
    public void TestTopDown2DimensionsSubSelect()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        targets = Seq.create(
            targets.Find( "1" , "2" ) , targets.Find( "2" , "2" )
            ).Somes();
        var result = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b , targets );

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
    public void TestTopDown2SimplifyData()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        var result = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b , targets );

        GetTargets( dimensions[1] ).Find( "1.2" ).IfSome( tgt =>
        {
            var targets2 = GetTargets( dimensions[0] ).Select( s => s.Add( tgt ) );
            var result2 = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b , targets2 );

            var test = from r in result.Results.Find( "2" , "1.2" )
                       from r2 in result2.Results.Find( "2" , "1.2" )
                       select r.Value.Equals( r.Value );
            test.ShouldBeSome( t => t.Should().BeTrue() );
        } );
    }

    //[Fact]
    //public void TestTopDown5Dimensions()
    //{
    //    var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
    //    var skeletons = GetLeavesSample( dimensions );
    //    var targets = GetTargets( dimensions );
    //    targets = targets.Take( 10000 )
    //        .ConcatFast( targets.Find( "2" , "2" , "2" , "2" , "2" ) )
    //        .ToSeq();
    //    var result = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b , targets );

    //    result.Status.Should().Be( AggregationStatus.OK );
    //    var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
    //    r2.IsSome.Should().BeTrue();
    //    r2.IfSome( r =>
    //    {
    //        r.Value.IsSome.Should().BeTrue();
    //        r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
    //    } );
    //    result.Results.Length.Should().Be( targets.Length );
    //}

    //[Fact]
    //public void TestTopDownGroup5Dimensions()
    //{
    //    var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
    //    var skeletons = GetLeavesSample( dimensions );
    //    var targets = GetTargets( dimensions );
    //    targets = targets.Take( 10000 )
    //        .ConcatFast( targets.Find( "2" , "2" , "2" , "2" , "2" ) )
    //        .ToSeq();

    //    var result = Aggregator.Aggregate( Method.TopDownGroup , skeletons , ( a , b ) => a + b , targets );

    //    result.Status.Should().Be( AggregationStatus.OK );
    //    var r2 = result.Results.Find( "2" , "2" , "2" , "2" , "2" );
    //    r2.IsSome.Should().BeTrue();
    //    r2.IfSome( r =>
    //    {
    //        r.Value.IsSome.Should().BeTrue();
    //        r.Value.IfSome( v => v.Should().Be( GetExpectedResult( 18 , dimensions.Length , 4 ) ) );
    //    } );
    //    result.Results.Length.Should().Be( targets.Length );
    //}

    [Fact]
    public void TestTopDown5DimensionsSubSelect()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        targets = Seq.create(
            targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
            ).Somes();
        var result = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b , targets );

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
    public void TestTopDownGroup5DimensionsSubSelect()
    {
        var dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        var skeletons = GetLeavesSample( dimensions );
        var targets = GetTargets( dimensions );
        targets = Seq.create(
            targets.Find( "1" , "2" , "2" , "2" , "2" ) , targets.Find( "2" , "2" , "2" , "2" , "2" )
            ).Somes();
        var result = Aggregator.Aggregate( Method.TopDownGroup , skeletons , ( a , b ) => a + b , targets );

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
    public void TestBottomTopWeight1Dimension()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesWeightSample( dimensions );
        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b ,
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
    public void TestTopDownWeight1Dimension()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = GetLeavesWeightSample( dimensions );
        var targets = GetWeightTargets( dimensions ).FindAll( "2" );

        var result = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b ,
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
    public void TestBottomTopWeight2Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesWeightSample( dimensions );
        var result = Aggregator.Aggregate( Method.BottomTop , skeletons , ( a , b ) => a + b ,
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
    public void TestTopDownWeight2Dimensions()
    {
        var dimensions = new[] { "Dim A" , "Dim B" };
        var skeletons = GetLeavesWeightSample( dimensions );
        var targets = GetWeightTargets( dimensions ).FindAll( "2" , "2" );

        var result = Aggregator.Aggregate( Method.TopDown , skeletons , ( a , b ) => a + b ,
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