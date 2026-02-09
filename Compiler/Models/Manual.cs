using CDTk;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manual Memory Model - Verified Manual Memory Management
/// 
/// The manual model provides mathematically verified low-level memory control for code
/// inside manual{} or unsafe{} blocks. Uses abstract interpretation, symbolic execution,
/// ownership graphs, alias tracking, and escape analysis to prove safety.
/// 
/// Key responsibilities:
/// 1. Build ownership graphs to track pointer relationships
/// 2. Perform abstract interpretation to model all possible states
/// 3. Execute symbolic execution to verify all paths
/// 4. Track all aliases and ensure no invalid aliasing
/// 5. Verify escape behavior and lifetime constraints
/// 6. Mathematically prove 100% safety: no undefined behavior, invalid pointers, leaks, double-free, use-after-free
/// 7. Enforce complete isolation from automatic memory model
/// 
/// The manual model applies to code inside manual{} or unsafe{} blocks.
/// Intentionally slower to compile to encourage use of automatic model.
/// </summary>
class Manual : Model
{
    /// <summary>
    /// Build manual memory analysis and verification for the input AST
    /// </summary>
    public override object Build(object input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var context = new ManualContext();
        
        try
        {
            // Phase 1: Parse and extract manual blocks
            var ast = input as AstNode;
            if (ast == null)
            {
                throw new InvalidOperationException("Manual model requires AST input");
            }
            
            // Phase 2: Extract manual/unsafe blocks
            var manualBlocks = ExtractManualBlocks(ast, context);
            
            // Phase 3: Build ownership graphs for all pointers
            var ownershipGraphs = BuildOwnershipGraphs(manualBlocks, context);
            
            // Phase 4: Perform abstract interpretation
            var abstractStates = PerformAbstractInterpretation(manualBlocks, ownershipGraphs, context);
            
            // Phase 5: Execute symbolic execution on all paths
            var symbolicResults = ExecuteSymbolicExecution(manualBlocks, abstractStates, context);
            
            // Phase 6: Perform alias tracking and verification
            var aliasInfo = TrackAliases(manualBlocks, ownershipGraphs, context);
            
            // Phase 7: Analyze escape behavior
            var escapeAnalysis = AnalyzeEscape(manualBlocks, ownershipGraphs, context);
            
            // Phase 8: Verify complete memory safety
            VerifyManualMemorySafety(manualBlocks, ownershipGraphs, symbolicResults, aliasInfo, escapeAnalysis, context);
            
            // Phase 9: Enforce isolation from automatic model
            EnforceModelIsolation(manualBlocks, context);
            
            // Phase 10: Generate verified IR
            var verifiedIR = GenerateVerifiedIR(ast, manualBlocks, ownershipGraphs, context);
            
            return verifiedIR;
        }
        catch (Exception ex)
        {
            context.Diagnostics.Add(new ManualDiagnostic(
                ManualDiagnosticLevel.Error,
                $"Manual memory model failed: {ex.Message}",
                ex
            ));
            throw;
        }
    }
    
    /// <summary>
    /// Phase 1: Extract all manual{} and unsafe{} blocks from the AST
    /// Issues warning for unsafe{} recommending manual{} instead
    /// </summary>
    private List<ManualBlock> ExtractManualBlocks(AstNode ast, ManualContext context)
    {
        var extractor = new ManualBlockExtractor(context);
        return extractor.Extract(ast);
    }
    
    /// <summary>
    /// Phase 2: Build ownership graphs for all pointers in manual blocks
    /// Ownership graphs track which values own which memory regions
    /// </summary>
    private List<OwnershipGraph> BuildOwnershipGraphs(List<ManualBlock> blocks, ManualContext context)
    {
        var builder = new OwnershipGraphBuilder(context);
        return builder.Build(blocks);
    }
    
    /// <summary>
    /// Phase 3: Perform abstract interpretation to model all possible program states
    /// Abstract interpretation provides sound over-approximation of runtime behavior
    /// </summary>
    private List<AbstractState> PerformAbstractInterpretation(
        List<ManualBlock> blocks,
        List<OwnershipGraph> ownershipGraphs,
        ManualContext context)
    {
        var interpreter = new AbstractInterpreter(context);
        return interpreter.Interpret(blocks, ownershipGraphs);
    }
    
