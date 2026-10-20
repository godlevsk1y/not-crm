# DirectoryService OpenAPI documentation

The API document is available at `/openapi/v1.json` and Scalar at `/scalar` outside production. Generation uses the existing ASP.NET Core OpenAPI stack. No documentation rule changes request validation or serialization.

## Where to edit

For `PATCH /api/locations/{id}`:

- Operation summary, behavior and response types: `Controllers/LocationsController.Update`, using built-in endpoint and response metadata.
- Request field meaning: `[property: Description]` on `UpdateLocationRequest.City` (and the other members) in Contracts. These are documentation-only attributes, not validation attributes.
- Applicable errors and their conditions: `LocationsDocumentation.Errors`, under `nameof(LocationsController.Update)`. Existing error factories supply codes, messages, categories and field names. Literal query validation errors are maintained against the corresponding validator and use `nameof` for members.
- Request and success examples: `ContractExamples`, indexed by CLR type and constructed from actual DTOs.
- Query rules: `QueryDocumentation` explicitly selects controller actions before applying shared pagination/list rules. There is no URL or type-name inference. Extend this selection when documenting a new query operation.
- Registration and common document conventions: `ApiDocumentation`; schema conventions: `SchemaDocumentation`. Common HTTP response formatting, error examples and bodyless 204s: `OperationDocumentation`. `ApiError` renders the error tables.

Adding a DTO field description only requires editing that DTO. An example requires a typed entry in `ContractExamples`; it does not require adding a rule for similarly named fields elsewhere. A new operation's custom errors belong to its controller's documentation class.

## Automatic and manual information

ASP.NET Core supplies routes, HTTP methods, parameter locations, summaries/descriptions and response declarations from MVC metadata. Its schema generator supplies JSON names, property types, nullable unions, enum representations and references from serializer metadata. See [Microsoft's metadata documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0).

Our schema transformer reads member descriptions and supplies typed request examples. For body types discovered through API Explorer it corrects constructor-derived `required` using MVC model metadata: the current configuration requires non-nullable reference members, while nullable constructor parameters may be omitted. This does not enable `RespectRequiredConstructorParameters` or change runtime options. Constructor defaults, omitted fields, null and empty values remain separate concerns.

Validation semantics, PATCH behavior, query limits/defaults, operation-specific reachable errors, and examples are maintained manually against validators, handlers, domain objects and persistence mappings. No code tries to infer business meaning from a property name. Error status mapping mirrors `EndpointResults`, except the model-state error is the explicit MVC 400 response. Keep that distinction when changing the runtime mapping.

Order is explicit in `ApiDocumentation.Configure`: schema enrichment is registered first; operation enrichment consumes generated schemas and controller metadata; the document transformer sets the final shared introduction. Schema generation is independent of database availability.

## Existing behavior and discrepancies

These were verified against the current implementation and documented without changing behavior:

- `ExceptionMiddleware` returns a raw `Error` for unexpected exceptions; `EndpointResults.Error` returns an envelope for handled errors. The 500 schema uses `oneOf` and examples for both shapes.
- `ErrorType.BadRequest` falls through to 500 in `EndpointResults.Error`. The model-state handler bypasses it and explicitly returns 400 with `invalid.model.state`; no current business handler returns BadRequest.
- Department PATCH treats omitted/null values as unchanged and the empty parent UUID as “make root”. It updates only the current department's path/depth and does not guard against descendant cycles. The transfer operation updates the subtree, checks cycles, and uses null/omission to make a root; an empty UUID is a parent lookup returning 404.
- Location PATCH cannot clear region, district or postal code: omission, null and `""` all retain the old value. Whitespace is stored. Position PATCH requires a name, unlike the other two PATCH operations.
- Names are limited to 100 characters **after trimming**. A schema `maxLength: 100` on the raw request would reject accepted input, so this constraint is described in prose. Slugs have no application-level length check; the 100-character database limit can produce 500. Address components similarly have database-only limits of 60 characters (10 for postal code).
- Repeated existing location IDs in department creation can throw during missing-ID detection and return raw 500; distinctness is usage guidance, not a validator-enforced rule. Missing IDs return 404.
- The root and child department queries do not select `created_at`, so `DepartmentNodeDto.CreatedAt` is serialized as the default DateTime. The examples show that value. Other DTOs' creation timestamps come from storage.
- Position creation emits `/positions/{id}` in Location, and there is no position-by-ID GET route.
- Location uniqueness includes stored soft-deleted locations. Department link counts can include soft-deleted departments until cleanup. Duplicate position names are allowed.
- Empty pages retain totalCount in all list/tree queries. Root/child/ancestor ordering is by name/depth without an ID tie-breaker; flat lists and tree search explicitly break ties by ID.
- Route mismatches return bare 404. Unsupported request content types can return 415 ProblemDetails through MVC, outside the business error tables.

## Verification

From `backend/DirectoryService`:

```sh
dotnet build DirectoryService.slnx --configuration Debug --disable-build-servers -m:1
dotnet test DirectoryService.slnx --no-build
```

The integration suite covers persistence and successful/failed business operations with PostgreSQL Testcontainers.
