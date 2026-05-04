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
            [id]                     UNIQUEIDENTIFIER  NOT NULL,
            [name]                   NVARCHAR(255)     NULL,
            [date_modified]          DATETIMEOFFSET(0) NULL,
            [state]                  NVARCHAR(8)       NULL,
            [version]                NVARCHAR(8)       NULL,
            [app_created_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_skills_app_created_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                    DATENAME(TzOffset,
                                                                                             SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                             'Eastern Standard Time'))),
            [app_updated_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_skills_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                    DATENAME(TzOffset,
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
               WHERE name = N'IX_skills_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'ref.skills'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_skills_app_updated_at_eastern]
            ON [ref].[skills] ([app_updated_at_eastern]);
    END
GO
/* endregion */

/* region ========== ** Presence Definitions ** ========== */
IF OBJECT_ID(N'ref.presence_definitions', N'U') IS NULL
    BEGIN
        CREATE TABLE [ref].[presence_definitions]
        (
            [id]                     UNIQUEIDENTIFIER  NOT NULL,
            [type]                   NVARCHAR(6)       NULL,
            [language_label]         NVARCHAR(255)     NULL,
            [system_presence]        NVARCHAR(9)       NULL,
            [division_id]            NVARCHAR(36)      NULL,
            [deactivated]            BIT               NULL,
            [app_created_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_presence_definitions_app_created_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                                  DATENAME(TzOffset,
                                                                                                           SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                           'Eastern Standard Time'))),
            [app_updated_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_presence_definitions_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                                  DATENAME(TzOffset,
                                                                                                           SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                           'Eastern Standard Time'))),

            CONSTRAINT [PK_presence_definitions] PRIMARY KEY ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_presence_definitions_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'ref.presence_definitions'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_presence_definitions_app_updated_at_eastern]
            ON [ref].[presence_definitions] ([app_updated_at_eastern]);
    END
GO
/* endregion */

