# DirectoryService REST API

DirectoryService manages departments, their hierarchy, locations and positions.
The API currently has no authentication. Requests and responses use JSON with
camelCase properties. Use the generated OpenAPI document for full DTO schemas.

## Run locally

Requirements: .NET SDK 10, Docker with Compose, and the `dotnet-ef` tool 10.x
(`dotnet tool install --global dotnet-ef --version '10.*'` if missing).

From the repository root:

1. Copy `backend/.env.example` to `backend/.env`. Replace every placeholder with
   local values for `POSTGRES_PORT`, `POSTGRES_USER`, `POSTGRES_PASSWORD` and
   `POSTGRES_DB`. The port is the host port; use an available port such as 5432.
2. Start dependencies:

   ```bash
   docker compose --env-file backend/.env -f backend/docker-compose-dev.yml up -d
   ```

   PostgreSQL 17 stores the data. Seq is available at <http://localhost:8081>.
   The development Compose configuration disables Seq authentication.
3. Set a connection string using the values chosen in step 1. The following
   uses placeholders; replace them before running. Do not commit credentials.

   ```bash
   export ConnectionStrings__DirectoryServiceDbContext='Host=localhost;Port=<POSTGRES_PORT>;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>'
   export ASPNETCORE_ENVIRONMENT=Development
   cd backend/DirectoryService
   dotnet restore DirectoryService.slnx
   dotnet build DirectoryService.slnx --configuration Debug --disable-build-servers -m:1
   dotnet ef database update \
     --project src/DirectoryService.Infrastructure.Postgres \
     --startup-project src/DirectoryService.Web
   dotnet run --project src/DirectoryService.Web --launch-profile http
   ```

   Apply migrations only to the local database you selected. The service does
   not apply them automatically. Migrations enable PostgreSQL `ltree` and
   `pg_trgm`; the local Compose database user has the required permissions.

Open:

- Scalar: <http://localhost:5231/scalar/>
- OpenAPI JSON: <http://localhost:5231/openapi/v1.json>
- Liveness: <http://localhost:5231/health/live>
- Readiness (PostgreSQL and Seq): <http://localhost:5231/health/ready>

OpenAPI and Scalar are exposed only outside `Production`. Health endpoints
return plain health status text, not the API envelope. `Seq:ServerUrl` defaults
to `http://localhost:8081`; override with `Seq__ServerUrl` if necessary. The
browser CORS policy permits `http://localhost:3000`.

Run integration tests from `backend/DirectoryService` with Docker running:

```bash
dotnet test DirectoryService.slnx --disable-build-servers -m:1
```

Tests create isolated PostgreSQL containers and apply migrations there.

## Responses and errors

Successful 200/201 responses have this envelope (example: position creation):

```json
{
  "result": { "id": "11111111-1111-1111-1111-111111111111", "name": "Engineer" },
  "error": null,
  "isError": false,
  "timeGenerated": "2026-10-17T09:00:00Z"
}
```

PATCH operations return the updated ID as a JSON string in `result`.
DELETE and link/unlink operations return 204 with **no body**.
201 responses also include a `Location` header. Department and location URIs
are `/api/departments/{id}` and `/api/locations/{id}`. Position creation currently
returns `/positions/{id}`; there is no GET route at that URI.

A failed operation has `result: null`, `isError: true`, and an `error` object:

```json
{
  "result": null,
  "error": {
    "messages": [
      {
        "code": "locations.page.size.invalid",
        "message": "The page size must be from 5 to 100.",
        "invalidField": "PageSize"
      }
    ],
    "type": "Validation"
  },
  "isError": true,
  "timeGenerated": "2026-10-17T09:00:00Z"
}
```

Use `code` for client logic. `message` is human-readable. `invalidField` may be
null and uses server-side names such as `PageSize`, `Search`, `DepartmentName`
or `Country`; it does not always match the JSON property name. Multiple errors
can be returned in `messages`.

| HTTP status | Meaning and common codes |
| --- | --- |
| 400 | Validation/domain errors; see the codes below. Malformed JSON, missing required fields or query/route binding failures use `invalid.model.state` with type `BadRequest` and no field detail. |
| 404 | `department.not.found`, `department.parent.not.found` (transfer), `location.not.found`, `position.not.found`, `department.location.not.found`, `department.position.not.found`. |
| 409 | `location.exists`, `department.location.already.added`, `department.position.already.added`, `department.has.active.children`, `department.transfer.parent_to_self`, `department.transfer.cycle`, `department.transfer.conflict`. |
| 500 | `internal.server.error` for persistence failures/unexpected exceptions. |

