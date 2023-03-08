using System.Collections.Generic;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using SampleGenerator;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace TestMultiDimensionsHierarchies;

public class SampleGeneratorTests
{
    [Fact]
    public void TestMapper()
    {
        var generator = new Generator( 1000 , 4 );
        var targets = LanguageExt.HashSet.createRange( generator.GenerateTargets( 100 ) );
        var samples = generator.Skeletons;
        var mappedSamples = generator.MappedComponents;

        samples.Length.Equals( mappedSamples.Length ).Should().BeTrue();

        var results = Aggregator.StreamAggregateResults( samples , targets , numbers => numbers.Sum() )
            .OrderBy( s => s.Key.FullPath )
            .ToSeq();

        var mapperResults = Aggregator.StreamAggregateResults<int , MappedComponentsItem<int>>( mappedSamples , targets , numbers => numbers.Sum() )
            .OrderBy( s => s.Key.FullPath )
            .ToSeq();

        results.SequenceEqual( mapperResults ).Should().Be( true );
    }
}