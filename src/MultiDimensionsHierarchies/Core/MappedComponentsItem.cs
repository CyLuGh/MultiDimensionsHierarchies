using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public class MappedComponentsItem<T> : IMappedComponents<T>
    {
        public Option<T> Value { get; }
        public HashMap<string , string> Components { get; }

        public MappedComponentsItem( Option<T> value , HashMap<string , string> components )
        {
            Value = value;
            Components = components;
        }

        public MappedComponentsItem( IMappedComponents<T> mappedComponents ) : this( mappedComponents.Value , mappedComponents.Components )
        { }

        public MappedComponentsItem( IMappedComponents<T> mappedComponents , string[] exceptDimensions ) : this( mappedComponents )
        {
            for ( int i = 0 ; i < exceptDimensions.Length ; i++ )
            {
                Components = Components.Remove( exceptDimensions[i] );
            }
        }
    }

    internal static class MappedComponentsItemExtensions
    {
        internal static IEnumerable<MappedComponentsItem<T>> GroupAggregate<T>( this IEnumerable<MappedComponentsItem<T>> items , Func<IEnumerable<T> , T> groupAggregator )
            => items.GroupBy( i => i.Components )
                .Select( g => new MappedComponentsItem<T>( groupAggregator( items.Select( i => i.Value ).Somes() ) , g.Key ) );
    }
}