Typical 400 codes:

- `department.name.empty` / `department.name.long`, `location.name.empty` /
  `location.name.long`, `position.name.empty` / `position.name.long`.
- `slug.empty`, `slug.invalid`, `department.locations.null`.
- `address.country.empty`, `address.city.empty`, `address.street.empty`,
  `address.housenumber.empty`.
- `departments.search.too.long`, `departments.search.query.invalid`,
  `departments.sort.by.invalid`, `departments.sort.direction.invalid`,
  `departments.page.invalid`, `departments.page.size.invalid`.
- `location.search.too.long`, `location.department.count.invalid`,
  `locations.sort.by.invalid`, `locations.sort.direction.invalid`,
  `locations.page.invalid`, `locations.page.size.invalid`.
- `department.id.empty` for an empty transfer ID; `department.parent.itself`
  for self-parenting through PATCH (type `Domain`).

An unmatched route (including an invalid UUID in a `:guid` route) can return
an empty 404 instead of an envelope. The children/ancestors routes bind UUIDs
without that route constraint, so malformed IDs return a binding error (400).

## Request fields

| Request | Fields and rules |
| --- | --- |
| Create department | Required `name`, `slug`, `locationIds`; optional `parentId` (null/omitted means root). `locationIds` can be `[]`; use distinct UUIDs of existing locations. Unknown IDs return 404. Duplicate IDs are unsupported and can result in 500. |
| PATCH department | Optional nullable `name`, `slug`, `parentId`; null/omitted means unchanged. The empty UUID in `parentId` makes a root. An ordinary UUID must identify an existing parent. `{}` is allowed. |
| Transfer department | `newParentId` is nullable; null/omitted means root. The empty UUID is looked up and returns 404. |
| Create location | Required `name`, `country`, `city`, `street`, `houseNumber`; optional nullable `region`, `district`, `postalCode`. Address fields are **flat** in requests. |
| PATCH location | All creation fields are nullable and optional. Null/omitted means unchanged; empty `region`, `district`, `postalCode` also preserve the current value. These optional address components cannot currently be cleared. `{}` is allowed. |
| Create/PATCH position | Required `name`. Duplicate names are allowed. |

All names must contain non-whitespace text and be at most 100 characters **after
trimming**; the trimmed name is saved. Location names are unique (including
soft-deleted rows until cleanup); department and position names are not unique.
Slugs match `^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$`: lowercase Latin letters, digits,
interior hyphens, including repeated hyphens. Slugs are not trimmed. There is
no application validator length limit, but the database column allows 100
characters; exceeding it fails during persistence (500).

Required address fields cannot be null/empty/whitespace when creating; when
patching, a supplied non-null value must contain non-whitespace text. Address
strings are saved without trimming. Database limits are 60 characters for
country/region/city/district/street/houseNumber and 10 for postalCode. These
limits are not validated in Core; exceeding them results in a persistence error
(500), rather than a field validation response.

## Operations

Paths below use `/api` and UUID route parameters. Request bodies are JSON.
All operations may return 500; operations with input can return 400 as above.

