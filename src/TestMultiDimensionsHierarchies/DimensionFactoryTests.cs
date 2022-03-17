using FluentAssertions;
using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System.Collections.Generic;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class DimensionFactoryTests
{
    private static IEnumerable<ParentHierarchyInput<int>> GetSingleParentSample()
    {
        yield return new ParentHierarchyInput<int> { Id = 1 , Label = "Root" };
    }

    private static IEnumerable<ChildHierarchyInput<int>> GetSingleChildSample()
    {
        yield return new ChildHierarchyInput<int> { Id = 1 , Label = "Root" };
    }

    private static IEnumerable<MultiChildrenHierarchyInput<int>> GetSingleMChildSample()
    {
        yield return new MultiChildrenHierarchyInput<int> { Id = 1 , Label = "Root" };
    }

    private static IEnumerable<ParentHierarchyInput<int>> GetSimpleParentSample()
    {
        yield return new ParentHierarchyInput<int> { Id = 1 , Label = "Root" };
        yield return new ParentHierarchyInput<int> { Id = 2 , Label = "Child 1" , ParentId = 1 };
        yield return new ParentHierarchyInput<int> { Id = 3 , Label = "Child 2" , ParentId = 1 };
    }

    private static IEnumerable<ChildHierarchyInput<int>> GetSimpleChildSample()
    {
        yield return new ChildHierarchyInput<int> { Id = 2 , Label = "Child 1" };
        yield return new ChildHierarchyInput<int> { Id = 3 , Label = "Child 2" };

        yield return new ChildHierarchyInput<int> { Id = 1 , Label = "Root" , ChildId = 2 };
        yield return new ChildHierarchyInput<int> { Id = 1 , Label = "Root" , ChildId = 3 };
    }

    private static IEnumerable<MultiChildrenHierarchyInput<int>> GetSimpleMChildSample()
    {
        yield return new MultiChildrenHierarchyInput<int> { Id = 2 , Label = "Child 1" };
        yield return new MultiChildrenHierarchyInput<int> { Id = 3 , Label = "Child 2" };

        yield return new MultiChildrenHierarchyInput<int> { Id = 1 , Label = "Root" , ChildrenIds = new[] { 2 , 3 } };
    }

    private static IEnumerable<ParentHierarchyInput<int>> GetFlatParentSample()
    {
        yield return new ParentHierarchyInput<int> { Id = 1 , Label = "Item 1" };
        yield return new ParentHierarchyInput<int> { Id = 2 , Label = "Item 2" };
        yield return new ParentHierarchyInput<int> { Id = 3 , Label = "Item 3" };
        yield return new ParentHierarchyInput<int> { Id = 4 , Label = "Item 4" };
    }

    private static IEnumerable<ParentHierarchyInput<int>> GetThreeLevelsParentSample()
    {
        yield return new ParentHierarchyInput<int> { Id = 1 , Label = "Root" };
        yield return new ParentHierarchyInput<int> { Id = 2 , Label = "Child 1" , ParentId = 1 };
        yield return new ParentHierarchyInput<int> { Id = 3 , Label = "Child 2" , ParentId = 1 };
        yield return new ParentHierarchyInput<int> { Id = 4 , Label = "Grand Child 1.1" , ParentId = 2 };
        yield return new ParentHierarchyInput<int> { Id = 5 , Label = "Grand Child 1.2" , ParentId = 2 };
        yield return new ParentHierarchyInput<int> { Id = 6 , Label = "Grand Child 2.1" , ParentId = 3 };
    }

    internal static IEnumerable<ParentHierarchyInput<string>> GetParentLinkHierarchy()
    {
        yield return new ParentHierarchyInput<string> { Id = "1" , Label = "1" }; // 1

        yield return new ParentHierarchyInput<string> { Id = "1.1" , Label = "1.1" , ParentId = "1" }; // 2
        yield return new ParentHierarchyInput<string> { Id = "1.1.1" , Label = "1.1.1" , ParentId = "1.1" }; // 3
        yield return new ParentHierarchyInput<string> { Id = "1.1.2" , Label = "1.1.2" , ParentId = "1.1" }; // 4
        yield return new ParentHierarchyInput<string> { Id = "1.1.1.1" , Label = "1.1.1.1" , ParentId = "1.1.1" }; // 4

        yield return new ParentHierarchyInput<string> { Id = "1.2" , Label = "1.2" , ParentId = "1" }; // 3
        yield return new ParentHierarchyInput<string> { Id = "1.2.1" , Label = "1.2.1" , ParentId = "1.2" }; // 4
        yield return new ParentHierarchyInput<string> { Id = "1.2.2" , Label = "1.2.2" , ParentId = "1.2" }; // 5

        yield return new ParentHierarchyInput<string> { Id = "2" , Label = "2" }; // 2

        yield return new ParentHierarchyInput<string> { Id = "2.1" , Label = "2.1" , ParentId = "2" }; // 3
        yield return new ParentHierarchyInput<string> { Id = "2.2" , Label = "2.2" , ParentId = "2" }; // 4
        yield return new ParentHierarchyInput<string> { Id = "2.3" , Label = "2.3" , ParentId = "2" }; // 5
        yield return new ParentHierarchyInput<string> { Id = "2.4" , Label = "2.4" , ParentId = "2" }; // 6
    }

    internal static IEnumerable<ChildHierarchyInput<string>> GetImplicitChildrenHierarchy()
    {
        yield return new ChildHierarchyInput<string> { Label = "ALL" , ChildId = "A" };
        yield return new ChildHierarchyInput<string> { Label = "ALL" , ChildId = "B" };

        yield return new ChildHierarchyInput<string> { Label = "A" , ChildId = "AA" };
        yield return new ChildHierarchyInput<string> { Label = "A" , ChildId = "AB" };

        yield return new ChildHierarchyInput<string> { Label = "B" , ChildId = "BA" };
    }

    internal static IEnumerable<ParentHierarchyInput<string>> GetImplicitParentHierarchy()
    {
        yield return new ParentHierarchyInput<string> { Label = "AA" , ParentId = "A" };
        yield return new ParentHierarchyInput<string> { Label = "BB" , ParentId = "B" };
    }

    [Fact]
    public void TestBuildWithParentLinkSimple()
    {
        var dimension = DimensionFactory.BuildWithParentLink(
            "Test dimension" ,
            GetSimpleParentSample() ,
            x => x.Id ,
            x => x.ParentId != 0 ? x.ParentId : Option<int>.None ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
    }

    [Fact]
    public void TestBuildWithChildLinkSimple()
    {
        var dimension = DimensionFactory.BuildWithChildLink(
            "Test dimension" ,
            GetSimpleChildSample() ,
            x => x.Id ,
             x => x.ChildId != 0 ? x.ChildId : Option<int>.None ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
    }

    [Fact]
    public void TestBuildWithMultiChildLinkSimple()
    {
        var dimension = DimensionFactory.BuildWithMultipleChildrenLink(
            "Test dimension" ,
            GetSimpleMChildSample() ,
            x => x.Id ,
            x => x.ChildrenIds ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
    }

    [Fact]
    public void TestBuildWithParentLinkSingle()
    {
        var dimension = DimensionFactory.BuildWithParentLink(
            "Test dimension" ,
            GetSingleParentSample() ,
            x => x.Id ,
            x => x.ParentId != 0 ? x.ParentId : Option<int>.None ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
    }

    [Fact]
    public void TestBuildWithChildLinkSingle()
    {
        var dimension = DimensionFactory.BuildWithChildLink(
            "Test dimension" ,
            GetSingleChildSample() ,
            x => x.Id ,
            x => x.ChildId != 0 ? x.ChildId : Option<int>.None ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
    }

    [Fact]
    public void TestBuildWithMultiChildLinkSingle()
    {
        var dimension = DimensionFactory.BuildWithMultipleChildrenLink(
            "Test dimension" ,
            GetSingleMChildSample() ,
            x => x.Id ,
            x => x.ChildrenIds ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
    }

    [Fact]
    public void TestBuildWithParentLinkFlat()
    {
        var dimension = DimensionFactory.BuildWithParentLink(
            "Test dimension" ,
            GetFlatParentSample() ,
            x => x.Id ,
            x => x.ParentId != 0 ? x.ParentId : Option<int>.None ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 4 );
    }

    [Fact]
    public void TestBuildWithParentLinkThreeLevels()
    {
        var dimension = DimensionFactory.BuildWithParentLink(
            "Test dimension" ,
            GetThreeLevelsParentSample() ,
            x => x.Id ,
            x => x.ParentId != 0 ? x.ParentId : Option<int>.None ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
        dimension.GetFlatList().Length.Should().Be( 6 );
    }

    [Fact]
    public void TestFullHierarchy()
    {
        var parentDimension = DimensionFactory.BuildWithParentLink(
            "Test dimension" ,
            GetParentLinkHierarchy() ,
            o => o.Id ,
            o => !string.IsNullOrEmpty( o.ParentId ) ? o.ParentId : Option<string?>.None ,
            o => o.Label );

        parentDimension.Frame.Length.Should().Be( 2 );
        parentDimension.GetFlatList().Length.Should().Be( 13 );
        parentDimension.GetLeaves().Length.Should().Be( 8 );
    }

    [Fact]
    public void TestImplicitChildren()
    {
        var dimension = DimensionFactory.BuildWithChildLink(
            "Test implicit" ,
            GetImplicitChildrenHierarchy() ,
            o => o.Label ,
            o => o.ChildId );

        dimension.Frame.Length.Should().Be( 1 );
        dimension.GetFlatList().Length.Should().Be( 6 );
        dimension.GetLeaves().Length.Should().Be( 3 );
    }

    [Fact]
    public void TestImplicitParent()
    {
        var dimension = DimensionFactory.BuildWithParentLink(
            "Test implicit" ,
            GetImplicitParentHierarchy() ,
            o => o.Label ,
            o => o.ParentId );

        dimension.Frame.Length.Should().Be( 2 );
        dimension.GetFlatList().Length.Should().Be( 4 );
    }
}