﻿using FluentAssertions;
using MultiDimensionsHierarchies.Core;
using System;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class SkeletonFactoryTests
{
    [Fact]
    public void TestBuild()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );

        var items = new[] { (DimA: "1.1", dimB: "2") , (DimA: "1.2", dimB: "2.3") };
        var parser = ( (string DimA, string DimB) t , string dim ) => dim switch
        {
            "Dimension A" => t.DimA,
            "Dimension B" => t.DimB,
            _ => string.Empty,
        };

        var skeletons = SkeletonFactory.BuildSkeletons( items , parser , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension B" } );
        skeletons.Length.Should().Be( 2 );

        skeletons[0].Bones.Length.Should().Be( 2 );
        skeletons[0].Bones.Find( b => b.DimensionName.Equals( "Dimension A" ) )
            .IfSome( b => dimA.Find( "1.1" ).IfSome( d =>
            {
                b.Should().BeSameAs( d );
            } ) );

        var skeletons2 = SkeletonFactory.BuildSkeletons( items , parser , new[] { dimA , dimB } );
        skeletons2.Length.Should().Be( 2 );

        skeletons2[0].Bones.Length.Should().Be( 2 );
        skeletons2[0].Bones.Find( b => b.DimensionName.Equals( "Dimension A" ) )
            .IfSome( b => dimA.Find( "1.1" ).IfSome( d =>
            {
                b.Should().BeSameAs( d );
            } ) );

        skeletons.Should().BeEquivalentTo( skeletons2 );
    }

    [Fact]
    public void TestMissingDimension()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );

        var items = new[] { (DimA: "1.1", dimB: "2") , (DimA: "1.2", dimB: "2.3") };
        var parser = ( (string DimA, string DimB) t , string dim ) => dim switch
        {
            "Dimension A" => t.DimA,
            "Dimension B" => t.DimB,
            _ => string.Empty,
        };

        var act = () => SkeletonFactory.BuildSkeletons( items , parser , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension C" } );
        act.Should().Throw<ArgumentException>()
            .WithMessage( "Some dimensions of interest aren't available in the dimensions collection: Dimension C." );

        act = () => SkeletonFactory.BuildSkeletons( items , parser , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension C" , "Dimension D" } );
        act.Should().Throw<ArgumentException>()
            .WithMessage( "Some dimensions of interest aren't available in the dimensions collection: Dimension C, Dimension D." );
    }

    [Fact]
    public void TestMissingValue()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );

        var items = new[] { (DimA: "1.1", dimB: "2") , (DimA: "1.2", dimB: "4") };
        var parser = ( (string DimA, string DimB) t , string dim ) => dim switch
        {
            "Dimension A" => t.DimA,
            "Dimension B" => t.DimB,
            _ => string.Empty,
        };

        var act = () => SkeletonFactory.BuildSkeletons( items , parser , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension B" } ).ToArray();
        act.Should().Throw<ApplicationException>()
                .WithMessage( "Couldn't find some dimensions values: Couldn't find 4 in dimension Dimension B" );
    }

    [Fact]
    public void TestStringBuild()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );
        var dimC = SkeletonTests.GetDimension( "Dimension C" );

        var items = new[] { "1.1:2.1:1.1.1" , "1.2:2.4:2.1" };
        var partitioner = ( string s ) => s.Split( ':' );
        var selectioner = ( string[] parts , string dim )
            => dim switch
            {
                "Dimension A" => parts[1],
                "Dimension B" => parts[2],
                "Dimension C" => parts[0],
                _ => string.Empty,
            };

        var skeletons = SkeletonFactory.BuildSkeletons( items , partitioner , selectioner , new[] { dimA , dimB , dimC } );
        skeletons.Length.Should().Be( 2 );

        skeletons[0].Bones.Length.Should().Be( 3 );
        skeletons[0].Bones.Find( b => b.DimensionName.Equals( "Dimension A" ) )
            .IfSome( b => dimA.Find( "2.1" ).IfSome( d =>
            {
                b.Should().BeSameAs( d );
            } ) );
    }

    [Fact]
    public void TestBuildGeneric()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );

        var items = new[] { (DimA: "1.1", DimB: "2", Value: 4.6) , (DimA: "1.2", DimB: "2.3", Value: 7d) };
        var parser = ( (string DimA, string DimB, double _) t , string dim ) => dim switch
        {
            "Dimension A" => t.DimA,
            "Dimension B" => t.DimB,
            _ => string.Empty,
        };
        var evaluator = ( (string DimA, string DimB, double Value) t ) => t.Value;

        var skeletons = SkeletonFactory.BuildSkeletons( items , parser , evaluator , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension B" } );
        skeletons.Length.Should().Be( 2 );

        skeletons[0].Bones.Length.Should().Be( 2 );
        skeletons[0].Bones.Find( b => b.DimensionName.Equals( "Dimension A" ) )
            .IfSome( b => dimA.Find( "1.1" ).IfSome( d =>
            {
                b.Should().BeSameAs( d );
            } ) );

        skeletons[0].Value.IsSome.Should().BeTrue();
        skeletons[0].Value.IfSome( v => v.Should().Be( 4.6 ) );
        skeletons[0].ValueUnsafe.Should().Be( 4.6 );
    }
}