| Method and path | Body | Success result | Expected resource/conflict errors |
| --- | --- | --- | --- |
| POST `/departments` | Create department | 201 `DepartmentDto` | 404 parent/location; 409 persistence conflict |
| PATCH `/departments/{id}` | PATCH department | 200 UUID | 404 department/parent |
| DELETE `/departments/{id}` | None | 204 | 404 department; 409 active children |
| GET `/departments/{id}` | None | 200 `DepartmentDto` | 404 department |
| GET `/departments` | None | 200 page of `DepartmentListItemDto` | — |
| GET `/departments/tree` | None | 200 page of `DepartmentNodeDto` roots | — |
| GET `/departments/{id}/children` | None | 200 page of `DepartmentNodeDto` | Missing parent: empty page |
| GET `/departments/{id}/ancestors` | None | 200 page of `DepartmentAncestorDto` | Missing department: empty page |
| GET `/departments/tree/search` | None | 200 page of `DepartmentWithAncestorsDto` | — |
| PUT `/departments/{id}/parent` | Transfer department | 200 `TransferredDepartmentDto` | 404 department/new parent; 409 self/cycle/concurrent transfer |
| POST `/departments/{departmentId}/locations/{locationId}` | None | 204 | 404 resource; 409 duplicate link |
| DELETE `/departments/{departmentId}/locations/{locationId}` | None | 204 | 404 link |
| POST `/departments/{departmentId}/positions/{positionId}` | None | 204 | 404 resource; 409 duplicate link |
| DELETE `/departments/{departmentId}/positions/{positionId}` | None | 204 | 404 link |
| POST `/locations` | Create location | 201 `LocationDto` | 409 name exists |
| PATCH `/locations/{id}` | PATCH location | 200 UUID | 404 location; 409 name exists |
| DELETE `/locations/{id}` | None | 204 | 404 location |
| GET `/locations/{id}` | None | 200 `LocationDto` | 404 location |
| GET `/locations` | None | 200 page of `LocationListItemDto` | — |
| GET `/locations/top` | None | 200 array of `LocationWithDepartmentCountDto` | — |
| POST `/positions` | Create position | 201 `PositionDto` | — |
| PATCH `/positions/{id}` | PATCH position | 200 UUID | 404 position |
| DELETE `/positions/{id}` | None | 204 | 404 position |

There are currently no position list/get endpoints or endpoints for listing
an individual department's location/position links.

`DepartmentDto` contains `id`, `name`, `slug`, `path`, `parentId`.
`TransferredDepartmentDto` and `DepartmentAncestorDto` add `depth`.
`DepartmentListItemDto` contains `id`, `name`, `slug`, `path`, `createdAt`.
`DepartmentNodeDto` contains `id`, `name`, `slug`, `path`, `depth`, `createdAt`,
`hasChildren`, `childCount`. Search results contain `id`, `name`, `slug`, `path`,
`depth`, `parentId`, `ancestors` (an array of `DepartmentAncestorDto`).

`LocationDto` contains `id`, `name`, `address` (all seven address fields).
`LocationListItemDto` adds `createdAt` and `departmentCount`.
Top-location items contain `location: LocationDto` and `departmentCount`.
`PositionDto` contains `id` and `name`.

## Lists and hierarchy

Every paged response puts `{ "results": [], "page": 1, "pageSize": 20,
"totalCount": 0 }` in `result`. `page` defaults to 1 and must be positive;
`pageSize` defaults to 20 and must be 5–100. These parameters are supported by
both flat lists and all four hierarchy queries.

| Endpoint | Additional query parameters | Ordering |
| --- | --- | --- |
| GET `/api/departments` | `search`: optional, max 100 characters; `sortBy`: `name` (default) or `createdAt`; `sortDirection`: `asc` (default) or `desc` | Chosen field/direction, then ID ascending |
| GET `/api/locations` | Same as departments; also `sortBy=departmentCount` and `minDepartmentCount` (integer >= 0, omitted means 0) | Chosen field/direction, then ID ascending |
| GET `/api/departments/tree` | None beyond pagination | Root name ascending |
| GET `/api/departments/{id}/children` | None beyond pagination | Child name ascending |
| GET `/api/departments/{id}/ancestors` | None beyond pagination | Depth ascending (root first) |
| GET `/api/departments/tree/search` | Required `q`, at least 2 characters; no maximum-length validator | Match name/ID ascending; ancestors by depth ascending |

Sorting values are case-sensitive. Searches use PostgreSQL `ILIKE '%query%'`:
case-insensitive matching with `%` and `_` treated as wildcards, not escaped
literal characters. `totalCount` is the matching count before pagination;
SQL-backed location/tree queries return 0 for an empty page, including an
out-of-range page. The department flat list computes its count separately.

`path` uses **dots**, e.g. `company.engineering.platform`; roots have depth 0.
The `/tree` response contains roots only, without recursively nested children.
Use `/children` to expand one level; its counts describe immediate active
children. `/ancestors` excludes the department itself. Search pages only the
matches; every match contains its complete active ancestor chain, even when
that chain exceeds pageSize. Deleted departments are excluded from these reads.

