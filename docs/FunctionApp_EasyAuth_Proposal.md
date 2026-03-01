# Function App HTTP Security Proposal (Easy Auth)

## Decision (Proposed)
- Use Azure Function App Authentication (Easy Auth) with Microsoft Entra ID as the primary access control for HTTP endpoints.
- Restrict allowed principals to internal BI team members via one Entra security group.
- Treat Function key as secondary/legacy protection while rollout is in progress.

## Why
- URL knowledge alone must not permit invocation.
- Team size is small; group-based access is simpler than app-level RBAC implementation.
- Access lifecycle is straightforward (add/remove group membership).

## Current Code Impact
- No mandatory application code change is required to enable Easy Auth at platform level.
- Existing trigger is currently `AuthorizationLevel.Function`; this can remain during transition.
- Optional hardening later: in-code claim/group validation for defense-in-depth.

## TDD Inputs For Future Work
Use the following acceptance criteria to derive tests once auth hardening stories are scheduled.

1. Anonymous request without bearer token returns unauthorized.
2. Authenticated user outside allowed Entra group is denied.
3. Authenticated user in allowed Entra group can call the endpoint.
4. Expired or invalid bearer token is rejected.
5. Existing business validation behavior remains unchanged for authorized callers.

## Dependencies / Prerequisites
- Function App Authentication enabled in Azure.
- Allowed tenant configured.
- Allowed Entra group created and assigned.

## Open Questions
- Keep `AuthorizationLevel.Function` long-term or switch to `Anonymous` once Easy Auth is fully enforced.
- Whether to add in-code authorization checks after platform rollout.
