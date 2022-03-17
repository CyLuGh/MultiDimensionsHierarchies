using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public class Dimension
    {
        public string Name { get; }
        public Seq<Bone> Frame { get; }

        internal Dimension( string name , IEnumerable<Bone> bones )
        {
            Name = name;
            Frame = new Seq<Bone>( bones );
        }

        public Seq<Bone> Flatten()
        {
            var elements = FetchFlatList().Memo();
            return elements();
        }

        public Seq<Bone> GetLeaves()
        {
            var leaves = FetchLeaves().Memo();
            return leaves();
        }

        public Either<string , bool> Check()
            => Frame.Check();

        public Option<Bone> Find( string label )
            => Frame.Flatten().Find( b => b.Label.Equals( label ) );

        private Func<Seq<Bone>> FetchFlatList()
            => () => Frame.Flatten().ToSeq();

        private Func<Seq<Bone>> FetchLeaves()
            => () => Frame.SelectMany( d => d.GetLeaves() ).ToSeq();
    }
}