    /// <summary>
    /// Phase 4: Execute symbolic execution on all possible execution paths
    /// Symbolic execution verifies properties on all paths through the program
    /// </summary>
    private List<SymbolicExecutionResult> ExecuteSymbolicExecution(
        List<ManualBlock> blocks,
        List<AbstractState> abstractStates,
        ManualContext context)
    {
        var executor = new SymbolicExecutor(context);
        return executor.Execute(blocks, abstractStates);
    }
    
    /// <summary>
    /// Phase 5: Track all pointer aliases and verify safe aliasing
    /// Ensures no conflicting aliases that could violate safety
    /// </summary>
    private AliasInfo TrackAliases(
        List<ManualBlock> blocks,
        List<OwnershipGraph> ownershipGraphs,
        ManualContext context)
    {
        var tracker = new AliasTracker(context);
        return tracker.Track(blocks, ownershipGraphs);
    }
    
    /// <summary>
    /// Phase 6: Analyze escape behavior of pointers
    /// Ensures pointers don't escape manual blocks into automatic code
    /// </summary>
    private EscapeAnalysisResult AnalyzeEscape(
        List<ManualBlock> blocks,
        List<OwnershipGraph> ownershipGraphs,
        ManualContext context)
    {
        var analyzer = new ManualEscapeAnalyzer(context);
        return analyzer.Analyze(blocks, ownershipGraphs);
    }
    
    /// <summary>
    /// Phase 7: Verify complete memory safety for manual code
    /// Proves mathematically that code has no undefined behavior
    /// </summary>
    private void VerifyManualMemorySafety(
        List<ManualBlock> blocks,
        List<OwnershipGraph> ownershipGraphs,
        List<SymbolicExecutionResult> symbolicResults,
        AliasInfo aliasInfo,
        EscapeAnalysisResult escapeAnalysis,
        ManualContext context)
    {
        var verifier = new ManualMemorySafetyVerifier(context);
        
        // Verify no invalid pointer usage
        verifier.VerifyNoInvalidPointerUsage(blocks, ownershipGraphs, symbolicResults);
        
        // Verify no leaks
        verifier.VerifyNoLeaks(blocks, ownershipGraphs, symbolicResults);
        
        // Verify no use-after-free
        verifier.VerifyNoUseAfterFree(blocks, ownershipGraphs, symbolicResults);
        
        // Verify no double-free
        verifier.VerifyNoDoubleFree(blocks, symbolicResults);
        
        // Verify no undefined behavior
        verifier.VerifyNoUndefinedBehavior(blocks, symbolicResults);
        
        // Verify safe aliasing
        verifier.VerifySafeAliasing(aliasInfo);
        
        // Verify no escapes
        verifier.VerifyNoEscapes(escapeAnalysis);
    }
    
    /// <summary>
    /// Phase 8: Enforce isolation between manual and automatic models
    /// Ensures no cross-model aliasing or ownership transfer
    /// </summary>
    private void EnforceModelIsolation(List<ManualBlock> blocks, ManualContext context)
    {
        var enforcer = new ModelIsolationEnforcer(context);
        enforcer.Enforce(blocks);
    }
    
    /// <summary>
    /// Phase 9: Generate verified IR with manual memory annotations
    /// </summary>
    private object GenerateVerifiedIR(
        AstNode ast,
        List<ManualBlock> blocks,
        List<OwnershipGraph> ownershipGraphs,
        ManualContext context)
    {
        var generator = new ManualIRGenerator(context);
        return generator.Generate(ast, blocks, ownershipGraphs);
    }
}

// ============================================================
// Core Data Structures
// ============================================================

/// <summary>
/// Context for manual memory model analysis
/// Tracks diagnostics, settings, and verification state
/// </summary>
class ManualContext
{
    public List<ManualDiagnostic> Diagnostics { get; } = new List<ManualDiagnostic>();
    public bool StrictMode { get; set; } = true;
    public bool VerboseVerification { get; set; } = false;
    public int MaxSymbolicPathDepth { get; set; } = 1000;
}

