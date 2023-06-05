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

    [Fact]
    public void TestAggregatorAndAccumulater()
    {
        var generator = new Generator( 1000 , 4 );
        var targets = generator.GenerateTargets( 1 );
        var samples = generator.Skeletons;

        var agg = Aggregator.Aggregate( samples , targets , ( a , b ) => a + b , @is => @is.Sum( i => i ) );
        var detailed = Aggregator.DetailedAggregate( samples , targets , @is => @is.Sum( t => t.value ) , @is => @is.Sum( i => i ) );
        Assert.Equivalent( agg.Results[0].Value , detailed.Results[0].Value );

        detailed = Aggregator.DetailedAggregate( samples , targets , @is => @is.Sum( t => t.value ) , @is => @is.Sum( i => i ) , simplifyData: true );
        Assert.Equivalent( agg.Results[0].Value , detailed.Results[0].Value );
    }
}