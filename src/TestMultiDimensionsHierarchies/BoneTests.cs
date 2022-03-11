using FluentAssertions;
using MultiDimensionsHierarchies.Core;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class BoneTests
{
    [Fact]
    public void TestChildren()
    {
        var child1 = new Bone( "Child 1" , "Test dimension" );
        var child2 = new Bone( "Child 2" , "Test dimension" );

        var parent = new Bone( "Parent" , "Test dimension" , child1 , child2 );

        parent.HasChild().Should().BeTrue();
        parent.Children.Length.Should().Be( 2 );
    }
}