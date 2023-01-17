using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System.Linq;

namespace TestMultiDimensionsHierarchies;

public class ComponentMapper : IMappedComponents
{
    public HashMap<string , string> Components { get; }
    public int Value { get; }

    public ComponentMapper( string raw ) : this( raw , 0 )
    {
    }

    public ComponentMapper( string raw , int value )
    {
        Components = HashMap.createRange( raw.Split( "|" )
            .Select( s =>
            {
                var elements = s.Split( ":" );
                return (elements[0], elements[1]);
            } ) );
        Value = value;
    }
}