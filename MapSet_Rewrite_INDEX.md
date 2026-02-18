# MapSet.cs Rewrite Documentation - Index

This directory contains comprehensive documentation for rewriting MapSet.cs to use the pure functional Map API exclusively.

---

## 📚 Documentation Files

### 1. **MapSet_Rewrite_Summary.md** ⭐ START HERE
**Purpose:** Executive summary and quick overview  
**Audience:** Project leads, architects, anyone wanting a high-level understanding  
**Read time:** 10-15 minutes

**Contains:**
- Quick facts and statistics
- Problem statement
- Solution overview
- Architecture compliance before/after
- Benefits of rewrite
- Effort estimate (3-5 days)
- Success criteria

**When to read:** First document to read for understanding scope and impact

---

### 2. **MapSet_Rewrite_Analysis.md** 📊 TECHNICAL DEEP DIVE
**Purpose:** Complete technical analysis with detailed breakdown  
**Audience:** Developers implementing the rewrite  
**Read time:** 30-45 minutes

**Contains:**
- List of all 11 methods to delete
- List of all 450+ Map definitions
- Categorization (functional vs template Maps)
- Understanding of semantic metadata fields
- Detailed rewrite plan for each component
- Before/after code examples
- Real-world examples
- Architecture compliance checklist

**When to read:** Before starting implementation work

---

### 3. **MapSet_Deletion_List.md** ✂️ LINE-BY-LINE GUIDE
**Purpose:** Detailed deletion instructions with exact line numbers  
**Audience:** Developers performing deletions  
**Read time:** 20-30 minutes

**Contains:**
- All 11 methods with exact line ranges
- Code snippets for each method
- Explanation of why each must be deleted
- References showing where each is called
- Summary table with line numbers

**When to read:** During Phase 1 (deletion phase)

---

### 4. **MapSet_Functional_Maps_Rewrite.md** 🔄 MAP REWRITE GUIDE
**Purpose:** Detailed instructions for rewriting the 3 functional Maps  
**Audience:** Developers rewriting Maps  
**Read time:** 30-40 minutes

**Contains:**
- IfStatement (correct example - keep as-is)
- MethodDeclaration (complete before/after)
- ConstructorDeclaration (complete before/after)
- Required semantic metadata classes
- Required Model implementations
- Integration examples
- Verification checklist
- Perfect functional Map pattern

**When to read:** During Phase 4 (Map rewriting phase)

---

### 5. **MapSet_Rewrite_Checklist.md** ✅ EXECUTION GUIDE
**Purpose:** Step-by-step checklist for executing the rewrite  
**Audience:** Developer performing the rewrite  
**Read time:** 15-20 minutes (reference throughout work)

**Contains:**
- Phase-by-phase checkboxes
- Exact code snippets to add
- Verification steps after each phase
- Testing checklist
- Code audit commands
- Troubleshooting guide
- Sign-off section

**When to read:** Throughout implementation, checking off items as completed

---

### 6. **FUNCTIONAL_MAP_API.md** 🏗️ ARCHITECTURE REFERENCE
**Purpose:** Architecture principles and design decisions  
**Audience:** Everyone (required reading)  
**Read time:** 15-20 minutes

**Contains:**
- User-defined semantic context explanation
- CDTk base MapSet vs WASM MapSet
- Why semantic fields are user-defined
- Examples of custom fields
- Real-world examples
- Architecture summary

**When to read:** First, to understand architectural principles

---

## 📖 Reading Order

### For Quick Understanding (30 minutes)
1. FUNCTIONAL_MAP_API.md (15 min)
2. MapSet_Rewrite_Summary.md (15 min)

### For Implementation (2-3 hours)
1. FUNCTIONAL_MAP_API.md (15 min)
2. MapSet_Rewrite_Summary.md (15 min)
3. MapSet_Rewrite_Analysis.md (45 min)
4. MapSet_Functional_Maps_Rewrite.md (40 min)
5. Skim MapSet_Deletion_List.md (10 min)
6. Print MapSet_Rewrite_Checklist.md for reference

### During Implementation (ongoing)
- Keep MapSet_Rewrite_Checklist.md open
- Reference MapSet_Deletion_List.md during Phase 1
- Reference MapSet_Functional_Maps_Rewrite.md during Phase 4
- Reference FUNCTIONAL_MAP_API.md for architecture questions

---

## 🎯 Quick Reference

