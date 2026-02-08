# CDTk Documentation

Welcome to the **CDTk (Compiler Description Toolkit)** documentation! CDTk is a modern, high-performance compiler framework for C#/.NET that makes language implementation fast, safe, expressive, and predictable.

## 📚 User Documentation (Wiki)

### Getting Started
- **[Getting Started](Wiki/01-GettingStarted.md)** - Your first steps with CDTk
- **[Quick Start Tutorial](Wiki/02-QuickStart.md)** - Build your first compiler in 10 minutes
- **[Installation](Wiki/03-Installation.md)** - How to add CDTk to your project

### Core Concepts
- **[Core Concepts Overview](Wiki/04-CoreConcepts.md)** - Understanding CDTk's architecture
- **[TokenSet: Lexical Analysis](Wiki/05-TokenSet.md)** - Define your language's tokens
- **[RuleSet: Grammar & Parsing](Wiki/06-RuleSet.md)** - Define your language's grammar
- **[AST Construction](Wiki/07-AST.md)** - How abstract syntax trees are built
- **[Models: Semantic Analysis](Wiki/08-Models.md)** - Add meaning and validation
- **[MapSet: Code Generation](Wiki/09-MapSet.md)** - Generate target code

### Deep Dives
- **[Lexical Analysis Deep Dive](Wiki/10-LexicalAnalysisDeepDive.md)** - Advanced tokenization techniques
- **[Parsing Deep Dive](Wiki/11-ParsingDeepDive.md)** - Understanding CDTk's parsing strategies
- **[AG-LL Parser Explained](Wiki/12-AGLLParser.md)** - The adaptive generalized LL(*) parser
- **[Diagnostics & Error Handling](Wiki/13-Diagnostics.md)** - Comprehensive error reporting
- **[Performance Optimization](Wiki/14-Performance.md)** - Getting the most out of CDTk

### Practical Guides
- **[Real-World Examples](Wiki/15-Examples.md)** - Complete language implementations
- **[Best Practices](Wiki/16-BestPractices.md)** - Patterns for success
- **[FAQ](Wiki/17-FAQ.md)** - Frequently asked questions
- **[Troubleshooting](Wiki/18-Troubleshooting.md)** - Common issues and solutions

## 🔧 Internal Documentation

### Architecture & Implementation
- **[Architecture Overview](Internal/01-ArchitectureOverview.md)** - System design and components
- **[TokenSet Implementation](Internal/02-TokenSetImplementation.md)** - Lexing internals
- **[Lexing DFA Implementation](Internal/03-LexingDFA.md)** - DFA-based tokenization
- **[RuleSet Implementation](Internal/04-RuleSetImplementation.md)** - Grammar processing
- **[AG-LL Parser Implementation](Internal/05-AGLLImplementation.md)** - Parser architecture

### Parser Subsystems
- **[ALL(*) Predictive Core](Internal/06-ALLPredictiveCore.md)** - Fast deterministic parsing
- **[GLL Fallback Mechanism](Internal/07-GLLFallback.md)** - General CFG handling
- **[GSS: Graph-Structured Stack](Internal/08-GSS.md)** - GLL stack management
- **[SPPF: Shared Packed Parse Forest](Internal/09-SPPF.md)** - Ambiguity representation

### Additional Systems
- **[AST Construction Internals](Internal/10-ASTInternals.md)** - AST building mechanics
- **[Model System Architecture](Internal/11-ModelSystem.md)** - Semantic analysis framework
- **[MapSet Implementation](Internal/12-MapSetImplementation.md)** - Code generation internals
- **[Diagnostics System](Internal/13-DiagnosticsSystem.md)** - Error reporting infrastructure
- **[Performance Infrastructure](Internal/14-PerformanceInfrastructure.md)** - Optimization techniques
- **[Memory Management](Internal/15-MemoryManagement.md)** - Safe, efficient memory use

## 🎯 Quick Links

- **[GitHub Repository](https://github.com/Tristin-Porter/CDTk)**
- **[Issue Tracker](https://github.com/Tristin-Porter/CDTk/issues)**
- **[Running Tests](../Testing/README.md)**

## 📖 About This Documentation

This documentation is organized into two main sections:

1. **Wiki (User-Facing)**: Teaches CDTk from beginner to expert, with a supportive and enthusiastic tone designed to help you build languages quickly and confidently.

2. **Internal (Implementation)**: Documents the exact implementation details in a professional, engineering-focused tone for contributors and those who want to understand how CDTk works internally.

---

**Ready to get started?** Begin with the [Getting Started Guide](Wiki/01-GettingStarted.md)!
