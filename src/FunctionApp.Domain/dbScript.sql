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
CREATE INDEX [ix_skills_state] ON [ref].[skills] ([state]);
GO
CREATE INDEX [ix_skills_app_updated_at] ON [ref].[skills] ([app_updated_at]);
GO
/* endregion */

/* endregion */
