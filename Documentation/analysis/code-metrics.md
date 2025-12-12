# Code Metrics

## Overall Statistics
- **Total Lines of Code:** 21,265 LOC
- **Total Source Files:** 153 C# files
- **Total Projects:** 21 (.csproj files)
- **Average File Size:** 139 LOC/file

## Module Breakdown
| Module | Files | LOC | Avg LOC/File |
|--------|-------|-----|--------------|
| Core | 20 | ~3,000 | 150 |
| Main | 22 | ~5,000 | 227 |
| EntityFramework | 3 | ~500 | 167 |
| Web | 6 | ~1,000 | 167 |
| Azure | ~5 | ~500 | 100 |
| Examples | ~70 | ~11,000 | 157 |
| Tests | ~27 | ~265 | ~10 |

## Complexity Indicators
- **High Complexity Files:** QueryModifiers.cs, AspectInjector.cs, CollectionExtensions.cs
- **Estimated Cyclomatic Complexity:** High in QueryModifiers (10+ per method), AspectInjector (8+)
- **Depth of Inheritance:** Shallow (max 2 levels: Aspect → specific aspects)
- **Class Coupling:** Medium (Core/Main tightly coupled)

## Quality Metrics
- **XML Documentation Coverage:** ~60% (needs improvement)
- **Public API Surface:** 100+ public types
- **Test Coverage:** Unknown (no build/execution in static analysis)
- **Maintainability Index:** Medium (complexity in QueryModifiers, PredicateBuilder)

**Document Version:** 1.0
