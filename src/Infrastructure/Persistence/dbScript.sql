-- Create database if it doesn't exist
IF NOT EXISTS (SELECT 1
               FROM sys.databases
               WHERE name = N'genesys_landing_crc')
    BEGIN
        EXEC ('CREATE DATABASE genesys_landing_crc');
    END;
GO

-- Create schema if it doesn't exist
IF SCHEMA_ID(N'ref') IS NULL
    EXEC (N'CREATE SCHEMA ref');
GO

/* region ========== *** References *** ========== */

/* region ========== ** Skills ** ========== */
IF OBJECT_ID(N'ref.skills', N'U') IS NULL
    BEGIN
        CREATE TABLE [ref].[skills]
        (
            [id]             UNIQUEIDENTIFIER  NOT NULL,
            [name]           NVARCHAR(255)     NULL,
            [date_modified]  DATETIMEOFFSET(0) NULL,
            [state]          NVARCHAR(8)       NULL,
            [version]        NVARCHAR(8)       NULL,
            [app_created_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_skills_app_created_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), DATENAME(TzOffset,
                                                                                                          SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                          'Eastern Standard Time'))),
            [app_updated_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_skills_app_updated_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), DATENAME(TzOffset,
                                                                                                          SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                          'Eastern Standard Time'))),

            CONSTRAINT [PK_skills] PRIMARY KEY ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_skills_name'
                 AND object_id = OBJECT_ID(N'ref.skills'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_skills_name]
            ON [ref].[skills] ([name]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_skills_app_updated_at'
                 AND object_id = OBJECT_ID(N'ref.skills'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_skills_app_updated_at]
            ON [ref].[skills] ([app_updated_at]);
    END
GO
/* endregion */

/* region ========== ** Presence Definitions ** ========== */
IF OBJECT_ID(N'ref.presence_definitions', N'U') IS NULL
    BEGIN
        CREATE TABLE [ref].[presence_definitions]
        (
            [id]              UNIQUEIDENTIFIER  NOT NULL,
            [type]            NVARCHAR(6)       NULL,
            [language_label]  NVARCHAR(255)     NULL,
            [system_presence] NVARCHAR(9)       NULL,
            [division_id]     NVARCHAR(36)      NULL,
            [deactivated]     BIT               NULL,
            [app_created_at]  DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_presence_definitions_app_created_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                          DATENAME(TzOffset,
                                                                                                   SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                   'Eastern Standard Time'))),
            [app_updated_at]  DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_presence_definitions_app_updated_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                          DATENAME(TzOffset,
                                                                                                   SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                   'Eastern Standard Time'))),

            CONSTRAINT [PK_presence_definitions] PRIMARY KEY ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_presence_definitions_app_updated_at'
                 AND object_id = OBJECT_ID(N'ref.presence_definitions'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_presence_definitions_app_updated_at]
            ON [ref].[presence_definitions] ([app_updated_at]);
    END
GO
/* endregion */

