using FluentAssertions;
using LanguageExt.UnitTesting;
using MultiDimensionsHierarchies.Core;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class BoneTests
{
    private static Bone GetSingleBone()
        => new( "Singleton" , "Test dimension" );

    private static Bone GetSimpleBone()
    {
        var child1 = new Bone( "Child 1" , "Test dimension" );
        var child2 = new Bone( "Child 2" , "Test dimension" );

        var parent = new Bone( "Parent" , "Test dimension" , child1 , child2 );

        return parent;
    }

    private static Bone GetThreeLevelsBone()
    {
        var grandChild11 = new Bone( "Grand Child 1.1" , "Test dimension" );
        var grandChild12 = new Bone( "Grand Child 1.2" , "Test dimension" );
        var grandChild21 = new Bone( "Grand Child 2.1" , "Test dimension" );

        var child1 = new Bone( "Child 1" , "Test dimension" , grandChild11 , grandChild12 );
        var child2 = new Bone( "Child 2" , "Test dimension" , grandChild21 );

        var root = new Bone( "Root" , "Test dimension" , child1 , child2 );

        return root;
    }

    private static Bone GetThreeLevelsBoneWithWeight()
    {
        var grandChild11 = new Bone( "Grand Child 1.1" , "Test dimension" , .5 );
        var grandChild12 = new Bone( "Grand Child 1.2" , "Test dimension" );
        var grandChild21 = new Bone( "Grand Child 2.1" , "Test dimension" );

        var child1 = new Bone( "Child 1" , "Test dimension" , .9 , grandChild11 , grandChild12 );
        var child2 = new Bone( "Child 2" , "Test dimension" , grandChild21 );

        var root = new Bone( "Root" , "Test dimension" , child1 , child2 );

        return root;
    }

    [Fact]
    public void TestChildren()
    {
        var parent = GetSimpleBone();

        parent.HasChild().Should().BeTrue();
        parent.IsLeaf().Should().BeFalse();
        parent.Children.Length.Should().Be( 2 );
        parent.Children[0].HasChild().Should().BeFalse();
        parent.Children[0].IsLeaf().Should().BeTrue();

        var singleton = GetSingleBone();
        singleton.HasChild().Should().BeFalse();
        singleton.Children.Length.Should().Be( 0 );
        singleton.IsLeaf().Should().BeTrue();
    }

    [Fact]
    public void TestDescendants()
    {
        var parent = GetSimpleBone();
        parent.Descendants().Length.Should().Be( 3 );

        var root = GetThreeLevelsBone();
        root.Descendants().Length.Should().Be( 6 );
    }

    [Fact]
    public void TestAncestors()
    {
        var parent = GetSimpleBone();
        parent.Ancestors().Length.Should().Be( 1 );
        parent.Children[0].Ancestors().Length.Should().Be( 2 );

        var root = GetThreeLevelsBone();
        root.Children[0].Children[0].Ancestors().Length.Should().Be( 3 );
    }

    [Fact]
    public void TestLeaves()
    {
        var parent = GetSimpleBone();
        parent.Leaves().Length.Should().Be( 2 );
    }

    [Fact]
    public void TestRoot()
    {
        var root = GetThreeLevelsBone();

        root.Descendants().Find( b => b.Label.Equals( "Grand Child 1.2" ) )
            .ShouldBeSome( b => { b.Root().Should().BeSameAs( root ); } );
    }

    [Fact]
    public void TestWeights()
    {
        var root = GetThreeLevelsBoneWithWeight();

        var gc = root.Descendants().Find( b => b.Label.Equals( "Grand Child 1.1" ) );
        var c = root.Descendants().Find( b => b.Label.Equals( "Child 1" ) );
        gc.ShouldBeSome( bgc =>
        {
            bgc.ResultingWeight( bgc ).Should().Be( 1d );
            c.ShouldBeSome( bc =>
            {
                bgc.ResultingWeight( bc ).Should().Be( 0.5 );
                bc.ResultingWeight( root ).Should().Be( .9 );
                bgc.ResultingWeight( root ).Should().Be( 0.45 );
            } );
        } );
    }
}