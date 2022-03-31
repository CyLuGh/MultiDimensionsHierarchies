namespace Benchmark
{
    public class ParentHierarchyInput<T>
    {
        public T Id { get; set; }
        public T ParentId { get; set; }
        public string Label { get; set; }

        public override string ToString()
            => $"{Id} {Label}";
    }
}