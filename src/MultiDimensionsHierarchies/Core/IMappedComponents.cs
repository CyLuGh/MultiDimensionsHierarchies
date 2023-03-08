using LanguageExt;

namespace MultiDimensionsHierarchies.Core
{
    public interface IMappedComponents
    {
        HashMap<string , string> Components { get; }
    }

    public interface IMappedComponents<T> : IMappedComponents
    {
        Option<T> Value { get; }
    }
}