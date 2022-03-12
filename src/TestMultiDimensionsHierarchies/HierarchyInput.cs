using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMultiDimensionsHierarchies;

public class HierarchyInput
{
    public int Id { get; set; }
    public string? Label { get; set; }
    public int ParentId { get; set; }
}