// See https://aka.ms/new-console-template for more information
using Bogus;
using CsvHelper;
using CsvHelper.Configuration;
using Demo;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using Spectre.Console;
using System.Globalization;

AnsiConsole.Write( new FigletText( "MDH demo" ).LeftAligned() );

AnsiConsole.WriteLine( "Welcome to this small demo that will try to explain the purpose of this library." );
await Task.Delay( TimeSpan.FromSeconds( 1 ) );

if ( AnsiConsole.Confirm( "Would you like to see the effect of hierarchies on computation?" ) )
{
    AnsiConsole.Clear();
    AnsiConsole.WriteLine( "When aggregating elements along several hierarchies, the growth of node to be computed is exponential." );
    await Task.Delay( TimeSpan.FromSeconds( 1 ) );
    AnsiConsole.WriteLine( "Let's have a look at a sample to illustrate this." );

    var dim = AnsiConsole.Status()
        .Start( "Building dimension sample" , _ => DimensionFactory.BuildWithParentLink( "Sample" , GetParentLinkHierarchy() , o => o.Id , o => o.ParentId ) );

    AnsiConsole.WriteLine();
    DisplayDimension( dim );
    AnsiConsole.WriteLine();
    await Task.Delay( TimeSpan.FromSeconds( 1 ) );

    AnsiConsole.MarkupLine( "This dimension has [red]{0}[/] nodes. Let's see what it means if we had more of those dimensions..." , dim.Complexity );
    await Task.Delay( TimeSpan.FromSeconds( 1 ) );

    var dimensions = Enumerable.Range( 0 , 10 ).Select( _ => dim ).ToArray();

    var complexities = Enumerable.Range( 2 , 9 )
        .Select( count => (count, dimensions.Take( count ).Complexity()) )
        .ToArray();

    var table = new Table();
    table.AddColumn( "Dimensions count" ).AddColumn( "Maximum nodes" );
    foreach ( var (count, complexity) in complexities )
        table.AddRow( $"{count}" , $"{complexity:N0}" );

    AnsiConsole.Write( table );
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine( "The growth is indeed exponential. Manual iteration through these nodes becomes difficult and efficiency tends to disappear. This library aims at making the first easier while not failing too much at the second." );
    await Task.Delay( TimeSpan.FromSeconds( 3 ) );
    AnsiConsole.WriteLine();
    AnsiConsole.WriteLine( "Press enter to continue..." );
    Console.ReadLine();
}

AnsiConsole.Clear();
AnsiConsole.MarkupLine( "Let's take a more concrete sample." );
await Task.Delay( TimeSpan.FromSeconds( .5 ) );
AnsiConsole.MarkupLine( "Our sample will have three dimensions: countries, activity sectors and whether it was some gain or some loss." );
await Task.Delay( TimeSpan.FromSeconds( 1 ) );

var countriesDimension = AnsiConsole.Status()
    .Start( "Reading countries definition file" , ctx =>
    {
        var countries = ReadFile<Zone>( "samples/countries.csv" );
        ctx.Status = "Building countries hierarchy";
        return DimensionFactory.BuildWithParentLink( "Countries" , countries , o => o.GeoZone , o => o.ParentZone );
    } );

var sectorsDimension = AnsiConsole.Status()
    .Start( "Build sectors dimension" , _ => GetSectorDimension() );

var flowsDimension = GetFlowDimension();

var dimTable = new Table();
dimTable.AddColumn( "Countries" ).AddColumn( "Sectors" ).AddColumn( "Flows" );
dimTable.AddRow( BuildTree( countriesDimension ) , BuildTree( sectorsDimension ) , BuildTree( flowsDimension ) );

AnsiConsole.Clear();
AnsiConsole.Write( dimTable );
AnsiConsole.WriteLine();
var sampleDimensions = new[] { countriesDimension , sectorsDimension , flowsDimension };
AnsiConsole.MarkupLine( "Those three dimensions accounts for [red]{0}[/] nodes." , sampleDimensions.Complexity() );

await Task.Delay( TimeSpan.FromSeconds( 3 ) );

AnsiConsole.WriteLine();

