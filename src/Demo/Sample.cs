namespace Demo
{
    public class Sample
    {
        public string? Producer { get; set; }
        public string? Consumer { get; set; }
        public string? Cooking { get; set; }
        public string? Shape { get; set; }
        public string? Mode { get; set; }
        public string? Sex { get; set; }
        public int Value { get; set; }

        public string Key => $"{Cooking}:{Consumer}:{Shape}:{Mode}:{Producer}:{Sex}";

        public override string ToString() => Key + " " + Value.ToString( "N2" );

        public string? Get( string variable ) =>
            variable switch
            {
                "Producers" => Producer ,
                "Consumers" => Consumer ,
                "COOKING" => Cooking ,
                "SHAPE" => Shape ,
                "MODE" => Mode ,
                "SEX" => Sex ,
                _ => string.Empty
            };
    }
}