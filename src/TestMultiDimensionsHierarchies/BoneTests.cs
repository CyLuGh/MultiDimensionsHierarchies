using FluentAssertions;
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
        parent.GetDescendants().Length.Should().Be( 3 );

        var root = GetThreeLevelsBone();
        root.GetDescendants().Length.Should().Be( 6 );
    }

    [Fact]
    public void TestAncestors()
    {
        var parent = GetSimpleBone();
        parent.GetAncestors().Length.Should().Be( 1 );
        parent.Children[0].GetAncestors().Length.Should().Be( 2 );

        var root = GetThreeLevelsBone();
        root.Children[0].Children[0].GetAncestors().Length.Should().Be( 3 );
    }

    [Fact]
    public void TestLeaves()
    {
        var parent = GetSimpleBone();
        parent.GetLeaves().Length.Should().Be( 2 );
    }

    [Fact]
    public void TestRoot()
    {
        var root = GetThreeLevelsBone();

        root.GetDescendants().Find( b => b.Label.Equals( "Grand Child 1.2" ) )
            .IfSome( b => { b.GetRoot().Should().BeSameAs( root ); } );
    }
}