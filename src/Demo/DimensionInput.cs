namespace Demo;

public class DimensionInput
{
    public string? Name { get; set; }
    public DimensionMember[]? Members { get; set; }
}

public class DimensionMember
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double? Weight { get; set; }
    public int? ParentId { get; set; }
}