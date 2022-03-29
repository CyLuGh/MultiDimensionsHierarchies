using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class DimensionFactory
    {
        public static Dimension BuildFromBones( string dimensionName , params Bone[] bones )
        {
            return new( dimensionName , bones );
        }

        public static Dimension BuildWithParentLink<TA, TB>(
            string dimensionName ,
            IEnumerable<TA> items ,
            Func<TA , TB> keySelector ,
            Func<TA , Option<TB>> parentKeySelector ,
            Func<TA , string> labeller = null ,
            Func<TA , double> weighter = null
            )
        {
            labeller ??= o => keySelector( o ).ToString();
            weighter ??= _ => 1d;

            var seqItems = items
                .Select( item => (Key: keySelector( item ), Label: labeller( item ), ParentKey: parentKeySelector( item ), Weight: weighter( item )) )
                .ToSeq();

            var keys = HashSet.createRange( seqItems.Select( i => i.Key ) );
            foreach ( var key in seqItems.Select( i => i.ParentKey ) )
            {
                key.IfSome( k =>
                {
                    if ( !keys.Contains( k ) )
                        seqItems = seqItems.Add( (Key: k, Label: k.ToString(), ParentKey: Option<TB>.None, Weight: 1d) );
                } );
            }

            return Build(
                dimensionName ,
                seqItems ,
                x => x.Key ,
                x => x.ParentKey ,
                x => x.Label ,
                x => x.Weight
                );
        }

        public static Dimension BuildWithChildLink<TA, TB>(
            string dimensionName ,
            IEnumerable<TA> items ,
            Func<TA , TB> keySelector ,
            Func<TA , Option<TB>> childKeySelector ,
            Func<TA , string> labeller = null )
        {
            labeller ??= o => keySelector( o ).ToString();

            var seqItems = items
                .Select( item => (Key: keySelector( item ), Label: labeller( item ), ChildKey: childKeySelector( item )) )
                .ToSeq();

            var keys = HashSet.createRange( seqItems.Select( i => i.Key ) );
            foreach ( var key in seqItems.Select( i => i.ChildKey ) )
            {
                key.IfSome( k =>
                {
                    if ( !keys.Contains( k ) )
                        seqItems = seqItems.Add( (Key: k, Label: k.ToString(), ChildKey: Option<TB>.None) );
                } );
            }

            var multiChildItems = seqItems.GroupBy( item => (item.Key, item.Label) )
                .Select( g => (
                    g.Key.Key,
                    g.Key.Label,
                    ChildrenIds: g.Select( i => i.ChildKey
                            .MatchUnsafe( o => o , () => default ) )
                        .Where( o => o?.Equals( default( TB ) ) == false ).ToArray()) );

            return BuildWithMultipleChildrenLink(
                dimensionName ,
                multiChildItems ,
                o => o.Key ,
                o => o.ChildrenIds ,
                o => o.Label
           );
        }

        public static Dimension BuildWithMultipleChildrenLink<TA, TB>(
            string dimensionName ,
            IEnumerable<TA> items ,
            Func<TA , TB> keySelector ,
            Func<TA , IEnumerable<TB>> childrenKeysSelector ,
            Func<TA , string> labeller = null )
        {
            labeller ??= o => keySelector( o ).ToString();

            var seq = items.ToSeq();
            var parentLinks = seq.Select( o => (Key: keySelector( o ),
                Label: labeller( o ),
                ParentId: seq.Find( x => childrenKeysSelector( x ).Contains( keySelector( o ) ) )
                    .Some( s => Option<TB>.Some( keySelector( s ) ) )
                    .None( () => Option<TB>.None )
                ) );

            return BuildWithParentLink( dimensionName ,
                parentLinks ,
                o => o.Key ,
                o => o.ParentId ,
                o => o.Label ,
                _ => 1d );
        }

        private static Option<(TB Key, Bone Value)> BuildBone<TA, TB>(
            string dimensionName ,
            LanguageExt.HashSet<(TB Key, TA Item)> hashSet ,
            Seq<TA> items ,
            Option<TB> parentKey ,
            Func<TA , string> labeller ,
            Func<TA , double> weighter )
                => parentKey.Some( pk =>
                {
                    var childrenBones = items.Select( l => new Bone( labeller( l ) , dimensionName , weighter( l ) ) )
                        .ToArray();
                    return hashSet.Find( x => x.Key.Equals( pk ) )
                        .Some( parent => Option<(TB, Bone)>.Some( (pk, new Bone( labeller( parent.Item ) , dimensionName , weighter( parent.Item ) , childrenBones )) ) )
                        .None( () => Option<(TB, Bone)>.None );
                } ).None( () => Option<(TB, Bone)>.None );

        private static Option<(TB Key, Bone Value)> BuildBone<TA, TB>(
            string dimensionName ,
            LanguageExt.HashSet<(TB Key, TA Item)> hashSet ,
            Seq<Bone> childrenBones ,
            Option<TB> parentKey ,
            Func<TA , string> labeller ,
            Func<TA , double> weighter )
                => parentKey.Some( pk =>
                {
                    return hashSet.Find( x => x.Key.Equals( pk ) )
                        .Some( parent => Option<(TB, Bone)>.Some( (pk, new Bone( labeller( parent.Item ) , dimensionName , weighter( parent.Item ) , childrenBones.ToArray() )) ) )
                        .None( () => Option<(TB, Bone)>.None );
                } ).None( () => Option<(TB, Bone)>.None );

        private static Dimension Build<TA, TB>(
            string dimensionName ,
            IEnumerable<TA> items ,
            Func<TA , TB> keySelector ,
            Func<TA , Option<TB>> parentKeySelector ,
            Func<TA , string> labeller ,
            Func<TA , double> weighter = null
            )
        {
            weighter ??= _ => 1d;
            var results = HashMap.create<TB , Bone>();

            var test = items.Select( i => (keySelector( i ), i) ).Distinct().ToArray();

            var hashSet = HashSet.createRange( items.Select( i => (Key: keySelector( i ), Value: i) ) );
            var parentKeys = HashSet.createRange( hashSet.Select( x => parentKeySelector( x.Value ) ).Somes() );

            if ( parentKeys.Length == 0 ) /* This is a flat list */
            {
                foreach ( var (Key, Bone) in hashSet
                    .Select( x => (x.Key, Value: new Bone( labeller( x.Value ) , dimensionName )) ) )
                {
                    results = results.Add( Key , Bone );
                }
            }
            else
            {
                /* For leaves */
                var elements = hashSet.Where( x => !parentKeys.Contains( x.Key ) )
                    .Select( x => x.Value )
                    .ToArray();

                var createdIds = new Seq<TB>();

                foreach ( var group in elements.GroupBy( x => parentKeySelector( x ) ) )
                {
                    var res = BuildBone( dimensionName , hashSet , group.ToSeq() , group.Key , labeller , weighter );
                    results = res
                        .Some( r => results.AddOrUpdate( r.Key , r.Value ) )
                        .None( () => results );

                    createdIds = res.Some( r => createdIds.Add( r.Key ) )
                        .None( () => createdIds );
                }

                while ( createdIds.Length > 0 )
                {
                    elements = hashSet.Where( x => createdIds.Contains( x.Key ) )
                        .Select( x => x.Value )
                        .ToArray();

                    createdIds = new Seq<TB>();

                    foreach ( var group in elements.GroupBy( x => parentKeySelector( x ) )
                        .Where( g => g.Key.IsSome ) )
                    {
                        var keys = group.Select( x => keySelector( x ) ).ToSeq();
                        var seq = results.Where( x => keys.Contains( x.Key ) )
                            .Select( x => x.Value ).ToSeq();
                        var res = BuildBone( dimensionName , hashSet , seq , group.Key , labeller , weighter );
                        results = res.Some( r => results
                                .AddOrUpdate( r.Key ,
                                    existing => existing.With( children: existing.Children.Concat( r.Value.Children ) ) ,
                                    r.Value )
                                .RemoveRange( keys ) )
                            .None( () => results );

                        createdIds = res.Some( r => createdIds.Add( r.Key ) )
                            .None( () => createdIds );
                    }
                }
            }

            return new Dimension( dimensionName , results.Values.Where( x => x.Parent.IsNone ) );
        }
    }
}