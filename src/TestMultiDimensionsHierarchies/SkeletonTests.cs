using FluentAssertions;
using LanguageExt;
using LanguageExt.UnitTesting;
using MultiDimensionsHierarchies.Core;
using System;
using System.Linq;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class SkeletonTests
{
    internal static Dimension GetDimension( string dimensionName ) =>
        DimensionFactory.BuildWithParentLink( dimensionName , DimensionFactoryTests.GetParentLinkHierarchy() ,
            x => x.Id , o => !string.IsNullOrEmpty( o.ParentId ) ? o.ParentId : Option<string?>.None , x => x.Label );

    internal static Dimension GetDimensionWithRedundantLabel( string dimensionName ) =>
        DimensionFactory.BuildWithParentLink( dimensionName , DimensionFactoryTests.GetRedundantDefinitionSample() ,
            x => x.Id , o => !string.IsNullOrEmpty( o.ParentId ) ? o.ParentId : Option<string?>.None , x => x.Label ,
            x => x.Weight );

    [Fact]
    public void TestSkeleton()
    {
        var boneA = new Bone( "A1" , "Dimension A" );
        var boneB = new Bone( "B1" , "Dimension B" );

        var skeleton = new Skeleton( boneB , boneA );

        skeleton.Bones[0].Should().BeSameAs( boneA );
        skeleton.Bones[1].Should().BeSameAs( boneB );
    }

    [Fact]
    public void TestSkeletonException()
    {
        var bone = new Bone( "Bad one" , string.Empty );
        var boneA1 = new Bone( "A1" , "Dimension A" );
        var boneA2 = new Bone( "A2" , "Dimension A" );

        var act = () => new Skeleton( true , bone , boneA1 );
        act.Should().Throw<ArgumentException>().WithMessage( "A bone should always define its dimension name!" );

        act = () => new Skeleton( true , boneA1 , boneA2 );
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage( "A bone with the same dimension name has been defined more than once!" );
    }

    [Fact]
    public void TestAddBone()
    {
        var boneA = new Bone( "A1" , "Dimension A" );
        var boneB = new Bone( "B1" , "Dimension B" );
        var boneC = new Bone( "C1" , "Dimension C" );

        var skeleton = new Skeleton( boneC , boneB );
        skeleton = skeleton.Add( boneA );

        skeleton.Bones[0].Should().BeSameAs( boneA );
        skeleton.Bones[1].Should().BeSameAs( boneB );
        skeleton.Bones[2].Should().BeSameAs( boneC );

        skeleton = new Skeleton( boneB );
        skeleton = skeleton.Add( new Bone[] { boneA , boneC } );

        skeleton.Bones[0].Should().BeSameAs( boneA );
        skeleton.Bones[1].Should().BeSameAs( boneB );
        skeleton.Bones[2].Should().BeSameAs( boneC );
    }

    [Fact]
    public void TestAncestors()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        dimA.Find( "2.1" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        var skeleton = new Skeleton( boneA , boneB );

                        var ancestors = skeleton.Ancestors();
                        ancestors.Length.Should().Be( 4 );
                    } );

                dimB.Find( "1.1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        var skeleton = new Skeleton( boneA , boneB );

                        var ancestors = skeleton.Ancestors();
                        ancestors.Length.Should().Be( 6 );

                        dimC.Find( "1.1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                skeleton = new Skeleton( boneA , boneB , boneC );
                                ancestors = skeleton.Ancestors();
                                ancestors.Length.Should().Be( 24 );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestAncestorsCache()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var cache = Prelude.AtomHashMap<string , Skeleton>();

        dimA.Find( "2.1" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        var skeleton = new Skeleton( boneA , boneB );

                        var ancestors = skeleton.Ancestors( cache );
                        ancestors.Length.Should().Be( 4 );
                    } );

                dimB.Find( "1.1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        var skeleton = new Skeleton( boneA , boneB );

                        var ancestors = skeleton.Ancestors( cache );
                        ancestors.Length.Should().Be( 6 );

                        dimC.Find( "1.1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                skeleton = new Skeleton( boneA , boneB , boneC );
                                ancestors = skeleton.Ancestors( cache );
                                ancestors.Length.Should().Be( 24 );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestAncestorsFilters()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        dimA.Find( "2.1" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        var skeleton = new Skeleton( boneA , boneB );

                        dimA.Find( "2" )
                            .ShouldBeSome( filterA =>
                            {
                                var filters = Seq.create( filterA );
                                var ancestors = skeleton.Ancestors( filters );
                                ancestors.Length.Should().Be( 2 );
                            } );
                    } );

                dimB.Find( "1.1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        var skeleton = new Skeleton( boneA , boneB );

                        dimA.Find( "2" )
                            .ShouldBeSome( filterA =>
                            {
                                dimB.Find( "1.1" )
                                    .ShouldBeSome( filterB =>
                                    {
                                        var filters = Seq.create( filterA , filterB );
                                        var ancestors = skeleton.Ancestors( filters );
                                        ancestors.Length.Should().Be( 1 );
                                    } );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestRoot()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        dimA.Find( "2.1" )
            .IfSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .IfSome( boneB =>
                    {
                        var skeleton = new Skeleton( boneA , boneB );

                        var root = skeleton.Root();
                        root.Bones[0]
                            .ToString()
                            .Should()
                            .Be( dimA.Find( "2" ).Some( b => b.ToString() ).None( () => "" ) );

                        root.Bones[0].Should().BeSameAs( dimA.Find( "2" ).Some( b => b ).None( () => Bone.None ) );
                    } );

                dimB.Find( "1.1.1" )
                    .IfSome( boneB =>
                    {
                        dimC.Find( "1.1.1.1" )
                            .IfSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var root = skeleton.Root();

                                root.Bones[0]
                                    .Should()
                                    .BeSameAs( dimA.Find( "2" ).Some( b => b ).None( () => Bone.None ) );
                                root.Bones[1]
                                    .Should()
                                    .BeSameAs( dimB.Find( "1" ).Some( b => b ).None( () => Bone.None ) );
                                root.Bones[2]
                                    .Should()
                                    .BeSameAs( dimC.Find( "1" ).Some( b => b ).None( () => Bone.None ) );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestLeaves()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        dimA.Find( "2" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneC , boneA , boneB );
                                var leaves = skeleton.Leaves();

                                leaves.Length.Should().Be( 8 );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestHasDimension()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        dimA.Find( "2" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneC , boneA , boneB );

                                skeleton.HasDimension( "Dim A" , "2" , "2.1" ).Should().BeTrue();
                                skeleton.HasDimension( "Dim B" , "1.2" , "1.1" ).Should().BeTrue();
                                skeleton.HasDimension( "Dim B" , "2.2" , "2.1" ).Should().BeFalse();
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestHasDimensions()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var dictionaryHit = new[]
        {
            ( Key: "Dim A" , Values: new[] { "2" , "2.1" } ) , ( Key: "Dim B" , Values: new[] { "1.2" , "1.1" } )
        }.ToDictionary( x => x.Key , x => x.Values );

        var dictionaryNoHit = new[]
        {
            ( Key: "Dim A" , Values: new[] { "2" , "2.1" } ) , ( Key: "Dim B" , Values: new[] { "2.2" , "2.1" } )
        }.ToDictionary( x => x.Key , x => x.Values );

        dimA.Find( "2" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneC , boneA , boneB );

                                skeleton.HasDimensions( dictionaryHit ).Should().BeTrue();
                                skeleton.HasDimensions( dictionaryNoHit ).Should().BeFalse();
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestUpdate()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var sBoneA = new Bone( "2.1" , "Dim A" );
        var sBoneB = new Bone( "1.1" , "Dim B" );
        var sBoneC = new Bone( "1.1.1" , "Dim C" );

        var skeleton = new Skeleton( sBoneA , sBoneB , sBoneC );
        /* Bones don't have a hierarchy */
        skeleton.Ancestors().Length.Should().Be( 1 );

        var updated = skeleton.Update( dimA );
        /* Dimension Dim A is updated with a hierarchy */
        updated.Ancestors().Length.Should().Be( 2 );

        updated = skeleton.Update( dimA , dimB , dimC );
        var ancestors = updated.Ancestors().ToArray();
        ancestors.Length.Should().Be( 12 );
    }

    [Fact]
    public void TestReplace()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        dimA.Find( "2.1" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var ancestors = skeleton.Ancestors();
                                ancestors.Length.Should().Be( 24 );

                                var replaced = skeleton.Replace( new Bone( "42" , "Dim C" ) );
                                ancestors = replaced.Ancestors();
                                ancestors.Length.Should().Be( 6 );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestStripHierarchies()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        dimA.Find( "2" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var ancestors = skeleton.Ancestors();
                                ancestors.Length.Should().Be( 6 );
                                var leaves = skeleton.Leaves();
                                leaves.Length.Should().Be( 8 );

                                var stripped = skeleton.StripHierarchies();
                                ancestors = stripped.Ancestors();
                                ancestors.Length.Should().Be( 1 );
                                leaves = stripped.Leaves();
                                leaves.Length.Should().Be( 1 );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestGenerateKey()
    {
        var sBoneA = new Bone( "2.1" , "Dim A" );
        var sBoneB = new Bone( "1.1" , "Dim B" );
        var sBoneC = new Bone( "1.1.1" , "Dim C" );

        var skeleton = new Skeleton( sBoneA , sBoneB , sBoneC );

        var listDim = Arr.create( "Dim C" , "Dim A" , "Dim B" );
        var key = skeleton.GenerateKey( listDim );
        key.Should().Be( "1.1.1:2.1:1.1" );
    }

    [Fact]
    public void TestComposingItems()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var mapped = new[]
        {
            new ComponentMapper( "Dim A:2.1|Dim B:1.1.1|Dim C:1.1.1" ) ,
            new ComponentMapper( "Dim A:2.1|Dim B:1.1.1|Dim C:2" )
        };

        dimA.Find( "2" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var composingItems = skeleton.GetComposingItems( mapped ).ToArray();

                                composingItems.Length.Should().Be( 1 );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestBuildComposingSkeletons()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var mapped = new[]
        {
            new ComponentMapper( "Dim A:2.1|Dim B:1.1.1|Dim C:1.1.1" , 42 ) ,
            new ComponentMapper( "Dim A:2.1|Dim B:1.1.1|Dim C:2" , 69 )
        };

        dimA.Find( "2" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var composing = skeleton.BuildComposingSkeletons( mapped , cm => cm.Value ).ToArray();
                                composing.Length.Should().Be( 1 );

                                var listDim = Arr.create( "Dim C" , "Dim A" , "Dim B" );
                                var key = composing[0].Key.GenerateKey( listDim );
                                key.Should().Be( "1.1.1:2.1:1.1.1" );
                                composing[0].Value.ShouldBeSome( v => v.Should().Be( 42 ) );

                                composing = skeleton.BuildComposingSkeletons( mapped , cm => cm.Value , false )
                                    .ToArray();
                                composing.Length.Should().Be( 1 );
                                key = composing[0].Key.GenerateKey( listDim );
                                key.Should().Be( "1.1.1:2.1:1.1.1" );
                                composing[0].Value.ShouldBeSome( v => v.Should().Be( 42 ) );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestComposingSkeletons()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var skels = Arr.create( dimA , dimB , dimC ).Combine();

        dimA.Find( "2" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var composing = skeleton.GetComposingSkeletons( new[] { dimA , dimB , dimC } , skels );
                                /* 5 in A * 4 in B * 2 in C = 40 */
                                composing.Length.Should().Be( 40 );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestComposingSkeletonsT()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var dimensions = Arr.create( dimA , dimB , dimC );

        var skels = dimensions.Combine().Select( s => new Skeleton<Unit>( s ) );

        dimA.Find( "2" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var composing = skeleton.GetComposingSkeletons( skels );
                                /* 5 in A * 4 in B * 2 in C = 40 */
                                composing.Length.Should().Be( 40 );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestEquals()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var skels = Arr.create( dimA , dimB , dimC ).Combine();

        dimA.Find( "2" )
            .ShouldBeSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .ShouldBeSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .ShouldBeSome( boneC =>
                            {
                                var skeletonA = new Skeleton( boneA , boneB , boneC );
                                var skeletonB = new Skeleton( boneC , boneA , boneB );
                                var skeletonC = new Skeleton( boneA , boneB );
                                var skeletonD = new Skeleton( boneA , boneB , new Bone( "1.1.1" , "Dim C" ) );

                                skeletonA.Should().BeRankedEquallyTo( skeletonB );
                                skeletonA.Should().NotBeRankedEquallyTo( skeletonC );
                                skeletonA.Should().NotBeRankedEquallyTo( skeletonD );

                                skeletonA.StripHierarchies().Should().BeEquivalentTo( skeletonD.StripHierarchies() );
                            } );
                    } );
            } );
    }

    [Fact]
    public void TestWeights()
    {
        var dimA = GetDimensionWithRedundantLabel( "Dim A" );
        var dimB = GetDimensionWithRedundantLabel( "Dim B" );
        var dimC = GetDimensionWithRedundantLabel( "Dim C" );

        var skels = Arr.create( dimA , dimB , dimC ).Combine().ToSeq();

        var items = skels.FindAll( "1" , "0" , "1" );
        items.Length.Should().Be( 2 );

        var root = skels.Find( "1" , "1" , "1" );
        root.ShouldBeSome( r =>
        {
            r.Descendants()
                .Find( "1" , "0" , "1" )
                .ShouldBeSome( s =>
                {
                    var other = items.First( s => !s.Root().Equals( r ) );
                    Skeleton.ComputeResultingWeight( s , r ).Should().Be( .5 );
                    Skeleton.ComputeResultingWeight( s , other ).Should().Be( 0 );
                } );

            r.Descendants()
                .Find( "1" , "0" , "0" )
                .ShouldBeSome( s => Skeleton.ComputeResultingWeight( s , r ).Should().Be( .25 ) );

            r.Descendants()
                .Find( "0" , "0" , "0" )
                .ShouldBeSome( s => Skeleton.ComputeResultingWeight( s , r ).Should().Be( .125 ) );
        } );
    }

    [Fact]
    public void TestFind()
    {
        var dimA = GetDimensionWithRedundantLabel( "Dim A" );
        var dimB = GetDimensionWithRedundantLabel( "Dim B" );
        var dimC = GetDimensionWithRedundantLabel( "Dim C" );
        var dimD = GetDimensionWithRedundantLabel( "Dim D" );

        var skels = Arr.create( dimA , dimB , dimC , dimD ).Combine().ToSeq();

        var items = skels.FindAll( new[] { "1" , "1" , "1" , "0" } , new[] { "2" , "1" , "1" , "0" } );
        items.Length.Should().Be( 4 );

        var items2 =
            skels.FindAll( new[] { ("1", "Dim A") , ("1", "Dim B") , ("1", "Dim C") , ("0", "Dim D") } ,
                new[] { ("2", "Dim A") , ("1", "Dim B") , ("1", "Dim C") , ("0", "Dim D") } );
        items2.Length.Should().Be( 4 );

        items.SequenceEqual( items2 ).Should().BeTrue();
    }

    [Fact]
    public void TestGetBone()
    {
        var boneA = new Bone( "A1" , "Dimension A" );
        var boneB = new Bone( "B1" , "Dimension B" );

        var skeleton = new Skeleton( boneB , boneA );

        skeleton.GetBone( 0 ).Should().BeSameAs( boneA );
        skeleton.GetBone( 2 ).Should().BeSameAs( Bone.None );
    }

    [Fact]
    public void TestCheckUse()
    {
        var dimensions = new[] { "Dim A" };
        var skeletons = AggregatorTests.GetLeavesSample( dimensions );
        var targets = AggregatorTests.GetTargets( dimensions );
        targets = Seq.create( targets.Find( "1" ) ).Somes();

        var checks = skeletons.CheckUse( targets );
        checks.Rights().Length.Should().Be( 4 );
        checks.Lefts().Length.Should().Be( 4 );

        dimensions = new[] { "Dim A" , "Dim B" , "Dim C" , "Dim D" , "Dim E" };
        skeletons = AggregatorTests.GetLeavesSample( dimensions );
        targets = AggregatorTests.GetTargets( dimensions );
        targets = Seq.create( targets.Find( "1" , "2" , "2" , "2" , "2" ) ,
                targets.Find( "2" , "2" , "2" , "2" , "2" ) )
            .Somes();

        checks = skeletons.CheckUse( targets );
        checks.Rights().Length.Should().Be( 2048 );
        checks.Lefts().Length.Should().Be( 30720 );
    }
}