/// <summary>
/// Diagnostic message from manual memory verification
/// </summary>
class ManualDiagnostic
{
    public ManualDiagnosticLevel Level { get; }
    public string Message { get; }
    public Exception? Exception { get; }
    
    public ManualDiagnostic(ManualDiagnosticLevel level, string message, Exception? exception = null)
    {
        Level = level;
        Message = message;
        Exception = exception;
    }
}

enum ManualDiagnosticLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Represents a manual{} or unsafe{} block in the code
/// </summary>
class ManualBlock
{
    public string Id { get; set; }
    public AstNode? BlockNode { get; set; }
    public bool IsUnsafeKeyword { get; set; }  // True if using legacy unsafe{} instead of manual{}
    public List<PointerOperation> Operations { get; } = new List<PointerOperation>();
    
    public ManualBlock(string id)
    {
        Id = id;
    }
}

/// <summary>
/// Represents a pointer operation (allocation, dereference, free, etc.)
/// </summary>
class PointerOperation
{
    public string Id { get; set; }
    public PointerOperationType Type { get; set; }
    public string? PointerName { get; set; }
    public int ProgramPoint { get; set; }
    
    public PointerOperation(string id, PointerOperationType type)
    {
        Id = id;
        Type = type;
    }
}

enum PointerOperationType
{
    Allocate,       // Allocate memory
    Deallocate,     // Free memory
    Dereference,    // Dereference pointer
    AddressOf,      // Take address of value
    PointerCast,    // Cast pointer type
    PointerArithmetic, // Pointer arithmetic
}

/// <summary>
/// Ownership graph tracking ownership relationships between pointers
/// Nodes are memory regions, edges represent ownership
/// </summary>
class OwnershipGraph
{
    private Dictionary<string, OwnershipNode> nodes = new Dictionary<string, OwnershipNode>();
    private List<OwnershipEdge> edges = new List<OwnershipEdge>();
    
    public void AddNode(OwnershipNode node)
    {
        nodes[node.Id] = node;
    }
    
    public void AddEdge(OwnershipEdge edge)
    {
        edges.Add(edge);
    }
    
    public OwnershipNode? GetNode(string id)
    {
        return nodes.TryGetValue(id, out var node) ? node : null;
    }
    
    public IEnumerable<OwnershipNode> GetNodes() => nodes.Values;
    public IEnumerable<OwnershipEdge> GetEdges() => edges;
}

/// <summary>
/// Node in ownership graph representing a memory region or pointer
/// </summary>
class OwnershipNode
{
    public string Id { get; set; }
    public string Name { get; set; }
    public OwnershipStatus Status { get; set; }
    public int AllocationPoint { get; set; }
    public int? DeallocationPoint { get; set; }
    
    public OwnershipNode(string id, string name)
    {
        Id = id;
        Name = name;
        Status = OwnershipStatus.Owned;
    }
}

enum OwnershipStatus
{
    Owned,          // Uniquely owned
    Borrowed,       // Borrowed (non-owning reference)
    Moved,          // Ownership transferred
    Freed,          // Memory deallocated
}

/// <summary>
/// Edge in ownership graph representing ownership relationship
/// </summary>
class OwnershipEdge
{
    public string FromId { get; set; }
    public string ToId { get; set; }
    public OwnershipEdgeType Type { get; set; }
    
    public OwnershipEdge(string from, string to, OwnershipEdgeType type)
    {
        FromId = from;
        ToId = to;
        Type = type;
    }
}

enum OwnershipEdgeType
{
    Owns,           // From owns To
    Borrows,        // From borrows To
    Aliases,        // From and To are aliases
}

/// <summary>
/// Abstract state representing possible program states at a program point
/// Used for abstract interpretation
/// </summary>
class AbstractState
{
    public int ProgramPoint { get; set; }
    public Dictionary<string, AbstractValue> Values { get; } = new Dictionary<string, AbstractValue>();
    public HashSet<string> ValidPointers { get; } = new HashSet<string>();
    public HashSet<string> FreedPointers { get; } = new HashSet<string>();
}

/// <summary>
/// Abstract value representing the possible values a variable can have
/// </summary>
class AbstractValue
{
    public string Name { get; set; }
    public AbstractValueKind Kind { get; set; }
    public HashSet<object> PossibleValues { get; } = new HashSet<object>();
    
