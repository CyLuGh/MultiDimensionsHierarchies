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

public class GenericSkeletonTests
{
    private static Dimension GetDimension( string dimensionName )
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

        var act = () => new Skeleton<double>( 12.7 , bone , boneA1 );
        act.Should().Throw<ArgumentException>()
            .WithMessage( "A bone should always define its dimension name!" );

        act = () => new Skeleton<double>( boneA1 , boneA2 );
        act.Should().Throw<ArgumentException>()
            .WithMessage( "A bone with the same dimension name has been defined more than once!" );
    }
}