using System;

namespace Benchmark
{
    public abstract class HierarchyInput<T>
    {
        public T Id { get; set; }
        public string Label { get; set; }

        public override string ToString()
            => $"{Id} {Label}";
    }

    public class ParentHierarchyInput<T> : HierarchyInput<T>
    {
        public T ParentId { get; set; }
    }

    public class ChildHierarchyInput<T> : HierarchyInput<T>
    {
        public T ChildId { get; set; }
    }

    public class MultiChildrenHierarchyInput<T> : HierarchyInput<T>
    {
        public T[] ChildrenIds { get; set; }
            = Array.Empty<T>();
    }
}