using LanguageExt;
using System;
using System.Collections.Generic;

namespace MultiDimensionsHierarchies.Core
{
    public class Dimension
    {
        public string Name { get; }
        public Seq<Bone> Frame { get; }

        public Seq<Bone> GetFlatList()
        {
            var elements = FetchFlatList().Memo();
            return elements();
        }

        internal Dimension( string name , IEnumerable<Bone> bones )
        {
            Name = name;
            Frame = new Seq<Bone>( bones );
        }

        private Func<Seq<Bone>> FetchFlatList()
            => () => Frame.GetDescendants().ToSeq();
    }
}