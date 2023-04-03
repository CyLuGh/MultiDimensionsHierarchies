# MultiDimensionsHierarchies

[![NuGet](https://raw.githubusercontent.com/NuGet/Media/main/Images/MainLogo/32x32/nuget_32.png)](https://www.nuget.org/packages/MultiDimensionsHierarchies/) [![CodeQL](https://github.com/CyLuGh/MultiDimensionsHierarchies/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/CyLuGh/MultiDimensionsHierarchies/actions/workflows/codeql-analysis.yml) [![CodeFactor](https://www.codefactor.io/repository/github/cylugh/multidimensionshierarchies/badge)](https://www.codefactor.io/repository/github/cylugh/multidimensionshierarchies) 

### Exponential growth problem

It is quite easy to do some aggregates along a single hierarchy through recursive methods. It is still easy enough with two hierarchies. But what happens when there are *n* hierarchies to iterate through?

Let's look at a simple example with three dimensions.

```mermaid
%%{init: {'theme':'base'}}%%
flowchart TD

subgraph Dimension C
	C1 --- C2 --- C3
end

subgraph Dimension B
	B1 --- B2 --- B3
end

subgraph Dimension A
	A1 --- A2 --- A3
end
```

These very simple dimensions can be combined to create $3^3$ items, which can be linked as below. This shows the complexity of going through these combinations, which will grow exponentially as dimensions are added and get more complex.

```mermaid
%%{init: {'theme':'base'}}%%
flowchart TD

A1:B1:C1 --- A2:B1:C1
A2:B1:C1 --- A3:B1:C1
A3:B1:C1 --- A3:B2:C1
A3:B2:C1 --- A3:B3:C1
A3:B2:C1 --- A3:B2:C2
A3:B3:C1 --- A3:B3:C2
A3:B3:C2 --- A3:B3:C3
A3:B2:C2 --- A3:B3:C2
A3:B2:C2 --- A3:B2:C3
A3:B2:C3 --- A3:B3:C3
A3:B1:C1 --- A3:B1:C2
A3:B1:C2 --- A3:B2:C2
A3:B1:C2 --- A3:B1:C3
A3:B1:C3 --- A3:B2:C3
A2:B1:C1 --- A2:B2:C1
A2:B2:C1 --- A3:B2:C1
A2:B2:C1 --- A2:B3:C1
A2:B2:C1 --- A2:B2:C2
A2:B1:C1 --- A2:B1:C2
A2:B1:C2 --- A3:B1:C2
A2:B1:C2 --- A2:B2:C2
A2:B1:C2 --- A2:B1:C3
A1:B1:C1 --- A1:B2:C1
A1:B2:C1 --- A2:B2:C1
A1:B2:C1 --- A1:B3:C1
A1:B2:C1 --- A1:B2:C2
A1:B1:C1 --- A1:B1:C2
A1:B1:C2 --- A2:B1:C2
A1:B1:C2 --- A1:B2:C2
A1:B1:C2 --- A1:B1:C3
A1:B3:C1 --- A2:B3:C1
A1:B3:C1 --- A1:B3:C2
A1:B1:C3 --- A2:B1:C3
A1:B1:C3 --- A1:B2:C3
A1:B2:C2 --- A2:B2:C2
A1:B2:C2 --- A1:B3:C2
A1:B2:C2 --- A1:B2:C3
A2:B2:C2 --- A3:B2:C2
A2:B2:C2 --- A2:B3:C2
A2:B2:C2 --- A2:B2:C3
A2:B1:C3 --- A3:B1:C3
A2:B1:C3 --- A2:B2:C3
A1:B3:C2 --- A2:B3:C2
A1:B3:C2 --- A1:B3:C3
A1:B2:C3 --- A2:B2:C3
A1:B2:C3 --- A1:B3:C3
A2:B3:C2 --- A3:B3:C2
A2:B3:C2 --- A2:B3:C3
A2:B2:C3 --- A3:B2:C3
A2:B2:C3 --- A2:B3:C3
A1:B3:C3 --- A2:B3:C3
A2:B3:C3 --- A3:B3:C3
A2:B3:C1 --- A3:B3:C1
A2:B3:C1 --- A2:B3:C2
```

### MultiDimensionsHierarchies library

This library provides a way of handling such problem, by simplifying the way we can go through the data. It still has limitation as very complex and numerous dimensions may very well enduce very long computation or run out of memory.

#### Setting up dimensions

The first step to be able to compute aggregates is to clearly define the dimensions. The library provides several way of doing this (through **DimensionFactory**), but two informations are most important:

- the unique identifier of the item in the hierarchy
- the unique identifier of its parent in the hierarchy if it has one

Optional informations are:
- the weight of the item in its parent hierarchy, which is 1 by default.
- the label that will be used for display and input parsing, which is equal to the identifier by default.

In the library, such elements are called **Bone** and they form the **Frame** of each **Dimension**. They'll be used by reference in the following processes.

#### Identifying items in the hierarchies

By combining the **Bone** elements, each element can be identified in the hierarchies. Such combination is represented by **Skeleton** in the library. It allows to easily get descendants or ancestors of the element, as well as manipulating **Dimension** composition.

These **Skeleton**s should only be constructed through the factory pattern, as the use of direct references is essential.

```mermaid
%%{init: {'theme':'base'}}%%

flowchart LR

subgraph Dimension C
	C1 --- C2 --- C3
end

subgraph Dimension B
	B1 --- B2 --- B3
end

subgraph Dimension A
	A1 --- A2 --- A3
end

subgraph Keys
A1:B2:C3
A3:B3:C3
end

subgraph Skeletons
A1B2C3[A1 B2 C3]
A3B3C3[A3 B3 C3]
end

A1 -..- A1B2C3
B2 -..- A1B2C3
C3 -..- A1B2C3
A1:B2:C3 -..- A1B2C3

A3 -..- A3B3C3
B3 -..- A3B3C3
C3 -..- A3B3C3
A3:B3:C3 -..- A3B3C3

```


!!! warning Repeating labels
- When parsing a key as string input, repeating labels will create more data.
- If the desired output reverts back to the string format, make sure that those labels are leaves or have identical descendants, otherwise the output may have wrong values as it will lose its ability to discriminate items.
!!!

```mermaid
%%{init: {'theme':'base'}}%%

flowchart LR

subgraph Raw[Raw data]
	xxx:yyy:xxx
	yyyy:yyy:xxx
	xxx:AAA:xxx
end

subgraph DimA[Dimension A]
	A --- A2.1 --- A3.1 --- AAA1
	A --- A2.2 --- A3.2 --- AAA2
	A3.2 --- AAA3
end

subgraph Generated[Hierarchical data]
	AA1[Skeleton with AAA for Dimension A]
	AA2[Skeleton with AAA for Dimension A]
	AA3[Skeleton with AAA for Dimension A]
end

AAA1[AAA]
AAA2[AAA]
AAA3[AAA]

DimA ==> Generated
Raw ===> Generated

AAA1 -.- AA1
AAA2 -.- AA2
AAA3 -.- AA3
xxx:AAA:xxx -.- AA1
xxx:AAA:xxx -.- AA2
xxx:AAA:xxx -.- AA3
```

#### Aggregating data

##### With a defined set of targets

With this method, the algorithm will go through each target **Skeleton** and look through the data to find composing **Skeleton** elements.

The first step will simply aggregate the items with the same key so it can limit the base data if it hasn't been optimized before the call.

The second step is to simplify the base data. If a dimension in the targets only asks for a single value, the algorithm can remove all data that don't match the requirement and then remove the dimension.

To efficiently retrieve components, each dimension is handled separately, progressively removing all elements that aren't descendants of the target. Once all non-composing elements have been removed, the aggregation method can be applied on the remaining items. By grouping the different values of the **Bone**s, the iterations on the data can be limited, which greatly improves the computation speed. 

To finalize the results, the algorithm adds back the previously removed dimensions with their single asked value.

##### Without targets

The first step will simply aggregate the items with the same key so it can limit the base data if it hasn't been optimized before the call.

The algorithm will go through each item and determine each of his ancestor items. It will add its contribution to the aggregation in a concurrent collection. Once all the data have been processed, this collection will contain the final results of the aggregation.

!!!warning  This method can rapidly create many results and see its efficiency drops as the possibilities grows exponentially.
!!!

#### Aggregation components

The algorithm has an alternative mode where it does not apply the aggregation method right away but keep tracks of the participating components. An extra parameter is then required: the list of dimensions that shouldn't be simplified to keep discrimination of components possible.

This way of using the algorithm allows the computation of derived information such as primary confidentiality.