/* region ========== ** Groups ** ========== */
IF OBJECT_ID(N'ref.groups', N'U') IS NULL
    BEGIN
        CREATE TABLE [ref].[groups]
        (
            [id]             UNIQUEIDENTIFIER  NOT NULL,
            [name]           NVARCHAR(255)     NULL,
            [description]    NVARCHAR(255)     NULL,
            [date_modified]  DATETIMEOFFSET(0) NULL,
            [member_count]   INT               NULL,
            [state]          NVARCHAR(8)       NULL,
            [version]        INT               NULL,
            [type]           NVARCHAR(8)       NULL,
            [rules_visible]  BIT               NULL,
            [visibility]     NVARCHAR(7)       NULL,
            [chat_jabber_id] NVARCHAR(255)     NULL,
            [roles_enabled]  BIT               NULL,
            [include_owners] BIT               NULL,
            [app_created_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_groups_app_created_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), DATENAME(TzOffset,
                                                                                                          SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                          'Eastern Standard Time'))),
            [app_updated_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_groups_app_updated_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), DATENAME(TzOffset,
                                                                                                          SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                          'Eastern Standard Time'))),

            CONSTRAINT [PK_groups] PRIMARY KEY ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_groups_name'
                 AND object_id = OBJECT_ID(N'ref.groups'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_groups_name]
            ON [ref].[groups] ([name]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_groups_app_updated_at'
                 AND object_id = OBJECT_ID(N'ref.groups'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_groups_app_updated_at]
            ON [ref].[groups] ([app_updated_at]);
    END
GO
/* endregion */

/* region ========== ** Wrap Up Codes ** ========== */
IF OBJECT_ID(N'ref.wrap_up_codes', N'U') IS NULL
    BEGIN
        CREATE TABLE [ref].[wrap_up_codes]
        (
            [id]             UNIQUEIDENTIFIER  NOT NULL,
            [name]           NVARCHAR(255)     NULL,
            [division_id]    NVARCHAR(36)      NULL,
            [division_name]  NVARCHAR(255)     NULL,
            [date_created]   DATETIMEOFFSET(0) NULL,
            [date_modified]  DATETIMEOFFSET(0) NULL,
            [created_by]     NVARCHAR(36)      NULL,
            [modified_by]    NVARCHAR(36)      NULL,
            [state]          NVARCHAR(8)       NULL,
            [app_created_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_wrap_up_codes_app_created_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                   DATENAME(TzOffset,
                                                                                            SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                            'Eastern Standard Time'))),
            [app_updated_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_wrap_up_codes_app_updated_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                   DATENAME(TzOffset,
                                                                                            SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                            'Eastern Standard Time'))),

            CONSTRAINT [PK_wrap_up_codes] PRIMARY KEY ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_wrap_up_codes_name'
                 AND object_id = OBJECT_ID(N'ref.wrap_up_codes'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_wrap_up_codes_name]
            ON [ref].[wrap_up_codes] ([name]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_wrap_up_codes_division_id'
                 AND object_id = OBJECT_ID(N'ref.wrap_up_codes'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_wrap_up_codes_division_id]
            ON [ref].[wrap_up_codes] ([division_id]);
    END
GO
/* endregion */

/* endregion */

/* region ========== *** User Details *** ========== */
IF OBJECT_ID(N'dbo.user_details_primary_presence_stg', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[user_details_primary_presence_stg]
        (
            [user_id]                  UNIQUEIDENTIFIER  NOT NULL,
            [start_time]               DATETIMEOFFSET(3) NOT NULL,
            [end_time]                 DATETIMEOFFSET(3) NULL,
            [duration_in_seconds]      BIGINT            NULL,
            [system_presence]          NVARCHAR(9)       NOT NULL,
            [organization_presence_id] NVARCHAR(255)     NULL,
            [app_created_at]           DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_user_details_primary_presence_stg_app_created_at] DEFAULT (SWITCHOFFSET(
                    SYSDATETIMEOFFSET(), DATENAME(TzOffset, SYSDATETIMEOFFSET() AT TIME ZONE 'Eastern Standard Time'))),
            [app_updated_at]           DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_user_details_primary_presence_stg_app_updated_at] DEFAULT (SWITCHOFFSET(
                    SYSDATETIMEOFFSET(), DATENAME(TzOffset, SYSDATETIMEOFFSET() AT TIME ZONE 'Eastern Standard Time'))),

            CONSTRAINT [PK_user_details_primary_presence_stg] PRIMARY KEY CLUSTERED ([user_id], [start_time])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_user_details_primary_presence_stg_system_presence'
                 AND object_id = OBJECT_ID(N'dbo.user_details_primary_presence_stg'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_user_details_primary_presence_stg_system_presence]
            ON [dbo].[user_details_primary_presence_stg] ([system_presence]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_user_details_primary_presence_stg_app_updated_at'
                 AND object_id = OBJECT_ID(N'dbo.user_details_primary_presence_stg'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_user_details_primary_presence_stg_app_updated_at]
            ON [dbo].[user_details_primary_presence_stg] ([app_updated_at]);
    END
GO

IF OBJECT_ID(N'dbo.user_details_routing_status_stg', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[user_details_routing_status_stg]
        (
            [user_id]             UNIQUEIDENTIFIER  NOT NULL,
            [start_time]          DATETIMEOFFSET(3) NOT NULL,
            [end_time]            DATETIMEOFFSET(3) NULL,
            [duration_in_seconds] BIGINT            NULL,
            [routing_status]      NVARCHAR(15)      NOT NULL,
            [app_created_at]      DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_user_details_routing_status_stg_app_created_at] DEFAULT (SWITCHOFFSET(
                    SYSDATETIMEOFFSET(), DATENAME(TzOffset, SYSDATETIMEOFFSET() AT TIME ZONE 'Eastern Standard Time'))),
            [app_updated_at]      DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_user_details_routing_status_stg_app_updated_at] DEFAULT (SWITCHOFFSET(
                    SYSDATETIMEOFFSET(), DATENAME(TzOffset, SYSDATETIMEOFFSET() AT TIME ZONE 'Eastern Standard Time'))),

            CONSTRAINT [PK_user_details_routing_status_stg] PRIMARY KEY CLUSTERED ([user_id], [start_time])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_user_details_routing_status_stg_routing_status'
                 AND object_id = OBJECT_ID(N'dbo.user_details_routing_status_stg'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_user_details_routing_status_stg_routing_status]
            ON [dbo].[user_details_routing_status_stg] ([routing_status]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_user_details_routing_status_stg_app_updated_at'
                 AND object_id = OBJECT_ID(N'dbo.user_details_routing_status_stg'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_user_details_routing_status_stg_app_updated_at]
            ON [dbo].[user_details_routing_status_stg] ([app_updated_at]);
    END
GO
/* endregion */

/* region ========== *** Sync Job Tracking *** ========== */
IF OBJECT_ID(N'dbo.sync_job_tracking', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[sync_job_tracking]
        (
            [id]                       [bigint] IDENTITY (1,1) NOT NULL,
            [category]                 [nvarchar](50)          NOT NULL,
            [interval]                 [nvarchar](50)          NULL,
            [page_number]              [int]                   NULL,
            [job_id]                   [nvarchar](100)         NULL,
            [is_incremental_completed] [bit]                   NOT NULL,
            [is_recovery_completed]    [bit]                   NOT NULL,
            [app_created_at]           DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_sync_job_tracking_app_created_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                       DATENAME(TzOffset,
                                                                                                SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                'Eastern Standard Time'))),
            [app_updated_at]           DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_sync_job_tracking_app_updated_at] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                       DATENAME(TzOffset,
                                                                                                SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                'Eastern Standard Time'))),

            CONSTRAINT [PK_sync_job_tracking] PRIMARY KEY CLUSTERED ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_job_tracking_category'
                 AND object_id = OBJECT_ID(N'dbo.sync_job_tracking'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_job_tracking_category]
            ON [dbo].[sync_job_tracking] ([category]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_job_tracking_is_incremental_completed'
                 AND object_id = OBJECT_ID(N'dbo.sync_job_tracking'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_job_tracking_is_incremental_completed]
            ON [dbo].[sync_job_tracking] ([is_incremental_completed]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_job_tracking_is_recovery_completed'
                 AND object_id = OBJECT_ID(N'dbo.sync_job_tracking'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_job_tracking_is_recovery_completed]
            ON [dbo].[sync_job_tracking] ([is_recovery_completed]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_job_tracking_app_updated_at'
                 AND object_id = OBJECT_ID(N'dbo.sync_job_tracking'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_job_tracking_app_updated_at]
            ON [dbo].[sync_job_tracking] ([app_updated_at]);
    END
GO
/* endregion */
