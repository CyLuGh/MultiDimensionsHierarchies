using Bogus;
using LanguageExt;
using MoreLinq;
using MultiDimensionsHierarchies.Core;
using Newtonsoft.Json;

namespace SampleGenerator;

public enum DimensionIdentifier { Countries = 1, Cooking = 2 }

public class Generator
{
    private readonly int _sampleSize;
    private readonly int _dimensionsCount;
    private readonly Option<DimensionIdentifier> _uniqueDimensionSynthetic;

    private Seq<ISample> _samples;
    private Seq<Skeleton<int>> _skeletons;
    private Seq<MappedComponentsItem<int>> _mappedComponents;

    public Seq<Dimension> Dimensions { get; }

    public Generator( int sampleSize , int dimensionsCount )
    {
        _sampleSize = sampleSize;
        _dimensionsCount = dimensionsCount;
        _uniqueDimensionSynthetic = Option<DimensionIdentifier>.None;
        Dimensions = CreateDimensions();
    }

    public Generator( int sampleSize , DimensionIdentifier identifier , int dimensionsCount )
    {
        _sampleSize = sampleSize;
        _dimensionsCount = dimensionsCount;
        _uniqueDimensionSynthetic = identifier;
        Dimensions = CreateDimensions( identifier , dimensionsCount );
    }

    public Seq<ISample> Samples
    {
        get
        {
            if ( _samples.IsEmpty ) _samples = GenerateDataSample();

            return _samples;
        }
    }

    public Seq<MappedComponentsItem<int>> MappedComponents
    {
        get
        {
            if ( _mappedComponents.IsEmpty )
            {
                var dimensions = Dimensions.Take( _dimensionsCount );
                _mappedComponents = Samples
                    .AsParallel()
                    .Select( s =>
                    {
                        var map = HashMap.createRange( dimensions.Select( d => (d.Name, s.Get( d.Name )) ) );
                        return new MappedComponentsItem<int>( s.Value , map );
                    } )
                    .ToSeq();
            }

            return _mappedComponents;
        }
    }

    public Seq<Skeleton<int>> Skeletons
    {
        get
        {
            if ( _skeletons.IsEmpty )
            {
                _skeletons = SkeletonFactory.FastBuild( Samples , ( o , s ) => o.Get( s ) , o => o.Value ,
                    Dimensions.Take( _dimensionsCount ) );
            }

            return _skeletons;
        }
    }

    public Seq<Skeleton> GenerateTargets( int size ) => ComposeTargets( size ).Take( size );

    private Seq<Skeleton> ComposeTargets( int size )
    {
        var parsed = Dimensions.Take( _dimensionsCount )
            .Select( ( d , i ) =>
            {
                var flat = d.Flatten();
                var depths = flat.Select( b => b.Depth ).Distinct().OrderBy( x => x ).ToSeq().Strict();
                var map = HashMap.createRange( flat.GroupBy( x => x.Depth )
                    .Select( g => (g.Key, g.ToSeq().Strict()) ) );

                return (Index: i, Depths: depths, Map: map);
            } )
            .ToSeq();

        var cartesians = parsed.OrderBy( t => t.Index )
            .Select( t => t.Depths )
            .Aggregate<Seq<int> , IEnumerable<Seq<int>>>( new[] { Seq<int>.Empty } ,
                ( array , ds ) => array.Cartesian( ds , ( s , i ) => s.Add( i ) ) )
            .ToSeq();

        var bonesMaps = HashMap.createRange( parsed.Select( t => (t.Index, t.Map) ) );

        var tgts = cartesians.OrderBy( x => x.Sum() );

        for ( int i = 0 ; i < _dimensionsCount ; i++ )
        {
            var idx = i;
            tgts = tgts.ThenBy( x => x[idx] );
        }

        var targets = tgts.ToSeq();

        var sampling = (int) Math.Round( targets.Count * .2 , MidpointRounding.ToPositiveInfinity );

        var sample = targets
            .Take( sampling )
            .AsParallel()
            .SelectMany( depths => depths.Select( ( d , i ) => bonesMaps[i][d] ).Combine() )
            .ToSeq();

        while ( sample.Length < size && sampling < targets.Count )
        {
            sampling = Math.Min( targets.Count ,
                sampling + (int) Math.Round( targets.Count * .1 , MidpointRounding.ToPositiveInfinity ) );
            sample = targets
                .Take( sampling )
                .AsParallel()
                .SelectMany( depths => depths.Select( ( d , i ) => bonesMaps[i][d] ).Combine() )
                .ToSeq();
        }

        return sample;
    }

    private Seq<ISample> GenerateDataSample()
    {
        return _uniqueDimensionSynthetic.IsSome
            ? GenerateUniqueDataSample( Dimensions , _sampleSize )
            : GenerateDataSample( Dimensions , _sampleSize );
    }

