namespace Demo
{
    public enum DataFlow { Gain, Loss }

    public class Sample
    {
        public string Enterprise { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public DataFlow Flow { get; set; }
        public double Value { get; set; }

        public static Func<Sample , string , string> Parser =>
             ( Sample item , string dimension ) => dimension switch
             {
                 "Countries" => item.Country,
                 "Sector" => item.Sector,
                 "Flow" => item.Flow.ToString(),
                 _ => string.Empty
             };
    }
}
