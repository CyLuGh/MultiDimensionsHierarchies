namespace SampleGenerator;

public interface ISample
{
    int Value { get; }
    string? Get( string variable );
}