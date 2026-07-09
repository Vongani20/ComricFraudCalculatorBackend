-- Azure SQL Row-Level Security policies for tenant isolation.
-- Run after EF Core migrations. TDE is enabled at the Azure SQL server/database level
-- via Azure Portal or: ALTER DATABASE [ComricFraud] SET ENCRYPTION ON;

-- Security predicate: row visible only when TenantId matches SESSION_CONTEXT
CREATE OR ALTER FUNCTION dbo.fn_TenantAccessPredicate(@TenantId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS AccessResult
WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS uniqueidentifier);
GO

-- Block predicate for INSERT/UPDATE: new TenantId must match session context
CREATE OR ALTER FUNCTION dbo.fn_TenantBlockPredicate(@TenantId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS BlockResult
WHERE @TenantId <> CAST(SESSION_CONTEXT(N'TenantId') AS uniqueidentifier)
   OR SESSION_CONTEXT(N'TenantId') IS NULL;
GO

-- Drop existing policies if re-running
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantIsolationPolicy_HrEvents')
    DROP SECURITY POLICY dbo.TenantIsolationPolicy_HrEvents;
GO
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantIsolationPolicy_MnoEvents')
    DROP SECURITY POLICY dbo.TenantIsolationPolicy_MnoEvents;
GO
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantIsolationPolicy_ActivityLogs')
    DROP SECURITY POLICY dbo.TenantIsolationPolicy_ActivityLogs;
GO

CREATE SECURITY POLICY dbo.TenantIsolationPolicy_HrEvents
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.HrEvents,
    ADD BLOCK PREDICATE dbo.fn_TenantBlockPredicate(TenantId) ON dbo.HrEvents AFTER INSERT,
    ADD BLOCK PREDICATE dbo.fn_TenantBlockPredicate(TenantId) ON dbo.HrEvents AFTER UPDATE
WITH (STATE = ON, SCHEMABINDING = ON);
GO

CREATE SECURITY POLICY dbo.TenantIsolationPolicy_MnoEvents
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.MnoEvents,
    ADD BLOCK PREDICATE dbo.fn_TenantBlockPredicate(TenantId) ON dbo.MnoEvents AFTER INSERT,
    ADD BLOCK PREDICATE dbo.fn_TenantBlockPredicate(TenantId) ON dbo.MnoEvents AFTER UPDATE
WITH (STATE = ON, SCHEMABINDING = ON);
GO

CREATE SECURITY POLICY dbo.TenantIsolationPolicy_ActivityLogs
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.ActivityLogs,
    ADD BLOCK PREDICATE dbo.fn_TenantBlockPredicate(TenantId) ON dbo.ActivityLogs AFTER INSERT,
    ADD BLOCK PREDICATE dbo.fn_TenantBlockPredicate(TenantId) ON dbo.ActivityLogs AFTER UPDATE
WITH (STATE = ON, SCHEMABINDING = ON);
GO

-- Signals table is intentionally NOT covered by RLS: cross-tenant anonymous aggregation.
-- Tenants table is managed by platform admins only (no RLS in PoC).
