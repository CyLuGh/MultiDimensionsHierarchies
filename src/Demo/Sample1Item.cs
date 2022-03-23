namespace Demo;

public class Sample1Item
{
    public string? DimA { get; set; }
    public string? DimB { get; set; }
    public string? DimC { get; set; }
    public string? Country { get; set; }
    public double Amount { get; set; }

    public override string ToString()
        => string.Format( "Dim A: {0} Dim B: {1} Dim C: {2} Country: {3} Amount: {4:N2}" , DimA , DimB , DimC , Country , Amount );
}
