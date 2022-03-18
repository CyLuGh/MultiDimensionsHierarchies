// See https://aka.ms/new-console-template for more information
using CsvHelper;
using CsvHelper.Configuration;
using Demo;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using Spectre.Console;
using System.Globalization;

AnsiConsole.MarkupLine( "Welcome to [underline red]MultiDimensionsHierarchies[/] demo" );

//AnsiConsole.Write( new FigletText( "MultiDimensionsHierarchies demo" ).LeftAligned() );

var countries = ReadFile<Zone>( "samples/countries.csv" );
var countriesDimension = DimensionFactory.BuildWithParentLink( "Countries" , countries , o => o.GeoZone , o => o.ParentZone );
var dimensionA = DimensionFactory.BuildWithParentLink( "DimA" , GetParentLinkHierarchy() , o => o.Id , o => o.ParentId );
var dimensionB = DimensionFactory.BuildWithParentLink( "DimB" , GetParentLinkHierarchy() , o => o.Id , o => o.ParentId );

var sample1 = ReadFile<Sample1Item>( "samples/sample1.csv" );

var parser = ( Sample1Item item , string s )
    => s switch
    {
        "DimA" => item.DimA,
        "DimB" => item.DimB,
        "Countries" => item.Country,
        _ => string.Empty
    };

var eval = ( Sample1Item item ) => item.Amount;

var skeletons = SkeletonFactory.BuildSkeletons( sample1 , parser , eval , new[] { countriesDimension , dimensionA , dimensionB } );
var heuristicResult = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b );

AnsiConsole.WriteLine( heuristicResult.Duration.ToString() );

var targets = Seq.create(
    new Skeleton( Seq.create( dimensionA.Find( "1.1" ) , dimensionB.Find( "2.1" ) , countriesDimension.Find( "EU" ) ).Somes() )
    );
var targetedResult = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets );

AnsiConsole.WriteLine( targetedResult.Duration.ToString() );



T[] ReadFile<T>( string path )
{
    if ( string.IsNullOrWhiteSpace( path ) || !File.Exists( path ) )
        return Array.Empty<T>();

    var config = new CsvConfiguration( CultureInfo.InvariantCulture )
    {
        Delimiter = ";" ,
    };

    using var reader = new StreamReader( path );
    using var csv = new CsvReader( reader , config );

    var content = csv.GetRecords<T>().ToArray();
    return content;
}

static IEnumerable<ParentHierarchyInput<string>> GetParentLinkHierarchy()
{
    yield return new ParentHierarchyInput<string> { Id = "1" , Label = "1" }; // 1 - 26 - 17

    yield return new ParentHierarchyInput<string> { Id = "1.1" , Label = "1.1" , ParentId = "1" }; // 2 - 13 - 4
    yield return new ParentHierarchyInput<string> { Id = "1.1.1" , Label = "1.1.1" , ParentId = "1.1" }; // 3 - 7 - 4
    yield return new ParentHierarchyInput<string> { Id = "1.1.2" , Label = "1.1.2" , ParentId = "1.1" }; // 4
    yield return new ParentHierarchyInput<string> { Id = "1.1.1.1" , Label = "1.1.1.1" , ParentId = "1.1.1" }; // 4

    yield return new ParentHierarchyInput<string> { Id = "1.2" , Label = "1.2" , ParentId = "1" }; // 3 - 12 - 9
    yield return new ParentHierarchyInput<string> { Id = "1.2.1" , Label = "1.2.1" , ParentId = "1.2" }; // 4
    yield return new ParentHierarchyInput<string> { Id = "1.2.2" , Label = "1.2.2" , ParentId = "1.2" }; // 5

    yield return new ParentHierarchyInput<string> { Id = "2" , Label = "2" }; // 2 - 20 - 18

    yield return new ParentHierarchyInput<string> { Id = "2.1" , Label = "2.1" , ParentId = "2" }; // 3
    yield return new ParentHierarchyInput<string> { Id = "2.2" , Label = "2.2" , ParentId = "2" }; // 4
    yield return new ParentHierarchyInput<string> { Id = "2.3" , Label = "2.3" , ParentId = "2" }; // 5
    yield return new ParentHierarchyInput<string> { Id = "2.4" , Label = "2.4" , ParentId = "2" }; // 6
}
