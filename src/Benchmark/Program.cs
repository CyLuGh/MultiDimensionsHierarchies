using Benchmark;
using BenchmarkDotNet.Running;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static class Program
{
    public static int Tries => 4;
    public static int Warmup => 1;

    public static void Main( string[] args )
    {
        BenchmarkRunner.Run<SampleBenchmark>();
        //BenchmarkRunner.Run<TargetedAggregate>();
        //BenchmarkRunner.Run<HeuristicAggregate>();
        // BenchmarkRunner.Run<SkelFactory>();

        //ManualBenchmark();
    }

    private static void ManualBenchmark()
    {
        var file = $"results_{Guid.NewGuid()}.log";

        Trace.Listeners.Add( new TextWriterTraceListener( Console.Out ) );
        Trace.Listeners.Add( new TextWriterTraceListener( file ) );
        Trace.AutoFlush = true;

        var heuristic = new HeuristicAggregate();
        var targeted = new TargetedAggregate();

        //if ( int.TryParse( args[0] , out var size )
        //    && int.TryParse( args[1] , out var dimension )
        //    && int.TryParse( args[2] , out var target ) )
        //{
        //    TestMethod( heuristic , "Group" , agg => agg.Group() , dimension , size );
        //    TestMethod( targeted , "Target" , agg => agg.Targeted() , dimension , size , target );
        //}

        var sizes = new[] { /*10_000 ,*/ 100_000 , 500_000 , 1_000_000 };
        var dimensions = new[] { 4 , 5 , 6 };
        var targets = new[] { 1_000 , 5_000 , 15_000 };

        foreach ( var dimension in dimensions )
        {
            foreach ( var size in sizes )
            {
                // TestMethod( heuristic , "Group" , agg => agg.Group() , dimension , size );
                // TestMethod( heuristic , "Group Cached" , agg => agg.GroupCache() , dimension , size );
                // TestMethod( heuristic , "Dictionary" , agg => agg.Dictionary() , dimension , size );
                // TestMethod( heuristic , "Dictionary Cached" , agg => agg.DictionaryCache() , dimension , size );

                foreach ( var target in targets )
                {
                    Configure( targeted , dimension , size , target );
                    if ( target > size || target > targeted.Targets.LongLength )
                        continue;

                    targeted.AggregationMethod = Method.TopDown;
                    TestMethod( targeted , "Top Down Target" , agg => agg.TargetedTopDown() , dimension , size , target );
                    targeted.AggregationMethod = Method.TopDownGroup;
                    TestMethod( targeted , "Top Down Target Group" , agg => agg.TargetedTopDown() , dimension , size , target );
                    // TestMethod( targeted , "Bottom Top Target" , agg => agg.BottomTop() , dimension , size , target );
                }
            }
        }
    }

    private static void Configure<T>( T agg , int dimensionCount , int sampleSize , int targetCount = 0 ) where T : AllMethodsAggregate
    {
        agg.DimensionsCount = dimensionCount;
        agg.SampleSize = sampleSize;

        if ( agg is TargetedAggregate targetedAggregate )
            targetedAggregate.TargetsCount = targetCount;

        agg.GlobalSetup();
    }

    private static void TestMethod<T, U>( T agg , string desc , Func<T , AggregationResult<U>> method , int dimensionCount , int sampleSize , int targetCount = 0 ) where T : AllMethodsAggregate
    {
        GC.Collect();

        Console.WriteLine( $"{agg} {desc} Dimensions: {dimensionCount} Sample size: {sampleSize} Targets count: {targetCount}" );

        Console.WriteLine( "Warming up..." );
        for ( int i = 0 ; i < Warmup ; i++ )
        {
            Configure( agg , dimensionCount , sampleSize , targetCount );
            var _ = method( agg );
            Console.WriteLine( _.Duration );
        }
        Console.WriteLine( "Warmed up!" );

        AggregationResult<U> results = null;
        var durations = new Queue<TimeSpan>();
        for ( int i = 0 ; i < Tries ; i++ )
        {
            Console.WriteLine( "Executing {0}" , i + 1 );
            Configure( agg , dimensionCount , sampleSize , targetCount );
            results = method( agg );
            durations.Enqueue( results.Duration );
            Console.WriteLine( results.Duration );
        }

        Console.WriteLine();

        Trace.WriteLine( $"{agg} {desc} Dimensions: {dimensionCount} Sample size: {sampleSize} Targets count: {targetCount}" );
        Trace.Indent();
        Trace.WriteLine( $"Complexity: {agg.Data.EstimateComplexity()}" );
        Trace.WriteLine( $"Average duration: {TimeSpan.FromTicks( (long) durations.Average( x => x.Ticks ) )} " +
            $"Results: {results?.Results.LongCount() ?? 0} " +
            $"Average per result: {( results?.Results.LongCount() ?? 0 ) / ( TimeSpan.FromTicks( (long) durations.Average( x => x.Ticks ) ) ).TotalSeconds}" );
        Trace.Unindent();

        Console.WriteLine();
    }

    private static void Test( HeuristicAggregate hA , TargetedAggregate tA , int dimensionCount , int sampleSize , int targetCount = 0 )
    {
        TestMethod( hA , "Group" , agg => agg.Group() , dimensionCount , sampleSize );
        TestMethod( hA , "Dictionary" , agg => agg.Dictionary() , dimensionCount , sampleSize );
        TestMethod( tA , "Target" , agg => agg.TargetedTopDown() , dimensionCount , sampleSize , targetCount );
        //TestMethod( tA , "Heuristic" , agg => agg.Heuristic() , dimensionCount , sampleSize , targetCount );
    }
}