var size = AnsiConsole.Prompt(
    new TextPrompt<int>( "How big should sample be?" )
    .ValidationErrorMessage( "This isn't a valid sample size." )
    .Validate( size => size switch
    {
        < 10 => Spectre.Console.ValidationResult.Error( "Sample size should be at least 10." ),
        > 10_000_000 => Spectre.Console.ValidationResult.Error( $"Sample size shouldn't exceed {10_000_000:N0} elements, just for patience sake." ),
        _ => Spectre.Console.ValidationResult.Success()
    } ) );

var sample = AnsiConsole.Status()
    .Start( "Generate sample data" , _ => GenerateSample( size ,
        countriesDimension.Flatten().Select( o => o.Label ).ToArray() ,
        sectorsDimension.Flatten().Select( o => o.Label ).ToArray() ) );

ShowSample( sample );

await Task.Delay( TimeSpan.FromSeconds( .5 ) );

var skeletons = AnsiConsole.Status()
    .Start( "Build skeletons" , _ => SkeletonFactory.BuildSkeletons( sample , Sample.Parser , s => s.Value , sampleDimensions ).ToArray() );

var heuristicAggregates = AnsiConsole.Status()
    .Start( "Compute all aggregates through Heuristic method" ,
        _ => Aggregator.Aggregate( Method.Heuristic , skeletons , ( a , b ) => a + b , vals => vals.Sum() , ( t , w ) => t * w ) );

AnsiConsole.Clear();

AnsiConsole.MarkupLine( "The computation took [orange3]{0}[/] seconds and created [orange3]{1}[/] results." , heuristicAggregates.Duration , heuristicAggregates.Results.Length );

CheckResults( sample , heuristicAggregates );

AnsiConsole.MarkupLine( "Let's compute those same keys through the Targeted method." );

var targets = sampleDimensions.Combine()
    .FindAll( new[] { ("WORLD", "Countries") , ("S1", "Sector") , ("Gain", "Flow") } ,
              new[] { ("WORLD", "Countries") , ("S1", "Sector") , ("Loss", "Flow") } ,
              new[] { ("WORLD", "Countries") , ("S1", "Sector") , ("Balance", "Flow") } ,
              new[] { ("WORLD", "Countries") , ("S2", "Sector") , ("Gain", "Flow") } ,
              new[] { ("WORLD", "Countries") , ("S2", "Sector") , ("Loss", "Flow") } ,
              new[] { ("WORLD", "Countries") , ("S2", "Sector") , ("Balance", "Flow") } );

var targetedAggregates = AnsiConsole.Status()
    .Start( "Compute all aggregates through Targeted method" ,
        _ => Aggregator.Aggregate( Method.Targeted , skeletons , ( a , b ) => a + b ,
        targets , vals => vals.Sum() , ( t , w ) => t * w ) );

AnsiConsole.MarkupLine( "The computation took [orange3]{0}[/] seconds and created [orange3]{1}[/] results." , targetedAggregates.Duration , targetedAggregates.Results.Length );

CheckResults( sample , targetedAggregates );

Console.ReadLine();

static void DisplayDimension( Dimension dim ) => AnsiConsole.Write( BuildTree( dim ) );

