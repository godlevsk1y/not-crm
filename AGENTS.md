# notCRM Overview

> Repository-wide instructions for AI coding agents.
> These instructions apply to the entire repository. More specific `AGENTS.md`
> files in subdirectories override or extend these instructions.

# Instruction Hierarchy

Instructions are applied from general to specific.

Priority order:

1. User request
2. Service/application AGENTS.md
3. Root AGENTS.md
4. Existing repository conventions

More specific instructions override more general instructions.

# Repository Overview

This is a monorepo containing:

- frontend applications
- backend microservices

Main directories:

- `/frontend` - web applications
- `/backend` - backend services

Each service in `/backend` directory is independently deployable and typically contains:

- its own solution (`*.sln`)
- one or more ASP.NET Core applications
- Docker configuration

Do not assume all services use the same libraries or architecture.
Before making changes, inspect the service-specific `AGENTS.md` (if present).

# General Principles

Always:

- Make the smallest change that solves the requested problem.
- Preserve existing architecture.
- Follow existing coding style.
- Keep changes focused.
- Avoid unrelated refactoring.
- Prefer consistency over introducing new patterns.
  
Never:

- Rewrite large portions of code without being asked.
- Rename public APIs.
- Remove tests.
- Change behavior outside the requested scope.
- Introduce unnecessary abstractions.
- Commit secrets or credentials.

# Before Making Changes

Inspect:

- the relevant solution file
- the relevant project file
- Directory.Build.props
- Directory.Packages.props
- existing tests
- any service-specific AGENTS.md

Understand the existing implementation before editing code.

# Architecture

Respect the architecture already used by each service.

Do not move responsibilities between layers unless explicitly requested.

Typical boundaries:

- API layer handles HTTP concerns.
- Core layer contains business logic.
- Domain layer contains business rules.
- Infrastructure handles persistence and external services.

Do not bypass existing abstractions.

Each project in `/backend` directory is separated in several layers:

| Layer          | Responsibilities                                                                                      |
|----------------|-------------------------------------------------------------------------------------------------------|
| Domain         | Contains business rules, which are represented via models and value objects                           |
| Core           | Contains business logic                                                                               |
| Infrastructure | Handles persistence and external services. This layer is NEVER referenced by Application or Domain    |
| Presentation   | Represents an "entry point" of the application. Usually, it is a Web API, that handles HTTP concernes |
| Contracts      | A layer for DTOs                                                                                      |