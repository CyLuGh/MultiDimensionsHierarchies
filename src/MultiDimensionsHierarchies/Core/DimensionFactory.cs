using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public class DimensionFactory
    {
        public static Dimension BuildWithParentLink<TA, TB>(
            string dimensionName ,
            IEnumerable<TA> items ,
            Func<TA , TB> keySelector ,
            Func<TA , Option<TB>> parentKeySelector ,
            Func<TA , string> labeller
            )
        {
            var results = HashMap.create<TB , Bone>();

            /* Put all items in a map */
            var hashMap = HashMap.createRange( items.Select( i => (keySelector( i ), i) ) );

            var parentKeys = new LanguageExt.HashSet<Option<TB>>( hashMap.Values.Select( x => parentKeySelector( x ) )
                .Where( o => o.IsSome ) );

            if ( parentKeys.Length == 0 ) /* This is a flat list */
            {
                foreach ( var bone in hashMap
                    .Select( x => (x.Key, Value: new Bone( labeller( x.Value ) , dimensionName )) ) )
                {
                    results = results.Add( bone.Key , bone.Value );
                }
            }
            else
            {
                /* For leaves */
                var elements = hashMap.Where( x => !parentKeys.Contains( x.Key ) )
                    .Select( x => x.Value )
                    .ToArray();

                var createdIds = new Seq<TB>();

                foreach ( var group in elements.GroupBy( x => parentKeySelector( x ) )
                    .Where( g => g.Key.IsSome ) )
                {
                    var res = BuildBone( dimensionName , hashMap , group.ToSeq() , group.Key , labeller );
                    results = res.Some( r => results.AddOrUpdate( r.Key , r.Value ) )
                        .None( () => results );

                    createdIds = res.Some( r => createdIds.Add( r.Key ) )
                        .None( () => createdIds );
                }

                while ( createdIds.Length > 0 )
                {
                    elements = hashMap.Where( x => createdIds.Contains( x.Key ) )
                        .Select( x => x.Value )
                        .ToArray();

                    createdIds = new Seq<TB>();

                    foreach ( var group in elements.GroupBy( x => parentKeySelector( x ) )
                        .Where( g => g.Key.IsSome ) )
                    {
                        var keys = group.Select( x => keySelector( x ) ).ToSeq();
                        var seq = results.Where( x => keys.Contains( x.Key ) )
                            .Select( x => x.Value ).ToSeq();
                        var res = BuildBone( dimensionName , hashMap , seq , group.Key , labeller );
                        results = res.Some( r => results
                                .AddOrUpdate( r.Key , r.Value )
                                .RemoveRange( keys ) )
                            .None( () => results );

                        createdIds = res.Some( r => createdIds.Add( r.Key ) )
                            .None( () => createdIds );
                    }
                }
            }

            return new Dimension( dimensionName , results.Values.Where( x => x.Parent.IsNone ) );
        }

        private static Option<(TB Key, Bone Value)> BuildBone<TA, TB>(
            string dimensionName ,
            HashMap<TB , TA> hashMap ,
            Seq<TA> items ,
            Option<TB> parentKey ,
            Func<TA , string> labeller )
                => parentKey.Some( pk =>
                {
                    var childrenBones = items.Select( l => new Bone( labeller( l ) , dimensionName ) )
                        .ToArray();
                    var parent = hashMap[pk];
                    return Option<(TB, Bone)>.Some( (pk, new Bone( labeller( parent ) , dimensionName , childrenBones )) );
                } ).None( () => Option<(TB, Bone)>.None );

        private static Option<(TB Key, Bone Value)> BuildBone<TA, TB>(
            string dimensionName ,
            HashMap<TB , TA> hashMap ,
            Seq<Bone> childrenBones ,
            Option<TB> parentKey ,
            Func<TA , string> labeller )
                => parentKey.Some( pk =>
                {
                    var parent = hashMap[pk];
                    return Option<(TB, Bone)>.Some( (pk, new Bone( labeller( parent ) , dimensionName , childrenBones.ToArray() )) );
                } ).None( () => Option<(TB, Bone)>.None );
    }
}