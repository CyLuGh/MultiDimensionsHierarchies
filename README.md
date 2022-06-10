# MultiDimensionsHierarchies

[![NuGet](https://raw.githubusercontent.com/NuGet/Media/main/Images/MainLogo/32x32/nuget_32.png)](https://www.nuget.org/packages/MultiDimensionsHierarchies/) [![CodeQL](https://github.com/CyLuGh/MultiDimensionsHierarchies/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/CyLuGh/MultiDimensionsHierarchies/actions/workflows/codeql-analysis.yml) [![CodeFactor](https://www.codefactor.io/repository/github/cylugh/multidimensionshierarchies/badge)](https://www.codefactor.io/repository/github/cylugh/multidimensionshierarchies) 

It is quite easy to do some aggregates along a single hierarchy through recursive methods. It is still easy enough with two hierarchies. But what happens when there are *n* hierarchies to iterate through? This library tries to bring an easy answer to this problem, even if it has some limitations: this won't replace a true data cube solution.

## Dimensions

The first step to solve our problem is to define the various hierarchies and the relationships between their components.

### Bones

This class represents an element in the hierarchy, with links to its parent or children if they exist. All the *Bone*s of a dimension make its *Frame*.

### DimensionFactory

*Dimension*s can't be directly created. The *DimensionFactory* class offers three methods to create a dimension.

```csharp
/// <summary>
/// Build dimension from its components. Each component defines the key of its parent element.
/// </summary>
/// <typeparam name="TA">Type of source component</typeparam>
/// <typeparam name="TB">Type of key identifying component in a unique way</typeparam>
/// <param name="dimensionName">Name used for dimension</param>
/// <param name="items">Items to be parsed</param>
/// <param name="keySelector">How to get the item id</param>
/// <param name="parentKeySelector">How to get the item parent (or an Option.None if no parent)</param>
/// <param name="labeller">(Optional) How to create the label from the item</param>
/// <param name="weighter">(Optional) How to determine weight from item in relation to its parent</param>
/// <returns>Dimension with properly linked hierarchy items</returns>
public static Dimension BuildWithParentLink<TA, TB>(
    string dimensionName ,
    IEnumerable<TA> items ,
    Func<TA , TB> keySelector ,
    Func<TA , Option<TB>> parentKeySelector ,
    Func<TA , string> labeller = null ,
    Func<TA , double> weighter = null
    )

/// <summary>
/// Build dimension from its components. Each component defines the key of one of its child elements.
/// </summary>
/// <typeparam name="TA">Type of source component</typeparam>
/// <typeparam name="TB">Type of key identifying component in a unique way</typeparam>
/// <param name="dimensionName">Name used for dimension</param>
/// <param name="items">Items to be parsed</param>
/// <param name="keySelector">How to get the item id</param>
/// <param name="childKeySelector">How to get the item child (or an Option.None if no child)</param>
/// <param name="labeller">(Optional) How to create the label from the item</param>
/// <returns>Dimension with properly linked hierarchy items</returns>
public static Dimension BuildWithChildLink<TA, TB>(
    string dimensionName ,
    IEnumerable<TA> items ,
    Func<TA , TB> keySelector ,
    Func<TA , Option<TB>> childKeySelector ,
    Func<TA , string> labeller = null )

/// <summary>
/// Build dimension from its components. Each component defines the keys of all its child elements.
/// </summary>
/// <typeparam name="TA">Type of source component</typeparam>
/// <typeparam name="TB">Type of key identifying component in a unique way</typeparam>
/// <param name="dimensionName">Name used for dimension</param>
/// <param name="items">Items to be parsed</param>
/// <param name="keySelector">How to get the item id</param>
/// <param name="childrenKeysSelector">How to get the child items</param>
/// <param name="labeller">(Optional) How to create the label from the item</param>
/// <returns>Dimension with properly linked hierarchy items</returns>
public static Dimension BuildWithMultipleChildrenLink<TA, TB>(
    string dimensionName ,
    IEnumerable<TA> items ,
    Func<TA , TB> keySelector ,
    Func<TA , IEnumerable<TB>> childrenKeysSelector ,
    Func<TA , string> labeller = null )
```

## Skeletons

A *Skeleton*, as a collection of **n** *Bone*s, defines an entry in **n** *Dimension*s. The generic type *Skeleton\<T\>* associates an entry and a value of type T.

### SkeletonFactory