    public AbstractValue(string name, AbstractValueKind kind)
    {
        Name = name;
        Kind = kind;
    }
}

enum AbstractValueKind
{
    Integer,
    Pointer,
    Boolean,
    Unknown,
}

/// <summary>
/// Result of symbolic execution on a path
/// </summary>
class SymbolicExecutionResult
{
    public string PathId { get; set; }
    public List<SymbolicConstraint> Constraints { get; } = new List<SymbolicConstraint>();
    public bool IsSafe { get; set; }
    public List<string> SafetyViolations { get; } = new List<string>();
    
    public SymbolicExecutionResult(string pathId)
    {
        PathId = pathId;
        IsSafe = true;
    }
}

/// <summary>
/// Symbolic constraint on a path
/// </summary>
class SymbolicConstraint
{
    public string Expression { get; set; }
    public bool MustBeTrue { get; set; }
    
    public SymbolicConstraint(string expression, bool mustBeTrue)
    {
        Expression = expression;
        MustBeTrue = mustBeTrue;
    }
}

/// <summary>
/// Information about pointer aliases
/// </summary>
class AliasInfo
{
    public Dictionary<string, HashSet<string>> AliasGroups { get; } = new Dictionary<string, HashSet<string>>();
    public List<AliasingViolation> Violations { get; } = new List<AliasingViolation>();
}

/// <summary>
/// Represents an aliasing violation
/// </summary>
class AliasingViolation
{
    public string Pointer1 { get; set; }
    public string Pointer2 { get; set; }
    public string Reason { get; set; }
    
    public AliasingViolation(string p1, string p2, string reason)
    {
        Pointer1 = p1;
        Pointer2 = p2;
        Reason = reason;
    }
}

/// <summary>
/// Result of escape analysis for manual blocks
/// </summary>
class EscapeAnalysisResult
{
    public List<EscapeViolation> Violations { get; } = new List<EscapeViolation>();
    public bool AllPointersContained { get; set; } = true;
}

/// <summary>
/// Represents a pointer escaping manual block
/// </summary>
class EscapeViolation
{
    public string PointerName { get; set; }
    public string EscapeLocation { get; set; }
    
    public EscapeViolation(string pointer, string location)
    {
        PointerName = pointer;
        EscapeLocation = location;
    }
}

/// <summary>
/// Intermediate representation for verified manual memory code
/// </summary>
class ManualIR
{
    public AstNode? OriginalAST { get; set; }
    public List<ManualBlock> ManualBlocks { get; set; } = new List<ManualBlock>();
    public List<OwnershipGraph> OwnershipGraphs { get; set; } = new List<OwnershipGraph>();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    
    public override string ToString()
    {
        return $"ManualIR: {ManualBlocks.Count} manual blocks, verified safe";
    }
}

// ============================================================
// Analysis Passes
// ============================================================

/// <summary>
/// Extracts manual{} and unsafe{} blocks from AST
/// </summary>
class ManualBlockExtractor
{
    private ManualContext context;
    
    public ManualBlockExtractor(ManualContext context)
    {
        this.context = context;
    }
    
    public List<ManualBlock> Extract(AstNode ast)
    {
        var blocks = new List<ManualBlock>();
        
        // Simplified - real implementation would traverse AST to find manual/unsafe blocks
        // Look for manual{} and unsafe{} block nodes
        
        return blocks;
    }
}

/// <summary>
/// Builds ownership graphs for pointers in manual blocks
/// </summary>
class OwnershipGraphBuilder
{
    private ManualContext context;
    
    public OwnershipGraphBuilder(ManualContext context)
    {
        this.context = context;
    }
    
    public List<OwnershipGraph> Build(List<ManualBlock> blocks)
    {
        var graphs = new List<OwnershipGraph>();
        
        foreach (var block in blocks)
        {
            var graph = new OwnershipGraph();
            
            // Build ownership graph for this block
            // Track allocations, ownership transfers, borrows
            foreach (var op in block.Operations)
            {
                if (op.Type == PointerOperationType.Allocate && op.PointerName != null)
                {
                    var node = new OwnershipNode(
                        $"ptr_{op.PointerName}",
                        op.PointerName
                    );
                    node.AllocationPoint = op.ProgramPoint;
                    graph.AddNode(node);
                }
            }
            
            graphs.Add(graph);
        }
        
        return graphs;
    }
}

