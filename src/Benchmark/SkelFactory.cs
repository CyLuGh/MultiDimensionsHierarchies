using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [/*SimpleJob( RuntimeMoniker.Net472 , iterationCount: 3 , warmupCount: 1 ),*/
    //  SimpleJob( RuntimeMoniker.Net60 , iterationCount: 3 , warmupCount: 1 ),
     SimpleJob( RuntimeMoniker.Net70 , iterationCount: 3 , warmupCount: 1 )]
    [MemoryDiagnoser( false )]
    public class SkelFactory
    {
        [Params( 100_000 , 500_000 , 1_000_000 , 2_500_000 )]
        public int SampleSize { get; set; }

        protected readonly Dimension dimA;
        protected readonly Dimension dimB;
        protected readonly Dimension dimC;
        protected readonly Dimension dimD;
        protected readonly Dimension dimE;
        protected readonly Dimension dimF;
        protected readonly Dimension dimG;

        public Dimension[] Dimensions => new[] { dimA , dimB , dimC , dimD , dimE , dimF , dimG };

        internal DataInput[] Sample { get; set; }

        public SkelFactory()
        {
            dimA = DimensionFactory.BuildWithParentLink( "Dim A" , AllMethodsAggregate.BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimB = DimensionFactory.BuildWithParentLink( "Dim B" , AllMethodsAggregate.BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimC = DimensionFactory.BuildWithParentLink( "Dim C" , AllMethodsAggregate.BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimD = DimensionFactory.BuildWithParentLink( "Dim D" , AllMethodsAggregate.BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimE = DimensionFactory.BuildWithParentLink( "Dim E" , AllMethodsAggregate.BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimF = DimensionFactory.BuildWithParentLink( "Dim F" , AllMethodsAggregate.BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimG = DimensionFactory.BuildWithParentLink( "Dim G" , AllMethodsAggregate.BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            Sample = AllMethodsAggregate.BuildSample( dimA.Flatten().Select( b => b.Label ).Distinct().ToArray() , SampleSize );
        }

        [Benchmark]
        public List<Skeleton<double>> FastBuild()
        {
            return SkeletonFactory.FastBuild(
                Sample.ToSeq() ,
                DataInput.Parser ,
                x => x.Value ,
                Dimensions.ToSeq() )
                .ToList();
        }

        [Benchmark]
        public List<Skeleton<double>> BuildSkeletons()
        {
            return SkeletonFactory.BuildSkeletons(
                Sample.ToSeq() ,
                DataInput.Parser ,
                x => x.Value ,
                Dimensions.ToSeq() )
                .ToList();
        }

        [Benchmark]
        public List<Skeleton<double>> TryBuildSkeletons()
        {
            return SkeletonFactory.TryBuildSkeletons(
                Sample.ToSeq() ,
                DataInput.Parser ,
                x => x.Value ,
                Dimensions.ToSeq() )
                .Rights()
                .ToList();
        }
    }
}