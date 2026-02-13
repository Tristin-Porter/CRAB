# GSS: Graph-Structured Stack

**Status**: ✅ **Fully Implemented** - Core component of GLL parser (v9.0.0+)

## Overview

The Graph-Structured Stack (GSS) is a key component of the GLL parser, enabling efficient handling of non-determinism, recursion, and backtracking in generalized parsing.

## Purpose

Unlike a traditional call stack, the GSS allows multiple parse paths to share common stack frames, dramatically reducing memory usage during non-deterministic parsing.

**Key Benefits**:
- Handles left recursion via cycles
- Supports ambiguous grammars
- Shares common parsing state
- Enables efficient backtracking

## Data Structure

```csharp
internal sealed class GSSNode
{
    public int Id { get; }              // Unique identifier
    public int InputPosition { get; }   // Position in input where node was created
    public string Label { get; }        // Grammar label (rule + slot)
    public List<GSSEdge> Edges { get; } // Outgoing edges to parent stacks
}

internal sealed class GSSEdge
{
    public GSSNode Target { get; }     // Parent GSS node
    public SPPFNode? SPPFNode { get; } // SPPF node representing parsed content
}
```

## How It Works

### Traditional Stack vs GSS

**Traditional Stack** (single path):
```
[A@3] -> [B@2] -> [C@1] -> [Start@0]
```

**GSS** (multiple paths sharing state):
```
       -> [A@3] -> [B@2] -
      /                    \
[D@5]                       -> [Start@0]
      \                    /
       -> [E@3] -> [F@2] -
```

Both paths through D share the common suffix back to Start.

### Node Creation

GSS nodes are created when:
1. Entering a non-terminal (rule call)
2. Processing a choice alternative
3. Handling recursion

**Uniqueness**: Nodes identified by (Label, InputPosition) - only one node per combination.

### Edge Management

Edges represent:
- **Return continuation**: Where to continue after rule completes
- **SPPF payload**: Parse tree fragment built so far

**Edge Addition**: Multiple edges can point to same target from same source, representing different derivations.

## Left Recursion Handling

GSS enables left recursion via **cycles**:

```
Example: Expr -> Expr '+' Number

GSS during parsing "1+2+3":
  [Expr@0] -> [Expr@0]  // Cycle!
     |
     v
  [Expr@2] -> [Expr@0]  // Another cycle
     |
     v
  [Expr@4] -> [Expr@0]  // Keeps growing
```

The cycles don't cause infinite loops because:
1. Each iteration advances input position
2. Descriptors are deduplicated
3. Progress is always forward through input

## Implementation Details

**Location**: `Boilerplate/CDTk.cs` lines ~11020-11100

### GetOrCreateGSSNode

```csharp
private GSSNode GetOrCreateGSSNode(string label, int position)
{
    var key = (label, position);
    if (!_gssNodes.TryGetValue(key, out var node))
    {
        node = new GSSNode(label.GetHashCode(), position);
        node.Label = label;
        _gssNodes[key] = node;
    }
    return node;
}
```

Nodes are cached in dictionary for reuse.

### Pop Operation

When a rule completes, Pop traverses GSS edges:

```csharp
private void Pop(SPPFNode? sppfNode)
{
    // Get GSS node for current rule entry (slot 0)
    var gssNode = GetOrCreateGSSNode(ruleName, entryPosition);
    
    // Traverse all outgoing edges (return continuations)
    foreach (var edge in gssNode.Edges)
    {
        // Combine SPPFs
        var combinedSPPF = CombineSPPF(edge.SPPFNode, sppfNode);
        
        // Continue at edge target
        AddDescriptor(new Descriptor(
            edge.Target.Label,
            edge.Target,
            currentPosition,
            combinedSPPF));
    }
}
```

**Critical**: Always pop from entry node (slot 0) for correct SPPF extent calculation.

## Performance Characteristics

**Memory**:
- One node per (label, position) combination
- Shared across all parse paths
- Typically O(n*m) where n=input length, m=grammar size

**Time**:
- Node lookup: O(1) via dictionary
- Edge traversal: O(edges) typically small
- Overall: Contributes to GLL's O(n³) worst case

**Optimizations**:
- Node reuse via caching
- Lazy edge creation
- Tail-call optimization (no intermediate nodes)

## Invariants

1. **Uniqueness**: Only one GSS node per (label, position)
2. **Position Ordering**: Edges only point to nodes at earlier or same positions
3. **Acyclic Construction**: Within a single parse, GSS is acyclic during construction
4. **Cycle Tolerance**: Cycles represent recursion, not infinite loops

## Integration with GLL

**Descriptor Processing**:
- Each descriptor references a GSS node
- Descriptor = (Label, GSSNode, Position, SPPFNode)

**Rule Entry**:
1. Create GSS node for rule at current position
2. Create return continuation node
3. Add edge from entry to return
4. Process rule body with entry node

**Rule Exit**:
1. Pop from entry node
2. Traverse all edges
3. Create continuations for each

## Testing

**Coverage**: Tested via:
- Left-recursive grammar tests (6 tests)
- Ambiguous grammar tests (14 tests)
- Nested structures test
- All pass with correct GSS usage

**Validation**: No GSS-related bugs in v9.0.0+

## See Also

- [GLL Fallback Mechanism](07-GLLFallbackMechanism.md) - Overall GLL algorithm
- [SPPF](09-SPPF.md) - Parse forest representation
- [AG-LL Implementation](05-AG-LLImplementation.md) - Complete parser architecture
- User Documentation: [AG-LL Parser Explained](../Wiki/12-AGLLParser.md)