Use PUT `/departments/{id}/parent` for subtree transfers: it locks affected
rows and updates descendant paths/depths in a transaction. Repeating the current
parent is a successful no-op. Self-parenting and moving beneath a descendant
are conflicts. PATCH accepts parent/slug edits but recalculates only the edited
department's path/depth; it does not propagate the change to descendants or
perform the transfer handler's cycle check.

Links are many-to-many. POST requires both active resources and fails on a
duplicate; DELETE removes only the link and fails when it is absent. Department
creation can establish location links, but position links are added separately.
Deleting a location/position removes its links transactionally, then soft-deletes
the resource. Deleting a department requires no active children and soft-deletes
it; links remain until cleanup. Location counts count stored links, so they can
include soft-deleted departments. `/locations/top` returns up to five active
locations, including zero-count locations, by count descending then name
ascending. Cleanup runs every 10 minutes with a 30-day retention by default.

## Examples

Use the IDs returned by creation in later requests. The UUIDs in example
responses are illustrative; timestamps and IDs are generated by the service.

```bash
API=http://localhost:5231/api
curl -i "$API/locations" -H 'Content-Type: application/json' -d \
  '{"name":"Moscow Office","country":"Russia","city":"Moscow","street":"Tverskaya","houseNumber":"1"}'
```

201 response (`Location: /api/locations/<generated-id>`):

```json
{
  "result": {
    "id": "22222222-2222-2222-2222-222222222222",
    "name": "Moscow Office",
    "address": { "country": "Russia", "region": null, "city": "Moscow", "district": null, "street": "Tverskaya", "houseNumber": "1", "postalCode": null }
  },
  "error": null, "isError": false, "timeGenerated": "2026-10-17T09:00:00Z"
}
```

Create a root department, link a position, and list departments:

```bash
curl -i "$API/departments" -H 'Content-Type: application/json' -d \
  '{"name":"Engineering","slug":"engineering","locationIds":[],"parentId":null}'
curl -i "$API/positions" -H 'Content-Type: application/json' -d '{"name":"Engineer"}'
# Replace these values with the returned IDs.
DEPARTMENT_ID='<department-id>'
POSITION_ID='<position-id>'
curl -i -X POST "$API/departments/$DEPARTMENT_ID/positions/$POSITION_ID"
curl -s "$API/departments?search=Engineering&sortBy=name&sortDirection=asc&page=1&pageSize=20"
```

Department creation returns 201 with:

```json
{
  "result": { "id": "33333333-3333-3333-3333-333333333333", "name": "Engineering", "slug": "engineering", "path": "engineering", "parentId": null },
  "error": null, "isError": false, "timeGenerated": "2026-10-17T09:00:00Z"
}
```

Inspect children, search with ancestors, transfer to root, and update a name:

```bash
curl -s "$API/departments/$DEPARTMENT_ID/children?page=1&pageSize=20"
curl -s "$API/departments/tree/search?q=Eng&page=1&pageSize=20"
curl -i -X PUT "$API/departments/$DEPARTMENT_ID/parent" \
  -H 'Content-Type: application/json' -d '{"newParentId":null}'
curl -i -X PATCH "$API/departments/$DEPARTMENT_ID" \
  -H 'Content-Type: application/json' -d '{"name":"Platform Engineering"}'
```

A transfer returns 200 with `result` containing the department fields plus
`depth` (0 after transfer to root). PATCH returns 200 with `result` equal to the
UUID string. To transfer under another department, supply its ID as `newParentId`.

Typical errors to try:

```bash
# 400 locations.page.size.invalid (the full envelope is shown above).
curl -i "$API/locations?pageSize=1"
# 400 position.name.empty, invalidField = PositionName, type = Validation.
curl -i "$API/positions" -H 'Content-Type: application/json' -d '{"name":" "}'
# 404 location.not.found (assuming this ID has not been created).
curl -i "$API/locations/99999999-9999-9999-9999-999999999999"
# 409 department.position.already.added: repeat the link request.
curl -i -X POST "$API/departments/$DEPARTMENT_ID/positions/$POSITION_ID"
```

The last response has `result: null` and an error like:

```json
{
  "messages": [{ "code": "department.position.already.added", "message": "The position has already been added to the department", "invalidField": null }],
  "type": "Conflict"
}
```

The text can include resource IDs; match the code instead of the message.
