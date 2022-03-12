using FluentAssertions;
using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System.Collections.Generic;
using Xunit;

namespace TestMultiDimensionsHierarchies;

public class DimensionFactoryTests
{
    private IEnumerable<HierarchyInput> GetSimpleSample()
    {
        yield return new HierarchyInput { Id = 1 , Label = "Root" };
        yield return new HierarchyInput { Id = 2 , Label = "Child 1" , ParentId = 1 };
        yield return new HierarchyInput { Id = 3 , Label = "Child 2" , ParentId = 1 };
    }

    private IEnumerable<HierarchyInput> GetSingleSample()
    {
        yield return new HierarchyInput { Id = 1 , Label = "Root" };
    }

    private IEnumerable<HierarchyInput> GetFlatSample()
    {
        yield return new HierarchyInput { Id = 1 , Label = "Item 1" };
        yield return new HierarchyInput { Id = 2 , Label = "Item 2" };
        yield return new HierarchyInput { Id = 3 , Label = "Item 3" };
        yield return new HierarchyInput { Id = 4 , Label = "Item 4" };
    }

    private IEnumerable<HierarchyInput> GetThreeLevelsSample()
    {
        yield return new HierarchyInput { Id = 1 , Label = "Root" };
        yield return new HierarchyInput { Id = 2 , Label = "Child 1" , ParentId = 1 };
        yield return new HierarchyInput { Id = 3 , Label = "Child 2" , ParentId = 1 };
        yield return new HierarchyInput { Id = 4 , Label = "Grand Child 1.1" , ParentId = 2 };
        yield return new HierarchyInput { Id = 5 , Label = "Grand Child 1.2" , ParentId = 2 };
        yield return new HierarchyInput { Id = 6 , Label = "Grand Child 2.1" , ParentId = 3 };
    }

    [Fact]
    public void TestBuildWithParentLinkSimple()
    {
        var dimension = DimensionFactory.BuildWithParentLink(
            "Test dimension" ,
            GetSimpleSample() ,
            x => x.Id ,
            x => x.ParentId != 0 ? x.ParentId : Option<int>.None ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
    }

    [Fact]
    public void TestBuildWithParentLinkSingle()
    {
        var dimension = DimensionFactory.BuildWithParentLink(
            "Test dimension" ,
            GetSingleSample() ,
            x => x.Id ,
            x => x.ParentId != 0 ? x.ParentId : Option<int>.None ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
    }

    [Fact]
    public void TestBuildWithParentLinkFlat()
    {
        var dimension = DimensionFactory.BuildWithParentLink(
            "Test dimension" ,
            GetFlatSample() ,
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
            GetThreeLevelsSample() ,
            x => x.Id ,
            x => x.ParentId != 0 ? x.ParentId : Option<int>.None ,
            x => x.Label );

        dimension.Should().NotBeNull();
        dimension.Frame.Length.Should().Be( 1 );
        dimension.GetFlatList().Length.Should().Be( 6 );
    }
}