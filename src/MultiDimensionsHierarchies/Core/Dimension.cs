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

        public Seq<Bone> GetFlatList()
        {
            var elements = FetchFlatList().Memo();
            return elements();
        }

        public Seq<Bone> GetLeaves()
        {
            var leaves = FetchLeaves().Memo();
            return leaves();
        }

        private Func<Seq<Bone>> FetchFlatList()
            => () => Frame.GetDescendants().ToSeq();

        private Func<Seq<Bone>> FetchLeaves()
            => () => Frame.SelectMany( d => d.GetLeaves() ).ToSeq();
    }
}