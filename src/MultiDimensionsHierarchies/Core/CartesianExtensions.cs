using LanguageExt;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class CartesianExtensions
    {
        public static IEnumerable<TResult> Cartesian<TSource, TResult>(
            this IEnumerable<IEnumerable<TSource>> enumerables ,
            Func<IEnumerable<TSource> , TResult> resultSelector )
        {
            if ( enumerables == null ) throw new ArgumentNullException( nameof( enumerables ) );
            if ( resultSelector == null ) throw new ArgumentNullException( nameof( resultSelector ) );

#pragma warning disable RCS1227 // Validate arguments correctly. => makes Unit tests fail if applied
            var enumerators = enumerables
                .Select( e => e?.GetEnumerator() ?? throw new ArgumentException( "One of the enumerables is null" ) )
                .Pipe( e => e.MoveNext() )
                .ToArray();
#pragma warning restore RCS1227 // Validate arguments correctly.

            do yield return resultSelector( enumerators.Select( e => e.Current ) ); while ( MoveNext() );

            foreach ( var enumerator in enumerators ) enumerator.Dispose();

            bool MoveNext()
            {
                for ( var i = enumerators.Length - 1 ; i >= 0 ; i-- )
                {
                    if ( enumerators[i].MoveNext() ) return true;
                    if ( i != 0 )
                    {
                        enumerators[i].Reset();
                        enumerators[i].MoveNext();
                    }
                }

                return false;
            }
        }
    }
}