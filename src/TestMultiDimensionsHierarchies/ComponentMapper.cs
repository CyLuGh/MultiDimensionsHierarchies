using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System.Linq;

namespace TestMultiDimensionsHierarchies;

public class ComponentMapper : IMappedComponents
{
    public HashMap<string , string> Components { get; }

    public ComponentMapper( string raw )
    {
        Components = HashMap.createRange( raw.Split( "|" )
            .Select( s =>
            {
                var elements = s.Split( ":" );
                return ( elements[0] , elements[1] );
            } ) );
    }
}