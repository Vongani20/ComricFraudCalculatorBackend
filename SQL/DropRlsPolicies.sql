IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantIsolationPolicy_HrEvents')
    DROP SECURITY POLICY dbo.TenantIsolationPolicy_HrEvents;
GO
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantIsolationPolicy_MnoEvents')
    DROP SECURITY POLICY dbo.TenantIsolationPolicy_MnoEvents;
GO
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantIsolationPolicy_ActivityLogs')
    DROP SECURITY POLICY dbo.TenantIsolationPolicy_ActivityLogs;
GO
