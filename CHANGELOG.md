# ChangeLog

## 1.0.0 (09/14/2026)

**Breaking changes**
- The following operations changed signature on `ClassifiedLeadsClient`:
  - `GetAllClassifiedLeadsAsync`: `(ClassifiedLeadsFilter?, string?, RequestOptions?, CancellationToken)` → `(string?, string?, string?, string?, string?, string?, RequestOptions?, CancellationToken)`
  - `GetClassifiedLeadsByItemIdAsync`: `(string, ClassifiedLeadsFilter?, string?, RequestOptions?, CancellationToken)` → `(string, string?, string?, string?, string?, string?, string?, RequestOptions?, CancellationToken)`
