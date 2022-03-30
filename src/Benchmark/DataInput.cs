﻿using System;

namespace Benchmark
{
    internal class DataInput
    {
        public string DimA { get; set; } = string.Empty;
        public string DimB { get; set; } = string.Empty;
        public string DimC { get; set; } = string.Empty;
        public string DimD { get; set; } = string.Empty;

        public double Value { get; set; }

        public static Func<DataInput , string , string> Parser =>
                 ( DataInput item , string dimension ) => dimension switch
                 {
                     "Dim A" => item.DimA,
                     "Dim B" => item.DimB,
                     "Dim C" => item.DimC,
                     "Dim D" => item.DimD,
                     _ => string.Empty
                 };
    }
}