### Key Metrics
- **File:** MapSet.cs (2,924 lines)
- **Total Maps:** 450+
- **Imperative Methods:** 11 (~538 lines to delete)
- **Functional Maps to Rewrite:** 2 (MethodDeclaration, ConstructorDeclaration)
- **Functional Maps Already Correct:** 1 (IfStatement)
- **Estimated Effort:** 3-5 days

### Core Principle

> **Maps must NEVER access AST nodes directly.**  
> **All semantic info comes from Models.**  
> **All structure comes from SPPF.**  
> **Maps only call child formatters and read semantic metadata.**

### The 11 Methods to Delete

1. `MapCSharpTypeToWasm` (line 30)
2. `ProcessCompilationUnitItem` (line 294)
3. `ProcessNamespaceItem` (line 349)
4. `ProcessTypeDeclaration` (line 371)
5. `ProcessClassMemberDeclaration` (line 448)
6. `EmitMethodDeclarationInline` (line 469)
7. `ExtractTypeFromNode` (line 711)
8. `EmitParameterList` (line 1431)
9. `EmitSingleParameter` (line 1499)
10. `MapTypeNodeToWasm` (line 1555)
11. `ProcessNamespaceDeclarationInline` (line 2410)

### The 2 Maps to Rewrite

1. **MethodDeclaration** (line 619)
   - Current: Accesses `self.Node`, calls imperative helpers
   - Target: Use child formatters, read `this.MethodMetadata`

2. **ConstructorDeclaration** (line 774)
   - Current: Accesses `self.Node`, calls imperative helpers
   - Target: Use child formatters, read `this.TypeMetadata`

### The 1 Map That's Perfect

1. **IfStatement** (line 880)
   - Already uses child formatters
   - Already reads semantic context (`this.OptHints`, `this.Dialect`, `this.Minify`)
   - **Keep as-is** - it's the perfect example!

---

## 🔍 Finding Information

### "How do I delete method X?"
→ **MapSet_Deletion_List.md** - Find method, see exact lines and code

### "How do I rewrite MethodDeclaration?"
→ **MapSet_Functional_Maps_Rewrite.md** - Section 2, complete before/after

### "What semantic fields exist?"
→ **MapSet_Rewrite_Analysis.md** - Part 3: Semantic Metadata Fields

### "What is the correct Map pattern?"
→ **MapSet_Functional_Maps_Rewrite.md** - Section on IfStatement + pattern template

### "How do I create a Model?"
→ **MapSet_Functional_Maps_Rewrite.md** - Required Model sections with code

### "What's the architecture principle?"
→ **FUNCTIONAL_MAP_API.md** - Complete architecture explanation

### "What's the step-by-step process?"
→ **MapSet_Rewrite_Checklist.md** - Phase-by-phase with checkboxes

---

## 🚀 Implementation Phases

### Phase 1: Delete Imperative Methods (30 min)
**Reference:** MapSet_Deletion_List.md  
**Checklist:** MapSet_Rewrite_Checklist.md (Phase 1)

### Phase 2: Add Semantic Metadata (1 hour)
**Reference:** MapSet_Rewrite_Analysis.md (Part 4.2)  
**Checklist:** MapSet_Rewrite_Checklist.md (Phase 2)

### Phase 3: Create Models (1-2 days)
**Reference:** MapSet_Functional_Maps_Rewrite.md (Model sections)  
**Checklist:** MapSet_Rewrite_Checklist.md (Phase 3)

### Phase 4: Rewrite Maps (2-3 hours)
**Reference:** MapSet_Functional_Maps_Rewrite.md (Complete guide)  
**Checklist:** MapSet_Rewrite_Checklist.md (Phase 4)

### Phase 5: Integration & Testing (1-2 days)
**Reference:** MapSet_Rewrite_Checklist.md (Testing section)  
**Checklist:** MapSet_Rewrite_Checklist.md (Phase 5)

---

## ✅ Verification Commands

After rewrite is complete, run these commands to verify compliance:

```bash
# Should return 0 matches
grep -n "self\.Node" MapSet.cs
grep -n "node\.Fields" MapSet.cs
grep -n "node\.Type" MapSet.cs

# Should return 0 matches
grep -n "ProcessCompilationUnitItem\|ProcessNamespaceItem\|ProcessTypeDeclaration" MapSet.cs
grep -n "ExtractTypeFromNode\|EmitParameterList\|EmitSingleParameter" MapSet.cs

# Should return 0 matches  
grep -n "MapCSharpTypeToWasm\|MapTypeNodeToWasm\|ProcessNamespaceDeclarationInline" MapSet.cs
grep -n "EmitMethodDeclarationInline\|ProcessClassMemberDeclaration" MapSet.cs
```

