using LanguageExt;
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

        public Dimension Rename( string name )
            => new( name , Frame.Select( b => b.With( dimensionName: name ) ) );

        public Seq<Bone> Flatten() => Frame.Flatten().ToSeq();

        public Seq<Bone> Leaves() => Frame.SelectMany( d => d.Leaves() ).ToSeq();

        public int Complexity => Frame.Sum( d => d.Complexity );

        public Either<string , bool> Check()
            => Frame.Check();

        public Option<Bone> Find( string label )
            => Frame.Flatten().Find( b => b.Label.Equals( label ) );

        public Seq<Bone> FindAll( string label )
            => Frame.Flatten().Where( b => b.Label.Equals( label ) );
    }
}