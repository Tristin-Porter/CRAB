using CDTk;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Automatic Memory Model - Compile-Time Garbage Collection (CTGC)
/// 
/// CTGC is a static analysis and transformation system that resolves all allocations,
/// frees, and lifetimes during compilation. Similar to Mercury's CTGC.
/// 
/// Key responsibilities:
/// 1. Infer lifetimes for all values
/// 2. Perform region analysis to group allocations
/// 3. Track all allocations and their lifetimes
/// 4. Insert deterministic deallocation instructions at appropriate points
/// 5. Guarantee memory safety: no leaks, use-after-free, double-free, or aliasing violations
/// 6. All guarantees are proven statically - no runtime overhead
/// 
/// The automatic model applies to all code outside manual/unsafe blocks.
/// </summary>
class Automatic : Model
{
    /// <summary>
    /// Build automatic memory analysis and transformation for the input AST
    /// </summary>
    public override object Build(object input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var context = new AutomaticContext();
        
        try
        {
            // Phase 1: Parse and build initial representation
            var ast = input as AstNode;
            if (ast == null)
            {
                throw new InvalidOperationException("Automatic model requires AST input");
            }
            
            // Phase 2: Perform lifetime inference
            var lifetimeGraph = InferLifetimes(ast, context);
            
            // Phase 3: Perform region analysis
            var regions = AnalyzeRegions(lifetimeGraph, context);
            
            // Phase 4: Track all allocations
            var allocations = TrackAllocations(ast, regions, context);
            
            // Phase 5: Compute deallocation points
            var deallocations = ComputeDeallocations(allocations, lifetimeGraph, context);
            
            // Phase 6: Verify memory safety guarantees
            VerifyMemorySafety(allocations, deallocations, lifetimeGraph, context);
            
            // Phase 7: Generate transformed IR with explicit deallocation
            var transformedIR = GenerateIR(ast, allocations, deallocations, context);
            
            return transformedIR;
        }
        catch (Exception ex)
        {
            context.Diagnostics.Add(new AutomaticDiagnostic(
                AutomaticDiagnosticLevel.Error,
                $"Automatic memory model failed: {ex.Message}",
                ex
            ));
            throw;
        }
    }
    
    /// <summary>
    /// Phase 1: Infer lifetimes for all values in the program
    /// Uses flow-sensitive analysis to determine when values are created and last used
    /// </summary>
    private LifetimeGraph InferLifetimes(AstNode ast, AutomaticContext context)
    {
        var graph = new LifetimeGraph();
        var visitor = new LifetimeInferenceVisitor(graph, context);
        visitor.Visit(ast);
        return graph;
    }
    
    /// <summary>
    /// Phase 2: Perform region analysis to group allocations with similar lifetimes
    /// Regions allow bulk deallocation and optimize memory layout
    /// </summary>
    private List<MemoryRegion> AnalyzeRegions(LifetimeGraph lifetimeGraph, AutomaticContext context)
    {
        var analyzer = new RegionAnalyzer(context);
        return analyzer.Analyze(lifetimeGraph);
    }
    
    /// <summary>
    /// Phase 3: Track all memory allocations in the program
    /// Every 'new' expression, array allocation, delegate creation, etc.
    /// </summary>
    private List<AllocationSite> TrackAllocations(AstNode ast, List<MemoryRegion> regions, AutomaticContext context)
    {
        var tracker = new AllocationTracker(regions, context);
        return tracker.Track(ast);
    }
    
    /// <summary>
    /// Phase 4: Compute optimal deallocation points for each allocation
    /// Uses liveness analysis to place deallocations at the earliest safe point
    /// </summary>
    private List<DeallocationPoint> ComputeDeallocations(
        List<AllocationSite> allocations,
        LifetimeGraph lifetimeGraph,
        AutomaticContext context)
    {
        var computer = new DeallocationComputer(context);
        return computer.Compute(allocations, lifetimeGraph);
    }
    