All commands should return **0 matches** for architecture compliance.

---

## 📋 Document Summary

| Document | Type | Length | Purpose |
|----------|------|--------|---------|
| MapSet_Rewrite_Summary.md | Executive | ~400 lines | High-level overview |
| MapSet_Rewrite_Analysis.md | Technical | ~650 lines | Complete analysis |
| MapSet_Deletion_List.md | Reference | ~550 lines | Line-by-line deletions |
| MapSet_Functional_Maps_Rewrite.md | Guide | ~750 lines | Map rewriting instructions |
| MapSet_Rewrite_Checklist.md | Checklist | ~650 lines | Execution checklist |
| FUNCTIONAL_MAP_API.md | Architecture | ~250 lines | Design principles |
| **Total** | **Documentation** | **~3,250 lines** | **Complete rewrite guide** |

---

## 🎓 Learning Path

### Beginner (New to Functional Maps)
1. Read FUNCTIONAL_MAP_API.md
2. Read MapSet_Rewrite_Summary.md
3. Study IfStatement example in MapSet_Functional_Maps_Rewrite.md
4. Review semantic fields in MapSet_Rewrite_Analysis.md

### Intermediate (Understanding Architecture)
1. Read all 6 documents in order
2. Study before/after examples
3. Review deleted methods
4. Understand semantic Models

### Advanced (Ready to Implement)
1. Read all documents
2. Print checklist
3. Set up test environment
4. Begin Phase 1 with deletion list
5. Progress through all phases
6. Verify compliance

---

## 💡 Key Insights

### Architecture
- **Separation:** Models analyze AST, Maps format output
- **Extensibility:** User-defined semantic fields allow unlimited customization
- **Purity:** Maps are pure functions (input → output, no side effects)
- **Safety:** Maps cannot access AST, ensuring architecture compliance

### Before Rewrite
- ❌ 11 imperative methods walking AST
- ❌ 2 Maps accessing `self.Node`
- ❌ ~90 AST access points
- ❌ Type extraction in Map helpers
- ❌ Imperative code generation

### After Rewrite
- ✅ 0 imperative methods
- ✅ 0 Maps accessing AST
- ✅ 0 AST access points in Maps
- ✅ Type info from semantic Models
- ✅ Pure functional formatting

---

## 📞 Support

### Questions About Architecture
→ Read FUNCTIONAL_MAP_API.md  
→ Review MapSet_Rewrite_Analysis.md Part 5

### Questions About Implementation
→ Check MapSet_Rewrite_Checklist.md  
→ Review MapSet_Functional_Maps_Rewrite.md

### Questions About Specific Methods
→ Use MapSet_Deletion_List.md  
→ Search MapSet_Rewrite_Analysis.md

### General Questions
→ Start with MapSet_Rewrite_Summary.md  
→ Deep dive in MapSet_Rewrite_Analysis.md

---

## 🏆 Success Criteria

### Code Quality
- [ ] Zero AST access in Maps
- [ ] Zero imperative helpers
- [ ] Pure functional Maps
- [ ] Semantic metadata from Models

### Architecture
- [ ] CDTk principles enforced
- [ ] Separation of concerns maintained
- [ ] Unlimited extensibility preserved
- [ ] Dialect switching enabled

### Functionality
- [ ] All tests pass
- [ ] WASM output identical
- [ ] No regressions
- [ ] Performance maintained

---

## 📅 Timeline

**Total Estimated Time:** 3-5 days

| Day | Activities |
|-----|------------|
| Day 1 | Read docs, create Models |
| Day 2 | Complete Models, test Models |
| Day 3 | Delete methods, rewrite Maps |
| Day 4 | Integration, testing |
| Day 5 | Verification, documentation |

---

## 🎉 Completion

Once rewrite is complete:

1. Run verification commands (all should return 0)
2. Check all boxes in MapSet_Rewrite_Checklist.md
3. Update code comments
4. Document new semantic fields
5. Update architecture documentation
6. Sign off on checklist

**Result:** MapSet.cs will be a pure functional Map API, fully compliant with CDTk architecture, providing unlimited extensibility for target language generation.

---

**Generated:** 2024  
**For:** CRAB Compiler Project  
**Purpose:** Complete MapSet.cs rewrite to pure functional Map API  
**Estimated Effort:** 3-5 days  
**Impact:** Complete architecture compliance + unlimited extensibility
