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
}