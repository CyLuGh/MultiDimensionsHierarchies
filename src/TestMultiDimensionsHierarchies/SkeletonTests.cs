using FluentAssertions;
using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class SkeletonTests
{
    internal static Dimension GetDimension( string dimensionName )
        => DimensionFactory.BuildWithParentLink(
            dimensionName ,
            DimensionFactoryTests.GetParentLinkHierarchy() ,
            x => x.Id ,
            o => !string.IsNullOrEmpty( o.ParentId ) ? o.ParentId : Option<string?>.None ,
            x => x.Label );

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

        var act = () => new Skeleton( bone , boneA1 );
        act.Should().Throw<ArgumentException>()
            .WithMessage( "A bone should always define its dimension name!" );

        act = () => new Skeleton( boneA1 , boneA2 );
        act.Should().Throw<ArgumentException>()
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
            .IfSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .IfSome( boneB =>
                    {
                        var skeleton = new Skeleton( boneA , boneB );

                        var ancestors = skeleton.GetAncestors();
                        ancestors.Length.Should().Be( 4 );
                    } );

                dimB.Find( "1.1.1" )
                    .IfSome( boneB =>
                    {
                        var skeleton = new Skeleton( boneA , boneB );

                        var ancestors = skeleton.GetAncestors();
                        ancestors.Length.Should().Be( 6 );

                        dimC.Find( "1.1.1.1" )
                            .IfSome( boneC =>
                            {
                                skeleton = new Skeleton( boneA , boneB , boneC );
                                ancestors = skeleton.GetAncestors();
                                ancestors.Length.Should().Be( 24 );
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

                        var root = skeleton.GetRoot();
                        root.Bones[0]
                            .ToString().Should().Be( dimA.Find( "2" ).Some( b => b.ToString() ).None( () => "" ) );

                        root.Bones[0].Should().BeSameAs(
                            dimA.Find( "2" ).Some( b => b ).None( () => Bone.None ) );
                    } );

                dimB.Find( "1.1.1" )
                    .IfSome( boneB =>
                    {
                        dimC.Find( "1.1.1.1" )
                            .IfSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var root = skeleton.GetRoot();

                                root.Bones[0].Should().BeSameAs(
                            dimA.Find( "2" ).Some( b => b ).None( () => Bone.None ) );
                                root.Bones[1].Should().BeSameAs(
                            dimB.Find( "1" ).Some( b => b ).None( () => Bone.None ) );
                                root.Bones[2].Should().BeSameAs(
                            dimC.Find( "1" ).Some( b => b ).None( () => Bone.None ) );
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
            .IfSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .IfSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .IfSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneC , boneA , boneB );
                                var leaves = skeleton.GetLeaves();

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
            .IfSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .IfSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .IfSome( boneC =>
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
            (Key: "Dim A", Values : new [] {"2","2.1"}),
            (Key: "Dim B", Values : new [] {"1.2","1.1"})
        }.ToDictionary( x => x.Key , x => x.Values );

        var dictionaryNoHit = new[]
        {
            (Key: "Dim A", Values : new [] {"2","2.1"}),
            (Key: "Dim B", Values : new [] {"2.2","2.1"})
        }.ToDictionary( x => x.Key , x => x.Values );

        dimA.Find( "2" )
            .IfSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .IfSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .IfSome( boneC =>
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
        skeleton.GetAncestors().Length.Should().Be( 1 );

        var updated = skeleton.Update( dimA );
        /* Dimension Dim A is updated with a hierarchy */
        updated.GetAncestors().Length.Should().Be( 2 );

        updated = skeleton.Update( dimA , dimB , dimC );
        var ancestors = updated.GetAncestors().ToArray();
        ancestors.Length.Should().Be( 12 );
    }

    [Fact]
    public void TestReplace()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        dimA.Find( "2.1" )
            .IfSome( boneA =>
            {
                dimB.Find( "1.1.1" )
                    .IfSome( boneB =>
                    {
                        dimC.Find( "1.1.1.1" )
                            .IfSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var ancestors = skeleton.GetAncestors();
                                ancestors.Length.Should().Be( 24 );

                                var replaced = skeleton.Replace( new Bone( "42" , "Dim C" ) );
                                ancestors = replaced.GetAncestors();
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
            .IfSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .IfSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .IfSome( boneC =>
                            {
                                var skeleton = new Skeleton( boneA , boneB , boneC );
                                var ancestors = skeleton.GetAncestors();
                                ancestors.Length.Should().Be( 6 );
                                var leaves = skeleton.GetLeaves();
                                leaves.Length.Should().Be( 8 );

                                var stripped = skeleton.StripHierarchies();
                                ancestors = stripped.GetAncestors();
                                ancestors.Length.Should().Be( 1 );
                                leaves = stripped.GetLeaves();
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
    public void TestComposingSkeletons()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var skels = Arr.create( dimA , dimB , dimC )
            .Combine();

        dimA.Find( "2" )
            .IfSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .IfSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .IfSome( boneC =>
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
    public void TestEquals()
    {
        var dimA = GetDimension( "Dim A" );
        var dimB = GetDimension( "Dim B" );
        var dimC = GetDimension( "Dim C" );

        var skels = Arr.create( dimA , dimB , dimC )
            .Combine();

        dimA.Find( "2" )
            .IfSome( boneA =>
            {
                dimB.Find( "1.1" )
                    .IfSome( boneB =>
                    {
                        dimC.Find( "1.1.1" )
                            .IfSome( boneC =>
                            {
                                var skeletonA = new Skeleton( boneA , boneB , boneC );
                                var skeletonB = new Skeleton( boneC , boneA , boneB );
                                var skeletonC = new Skeleton( boneA , boneB );
                                var skeletonD = new Skeleton( boneA , boneB , new Bone( "1.1.1" , "Dim C" ) );

                                skeletonA.Should().BeEquivalentTo( skeletonB );
                                skeletonA.Should().NotBeEquivalentTo( skeletonC );
                                skeletonA.Should().NotBeEquivalentTo( skeletonD );

                                skeletonA.StripHierarchies()
                                    .Should().BeEquivalentTo( skeletonD.StripHierarchies() );
                            } );
                    } );
            } );
    }
}