using FluentAssertions;
using LanguageExt;
using LanguageExt.UnitTesting;
using MultiDimensionsHierarchies.Core;
using System;
using System.Linq;
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
        skeletons.Find( ("1.1", "Dimension A") , ("2", "Dimension B") )
            .ShouldBeSome( s => dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) ) );

        var skeletons2 = SkeletonFactory.BuildSkeletons( items , parser , new[] { dimA , dimB } );
        skeletons2.Length.Should().Be( 2 );

        skeletons2[0].Bones.Length.Should().Be( 2 );
        skeletons2.Find( ("1.1", "Dimension A") , ("2", "Dimension B") )
            .ShouldBeSome( s => dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) ) );

        skeletons.Should().BeEquivalentTo( skeletons2 );
    }

    [Fact]
    public void TestTryBuild()
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

        var skeletons = SkeletonFactory.TryBuildSkeletons( items , parser , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension B" } );
        skeletons.Length.Should().Be( 2 );

        skeletons[0]
            .ShouldBeRight( s => s.Bones.Length.Should().Be( 2 ) );
        skeletons.Rights()
            .Find( ("1.1", "Dimension A") , ("2", "Dimension B") )
            .ShouldBeSome( s => dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) ) );

        var skeletons2 = SkeletonFactory.TryBuildSkeletons( items , parser , new[] { dimA , dimB } );
        skeletons2.Length.Should().Be( 2 );

        skeletons2[0]
            .ShouldBeRight( s => s.Bones.Length.Should().Be( 2 ) );
        skeletons2.Rights().Find( ("1.1", "Dimension A") , ("2", "Dimension B") )
            .ShouldBeSome( s => dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) ) );

        skeletons.Should().BeEquivalentTo( skeletons2 );
    }

    [Fact]
    public void TestFastBuild()
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

        var skeletons = SkeletonFactory.FastBuild( items , parser , new[] { dimA , dimB } );
        skeletons.Length.Should().Be( 2 );

        skeletons[0].Bones.Length.Should().Be( 2 );
        skeletons.Find( ("1.1", "Dimension A") , ("2", "Dimension B") )
            .ShouldBeSome( s => dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) ) );

        var skeletons2 = SkeletonFactory.FastBuild( items , parser , new[] { dimA , dimB } );
        skeletons2.Length.Should().Be( 2 );

        skeletons2[0].Bones.Length.Should().Be( 2 );
        skeletons2.Find( ("1.1", "Dimension A") , ("2", "Dimension B") )
            .ShouldBeSome( s => dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) ) );

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
    public void TestTryMissingDimension()
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

        var act = () => SkeletonFactory.TryBuildSkeletons( items , parser , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension C" } );
        act.Should().Throw<ArgumentException>()
            .WithMessage( "Some dimensions of interest aren't available in the dimensions collection: Dimension C." );

        act = () => SkeletonFactory.TryBuildSkeletons( items , parser , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension C" , "Dimension D" } );
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
    public void TestTryMissingValue()
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

        var act = () => SkeletonFactory.TryBuildSkeletons( items , parser , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension B" } ).ToArray();
        act.Should().NotThrow<ApplicationException>();

        var skeletons = SkeletonFactory.TryBuildSkeletons( items , parser , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension B" } ).ToArray();
        skeletons.Rights().Should().HaveCount( 1 );
        skeletons.Lefts().Should().HaveCount( 1 );

        skeletons.Lefts().First()
            .Message.Should().Be( "Couldn't find some dimensions values for (1.2, 4): Couldn't find 4 in dimension Dimension B" );
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

        skeletons.Find( "2.1" , "1.1.1" , "1.1" )
            .ShouldBeSome( s => dimA.Find( "2.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) ) );
    }

    [Fact]
    public void TestTryStringBuild()
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

        var skeletons = SkeletonFactory.TryBuildSkeletons( items , partitioner , selectioner , new[] { dimA , dimB , dimC } );
        skeletons.Length.Should().Be( 2 );

        skeletons[0]
            .ShouldBeRight( s => s.Bones.Length.Should().Be( 3 ) );

        skeletons.Rights().Find( "2.1" , "1.1.1" , "1.1" )
            .ShouldBeSome( s => dimA.Find( "2.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) ) );
    }

    [Fact]
    public void TestRedundantItems()
    {
        var dimA = SkeletonTests.GetDimensionWithRedundantLabel( "Dimension A" );
        var dimB = SkeletonTests.GetDimensionWithRedundantLabel( "Dimension B" );
        var dimC = SkeletonTests.GetDimensionWithRedundantLabel( "Dimension C" );

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

        items = new[] { "1.1:0:1.1.1" , "1.2:2.4:2.1" };
        skeletons = SkeletonFactory.BuildSkeletons( items , partitioner , selectioner , new[] { dimA , dimB , dimC } );
        skeletons.Length.Should().Be( 3 );
    }

    [Fact]
    public void TestTryRedundantItems()
    {
        var dimA = SkeletonTests.GetDimensionWithRedundantLabel( "Dimension A" );
        var dimB = SkeletonTests.GetDimensionWithRedundantLabel( "Dimension B" );
        var dimC = SkeletonTests.GetDimensionWithRedundantLabel( "Dimension C" );

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

        var skeletons = SkeletonFactory.TryBuildSkeletons( items , partitioner , selectioner , new[] { dimA , dimB , dimC } );
        skeletons.Length.Should().Be( 2 );

        items = new[] { "1.1:0:1.1.1" , "1.2:2.4:2.1" };
        skeletons = SkeletonFactory.TryBuildSkeletons( items , partitioner , selectioner , new[] { dimA , dimB , dimC } );
        skeletons.Length.Should().Be( 3 );
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

        skeletons.All( s => s.Bones.Length == 2 ).Should().BeTrue();
        skeletons.Find( "1.1" , "2" )
            .ShouldBeSome( s =>
            {
                dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) );

                s.Value.ShouldBeSome( v => v.Should().Be( 4.6 ) );
                s.ValueUnsafe.Should().Be( 4.6 );
            } );
    }

    [Fact]
    public void TestTryBuildGeneric()
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

        var skeletons = SkeletonFactory.TryBuildSkeletons( items , parser , evaluator , new[] { dimA , dimB } , new[] { "Dimension A" , "Dimension B" } );
        skeletons.Length.Should().Be( 2 );

        skeletons.Rights().All( s => s.Bones.Length == 2 ).Should().BeTrue();
        skeletons.Rights().Find( "1.1" , "2" )
            .ShouldBeSome( s =>
            {
                dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) );

                s.Value.ShouldBeSome( v => v.Should().Be( 4.6 ) );
                s.ValueUnsafe.Should().Be( 4.6 );
            } );
    }

    [Fact]
    public void TestBuildDimensionsArray()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );
        var dimC = SkeletonTests.GetDimension( "Dimension C" );
        var dimensions = Arr.create( dimA , dimB , dimC );

        var items = new[] { "1.1:2.1:1.2" , "2:1:2.4" };
        var skeletons = SkeletonFactory.BuildSkeletons( items , s => s.Split( ':' ) , dimensions );
        skeletons.Length.Should().Be( 2 );
        skeletons.Find( "2" , "1" , "2.4" )
            .ShouldBeSome();

        var items2 = new[] { "1.1:2.1:1.2" , "2:1:4" , "1:1:1:1" };
        var act = () => SkeletonFactory.BuildSkeletons( items2 , s => s.Split( ':' ) , dimensions ).Strict();
        act.Should().Throw<AggregateException>()
            .WithInnerException( typeof( ArgumentException ) , "Some dimensions couldn't be resolved: Dimension C." );
        act.Should().Throw<AggregateException>()
            .WithInnerException( typeof( ArgumentException ) , "Dimensions count doesn't match parsed string 1:1:1:1" );
    }

    [Fact]
    public void TestTryBuildDimensionsArray()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );
        var dimC = SkeletonTests.GetDimension( "Dimension C" );
        var dimensions = Arr.create( dimA , dimB , dimC );

        var items = new[] { "1.1:2.1:1.2" , "2:1:2.4" };
        var skeletons = SkeletonFactory.TryBuildSkeletons( items , s => s.Split( ':' ) , dimensions );
        skeletons.Length.Should().Be( 2 );
        skeletons.Rights().Find( "2" , "1" , "2.4" )
            .ShouldBeSome();

        items = new[] { "1.1:2.1:1.2" , "2:1:4" , "1:1:1:1" };
        skeletons = SkeletonFactory.TryBuildSkeletons( items , s => s.Split( ':' ) , dimensions );
        skeletons.Rights().Count.Should().Be( 1 );
        skeletons.Lefts().Count.Should().Be( 2 );
    }

    [Fact]
    public void TestFastBuildGeneric()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );

        var items = Seq.create( (DimA: "1.1", DimB: "2", Value: 4.6) , (DimA: "1.2", DimB: "2.3", Value: 7d) );
        var parser = ( (string DimA, string DimB, double _) t , string dim ) => dim switch
        {
            "Dimension A" => t.DimA,
            "Dimension B" => t.DimB,
            _ => string.Empty,
        };
        var evaluator = ( (string DimA, string DimB, double Value) t ) => t.Value;

        var skeletons = SkeletonFactory.FastBuild( items , parser , evaluator , Seq.create( dimA , dimB ) );
        skeletons.Length.Should().Be( 2 );

        skeletons.All( s => s.Bones.Length == 2 ).Should().BeTrue();
        skeletons.Find( "1.1" , "2" )
            .ShouldBeSome( s =>
            {
                dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) );

                s.Value.ShouldBeSome( v => v.Should().Be( 4.6 ) );
                s.ValueUnsafe.Should().Be( 4.6 );
            } );
    }

    [Fact]
    public void TestFastBuildGroupInput()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );

        var items = Seq.create(
            new SampleRecord( DimA: "1.1" , DimB: "2" , Value: 4.6 ) ,
            new SampleRecord( DimA: "1.2" , DimB: "2.3" , Value: 7d ) ,
            new SampleRecord( DimA: "1.1" , DimB: "2" , Value: 4d ) );
        var parser = ( SampleRecord t , string dim ) => dim switch
        {
            "Dimension A" => t.DimA,
            "Dimension B" => t.DimB,
            _ => string.Empty,
        };
        var evaluator = ( SampleRecord t ) => t.Value;

        var skeletons = SkeletonFactory.FastBuild( items , parser , evaluator , Seq.create( dimA , dimB ) );
        skeletons.Length.Should().Be( items.Count );

        skeletons.All( s => s.Bones.Length == 2 ).Should().BeTrue();
        var found = skeletons.FindAll( "1.1" , "2" );
        found.Length.Should().Be( 2 );
        found.Sum( s => s.ValueUnsafe ).Should().Be( 8.6 );

        skeletons = SkeletonFactory.FastBuild( items , parser , evaluator , Seq.create( dimA , dimB ) ,
            r => new SampleKey( r.DimA , r.DimB ) , records =>
                new SampleRecord( DimA: records.Key.DimA , DimB: records.Key.DimB , Value: records.Sum( x => x.Value ) ) );
        skeletons.Length.Should().Be( items.Count - 1 );
        skeletons.All( s => s.Bones.Length == 2 ).Should().BeTrue();

        skeletons.Find( "1.1" , "2" )
            .ShouldBeSome( s =>
            {
                dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) );

                s.Value.ShouldBeSome( v => v.Should().Be( 8.6 ) );
                s.ValueUnsafe.Should().Be( 8.6 );
            } );
    }

    [Fact]
    public void TestFastBuildGroupSkeletons()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );

        var items = Seq.create(
            new SampleRecord( DimA: "1.1" , DimB: "2" , Value: 4.6 ) ,
            new SampleRecord( DimA: "1.2" , DimB: "2.3" , Value: 7d ) ,
            new SampleRecord( DimA: "1.1" , DimB: "2" , Value: 4d ) );
        var parser = ( SampleRecord t , string dim ) => dim switch
        {
            "Dimension A" => t.DimA,
            "Dimension B" => t.DimB,
            _ => string.Empty,
        };
        var evaluator = ( SampleRecord t ) => t.Value;

        var skeletons = SkeletonFactory.FastBuild( items , parser , evaluator , Seq.create( dimA , dimB ) );
        skeletons.Length.Should().Be( items.Count );

        skeletons.All( s => s.Bones.Length == 2 ).Should().BeTrue();
        var found = skeletons.FindAll( "1.1" , "2" );
        found.Length.Should().Be( 2 );
        found.Sum( s => s.ValueUnsafe ).Should().Be( 8.6 );

        skeletons = SkeletonFactory.FastBuild( items , parser , evaluator , Seq.create( dimA , dimB ) ,
            groupAggregator: ds => ds.Sum() );
        skeletons.Length.Should().Be( items.Count - 1 );
        skeletons.All( s => s.Bones.Length == 2 ).Should().BeTrue();

        skeletons.Find( "1.1" , "2" )
            .ShouldBeSome( s =>
            {
                dimA.Find( "1.1" ).ShouldBeSome( d => s.Bones["Dimension A"].Should().BeSameAs( d ) );

                s.Value.ShouldBeSome( v => v.Should().Be( 8.6 ) );
                s.ValueUnsafe.Should().Be( 8.6 );
            } );
    }

    [Fact]
    public void TestFastBuildAndCheck()
    {
        var dimA = SkeletonTests.GetDimension( "Dimension A" );
        var dimB = SkeletonTests.GetDimension( "Dimension B" );

        var items = Seq.create( (DimA: "1.1", DimB: "2", Value: 4.6) , (DimA: "1.2", DimB: "2.3", Value: 7d) );
        var parser = ( (string DimA, string DimB, double _) t , string dim ) => dim switch
        {
            "Dimension A" => t.DimA,
            "Dimension B" => t.DimB,
            _ => string.Empty,
        };
        var evaluator = ( (string DimA, string DimB, double Value) t ) => t.Value;

        var skeletons = SkeletonFactory.FastBuild( items , parser , evaluator , Seq.create( dimA , dimB ) );
        skeletons.Length.Should().Be( 2 );

        var targets = skeletons.FindAll( "1.2" , "2.3" ).Select( x => x.Key );
        skeletons = SkeletonFactory.FastBuild( items , parser , evaluator , Seq.create( dimA , dimB ) , checkTargets: targets );
        skeletons.Length.Should().Be( 1 );
    }
}