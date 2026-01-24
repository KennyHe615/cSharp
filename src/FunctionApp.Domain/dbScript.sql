-- Create the schema if it doesn't exist
IF SCHEMA_ID(N'ref') IS NULL EXEC (N'CREATE SCHEMA [ref];');
GO

/* region ========== *** References *** ========== */

/* region ========== ** Skills ** ========== */
CREATE TABLE [ref].[skills]
(
    [id]             uniqueidentifier  NOT NULL,
    [name]           nvarchar(255)     NULL,
    [date_modified]  datetimeoffset(0) NULL,
    [state]          nvarchar(8)       NULL,
    [version]        nvarchar(8)       NULL,
    [app_created_at] datetimeoffset(0) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [app_updated_at] datetimeoffset(0) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),

    CONSTRAINT [pk_skills] PRIMARY KEY ([id])
);
GO

CREATE INDEX [ix_skills_name] ON [ref].[skills] ([name]);
GO
CREATE INDEX [ix_skills_app_updated_at] ON [ref].[skills] ([app_updated_at]);
GO
/* endregion */

/* region ========== ** Presence Definitions ** ========== */
CREATE TABLE [ref].[presence_definitions]
(
    [id]              uniqueidentifier  NOT NULL,
    [language_label]  nvarchar(255)     NULL,
    [system_presence] nvarchar(9)       NULL,
    [type]            nvarchar(6)       NULL,
    [deactivated]     bit               NULL,
    [division_id]     nvarchar(36)      NULL,
    [app_created_at]  datetimeoffset(0) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [app_updated_at]  datetimeoffset(0) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),

    CONSTRAINT [pk_presence_definitions] PRIMARY KEY ([id])
);
GO

CREATE INDEX [ix_presence_definitions_app_updated_at] ON [ref].[presence_definitions] ([app_updated_at]);
GO
/* endregion */
/* endregion */
