using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class DimensionExtensions
    {
        public static long Complexity( this IEnumerable<Dimension> dimensions )
            => dimensions.Aggregate( 1L , ( prv , nxt ) => prv * nxt.Complexity );
    }
}
