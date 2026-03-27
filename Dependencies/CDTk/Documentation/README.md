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
- **[AG-LL Parser Explained](Wiki/12-AGLLParser.md)** - The adaptive generalized LL(*) parser

## 🔧 Internal Documentation

- **[Architecture Overview](Internal/01-ArchitectureOverview.md)** - System design and components
- **[GSS: Graph-Structured Stack](Internal/08-GSS.md)** - GLL stack management
- **[SPPF: Shared Packed Parse Forest](Internal/09-SPPF.md)** - Ambiguity representation

## 🎯 Quick Links

- **[GitHub Repository](https://github.com/Tristin-Porter/CDTk)**
- **[Issue Tracker](https://github.com/Tristin-Porter/CDTk/issues)**

## 📖 About This Documentation

This documentation is organized into two main sections:

1. **Wiki (User-Facing)**: Teaches CDTk from beginner to expert, with a supportive and enthusiastic tone designed to help you build languages quickly and confidently.

2. **Internal (Implementation)**: Documents the exact implementation details in a professional, engineering-focused tone for contributors and those who want to understand how CDTk works internally.

---

**Ready to get started?** Begin with the [Getting Started Guide](Wiki/01-GettingStarted.md)!
