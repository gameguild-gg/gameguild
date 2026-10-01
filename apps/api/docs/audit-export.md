# Audit export API

Audit exports are restricted to callers authorized by the `SystemAdmin` policy. CSV and JSON exports use the same filters and run through the `ExpensiveOperations` rate limit policy.

## Filters

Both export routes accept an `AuditExportRequest` JSON body. Supported filters are `userId`, `tenantId`, `actionType`, `resourceType`, `category`, `riskLevel`, `success`, `startDate`, `endDate`, and `ipAddress`. Dates use ISO 8601. `startDate` must not be later than `endDate`.

Pagination is optional for CSV. To request one page, provide both `pageNumber` (1-based) and `pageSize` (1–1,000). Omitting both exports every matching record as a stream. JSON always paginates: omitted values default to page 1 and 500 records; `pageSize` is capped at 1,000.

## CSV

`POST /v1/admin/audit-logs/export/csv` streams an RFC 4180 CSV file encoded as UTF-8 without a byte-order mark. It supports configurable column selection and ordering through the `columns` array. Omit `columns` to include all supported fields: `Id`, `ActionType`, `ResourceType`, `ResourceId`, `UserId`, `TenantId`, `IpAddress`, `UserAgent`, `SessionId`, `Description`, `Metadata`, `Success`, `ErrorMessage`, `RiskLevel`, `Category`, `CorrelationId`, and `CreatedAt`.

The response includes `Content-Disposition: attachment`, `X-Audit-Export-Id`, and `X-Audit-Total-Records`; paged exports also include `X-Audit-Page` and `X-Audit-Page-Size`. The former `POST /v1/admin/audit-logs:export` route remains available for compatibility and uses the same CSV behavior.

## JSON

`POST /v1/admin/audit-logs/export/json` returns a streamed JSON envelope with `schemaVersion`, `pagination`, and `records`. Each record groups related data under `event`, `actor`, `resource`, `network`, and `outcome`. Metadata is always an object: JSON objects are preserved, valid scalar or array values are wrapped in `value`, and invalid legacy text is returned in `raw`. The response includes the same export ID, total count, page, and page-size headers as CSV.

The API's response-compression middleware supports gzip and Brotli for `application/json`. Clients can request gzip with `Accept-Encoding: gzip`; the middleware negotiates and sets the corresponding response headers.

## Export progress and errors

Both formats return an export ID in `X-Audit-Export-Id`. While the response is being generated, query `GET /v1/admin/audit-logs/export/{exportId}/progress` to read its status and record count. Progress is visible only to the administrator who started the export and is retained for up to two hours. Unknown IDs and IDs owned by another administrator both return 404.

Invalid filters or columns return a 400 Problem Details response. Authorization is enforced by the controller policy. If a streaming response has already started when a storage or query error occurs, the server terminates the response; the progress resource records the failed status when it remains available.

Scheduled exports are not exposed by these routes yet.
