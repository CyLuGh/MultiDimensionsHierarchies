using FluentAssertions;
using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Linq;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class GenericSkeletonTests
{
    internal class TestObject
    {
        internal int Value { get; set; }
    }

    [Fact]
    public void TestSkeleton()
    {
        var boneA = new Bone( "A1" , "Dimension A" );
        var boneB = new Bone( "B1" , "Dimension B" );

        var skeleton = new Skeleton<double>( new Skeleton( boneB , boneA ) );

        skeleton.Bones[0].Should().BeSameAs( boneA );
        skeleton.Bones[1].Should().BeSameAs( boneB );
    }

    [Fact]
    public void TestSkeletonException()
    {
        var bone = new Bone( "Bad one" , string.Empty );
        var boneA1 = new Bone( "A1" , "Dimension A" );
        var boneA2 = new Bone( "A2" , "Dimension A" );

        var act = () => new Skeleton<double>( true , 12.7 , bone , boneA1 );
        act.Should().Throw<ArgumentException>()
            .WithMessage( "A bone should always define its dimension name!" );

        act = () => new Skeleton<double>( true , boneA1 , boneA2 );
        act.Should().Throw<ArgumentException>()
            .WithMessage( "A bone with the same dimension name has been defined more than once!" );
    }

    [Fact]
    public void TestAggregate()
    {
        var boneA = new Bone( "A1" , "Dimension A" );
        var boneB = new Bone( "B1" , "Dimension B" );

        var skeleton = new Skeleton<int>( 14 , boneB , boneA );
        var skeleton2 = new Skeleton<int>( 10 , boneB , boneA );
        var skeleton3 = new Skeleton<int>( 5 , boneB , boneA );
        var skeleton4 = new Skeleton<int>( 1 , boneB , boneA );

        var sum = new[] { skeleton , skeleton2 , skeleton3 , skeleton4 }
            .Aggregate( ( a , b ) => a + b );
        sum.IsSome.Should().BeTrue();
        sum.IfSome( s =>
        {
            s.Value.IsSome.Should().BeTrue();
            s.Value.IfSome( v => v.Should().Be( 30 ) );
            s.Bones[0].Should().BeSameAs( boneA );
            s.Bones[1].Should().BeSameAs( boneB );
        } );

        skeleton = new Skeleton<int>( boneA , boneB );
        sum = new[] { skeleton , skeleton2 , skeleton3 , skeleton4 }
            .Aggregate( ( a , b ) => a + b );
        sum.IsSome.Should().BeTrue();
        sum.IfSome( s =>
        {
            s.Value.IsSome.Should().BeTrue();
            s.Value.IfSome( v => v.Should().Be( 16 ) );
            s.Bones[0].Should().BeSameAs( boneA );
            s.Bones[1].Should().BeSameAs( boneB );
        } );

        skeleton2 = new Skeleton<int>( boneA , boneB );
        skeleton3 = new Skeleton<int>( boneA , boneB );
        skeleton4 = new Skeleton<int>( boneA , boneB );
        sum = new[] { skeleton , skeleton2 , skeleton3 , skeleton4 }
            .Aggregate( ( a , b ) => a + b );
        sum.IsSome.Should().BeTrue();
        sum.IfSome( s =>
        {
            s.Value.IsSome.Should().BeTrue();
            s.Value.IfSome( v => v.Should().Be( 0 ) );
        } );

        // Test with some nullable T
        var skelTest1 = new Skeleton<TestObject>( new TestObject { Value = 1 } , boneA , boneB );
        var skelTest2 = new Skeleton<TestObject>( new TestObject { Value = 4 } , boneA , boneB );
        var skelTest3 = new Skeleton<TestObject>( new TestObject { Value = 9 } , boneA , boneB );

        var aggregator = ( TestObject a , TestObject b )
            => new TestObject { Value = ( a?.Value ?? 0 ) + ( b?.Value ?? 0 ) };

        var sumTest = new[] { skelTest1 , skelTest2 , skelTest3 }.Aggregate( aggregator );
        sumTest.IsSome.Should().BeTrue();
        sumTest.IfSome( s =>
        {
            s.Value.IsSome.Should().BeTrue();
            s.Value.IfSome( v => v.Value.Should().Be( 14 ) );
        } );

        skelTest1 = new Skeleton<TestObject>( boneA , boneB );
        sumTest = new[] { skelTest1 , skelTest2 , skelTest3 }.Aggregate( aggregator );
        sumTest.IsSome.Should().BeTrue();
        sumTest.IfSome( s =>
        {
            s.Value.IsSome.Should().BeTrue();
            s.Value.IfSome( v => v.Value.Should().Be( 13 ) );
        } );

        skelTest2 = new Skeleton<TestObject>( boneA , boneB );
        skelTest3 = new Skeleton<TestObject>( boneA , boneB );
        sumTest = new[] { skelTest1 , skelTest2 , skelTest3 }.Aggregate( aggregator );
        sumTest.IsSome.Should().BeTrue();
        sumTest.IfSome( s => s.Value.IsSome.Should().BeFalse() );
    }

    [Fact]
    public void TestGroupAggregate()
    {
        var boneA = new Bone( "A1" , "Dimension A" );
        var boneB = new Bone( "B1" , "Dimension B" );

        var skeleton = new Skeleton<int>( 14 , boneB , boneA );
        var skeleton2 = new Skeleton<int>( 10 , boneB , boneA );

        var sum = new[] { skeleton , skeleton2 }.Aggregate( values => values.Sum() );

        sum.IsSome.Should().BeTrue();
        sum.IfSome( s =>
        {
            s.Value.IsSome.Should().BeTrue();
            s.Value.IfSome( v => v.Should().Be( 24 ) );
            s.Bones[0].Should().BeSameAs( boneA );
            s.Bones[1].Should().BeSameAs( boneB );
        } );

        var boneTotA = new Bone( "AAA" , "Dimension A" );
        var boneTotB = new Bone( "BBB" , "Dimension B" );

        var skeletonTot = new Skeleton( boneTotA , boneTotB );
        sum = new[] { skeleton , skeleton2 }.Aggregate( skeletonTot , values => values.Sum() );
        sum.IsSome.Should().BeTrue();
        sum.IfSome( s =>
        {
            s.Value.IsSome.Should().BeTrue();
            s.Value.IfSome( v => v.Should().Be( 24 ) );
            s.Bones[0].Should().NotBeSameAs( boneA );
            s.Bones[1].Should().NotBeSameAs( boneB );
            s.Bones[0].Should().BeSameAs( boneTotA );
            s.Bones[1].Should().BeSameAs( boneTotB );
        } );

        skeleton = new Skeleton<int>( boneB , boneA );
        skeleton2 = new Skeleton<int>( boneB , boneA );
        sum = new[] { skeleton , skeleton2 }.Aggregate( values => values.Sum() );
        sum.IsSome.Should().BeTrue();
        sum.IfSome( s =>
        {
            s.Value.IsSome.Should().BeTrue();
            s.Value.IfSome( v => v.Should().Be( default ) );
            s.Bones[0].Should().BeSameAs( boneA );
            s.Bones[1].Should().BeSameAs( boneB );
        } );
    }
}