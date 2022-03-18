namespace Demo;

public class Sample1Item
{
    public string? DimA { get; set; }
    public string? DimB { get; set; }
    public string? Country { get; set; }
    public double Amount { get; set; }

    public override string ToString()
        => string.Format( "Dim A: {0} Dim B: {1} Country: {2} Amount: {3:N2}" , DimA , DimB , Country , Amount );
}
