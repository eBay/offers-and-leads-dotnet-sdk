# ChangeLog

## 0.3.0 (09/25/2026)

### Added

- Target frameworks `net8.0` and `net9.0`, in addition to `net10.0`.

## 0.2.0 (09/14/2026)

### Breaking changes

#### Changed signatures

`ClassifiedLeadsClient`:

| Operation | Previous parameters | Current parameters |
| --- | --- | --- |
| `GetAllClassifiedLeadsAsync` | `(ClassifiedLeadsFilter? filter, string? accessToken, RequestOptions? requestOptions, CancellationToken ct)` | `(string? endTime, string? includeMessages, string? startTime, string? status, string? acceptEncoding, string? accessToken, RequestOptions? requestOptions, CancellationToken ct)` |
| `GetClassifiedLeadsByItemIdAsync` | `(string itemId, ClassifiedLeadsFilter? filter, string? accessToken, RequestOptions? requestOptions, CancellationToken ct)` | `(string itemId, string? endTime, string? includeMessages, string? startTime, string? status, string? acceptEncoding, string? accessToken, RequestOptions? requestOptions, CancellationToken ct)` |
