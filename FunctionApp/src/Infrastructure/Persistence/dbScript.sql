-- Create database if it doesn't exist
IF NOT EXISTS (SELECT 1
               FROM sys.databases
               WHERE name = N'genesys_crc_landing')
    BEGIN
        EXEC ('CREATE DATABASE genesys_crc_landing');
    END;
GO

-- Create schema if it doesn't exist
IF NOT EXISTS (SELECT 1
               FROM sys.schemas
               WHERE name = 'ref')
    BEGIN
        EXEC ('CREATE SCHEMA ref');
    END
GO

/* region ========== *** References *** ========== */

/* region ========== ** Skills ** ========== */
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
GO

CREATE INDEX [IX_skills_name] ON [ref].[skills] ([name]);
GO
CREATE INDEX [IX_skills_app_updated_at] ON [ref].[skills] ([app_updated_at]);
GO
/* endregion */

/* region ========== ** Presence Definitions ** ========== */
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
GO

CREATE INDEX [IX_presence_definitions_app_updated_at] ON [ref].[presence_definitions] ([app_updated_at]);
GO
/* endregion */

/* region ========== ** Groups ** ========== */
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
GO

CREATE INDEX [IX_groups_name] ON [ref].[groups] ([name]);
GO
CREATE INDEX [IX_groups_app_updated_at] ON [ref].[groups] ([app_updated_at]);
GO
/* endregion */

/* endregion */
