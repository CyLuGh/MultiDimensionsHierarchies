# MultiDimensionsHierarchies

[![NuGet](https://raw.githubusercontent.com/NuGet/Media/main/Images/MainLogo/32x32/nuget_32.png)](https://www.nuget.org/packages/MultiDimensionsHierarchies/) [![CodeQL](https://github.com/CyLuGh/MultiDimensionsHierarchies/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/CyLuGh/MultiDimensionsHierarchies/actions/workflows/codeql-analysis.yml) [![CodeFactor](https://www.codefactor.io/repository/github/cylugh/multidimensionshierarchies/badge)](https://www.codefactor.io/repository/github/cylugh/multidimensionshierarchies) 

It is quite easy to do some aggregates along a single hierarchy through recursive methods. It is still easy enough with two hierarchies. But what happens when there are *n* hierarchies to iterate through? This library tries to bring an answer to this problem, even if it has some limitations: this won't replace a true data cube solution.

The library can also keep track of data contribution, which may then be used to compute extra information, such as primary confidentiality.

*This library is still a work in progress, even if already used in several working applications. The results are correct but there's always room for improvements. A cleanup might occur soon, as it could make sense keeping around slower methods, but the API shouldn't change.*

## Dimensions

The first step to solve our problem is to define the various hierarchies and the relationships between their components.

### Bones

This class represents an element in the hierarchy, with links to its parent or children if they exist. All the *Bone*s of a dimension make its *Frame*.

#### Example

All along these examples, we'll talk about fries. We could imagine several dimensions about them: where the potatoes have been grown, where the fries have been eaten, how they were cooked, which shape they were cut into, whether they were fresh or frozen...

Dimension ***GEO*** could be represented as such, with each country being a *Bone*.

```mermaid
flowchart TD
World --> Europe
Europe --> Benelux
Europe --> France
Europe --> Germany
Europe --> Italy
Europe --> Spain
Europe --> EOthers[...]
Benelux --> Belgium
Benelux --> Netherlands
Benelux --> Luxemburg
World --> Asia
Asia --> China
Asia --> Japan
Asia --> Korea
Asia --> AOthers[...]
World --> WOthers[...]
```

Dimension ***COOKING*** could look like this:

```mermaid
flowchart TD
Any --> Air[Air-Dried]
Any --> Grease
Any --> Oil
Any --> Oven
Grease --> Beef
Oil --> Olive
Oil --> Sunflower
Oil --> Colza
```

### DimensionFactory

*Dimension*s can't be directly created. The *DimensionFactory* class offers three methods to create a dimension, which can be chosen depending on how the hierarchies are defined in input.

**A bone may have several children but can never have more than one parent.**

|||
|--|--|
|⚠|**The identifiers must be unique. An element can be found several times at different places in a hierarchy, but this will be handled by its label, its id has to be unique (by containing the path of all its parent for example).**|

#### With parent link:
Each item is defined with a reference to its parent. Data could look like:
```json
[
    { id: "World" },
    { id: "Europe", parentId: "World" },
    { id: "Italy", parentId: "Europe" }
    ...
]
```

```csharp
public static Dimension BuildWithParentLink<TA, TB>(
    string dimensionName ,
    IEnumerable<TA> items ,
    Func<TA , TB> keySelector ,
    Func<TA , Option<TB>> parentKeySelector ,
    Func<TA , string> labeller = null ,
    Func<TA , double> weighter = null
    )
```

- `Func<TA , string> labeller` will determine the *Bone* label. If not provided, it will be use the key `ToString()` method.
- `Func<TA , double> weighter` can set a weight to be applied on the child contribution when computing the parent aggregate. If not set, the weight will be **1**, which means the child value is unaffected.


#### With single child link:
An item has a link to a single child. If there are more than one child, the item appears several times.

```json
[
    { id: "World", childId: "Europe" },
    { id: "Europe", childId: "Italy" },
    { id: "Europe", childId: "France" },
    { id: "Europe", childId: "Germany" }
    ...
]
```

```csharp
public static Dimension BuildWithChildLink<TA, TB>(
    string dimensionName ,
    IEnumerable<TA> items ,
    Func<TA , TB> keySelector ,
    Func<TA , Option<TB>> childKeySelector ,
    Func<TA , string> labeller = null )
```

#### With multiple children links:
An item defines its children in group.

```json
[
    { id: "World", children: ["Europe"] },
    { id: "Europe", children: ["Italy","France","Germany","Benelux"] },
    { id: "Benelux", children: ["Belgium","Netherlands","Luxemburg"] },
    ...
]
```

```csharp
public static Dimension BuildWithMultipleChildrenLink<TA, TB>(
    string dimensionName ,
    IEnumerable<TA> items ,
    Func<TA , TB> keySelector ,
    Func<TA , IEnumerable<TB>> childrenKeysSelector ,
    Func<TA , string> labeller = null )
```

## Skeletons

A *Skeleton*, as a collection of **n** *Bone*s, defines an entry in **n** *Dimension*s. They are the tool that allows us to navigate through the hierarchies. The generic type *Skeleton\<T>* associates an entry and a value of type T. *SkeletonsAccumulator\<T>* keeps track of all the components that are used to determine its associated value.

If we wished to identify the fries produced in Belgium and cooked properly, we'd have `Belgium` for the **GEO** dimension and `Beef` for the **COOKING** dimension. The resulting skeleton would look like `Beef:Belgium`. Inside a *Skeleton*, the *Bones* are sorted by the alphabetical order of their *Dimension* name, so don't worry if its key looks different from the input.

|||
|--|--|
|⚠|***A Skeleton can't have two dimensions with the same name.*** |

If for some reason, we'd need two countries definition, you'd have to create a **GEO1** and a **GEO2**, or be more explicit with **PRODUCTION_COUNTRY** and **CONSUMPTION_COUNTRY**.

### SkeletonFactory

As the library uses references to improve memory and speed, *Skeleton*s should be created through a factory.

The factory offers several methods:

- `BuildSkeletons` will create *Skeleton*s in defined dimensions. This method will throw exceptions if data aren't matching.
- `TryBuildSkeletons` will create *Skeleton*s in defined dimensions. This method will return two collections: properly created items and error messages, but won't throw exceptions.
- `FastBuild` will create *Skeleton*s in defined dimensions, but will ignore invalid elements. It also offers some options that may speed up *Skeleton*s build if the data weren't cleaned up beforehands.

## Aggregation

When computing aggregation, it is important to know whether or not you will limit the results to a defined set. Computing every keys with multiple hierarchical dimensions often makes no sense: results count exponentially grows and most of those keys don't have a real meaning.

The library offers two kind of algorithms: working from source data to higher elements in the hierarchy (BottomTop) or starting from a list of desired result keys (TopDown).

The **Aggregator** class exposes the API used to make the computations.

```csharp
public static AggregationResult<T> Aggregate<T>(
    Method method ,
    IEnumerable<Skeleton<T>> inputs ,
    Func<T , T , T> aggregator ,
    Func<IEnumerable<T> , T> groupAggregator = null ,
    Func<T , double , T> weightEffect = null ,
    bool useCachedSkeletons = true ,
    bool checkUse = false );
```

```csharp
public static AggregationResult<T> Aggregate<T>(
    Method method ,
    IEnumerable<Skeleton<T>> inputs ,
    Func<T , T , T> aggregator ,
    IEnumerable<Skeleton> targets ,
    Func<IEnumerable<T> , T> groupAggregator = null ,
    Func<T , double , T> weightEffect = null ,
    bool useCachedSkeletons = true ,
    bool checkUse = false )
```

```csharp
public static DetailedAggregationResult<T> DetailedAggregate<T>(
    Method method ,
    IEnumerable<Skeleton<T>> inputs ,
    Func<IEnumerable<(T value, double weight)> , T> aggregator ,
    IEnumerable<Skeleton> targets = null ,
    bool simplifyData = false ,
    string[] dimensionsToPreserve = null ,
    Func<IEnumerable<T> , T> groupAggregator = null )
```

```csharp
public static IEnumerable<Skeleton<T>> StreamAggregateResults<T>( 
    Seq<Skeleton<T>> baseData ,
    LanguageExt.HashSet<Skeleton> targets ,
    Func<IEnumerable<T> , T> groupAggregator ,
    Func<T , double , T> weightEffect = null ,
    bool group = false ,
    bool checkUse = false )
        
```

```csharp
public static IEnumerable<SkeletonsAccumulator<T>> StreamDetailedAggregateResults<T>( 
    Seq<Skeleton<T>> baseData ,
    LanguageExt.HashSet<Skeleton> targets ,
    Func<IEnumerable<(T, double)> , T> aggregator ,
    bool group = false ,
    bool simplifyData = false ,
    string[] dimensionsToPreserve = null ,
    Func<IEnumerable<T> , T> groupAggregator = null ,
    bool checkUse = false )
```

### Algorithms
Two algorithms are available through the *Method* enum.

#### BottomTop
The *BottomTop* algorithm will go through each input item and add its contribution to every possible ancestor.

```mermaid
flowchart TD
Beef:Belgium --> Beef:Benelux --> Beef:Europe --> Beef:World
Grease:Belgium --> Grease:Benelux --> Grease:Europe --> Grease:World
Any:Belgium --> Any:Benelux --> Any:Europe --> Any:World
Beef:Belgium --> Grease:Belgium --> Any:Belgium
Beef:Benelux --> Grease:Benelux --> Any:Benelux
Beef:Europe --> Grease:Europe --> Any:Europe
Beef:World --> Grease:World --> Any:World
```

Even with only two simple hierarchies, we can see that multiple paths can be followed through the hierarchies. The algorithm will avoid duplicate pathing.

Several variants are available for this algorithm:
- **BottomTopGroup**: the algorithm will use the *GroupBy* operator from *Linq* to go through the nodes, which is faster but needs more memory. The required memory will also be more affected by the size of input data.
- **BottomTopDictionary**: the algorithm will go through the nodes and use a *ConcurrentDictionary* to store the results. This requires a lot less memory, but it tends to be half as fast as the other method, because of the threading synchronization happening on the dictionary.
- **BottomTopGroupCached** and **BottomTopDictionaryCached**: they implement the same algorithm as previously described but use another *ConcurrentDictionary* to reuse some previously computed nodes. This is faster as long as the computed nodes remain below 1,500,000 items.

The **BottomTop** method tends to be memory efficient but may less scale with multi cores processing.

#### TopDown

The *TopDown* algorithm requires a defined output set. For each target, it will find which input items are contributing and compute the result. While a little less efficient, this algorithm tends to be able to put more pressure on the CPU, making use of higher CPUs count.

*TopDownGroup* algorithm is the latest algorithm, iterating on the previous algorithm, but using the *GroupBy* operator. This currently is the fastest algorithm.

## Samples

*Demo project needs to be updated to showcase latest developments*

Some samples can be found in the [Demo project](https://github.com/CyLuGh/MultiDimensionsHierarchies/tree/main/src/Demo) and in the [Unit tests](https://github.com/CyLuGh/MultiDimensionsHierarchies/tree/main/src/TestMultiDimensionsHierarchies)