# ChangeLog

## 0.2.0 (09/14/2026)

### Breaking changes

#### Changed signatures

`ClassifiedLeadsClient`:

| Operation | Previous parameters | Current parameters |
| --- | --- | --- |
| `GetAllClassifiedLeadsAsync` | `(ClassifiedLeadsFilter?, string?, RequestOptions?, CancellationToken)` | `(string?, string?, string?, string?, string?, string?, RequestOptions?, CancellationToken)` |
| `GetClassifiedLeadsByItemIdAsync` | `(string, ClassifiedLeadsFilter?, string?, RequestOptions?, CancellationToken)` | `(string, string?, string?, string?, string?, string?, string?, RequestOptions?, CancellationToken)` |
