// See https://aka.ms/new-console-template for more information
using CsvHelper;
using CsvHelper.Configuration;
using Demo;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using Spectre.Console;
using System.Globalization;

AnsiConsole.Write( new FigletText( "MDH demo" ).LeftAligned() );

var parser = ( Sample1Item item , string s )
    => s switch
    {
        "Dim A" => item.DimA,
        "Dim B" => item.DimB,
        "Dim C" => item.DimC,
        "Countries" => item.Country,
        _ => string.Empty
    };
var eval = ( Sample1Item item ) => item.Amount;

var (countriesDimension, sampleDimension, complexity, heuristicResult, targetedResult) = AnsiConsole.Status()
    .AutoRefresh( true )
    .Spinner( Spinner.Known.Dots )
    .Start( "Executing demo..." , ctx =>
    {
        ctx.Status = "Reading countries hierarchy input file";
        var countries = ReadFile<Zone>( "samples/countries.csv" );

        ctx.Status = "Building countries hierarchy";
        var countriesDimension = DimensionFactory.BuildWithParentLink( "Countries" , countries , o => o.GeoZone , o => o.ParentZone );

        ctx.Status = "Building other dimensions";
        var dimInputs = new[] { "Dim A" , "Dim B" , "Dim C" };
        var otherDimensions = dimInputs.Select( s => DimensionFactory.BuildWithParentLink( s , GetParentLinkHierarchy() , o => o.Id , o => o.ParentId ) ).ToArray();

        var dimensions = otherDimensions.Concat( new[] { countriesDimension } ).ToSeq();

        ctx.Status = "Computing complexity";
        var complexity = dimensions.Complexity();

        ctx.Status = "Reading data file";
        var sample1 = ReadFile<Sample1Item>( "samples/sample1.csv" );

        ctx.Status = "Building data infor";
        var skeletons = SkeletonFactory.BuildSkeletons( sample1 , parser , eval , dimensions );

        ctx.Status = "Computing aggregates with Heuristic method";
        var heuristicResult = Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b );

        ctx.Status = "Computing aggregates with Targeted method";
        var targets = dimensions.Combine().FindAll( ("1.1", "Dim A") , ("2.1", "Dim B") , ("2", "Dim C") , ("EU", "Countries") );
        var targetedResult = Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b , targets );

        return (countriesDimension, otherDimensions[0], complexity, heuristicResult, targetedResult);
    } );

if ( AnsiConsole.Confirm( "Would you like to see the hierarchies?" ) )
{
    DisplayDimension( countriesDimension );
    DisplayDimension( sampleDimension );
}

AnsiConsole.MarkupLine( "Those dimensions could lead up to [red bold]{0}[/] results." , complexity );


var hTable = new Table();
hTable.AddColumn( "Method" ).AddColumn( "Status" ).AddColumn( "Results" ).AddColumn( "Duration" );
hTable.AddRow( "Heuristic" , $"{heuristicResult.Status}" , $"{heuristicResult.Results.Length} results" , $"{heuristicResult.Duration}" );
foreach ( var r in heuristicResult.Results )
    hTable.AddRow( "" , "" , $"{r}" , "" );

AnsiConsole.Write( hTable );

var tTable = new Table();
tTable.AddColumn( "Method" ).AddColumn( "Status" ).AddColumn( "Results" ).AddColumn( "Duration" );
tTable.AddRow( "Targeted" , $"{targetedResult.Status}" , $"{targetedResult.Results.Length} results" , $"{targetedResult.Duration}" );
foreach ( var r in targetedResult.Results )
    tTable.AddRow( "" , "" , $"{r}" , "" );

AnsiConsole.Write( tTable );

static void DisplayDimension( Dimension dim )
{
    var tree = new Tree( dim.Name );

    foreach ( var root in dim.Frame.OrderBy( x => x.Label ) )
    {
        var node = tree.AddNode( root.Label );
        foreach ( var child in root.Children.OrderBy( x => x.Label ) )
            AddNode( node , child );
    }

    AnsiConsole.Write( tree );
}

static void AddNode( TreeNode node , Bone bone )
{
    var newNode = node.AddNode( bone.Label );
    foreach ( var child in bone.Children )
        AddNode( newNode , child );
}

static T[] ReadFile<T>( string path )
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
