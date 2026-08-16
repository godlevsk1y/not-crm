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

# Git Workflow

notCRM follows GitHub Flow. When a new feature or change is needed, create a
new branch from `main`. Make the changes on that branch, open a pull request
against `main`, and merge it after review and the required checks pass.

## Commit Naming Conventions

Use Conventional Commits for commit messages:

```text
<type>(<scope>): <short imperative description>
```

Use a lowercase type such as `feat`, `fix`, `docs`, `refactor`, `test`, or
`chore`. The scope is optional and should identify the affected area. Keep the
description concise, start it with a verb, and do not end it with a period.
For breaking changes, append `!` after the type or scope and explain the
breaking change in the commit body or footer.

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