/* region ========== ** Groups ** ========== */
IF OBJECT_ID(N'ref.groups', N'U') IS NULL
    BEGIN
        CREATE TABLE [ref].[groups]
        (
            [id]                     UNIQUEIDENTIFIER  NOT NULL,
            [name]                   NVARCHAR(255)     NULL,
            [description]            NVARCHAR(255)     NULL,
            [date_modified]          DATETIMEOFFSET(0) NULL,
            [member_count]           INT               NULL,
            [state]                  NVARCHAR(8)       NULL,
            [version]                INT               NULL,
            [type]                   NVARCHAR(8)       NULL,
            [rules_visible]          BIT               NULL,
            [visibility]             NVARCHAR(7)       NULL,
            [chat_jabber_id]         NVARCHAR(255)     NULL,
            [roles_enabled]          BIT               NULL,
            [include_owners]         BIT               NULL,
            [app_created_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_groups_app_created_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                    DATENAME(TzOffset,
                                                                                             SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                             'Eastern Standard Time'))),
            [app_updated_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_groups_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                    DATENAME(TzOffset,
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
               WHERE name = N'IX_groups_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'ref.groups'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_groups_app_updated_at_eastern]
            ON [ref].[groups] ([app_updated_at_eastern]);
    END
GO
/* endregion */

/* region ========== ** Wrap Up Codes ** ========== */
IF OBJECT_ID(N'ref.wrap_up_codes', N'U') IS NULL
    BEGIN
        CREATE TABLE [ref].[wrap_up_codes]
        (
            [id]                     UNIQUEIDENTIFIER  NOT NULL,
            [name]                   NVARCHAR(255)     NULL,
            [division_id]            NVARCHAR(36)      NULL,
            [division_name]          NVARCHAR(255)     NULL,
            [date_created]           DATETIMEOFFSET(0) NULL,
            [date_modified]          DATETIMEOFFSET(0) NULL,
            [created_by]             NVARCHAR(36)      NULL,
            [modified_by]            NVARCHAR(36)      NULL,
            [state]                  NVARCHAR(8)       NULL,
            [app_created_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_wrap_up_codes_app_created_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                           DATENAME(TzOffset,
                                                                                                    SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                    'Eastern Standard Time'))),
            [app_updated_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_wrap_up_codes_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
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
            [app_created_at_eastern]   DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_user_details_primary_presence_stg_app_created_at_eastern] DEFAULT (SWITCHOFFSET(
                    SYSDATETIMEOFFSET(), DATENAME(TzOffset, SYSDATETIMEOFFSET() AT TIME ZONE 'Eastern Standard Time'))),
            [app_updated_at_eastern]   DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_user_details_primary_presence_stg_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(
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
               WHERE name = N'IX_user_details_primary_presence_stg_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.user_details_primary_presence_stg'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_user_details_primary_presence_stg_app_updated_at_eastern]
            ON [dbo].[user_details_primary_presence_stg] ([app_updated_at_eastern]);
    END
GO

IF OBJECT_ID(N'dbo.user_details_routing_status_stg', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[user_details_routing_status_stg]
        (
            [user_id]                UNIQUEIDENTIFIER  NOT NULL,
            [start_time]             DATETIMEOFFSET(3) NOT NULL,
            [end_time]               DATETIMEOFFSET(3) NULL,
            [duration_in_seconds]    BIGINT            NULL,
            [routing_status]         NVARCHAR(15)      NOT NULL,
            [app_created_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_user_details_routing_status_stg_app_created_at_eastern] DEFAULT (SWITCHOFFSET(
                    SYSDATETIMEOFFSET(), DATENAME(TzOffset, SYSDATETIMEOFFSET() AT TIME ZONE 'Eastern Standard Time'))),
            [app_updated_at_eastern] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_user_details_routing_status_stg_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(
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
               WHERE name = N'IX_user_details_routing_status_stg_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.user_details_routing_status_stg'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_user_details_routing_status_stg_app_updated_at_eastern]
            ON [dbo].[user_details_routing_status_stg] ([app_updated_at_eastern]);
    END
GO
/* endregion */

/* region ========== *** Sync Tracking *** ========== */

/* region ========== ** Sync Tracking: Request ** ========== */
IF OBJECT_ID(N'dbo.sync_request', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[sync_request]
        (
            [id]                     [bigint] IDENTITY (1,1) NOT NULL,
            -- Client-facing immutable identifier (internal joins still use bigint id).
            [public_id]              [uniqueidentifier]      NOT NULL
                CONSTRAINT [DF_sync_request_public_id] DEFAULT (NEWSEQUENTIALID()),
            [category]               [nvarchar](50)          NOT NULL,
            [mode]                   [nvarchar](20)          NOT NULL,
            -- Request-level lifecycle state used by recovery reuse/create decision logic.
            [status]                 [nvarchar](20)          NOT NULL
                CONSTRAINT [DF_sync_request_status] DEFAULT ('PENDING'),
            -- Number of reopen operations applied to this request.
            [reopen_count]           [int]                   NOT NULL
                CONSTRAINT [DF_sync_request_reopen_count] DEFAULT ((0)),
            [interval]               [nvarchar](50)          NULL,
            [page_number]            [int]                   NULL,
            [genesys_job_id]         [nvarchar](100)         NULL,
            -- Canonical scope identity: category|mode|interval|page|job
            [scope_key]              [nvarchar](255)         NOT NULL,
            [current_run_id]         [bigint]                NULL,
            [app_created_at_eastern] DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_sync_request_app_created_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                          DATENAME(TzOffset,
                                                                                                   SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                   'Eastern Standard Time'))),
            [app_updated_at_eastern] DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_sync_request_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                          DATENAME(TzOffset,
                                                                                                   SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                   'Eastern Standard Time'))),

            CONSTRAINT [PK_sync_request] PRIMARY KEY CLUSTERED ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'UX_sync_request_public_id'
                 AND object_id = OBJECT_ID(N'dbo.sync_request'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [UX_sync_request_public_id]
            ON [dbo].[sync_request] ([public_id]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'UX_sync_request_scope_key_incremental'
                 AND object_id = OBJECT_ID(N'dbo.sync_request'))
    BEGIN
        -- Incremental mode: one logical request row per scope.
        CREATE UNIQUE NONCLUSTERED INDEX [UX_sync_request_scope_key_incremental]
            ON [dbo].[sync_request] ([scope_key])
            WHERE [mode] = 'INCREMENTAL';
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'UX_sync_request_scope_key_recovery_active'
                 AND object_id = OBJECT_ID(N'dbo.sync_request'))
    BEGIN
        -- Recovery mode: allow history, but enforce only one active request per scope.
        CREATE UNIQUE NONCLUSTERED INDEX [UX_sync_request_scope_key_recovery_active]
            ON [dbo].[sync_request] ([scope_key])
            WHERE [mode] = 'RECOVERY' AND [status] IN ('PENDING', 'RUNNING');
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_request_mode_scope_key_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.sync_request'))
    BEGIN
        -- Supports latest-recovery-row lookup by scope.
        CREATE NONCLUSTERED INDEX [IX_sync_request_mode_scope_key_app_updated_at_eastern]
            ON [dbo].[sync_request] ([mode], [scope_key], [app_updated_at_eastern]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_request_category_mode_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.sync_request'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_request_category_mode_app_updated_at_eastern]
            ON [dbo].[sync_request] ([category], [mode], [app_updated_at_eastern]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_request_current_run_id'
                 AND object_id = OBJECT_ID(N'dbo.sync_request'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_request_current_run_id]
            ON [dbo].[sync_request] ([current_run_id]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_request_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.sync_request'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_request_app_updated_at_eastern]
            ON [dbo].[sync_request] ([app_updated_at_eastern]);
    END
GO
/* endregion */

/* region ========== ** Sync Tracking: Run ** ========== */
IF OBJECT_ID(N'dbo.sync_run', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[sync_run]
        (
            [id]                       [bigint] IDENTITY (1,1) NOT NULL,
            [request_id]               [bigint]                NOT NULL,
            [status]                   [nvarchar](20)          NOT NULL,
            [superseded_by_run_id]     [bigint]                NULL,
            [attempt_no]               [int]                   NOT NULL
                CONSTRAINT [DF_sync_run_attempt_no] DEFAULT ((1)),
            [run_started_at_eastern]   DATETIMEOFFSET(0)       NULL,
            [run_completed_at_eastern] DATETIMEOFFSET(0)       NULL,
            [failure_reason]           [nvarchar](1000)        NULL,
            [app_created_at_eastern]   DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_sync_run_app_created_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                      DATENAME(TzOffset,
                                                                                               SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                               'Eastern Standard Time'))),
            [app_updated_at_eastern]   DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_sync_run_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                      DATENAME(TzOffset,
                                                                                               SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                               'Eastern Standard Time'))),

            CONSTRAINT [PK_sync_run] PRIMARY KEY CLUSTERED ([id]),
            CONSTRAINT [FK_sync_run_request_id] FOREIGN KEY ([request_id]) REFERENCES [dbo].[sync_request] ([id]),
            CONSTRAINT [FK_sync_run_superseded_by_run_id] FOREIGN KEY ([superseded_by_run_id]) REFERENCES [dbo].[sync_run] ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_run_request_id_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.sync_run'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_run_request_id_app_updated_at_eastern]
            ON [dbo].[sync_run] ([request_id], [app_updated_at_eastern]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_run_status_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.sync_run'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_run_status_app_updated_at_eastern]
            ON [dbo].[sync_run] ([status], [app_updated_at_eastern]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_run_superseded_by_run_id'
                 AND object_id = OBJECT_ID(N'dbo.sync_run'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_run_superseded_by_run_id]
            ON [dbo].[sync_run] ([superseded_by_run_id]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'UX_sync_run_request_active'
                 AND object_id = OBJECT_ID(N'dbo.sync_run'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [UX_sync_run_request_active]
            ON [dbo].[sync_run] ([request_id])
            WHERE [status] IN ('PENDING', 'RUNNING');
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_run_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.sync_run'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_run_app_updated_at_eastern]
            ON [dbo].[sync_run] ([app_updated_at_eastern]);
    END
GO
/* endregion */

/* region ========== ** Cross-table FK (sync_request.current_run_id -> sync_run.id) ** ========== */
IF NOT EXISTS (SELECT 1
               FROM sys.foreign_keys
               WHERE name = N'FK_sync_request_current_run_id'
                 AND parent_object_id = OBJECT_ID(N'dbo.sync_request'))
    BEGIN
        ALTER TABLE [dbo].[sync_request]
            ADD CONSTRAINT [FK_sync_request_current_run_id]
                FOREIGN KEY ([current_run_id]) REFERENCES [dbo].[sync_run] ([id]);
    END
GO
/* endregion */

/* region ========== ** Sync Tracking: Run Item ** ========== */
IF OBJECT_ID(N'dbo.sync_run_item', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[sync_run_item]
        (
            [id]                     [bigint] IDENTITY (1,1) NOT NULL,
            [run_id]                 [bigint]                NOT NULL,
            [step]                   [nvarchar](50)          NOT NULL,
            [cursor]                 [nvarchar](200)         NOT NULL,
            [status]                 [nvarchar](20)          NOT NULL,
            [failure_reason]         [nvarchar](1000)        NULL,
            [app_created_at_eastern] DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_sync_run_item_app_created_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                           DATENAME(TzOffset,
                                                                                                    SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                    'Eastern Standard Time'))),
            [app_updated_at_eastern] DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_sync_run_item_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(),
                                                                                           DATENAME(TzOffset,
                                                                                                    SYSDATETIMEOFFSET() AT TIME ZONE
                                                                                                    'Eastern Standard Time'))),

            CONSTRAINT [PK_sync_run_item] PRIMARY KEY CLUSTERED ([id]),
            CONSTRAINT [FK_sync_run_item_run_id] FOREIGN KEY ([run_id]) REFERENCES [dbo].[sync_run] ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'UX_sync_run_item_run_step_cursor'
                 AND object_id = OBJECT_ID(N'dbo.sync_run_item'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [UX_sync_run_item_run_step_cursor]
            ON [dbo].[sync_run_item] ([run_id], [step], [cursor]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_run_item_run_status_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.sync_run_item'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_run_item_run_status_app_updated_at_eastern]
            ON [dbo].[sync_run_item] ([run_id], [status], [app_updated_at_eastern]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_sync_run_item_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.sync_run_item'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_sync_run_item_app_updated_at_eastern]
            ON [dbo].[sync_run_item] ([app_updated_at_eastern]);
    END
GO
/* endregion */

/* region ========== ** Sync Tracking: Incremental Sync Window ** ========== */
IF OBJECT_ID(N'dbo.incremental_sync_window', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[incremental_sync_window]
        (
            [id]                      [bigint] IDENTITY (1,1) NOT NULL,
            [category]                [nvarchar](50)          NOT NULL,
            [next_interval_start_utc] DATETIMEOFFSET(0)       NOT NULL,
            [last_reserved_start_utc] DATETIMEOFFSET(0)       NULL,
            [last_reserved_end_utc]   DATETIMEOFFSET(0)       NULL,
            [row_version]             [rowversion]            NOT NULL,
            [app_created_at_eastern]  DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_incremental_sync_window_app_created_at_eastern] DEFAULT (SWITCHOFFSET(
                    SYSDATETIMEOFFSET(),
                    DATENAME(TzOffset,
                             SYSDATETIMEOFFSET() AT TIME ZONE
                             'Eastern Standard Time'))),
            [app_updated_at_eastern]  DATETIMEOFFSET(0)       NOT NULL
                CONSTRAINT [DF_incremental_sync_window_app_updated_at_eastern] DEFAULT (SWITCHOFFSET(
                    SYSDATETIMEOFFSET(),
                    DATENAME(TzOffset,
                             SYSDATETIMEOFFSET() AT TIME ZONE
                             'Eastern Standard Time'))),

            CONSTRAINT [PK_incremental_sync_window] PRIMARY KEY CLUSTERED ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'UX_incremental_sync_window_category'
                 AND object_id = OBJECT_ID(N'dbo.incremental_sync_window'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [UX_incremental_sync_window_category]
            ON [dbo].[incremental_sync_window] ([category]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_incremental_sync_window_app_updated_at_eastern'
                 AND object_id = OBJECT_ID(N'dbo.incremental_sync_window'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_incremental_sync_window_app_updated_at_eastern]
            ON [dbo].[incremental_sync_window] ([app_updated_at_eastern]);
    END
GO
/* endregion */

/* endregion */