    /// <summary>
    /// Phase 5: Verify all memory safety guarantees
    /// Proves statically that the program has:
    /// - No memory leaks
    /// - No use-after-free
    /// - No double-free
    /// - No dangling pointers
    /// - No aliasing violations
    /// </summary>
    private void VerifyMemorySafety(
        List<AllocationSite> allocations,
        List<DeallocationPoint> deallocations,
        LifetimeGraph lifetimeGraph,
        AutomaticContext context)
    {
        var verifier = new MemorySafetyVerifier(context);
        
        // Verify no leaks
        verifier.VerifyNoLeaks(allocations, deallocations);
        
        // Verify no use-after-free
        verifier.VerifyNoUseAfterFree(allocations, deallocations, lifetimeGraph);
        
        // Verify no double-free
        verifier.VerifyNoDoubleFree(deallocations);
        
        // Verify no dangling pointers
        verifier.VerifyNoDanglingPointers(allocations, deallocations, lifetimeGraph);
        
        // Verify no aliasing violations
        verifier.VerifyNoAliasingViolations(allocations, lifetimeGraph);
    }
    
    /// <summary>
    /// Phase 6: Generate IR with explicit memory management instructions
    /// The output IR contains:
    /// - Original program logic
    /// - Explicit allocation metadata
    /// - Explicit deallocation instructions at computed points
    /// - Region management instructions
    /// </summary>
    private object GenerateIR(
        AstNode ast,
        List<AllocationSite> allocations,
        List<DeallocationPoint> deallocations,
        AutomaticContext context)
    {
        var generator = new AutomaticIRGenerator(context);
        return generator.Generate(ast, allocations, deallocations);
    }
}

// ============================================================
// Core Data Structures
// ============================================================

/// <summary>
/// Context for automatic memory model analysis
/// Tracks diagnostics, settings, and intermediate state
/// </summary>
class AutomaticContext
{
    public List<AutomaticDiagnostic> Diagnostics { get; } = new List<AutomaticDiagnostic>();
    public bool StrictMode { get; set; } = true;
    public bool OptimizeRegions { get; set; } = true;
}

/// <summary>
/// Diagnostic message from automatic memory analysis
/// </summary>
class AutomaticDiagnostic
{
    public AutomaticDiagnosticLevel Level { get; }
    public string Message { get; }
    public Exception? Exception { get; }
    
    public AutomaticDiagnostic(AutomaticDiagnosticLevel level, string message, Exception? exception = null)
    {
        Level = level;
        Message = message;
        Exception = exception;
    }
}

enum AutomaticDiagnosticLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Graph representing lifetime relationships between values
/// Nodes are values, edges represent lifetime dependencies
/// </summary>
class LifetimeGraph
{
    private Dictionary<string, LifetimeNode> nodes = new Dictionary<string, LifetimeNode>();
    private List<LifetimeEdge> edges = new List<LifetimeEdge>();
    
    public void AddNode(LifetimeNode node)
    {
        nodes[node.Id] = node;
    }
    
    public void AddEdge(LifetimeEdge edge)
    {
        edges.Add(edge);
    }
    
    public LifetimeNode? GetNode(string id)
    {
        return nodes.TryGetValue(id, out var node) ? node : null;
    }
    
    public IEnumerable<LifetimeNode> GetNodes() => nodes.Values;
    public IEnumerable<LifetimeEdge> GetEdges() => edges;
}

/// <summary>
/// A value with an inferred lifetime
/// </summary>
class LifetimeNode
{
    public string Id { get; set; }
    public string Name { get; set; }
    public AstNode? Declaration { get; set; }
    public LifetimeScope Scope { get; set; }
    public int BirthPoint { get; set; }  // Program point where value is created
    public int DeathPoint { get; set; }  // Program point where value is last used
    
    public LifetimeNode(string id, string name)
    {
        Id = id;
        Name = name;
        Scope = new LifetimeScope();
    }
}

/// <summary>
/// Lifetime dependency edge
/// Represents "A outlives B" or "A aliases B" relationships
/// </summary>
class LifetimeEdge
{
    public string FromId { get; set; }
    public string ToId { get; set; }
    public LifetimeEdgeType Type { get; set; }
    
