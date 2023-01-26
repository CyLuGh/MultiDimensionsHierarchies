using Bogus;
using LanguageExt;
using MoreLinq;
using MultiDimensionsHierarchies.Core;
using Newtonsoft.Json;

namespace SampleGenerator;

public class Generator
{
    private readonly int _sampleSize;
    private Seq<Sample> _samples;
    private Seq<Skeleton<int>> _skeletons;

    public Seq<Dimension> Dimensions { get; }

    public Generator( int sampleSize )
    {
        _sampleSize = sampleSize;
        Dimensions = CreateDimensions();
    }

    public Seq<Sample> Samples
    {
        get
        {
            if ( _samples.IsEmpty ) _samples = GenerateDataSample();

            return _samples;
        }
    }

    public Seq<Skeleton<int>> Skeletons
    {
        get
        {
            if ( _skeletons.IsEmpty )
                _skeletons = SkeletonFactory.FastBuild( Samples , ( o , s ) => o.Get( s ) , o => o.Value , Dimensions );

            return _skeletons;
        }
    }

    public IEnumerable<Skeleton> GenerateTargets()
    {
        var depths = Dimensions.Select( d =>
            d.Flatten().Select( b => b.Depth ).Distinct().OrderBy( x => x ).ToSeq().Strict() );
        
        var cartesians = depths.Aggregate<Seq<int> , IEnumerable<Seq<int>>>( new[] { Seq<int>.Empty } ,
                ( array , ds ) => array.Cartesian( ds , ( s , i ) => s.Add( i ) ) )
            .ToSeq();

        var test = cartesians
            .OrderBy( x => x.Sum() )
            .ThenBy( x => x[0] )
            .ThenBy( x => x[1] )
            .ThenBy( x => x[2] )
            .ThenBy( x => x[3] )
            .ThenBy( x => x[4] )
            .ThenBy( x => x[5] )
            .Take( 3 )
            .Select( x =>
            {
                return x.SelectMany( ( depth , i ) => Dimensions[i].Flatten().Where( o => o.Depth == depth ).Strict() ).ToSeq().Strict();
            } )
            .Combine()
            .ToSeq()
            .Strict();

        // var test = Dimensions.Select( d => d.Flatten().Strict() )
        //     .Aggregate<Seq<Bone> , IEnumerable<Seq<Bone>>>( new[] { new Seq<Bone>() } ,
        //         ( lists , bones ) => lists.Cartesian( bones , ( l , b ) => l.Add( b ) ) );
        yield return new Skeleton();
    }

    private Seq<Sample> GenerateDataSample()
    {
        return GenerateDataSample( Dimensions , _sampleSize );
    }

    private static Seq<Sample> GenerateDataSample( Seq<Dimension> dimensions , int sampleSize )
    {
        Randomizer.Seed = new Random( sampleSize ); // So that samples of the same size always generate the same data

        var leaves = HashMap.createRange( dimensions.Select( d =>
            ( d.Name , d.Leaves().Select( b => b.Label ).Distinct().OrderBy( x => x ).ToSeq().Strict() ) ) );

        var sampler = new Faker<Sample>().RuleFor( o => o.Consumer , f => f.PickRandom<string>( leaves["Consumers"] ) )
            .RuleFor( o => o.Producer , f => f.PickRandom<string>( leaves["Producers"] ) )
            .RuleFor( o => o.Cooking , f => f.PickRandom<string>( leaves["COOKING"] ) )
            .RuleFor( o => o.Shape , f => f.PickRandom<string>( leaves["SHAPE"] ) )
            .RuleFor( o => o.Mode , f => f.PickRandom<string>( leaves["MODE"] ) )
            .RuleFor( o => o.Sex , f => f.PickRandom<string>( leaves["SEX"] ) )
            .RuleFor( o => o.Value , f => f.Random.Int( min: 0 , max: 50 ) );

        return Seq.createRange( sampler.Generate( sampleSize ) );
    }

    public static Seq<Dimension> CreateDimensions()
    {
        var countriesInfo = GetCountriesInfo();
        var producerDimension = BuildGeoDimension( "Producers" , countriesInfo );
        var consumerDimension = BuildGeoDimension( "Consumers" , countriesInfo );
        var dimensions = BuildDimensions( GetDimensionInputs() );

        return Seq.create( dimensions[0] , consumerDimension , dimensions[1] , dimensions[2] , producerDimension ,
            dimensions[3] );
    }

    private static Seq<Dimension> BuildDimensions( Seq<DimensionInput> inputs )
    {
        return inputs.Select( input => DimensionFactory.BuildWithParentLink( input.Name , input.Members , x => x.Id ,
            x => x.ParentId.HasValue ? x.ParentId.Value : Option<int>.None , x => x.Name ) );
    }

    private static Dimension BuildGeoDimension( string name , Seq<CountryInfo> countries )
    {
        var raw = countries.Select( c => c.Global )
            .Distinct()
            .SelectMany( s =>
            {
                (string Label , Guid Id , Guid ParentId) global = ( Label: s , Id: Guid.NewGuid() ,
                    ParentId: Guid.Empty );
                var regions = BuildRegions( global.Id , countries.Where( c => c.Global == global.Label ) ).Strict();
                return regions.Add( global );
            } )
            .ToSeq();

        return DimensionFactory.BuildWithParentLink( name , raw , x => x.Id ,
            x => x.ParentId != Guid.Empty ? x.ParentId : Option<Guid>.None , x => x.Label );
    }

    private static Seq<(string Label , Guid Id , Guid ParentId)> BuildRegions( Guid globalGuid ,
        Seq<CountryInfo> countries )
    {
        return countries.Select( c => c.Region )
            .Distinct()
            .SelectMany( s =>
            {
                var region = ( Label: s , Id: Guid.NewGuid() , ParentId: globalGuid );
                var cnts = BuildCountries( region.Id , countries.Where( c => c.Region == region.Label ) ).Strict();

                return cnts.Add( region );
            } )
            .ToSeq();
    }

    private static Seq<(string Label , Guid Id , Guid ParentId)> BuildCountries( Guid parentGuid ,
        Seq<CountryInfo> countries )
    {
        return countries.Select( c => ( c.Code , Guid.NewGuid() , parentGuid ) );
    }

    private static Seq<CountryInfo> GetCountriesInfo()
    {
        var path = Path.Combine(
            Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ) ?? string.Empty ,
            "samples" , "countries.json" );

        if ( !File.Exists( path ) ) return Seq<CountryInfo>.Empty;

        using var rdr = new StreamReader( path );
        var json = rdr.ReadToEnd();
        return JsonConvert.DeserializeObject<Seq<CountryInfo>>( json );
    }

    private static Seq<DimensionInput> GetDimensionInputs()
    {
        var path = Path.Combine(
            Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ) ?? string.Empty ,
            "samples" , "dimensions.json" );

        if ( !File.Exists( path ) ) return Seq<DimensionInput>.Empty;

        using var rdr = new StreamReader( path );
        var json = rdr.ReadToEnd();
        return JsonConvert.DeserializeObject<Seq<DimensionInput>>( json );
    }
}