    private static Seq<ISample> GenerateUniqueDataSample( Seq<Dimension> dimensions , int sampleSize )
    {
        var leaves = dimensions[0].Leaves().Select( b => b.Label ).Distinct().OrderBy( x => x ).ToSeq().Strict();

        var sampler = new Faker<SyntheticSample>()
            .UseSeed( sampleSize ) // So that samples of the same size always generate the same data
            .CustomInstantiator( f =>
            {
                var items = Enumerable.Range( 0 , dimensions.Length )
                    .Select( _ => f.PickRandom<string>( leaves ) )
                    .ToArray();
                return new SyntheticSample( items );
            } );

        return Seq.createRange( sampler.Generate( sampleSize ) )
                .Cast<ISample>();
    }

    private static Seq<ISample> GenerateDataSample( Seq<Dimension> dimensions , int sampleSize )
    {
        var leaves = HashMap.createRange( dimensions.Select( d =>
            (d.Name, d.Leaves().Select( b => b.Label ).Distinct().OrderBy( x => x ).ToSeq().Strict()) ) );

        var sampler = new Faker<Sample>()
            .UseSeed( sampleSize ) // So that samples of the same size always generate the same data
            .RuleFor( o => o.Consumer , f => f.PickRandom<string>( leaves["Consumers"] ) )
            .RuleFor( o => o.Producer , f => f.PickRandom<string>( leaves["Producers"] ) )
            .RuleFor( o => o.Cooking , f => f.PickRandom<string>( leaves["COOKING"] ) )
            .RuleFor( o => o.Shape , f => f.PickRandom<string>( leaves["SHAPE"] ) )
            .RuleFor( o => o.Mode , f => f.PickRandom<string>( leaves["MODE"] ) )
            .RuleFor( o => o.Sex , f => f.PickRandom<string>( leaves["SEX"] ) )
            .RuleFor( o => o.Value , f => f.Random.Int( min: 0 , max: 50 ) );

        return Seq.createRange( sampler.Generate( sampleSize ) )
                .Cast<ISample>();
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

    public static Seq<Dimension> CreateDimensions( DimensionIdentifier identifier , int dimensionsCount )
    {
        switch ( identifier )
        {
            case DimensionIdentifier.Countries:
                var countriesInfo = GetCountriesInfo();
                return Enumerable.Range( 0 , dimensionsCount )
                    .Select( i => BuildGeoDimension( $"{i + 1}" , countriesInfo ) )
                    .ToSeq();

            case DimensionIdentifier.Cooking:
                var input = GetDimensionInputs().Find( o => o.Name?.Equals( "COOKING" , StringComparison.OrdinalIgnoreCase ) == true );
                return input
                    .Some( inp => Enumerable.Range( 0 , dimensionsCount )
                        .Select( i => DimensionFactory.BuildWithParentLink( $"{i + 1}" , inp.Members , x => x.Id ,
                            x => x.ParentId ?? Option<int>.None , x => x.Name ) )
                        .ToSeq() )
                    .None( () => Seq<Dimension>.Empty );

            default:
                return Seq<Dimension>.Empty;
        }
    }

    private static Seq<Dimension> BuildDimensions( Seq<DimensionInput> inputs )
    {
        return inputs.Select( input => DimensionFactory.BuildWithParentLink( input.Name , input.Members , x => x.Id ,
            x => x.ParentId ?? Option<int>.None , x => x.Name ) );
    }

    private static Dimension BuildGeoDimension( string name , Seq<CountryInfo> countries )
    {
        var world = (Label: "World", Id: Guid.NewGuid(), ParentId: Guid.Empty);

        var raw = countries.Select( c => c.Global )
            .Distinct()
            .SelectMany( s =>
            {
                (string Label, Guid Id, Guid ParentId) global = (Label: s, Id: Guid.NewGuid(),
                    ParentId: world.Id);
                var regions = BuildRegions( global.Id , countries.Where( c => c.Global == global.Label ) ).Strict();
                return regions.Add( global );
            } )
            .ToSeq()
            .Add( world );

        return DimensionFactory.BuildWithParentLink( name , raw , x => x.Id ,
            x => x.ParentId != Guid.Empty ? x.ParentId : Option<Guid>.None , x => x.Label );
    }

    private static Seq<(string Label, Guid Id, Guid ParentId)> BuildRegions( Guid globalGuid ,
        Seq<CountryInfo> countries )
    {
        return countries.Select( c => c.Region )
            .Distinct()
            .SelectMany( s =>
            {
                var region = (Label: s, Id: Guid.NewGuid(), ParentId: globalGuid);
                var cnts = BuildCountries( region.Id , countries.Where( c => c.Region == region.Label ) ).Strict();

                return cnts.Add( region );
            } )
            .ToSeq();
    }

    private static Seq<(string Label, Guid Id, Guid ParentId)> BuildCountries( Guid parentGuid ,
        Seq<CountryInfo> countries )
    {
        return countries.Select( c => (c.Code, Guid.NewGuid(), parentGuid) );
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