static Tree BuildTree( Dimension dim )
{
    var tree = new Tree( dim.Name );

    foreach ( var root in dim.Frame.OrderBy( x => x.Label ) )
    {
        var node = tree.AddNode( root.Label );
        foreach ( var child in root.Children.OrderBy( x => x.Label ) )
            AddNode( node , child );
    }

    return tree;
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

    return csv.GetRecords<T>().ToArray();
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

static Dimension GetFlowDimension()
{
    var gainBone = new Bone( "Gain" , "Flow" );
    var lossBone = new Bone( "Loss" , "Flow" , -1d );
    var balanceBone = new Bone( "Balance" , "Flow" , gainBone , lossBone );

    return DimensionFactory.BuildFromBones( "Flow" , balanceBone );
}

static Dimension GetSectorDimension()
{
    var s1 = new { Label = "S1" , Children = new[] { "S11" , "S12" , "S13" } };

    var s11 = new { Label = "S11" , Children = Array.Empty<string>() };
    var s12 = new { Label = "S12" , Children = new[] { "S121" , "S122" } };
    var s121 = new { Label = "S121" , Children = Array.Empty<string>() };
    var s122 = new { Label = "S122" , Children = Array.Empty<string>() };
    var s13 = new { Label = "S13" , Children = Array.Empty<string>() };

    var s2 = new { Label = "S2" , Children = new[] { "S21" , "S22" } };

    var s21 = new { Label = "S21" , Children = Array.Empty<string>() };
    var s22 = new { Label = "S22" , Children = Array.Empty<string>() };

    var items = new[] { s1 , s2 , s11 , s12 , s121 , s122 , s13 , s21 , s22 };

    return DimensionFactory.BuildWithMultipleChildrenLink( "Sector" , items , x => x.Label , x => x.Children );
}

static List<Sample> GenerateSample( int sampleSize , string[] countries , string[] sectors )
{
    Randomizer.Seed = new Random( sampleSize ); // So that samples of the same size always generate the same data
    var sampler = new Faker<Sample>()
        .RuleFor( o => o.Enterprise , f => f.Company.CompanyName() )
        .RuleFor( o => o.Country , f => f.PickRandom( countries ) )
        .RuleFor( o => o.Sector , f => f.PickRandom( sectors ) )
        .RuleFor( o => o.Flow , f => f.PickRandom<DataFlow>() )
        .RuleFor( o => o.Value , f => f.Random.Double( max: 500 ) );

    return sampler.Generate( sampleSize );
}

static void ShowSample( IEnumerable<Sample> sample )
{
    var table = new Table();
    table.AddColumn( "Enterprise" ).AddColumn( "Country" ).AddColumn( "Sector" ).AddColumn( "Flow" ).AddColumn( "Value" );
    foreach ( var s in sample.Take( 20 ) )
        table.AddRow( s.Enterprise , s.Country , s.Sector , s.Flow.ToString() , s.Value.ToString( "N2" ) );
    table.AddRow( "..." );

    AnsiConsole.WriteLine();
    AnsiConsole.Write( table );
    AnsiConsole.WriteLine();
}

static (double, double) CheckResult( AggregationResult<double> aggregationResult , IEnumerable<Sample> sample , string sector , DataFlow flow )
{
    var sumResult = sample.AsParallel().Where( s => s.Sector.StartsWith( sector ) && s.Flow == flow ).Sum( x => x.Value );
    var aggResult = aggregationResult.Results.Find( ("WORLD", "Countries") , (sector, "Sector") , (flow.ToString(), "Flow") )
        .Some( r => r.Value.Some( v => v ).None( () => double.NaN ) ).None( () => double.NaN );

    return (sumResult, aggResult);
}

static (double, double) CheckBalance( AggregationResult<double> aggregationResult , IEnumerable<Sample> sample , string sector )
{
    var sumResult = sample.AsParallel().Where( s => s.Sector.StartsWith( sector ) && s.Flow == DataFlow.Gain ).Sum( x => x.Value )
        - sample.AsParallel().Where( s => s.Sector.StartsWith( sector ) && s.Flow == DataFlow.Loss ).Sum( x => x.Value );
    var aggResult = aggregationResult.Results.Find( ("WORLD", "Countries") , (sector, "Sector") , ("Balance", "Flow") )
        .Some( r => r.Value.Some( v => v ).None( () => double.NaN ) ).None( () => double.NaN );

    return (sumResult, aggResult);
}

static void CheckResults( List<Sample> sample , AggregationResult<double> heuristicAggregates )
{
    var checks = new[] { "S1" , "S2" }
        .SelectMany( s => new[] { (s,DataFlow.Gain,CheckResult( heuristicAggregates , sample , s , DataFlow.Gain )) ,
            (s,DataFlow.Loss,CheckResult( heuristicAggregates , sample , s , DataFlow.Loss ) )} );

    var balances = new[] { "S1" , "S2" }
        .Select( s => (s, CheckBalance( heuristicAggregates , sample , s )) );

    var table = new Table();
    table.AddColumn( "Zone" ).AddColumn( "Sector" ).AddColumn( "Flow" ).AddColumn( "LINQ Sum" ).AddColumn( "Aggregate Result" ).AddColumn( "Difference" );

    foreach ( var (sector, flow, (linqRes, aggRes)) in checks )
        table.AddRow( "WORLD" , sector , flow.ToString() , linqRes.ToString() , aggRes.ToString() , ( linqRes - aggRes ).ToString() );

    foreach ( var (sector, (linqRes, aggRes)) in balances )
        table.AddRow( "WORLD" , sector , "Balance" , linqRes.ToString() , aggRes.ToString() , ( linqRes - aggRes ).ToString() );

    AnsiConsole.Write( table );
}