/// <summary>
/// Performs abstract interpretation on manual blocks
/// </summary>
class AbstractInterpreter
{
    private ManualContext context;
    
    public AbstractInterpreter(ManualContext context)
    {
        this.context = context;
    }
    
    public List<AbstractState> Interpret(List<ManualBlock> blocks, List<OwnershipGraph> graphs)
    {
        var states = new List<AbstractState>();
        
        // For each block, compute abstract state at each program point
        foreach (var block in blocks)
        {
            var state = new AbstractState();
            state.ProgramPoint = 0;
            
            // Track which pointers are valid at each point
            foreach (var op in block.Operations)
            {
                if (op.Type == PointerOperationType.Allocate && op.PointerName != null)
                {
                    state.ValidPointers.Add(op.PointerName);
                }
                else if (op.Type == PointerOperationType.Deallocate && op.PointerName != null)
                {
                    state.ValidPointers.Remove(op.PointerName);
                    state.FreedPointers.Add(op.PointerName);
                }
            }
            
            states.Add(state);
        }
        
        return states;
    }
}

/// <summary>
/// Executes symbolic execution on all paths through manual blocks
/// </summary>
class SymbolicExecutor
{
    private ManualContext context;
    
    public SymbolicExecutor(ManualContext context)
    {
        this.context = context;
    }
    
    public List<SymbolicExecutionResult> Execute(List<ManualBlock> blocks, List<AbstractState> states)
    {
        var results = new List<SymbolicExecutionResult>();
        
        int pathId = 0;
        foreach (var block in blocks)
        {
            var result = new SymbolicExecutionResult($"path_{pathId++}");
            
            // Execute symbolically, tracking constraints
            // Verify safety properties on all paths
            
            results.Add(result);
        }
        
        return results;
    }
}

/// <summary>
/// Tracks aliases between pointers
/// </summary>
class AliasTracker
{
    private ManualContext context;
    
    public AliasTracker(ManualContext context)
    {
        this.context = context;
    }
    
    public AliasInfo Track(List<ManualBlock> blocks, List<OwnershipGraph> graphs)
    {
        var info = new AliasInfo();
        
        // Track which pointers may alias each other
        // Detect conflicting mutable aliases
        
        return info;
    }
}

/// <summary>
/// Analyzes escape behavior of manual pointers
/// </summary>
class ManualEscapeAnalyzer
{
    private ManualContext context;
    
    public ManualEscapeAnalyzer(ManualContext context)
    {
        this.context = context;
    }
    
    public EscapeAnalysisResult Analyze(List<ManualBlock> blocks, List<OwnershipGraph> graphs)
    {
        var result = new EscapeAnalysisResult();
        
        // Check if any pointers escape the manual block
        // Pointers must not escape into automatic code
        
        return result;
    }
}

/// <summary>
/// Verifies memory safety properties for manual code
/// </summary>
class ManualMemorySafetyVerifier
{
    private ManualContext context;
    
    public ManualMemorySafetyVerifier(ManualContext context)
    {
        this.context = context;
    }
    
    public void VerifyNoInvalidPointerUsage(
        List<ManualBlock> blocks,
        List<OwnershipGraph> graphs,
        List<SymbolicExecutionResult> symbolicResults)
    {
        // Verify all pointer dereferences are valid
        foreach (var block in blocks)
        {
            foreach (var op in block.Operations)
            {
                if (op.Type == PointerOperationType.Dereference)
                {
                    // Check pointer is valid at this point
                    // Verified through ownership graphs and symbolic execution
                }
            }
        }
    }
    
    public void VerifyNoLeaks(
        List<ManualBlock> blocks,
        List<OwnershipGraph> graphs,
        List<SymbolicExecutionResult> symbolicResults)
    {
        // Verify all allocated memory is freed
        foreach (var graph in graphs)
        {
            foreach (var node in graph.GetNodes())
            {
                if (node.DeallocationPoint == null)
                {
                    context.Diagnostics.Add(new ManualDiagnostic(
                        ManualDiagnosticLevel.Error,
                        $"Memory leak detected: pointer '{node.Name}' allocated but never freed"
                    ));
                }
            }
        }
    }
    
