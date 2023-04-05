namespace SampleGenerator;

public class SyntheticSample : ISample
{
    private readonly string[] _items;

    public int Value { get; set; }

    public SyntheticSample( string[] items )
    {
        _items = items;
    }

    public string? Get( string variable )
    {
        if ( int.TryParse( variable , out var position ) && position <= _items.Length )
            return _items[position - 1];

        return string.Empty;
    }
}