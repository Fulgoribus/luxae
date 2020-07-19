CREATE TABLE [dbo].[SeriesAuthors](
    [SeriesId] [int] NOT NULL,
    [PersonId] [int] NOT NULL,
    [Name] [nvarchar](max) NULL,
    [RoleDesc] [nvarchar](100) NULL,
    [SortOrder] [smallint] NOT NULL CONSTRAINT [DF_SeriesAuthors_SortOrder] DEFAULT (0),
    CONSTRAINT [PK_SeriesAuthors] PRIMARY KEY CLUSTERED ([SeriesId] ASC, [PersonId] ASC),
    CONSTRAINT [FK_SeriesAuthors_Series] FOREIGN KEY ([SeriesId]) REFERENCES [dbo].[Series] ([SeriesId]) ON DELETE CASCADE,
    CONSTRAINT [FK_SeriesAuthors_People] FOREIGN KEY ([PersonId]) REFERENCES [dbo].[People] ([PersonId]) ON DELETE CASCADE
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_SeriesAuthors_PersonId ON [dbo].[SeriesAuthors] ([PersonId] ASC)
GO