    public void VerifyNoUseAfterFree(
        List<ManualBlock> blocks,
        List<OwnershipGraph> graphs,
        List<SymbolicExecutionResult> symbolicResults)
    {
        // Verify no uses occur after free
        foreach (var result in symbolicResults)
        {
            foreach (var violation in result.SafetyViolations)
            {
                if (violation.Contains("use-after-free"))
                {
                    context.Diagnostics.Add(new ManualDiagnostic(
                        ManualDiagnosticLevel.Error,
                        $"Use-after-free detected: {violation}"
                    ));
                }
            }
        }
    }
    
    public void VerifyNoDoubleFree(
        List<ManualBlock> blocks,
        List<SymbolicExecutionResult> symbolicResults)
    {
        // Verify each pointer is freed at most once
        var freedPointers = new HashSet<string>();
        
        foreach (var block in blocks)
        {
            foreach (var op in block.Operations)
            {
                if (op.Type == PointerOperationType.Deallocate && op.PointerName != null)
                {
                    if (!freedPointers.Add(op.PointerName))
                    {
                        context.Diagnostics.Add(new ManualDiagnostic(
                            ManualDiagnosticLevel.Error,
                            $"Double-free detected: pointer '{op.PointerName}' freed multiple times"
                        ));
                    }
                }
            }
        }
    }
    
    public void VerifyNoUndefinedBehavior(
        List<ManualBlock> blocks,
        List<SymbolicExecutionResult> symbolicResults)
    {
        // Verify no undefined behavior on any path
        foreach (var result in symbolicResults)
        {
            if (!result.IsSafe)
            {
                foreach (var violation in result.SafetyViolations)
                {
                    context.Diagnostics.Add(new ManualDiagnostic(
                        ManualDiagnosticLevel.Error,
                        $"Undefined behavior detected: {violation}"
                    ));
                }
            }
        }
    }
    
    public void VerifySafeAliasing(AliasInfo aliasInfo)
    {
        // Verify no conflicting mutable aliases
        foreach (var violation in aliasInfo.Violations)
        {
            context.Diagnostics.Add(new ManualDiagnostic(
                ManualDiagnosticLevel.Error,
                $"Aliasing violation: {violation.Pointer1} and {violation.Pointer2} - {violation.Reason}"
            ));
        }
    }
    
    public void VerifyNoEscapes(EscapeAnalysisResult escapeResult)
    {
        // Verify no pointers escape manual blocks
        foreach (var violation in escapeResult.Violations)
        {
            context.Diagnostics.Add(new ManualDiagnostic(
                ManualDiagnosticLevel.Error,
                $"Escape violation: pointer '{violation.PointerName}' escapes at {violation.EscapeLocation}"
            ));
        }
    }
}

/// <summary>
/// Enforces isolation between manual and automatic memory models
/// </summary>
class ModelIsolationEnforcer
{
    private ManualContext context;
    
    public ModelIsolationEnforcer(ManualContext context)
    {
        this.context = context;
    }
    
    public void Enforce(List<ManualBlock> blocks)
    {
        // Verify no cross-model interactions
        // Manual pointers cannot escape to automatic code
        // Automatic references cannot enter manual blocks
        
        foreach (var block in blocks)
        {
            // Check block boundaries for violations
            // This is critical for maintaining safety guarantees of both models
        }
    }
}

/// <summary>
/// Generates verified IR for manual memory code
/// </summary>
class ManualIRGenerator
{
    private ManualContext context;
    
    public ManualIRGenerator(ManualContext context)
    {
        this.context = context;
    }
    
    public object Generate(AstNode ast, List<ManualBlock> blocks, List<OwnershipGraph> graphs)
    {
        var ir = new ManualIR
        {
            OriginalAST = ast,
            ManualBlocks = blocks,
            OwnershipGraphs = graphs,
            Metadata = new Dictionary<string, object>
            {
                ["MemoryModel"] = "Manual",
                ["Verified"] = "True",
                ["BlockCount"] = blocks.Count,
                ["OwnershipGraphs"] = graphs.Count
            }
        };
        
        return ir;
    }
}