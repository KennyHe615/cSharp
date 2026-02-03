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
                CONSTRAINT [DF_skills_app_created_at] DEFAULT SYSDATETIMEOFFSET(),
            [app_updated_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_skills_app_updated_at] DEFAULT SYSDATETIMEOFFSET(),

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
                CONSTRAINT [DF_presence_definitions_app_created_at] DEFAULT SYSDATETIMEOFFSET(),
            [app_updated_at]  DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_presence_definitions_app_updated_at] DEFAULT SYSDATETIMEOFFSET(),

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
                CONSTRAINT [DF_groups_app_created_at] DEFAULT SYSDATETIMEOFFSET(),
            [app_updated_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_groups_app_updated_at] DEFAULT SYSDATETIMEOFFSET(),

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

/* region ========== ** Wrapup Codes ** ========== */
IF OBJECT_ID(N'ref.wrapup_codes', N'U') IS NULL
    BEGIN
        CREATE TABLE [ref].[wrapup_codes]
        (
            [id]             UNIQUEIDENTIFIER  NOT NULL,
            [name]           NVARCHAR(255)     NULL,
            [division_id]    NVARCHAR(36)      NULL,
            [division_name]  NVARCHAR(255)     NULL,
            [date_created]   DATETIMEOFFSET(0) NULL,
            [date_modified]  DATETIMEOFFSET(0) NULL,
            [created_by]     NVARCHAR(36)      NULL,
            [modified_by]    NVARCHAR(36)      NULL,
            [app_created_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_wrapup_codes_app_created_at] DEFAULT SYSDATETIMEOFFSET(),
            [app_updated_at] DATETIMEOFFSET(0) NOT NULL
                CONSTRAINT [DF_wrapup_codes_app_updated_at] DEFAULT SYSDATETIMEOFFSET(),

            CONSTRAINT [PK_wrapup_codes] PRIMARY KEY ([id])
        );
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_wrapup_codes_name'
                 AND object_id = OBJECT_ID(N'ref.wrapup_codes'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_wrapup_codes_name]
            ON [ref].[wrapup_codes] ([name]);
    END
GO

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'IX_wrapup_codes_division_id'
                 AND object_id = OBJECT_ID(N'ref.wrapup_codes'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_wrapup_codes_division_id]
            ON [ref].[wrapup_codes] ([division_id]);
    END
GO
/* endregion */

/* endregion */
