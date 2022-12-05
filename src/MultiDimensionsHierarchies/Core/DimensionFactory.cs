using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class DimensionFactory
    {
        /// <summary>
        /// Build dimension from its components. Each component defines the key of its parent element.
        /// </summary>
        /// <typeparam name="TA">Type of source component</typeparam>
        /// <typeparam name="TB">Type of key identifying component in a unique way</typeparam>
        /// <param name="dimensionName">Name used for dimension</param>
        /// <param name="items">Items to be parsed</param>
        /// <param name="keySelector">How to get the item id</param>
        /// <param name="parentKeySelector">How to get the item parent (or an Option.None if no parent)</param>
        /// <param name="labeller">(Optional) How to create the label from the item</param>
        /// <param name="weighter">(Optional) How to determine weight from item in relation to its parent</param>
        /// <returns>Dimension with properly linked hierarchy items</returns>
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

        /// <summary>
        /// Build dimension from its components. Each component defines the key of one of its child elements.
        /// </summary>
        /// <typeparam name="TA">Type of source component</typeparam>
        /// <typeparam name="TB">Type of key identifying component in a unique way</typeparam>
        /// <param name="dimensionName">Name used for dimension</param>
        /// <param name="items">Items to be parsed</param>
        /// <param name="keySelector">How to get the item id</param>
        /// <param name="childKeySelector">How to get the item child (or an Option.None if no child)</param>
        /// <param name="labeller">(Optional) How to create the label from the item</param>
        /// <returns>Dimension with properly linked hierarchy items</returns>
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

        /// <summary>
        /// Build dimension from its components. Each component defines the keys of all its child elements.
        /// </summary>
        /// <typeparam name="TA">Type of source component</typeparam>
        /// <typeparam name="TB">Type of key identifying component in a unique way</typeparam>
        /// <param name="dimensionName">Name used for dimension</param>
        /// <param name="items">Items to be parsed</param>
        /// <param name="keySelector">How to get the item id</param>
        /// <param name="childrenKeysSelector">How to get the child items</param>
        /// <param name="labeller">(Optional) How to create the label from the item</param>
        /// <returns>Dimension with properly linked hierarchy items</returns>
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

                // Single root
                foreach ( var (Key, Bone) in hashSet.Where( x => !parentKeys.Contains( x.Key )
                    && parentKeySelector( x.Value ).IsNone )
                    .Select( x => (x.Key, Value: new Bone( labeller( x.Value ) , dimensionName )) ) )
                {
                    results = results.Add( Key , Bone );
                }

                foreach ( var group in elements.GroupBy( x => parentKeySelector( x ) )
                    .Where( g => g.Key.IsSome ) )
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