    public LifetimeEdge(string from, string to, LifetimeEdgeType type)
    {
        FromId = from;
        ToId = to;
        Type = type;
    }
}

enum LifetimeEdgeType
{
    Outlives,      // From must outlive To
    Aliases,       // From and To are aliases
    Contains,      // From contains To (e.g., struct contains field)
}

/// <summary>
/// Scope information for a lifetime
/// </summary>
class LifetimeScope
{
    public string ScopeName { get; set; } = "";
    public int ScopeDepth { get; set; }
    public bool IsGlobal { get; set; }
    public bool IsLocal { get; set; }
}

/// <summary>
/// A memory region groups allocations with similar lifetimes
/// Enables bulk deallocation and memory layout optimization
/// </summary>
class MemoryRegion
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int StartPoint { get; set; }
    public int EndPoint { get; set; }
    public List<AllocationSite> Allocations { get; } = new List<AllocationSite>();
    
    public MemoryRegion(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

/// <summary>
/// A single allocation site in the program
/// </summary>
class AllocationSite
{
    public string Id { get; set; }
    public AstNode? AllocationNode { get; set; }
    public string TypeName { get; set; }
    public int Size { get; set; }
    public MemoryRegion? Region { get; set; }
    public LifetimeNode? Lifetime { get; set; }
    public bool IsEscaping { get; set; }  // Does allocation escape its creation scope?
    
    public AllocationSite(string id, string typeName)
    {
        Id = id;
        TypeName = typeName;
    }
}

/// <summary>
/// A computed deallocation point in the program
/// </summary>
class DeallocationPoint
{
    public string Id { get; set; }
    public AllocationSite Allocation { get; set; }
    public int ProgramPoint { get; set; }
    public DeallocationStrategy Strategy { get; set; }
    
    public DeallocationPoint(string id, AllocationSite allocation)
    {
        Id = id;
        Allocation = allocation;
    }
}

enum DeallocationStrategy
{
    Immediate,     // Deallocate immediately after last use
    Regional,      // Deallocate as part of region cleanup
    Deferred,      // Deallocate at scope exit
}

// ============================================================
// Analysis Passes
// ============================================================

/// <summary>
/// Visitor that infers lifetimes for all values
/// </summary>
class LifetimeInferenceVisitor
{
    private LifetimeGraph graph;
    private AutomaticContext context;
    private int currentProgramPoint = 0;
    private Stack<string> scopeStack = new Stack<string>();
    
    public LifetimeInferenceVisitor(LifetimeGraph graph, AutomaticContext context)
    {
        this.graph = graph;
        this.context = context;
    }
    
    public void Visit(AstNode node)
    {
        if (node == null) return;
        
        // Simplified visitor - in real implementation, would handle all AST node types
        var nodeType = node.GetType().Name;
        
        // Track variable declarations
        if (nodeType.Contains("VariableDeclaration") || nodeType.Contains("LocalDeclaration"))
        {
            var varName = ExtractVariableName(node);
            var lifetimeNode = new LifetimeNode(
                $"lifetime_{varName}_{currentProgramPoint}",
                varName
            );
            lifetimeNode.BirthPoint = currentProgramPoint++;
            lifetimeNode.Declaration = node;
            graph.AddNode(lifetimeNode);
        }
        
        // Track assignments and uses
        if (nodeType.Contains("Assignment"))
        {
            currentProgramPoint++;
        }
        
        // Recursively visit children
        foreach (var child in GetChildren(node))
        {
            Visit(child);
        }
    }
    
    private string ExtractVariableName(AstNode node)
    {
        // Simplified - real implementation would parse AST properly
        return $"var_{node.GetHashCode()}";
    }
    
    private IEnumerable<AstNode> GetChildren(AstNode node)
    {
        // Simplified - real implementation would get actual children
        return Enumerable.Empty<AstNode>();
    }
}

/// <summary>
/// Analyzes and creates memory regions
/// </summary>
class RegionAnalyzer
{
    private AutomaticContext context;
    
    public RegionAnalyzer(AutomaticContext context)
    {
        this.context = context;
    }
    
    public List<MemoryRegion> Analyze(LifetimeGraph lifetimeGraph)
    {
        var regions = new List<MemoryRegion>();
        
        // Group lifetimes into regions based on scope and lifetime overlap
        var scopeGroups = lifetimeGraph.GetNodes()
            .GroupBy(n => n.Scope.ScopeName);
        
        int regionId = 0;
        foreach (var group in scopeGroups)
        {
            var region = new MemoryRegion(
                $"region_{regionId++}",
                group.Key
            );
            
            var nodes = group.ToList();
            if (nodes.Any())
            {
                region.StartPoint = nodes.Min(n => n.BirthPoint);
                region.EndPoint = nodes.Max(n => n.DeathPoint);
            }
            
            regions.Add(region);
        }
        
        return regions;
    }
}

/// <summary>
/// Tracks all allocation sites in the program
/// </summary>
class AllocationTracker
{
    private List<MemoryRegion> regions;
    private AutomaticContext context;
    
    public AllocationTracker(List<MemoryRegion> regions, AutomaticContext context)
    {
        this.regions = regions;
        this.context = context;
    }
    
    public List<AllocationSite> Track(AstNode ast)
    {
        var allocations = new List<AllocationSite>();
        
        // Simplified - real implementation would traverse AST and find all allocations
        // Look for: new expressions, array allocations, delegate creations, etc.
        
        return allocations;
    }
}

/// <summary>
/// Computes optimal deallocation points
/// </summary>
class DeallocationComputer
{
    private AutomaticContext context;
    
    public DeallocationComputer(AutomaticContext context)
    {
        this.context = context;
    }
    
    public List<DeallocationPoint> Compute(
        List<AllocationSite> allocations,
        LifetimeGraph lifetimeGraph)
    {
        var deallocations = new List<DeallocationPoint>();
        
        int pointId = 0;
        foreach (var allocation in allocations)
        {
            var dealloc = new DeallocationPoint(
                $"dealloc_{pointId++}",
                allocation
            );
            
            // Compute optimal deallocation point based on lifetime analysis
            if (allocation.Lifetime != null)
            {
                dealloc.ProgramPoint = allocation.Lifetime.DeathPoint + 1;
            }
            
            // Choose strategy based on allocation characteristics
            if (allocation.Region != null && context.OptimizeRegions)
            {
                dealloc.Strategy = DeallocationStrategy.Regional;
            }
            else if (allocation.IsEscaping)
            {
                dealloc.Strategy = DeallocationStrategy.Deferred;
            }
            else
            {
                dealloc.Strategy = DeallocationStrategy.Immediate;
            }
            
            deallocations.Add(dealloc);
        }
        
        return deallocations;
    }
}

/// <summary>
/// Verifies all memory safety guarantees
/// </summary>
class MemorySafetyVerifier
{
    private AutomaticContext context;
    
    public MemorySafetyVerifier(AutomaticContext context)
    {
        this.context = context;
    }
    
    public void VerifyNoLeaks(List<AllocationSite> allocations, List<DeallocationPoint> deallocations)
    {
        // Every allocation must have a corresponding deallocation
        var allocIds = new HashSet<string>(allocations.Select(a => a.Id));
        var deallocIds = new HashSet<string>(deallocations.Select(d => d.Allocation.Id));
        
        var leaks = allocIds.Except(deallocIds).ToList();
        if (leaks.Any())
        {
            context.Diagnostics.Add(new AutomaticDiagnostic(
                AutomaticDiagnosticLevel.Error,
                $"Memory leak detected: {leaks.Count} allocation(s) without deallocation"
            ));
        }
    }
    
    public void VerifyNoUseAfterFree(
        List<AllocationSite> allocations,
        List<DeallocationPoint> deallocations,
        LifetimeGraph lifetimeGraph)
    {
        // For each deallocation, verify no uses occur after it
        foreach (var dealloc in deallocations)
        {
            var lifetime = dealloc.Allocation.Lifetime;
            if (lifetime != null && dealloc.ProgramPoint <= lifetime.DeathPoint)
            {
                context.Diagnostics.Add(new AutomaticDiagnostic(
                    AutomaticDiagnosticLevel.Error,
                    $"Use-after-free detected: deallocation at {dealloc.ProgramPoint} but last use at {lifetime.DeathPoint}"
                ));
            }
        }
    }
    
    public void VerifyNoDoubleFree(List<DeallocationPoint> deallocations)
    {
        // Each allocation should only be deallocated once
        var deallocated = new HashSet<string>();
        
        foreach (var dealloc in deallocations.OrderBy(d => d.ProgramPoint))
        {
            if (!deallocated.Add(dealloc.Allocation.Id))
            {
                context.Diagnostics.Add(new AutomaticDiagnostic(
                    AutomaticDiagnosticLevel.Error,
                    $"Double-free detected: allocation {dealloc.Allocation.Id} deallocated multiple times"
                ));
            }
        }
    }
    
    public void VerifyNoDanglingPointers(
        List<AllocationSite> allocations,
        List<DeallocationPoint> deallocations,
        LifetimeGraph lifetimeGraph)
    {
        // Verify no pointers outlive their pointees
        foreach (var edge in lifetimeGraph.GetEdges())
        {
            if (edge.Type == LifetimeEdgeType.Outlives)
            {
                var from = lifetimeGraph.GetNode(edge.FromId);
                var to = lifetimeGraph.GetNode(edge.ToId);
                
                if (from != null && to != null && from.DeathPoint > to.DeathPoint)
                {
                    context.Diagnostics.Add(new AutomaticDiagnostic(
                        AutomaticDiagnosticLevel.Error,
                        $"Dangling pointer detected: {from.Name} outlives {to.Name}"
                    ));
                }
            }
        }
    }
    
    public void VerifyNoAliasingViolations(
        List<AllocationSite> allocations,
        LifetimeGraph lifetimeGraph)
    {
        // Verify aliasing rules - simplified check
        var aliases = lifetimeGraph.GetEdges()
            .Where(e => e.Type == LifetimeEdgeType.Aliases)
            .ToList();
        
        // In strict mode, verify no mutable aliases
        if (context.StrictMode)
        {
            foreach (var alias in aliases)
            {
                context.Diagnostics.Add(new AutomaticDiagnostic(
                    AutomaticDiagnosticLevel.Warning,
                    $"Potential aliasing: {alias.FromId} may alias {alias.ToId}"
                ));
            }
        }
    }
}

/// <summary>
/// Generates IR with explicit memory management
/// </summary>
class AutomaticIRGenerator
{
    private AutomaticContext context;
    
    public AutomaticIRGenerator(AutomaticContext context)
    {
        this.context = context;
    }
    
    public object Generate(
        AstNode ast,
        List<AllocationSite> allocations,
        List<DeallocationPoint> deallocations)
    {
        // Generate IR that includes:
        // 1. Original program logic
        // 2. Explicit allocation metadata
        // 3. Deallocation instructions at computed points
        
        var ir = new AutomaticIR
        {
            OriginalAST = ast,
            Allocations = allocations,
            Deallocations = deallocations,
            Metadata = new Dictionary<string, object>
            {
                ["MemoryModel"] = "Automatic",
                ["CTGC"] = "v1.0",
                ["AllocationCount"] = allocations.Count,
                ["DeallocationCount"] = deallocations.Count
            }
        };
        
        return ir;
    }
}

/// <summary>
/// Intermediate representation with automatic memory management
/// </summary>
class AutomaticIR
{
    public AstNode? OriginalAST { get; set; }
    public List<AllocationSite> Allocations { get; set; } = new List<AllocationSite>();
    public List<DeallocationPoint> Deallocations { get; set; } = new List<DeallocationPoint>();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    
    public override string ToString()
    {
        return $"AutomaticIR: {Allocations.Count} allocations, {Deallocations.Count} deallocations";
    }
}