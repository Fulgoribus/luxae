ALTER TABLE [dbo].[Books] DROP COLUMN Author;
GO
CREATE TABLE [dbo].[People](
    [PersonId] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](max) NOT NULL,
    CONSTRAINT [PK_People] PRIMARY KEY CLUSTERED ([PersonId] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE TABLE [dbo].[BookAuthors](
    [BookId] [int] NOT NULL,
    [PersonId] [int] NOT NULL,
    [Name] [nvarchar](max) NULL,
    [RoleDesc] [nvarchar](100) NULL,
    [SortOrder] [smallint] NOT NULL CONSTRAINT [DF_BookAuthors_SortOrder] DEFAULT (0),
    CONSTRAINT [PK_BookAuthors] PRIMARY KEY CLUSTERED ([BookId] ASC, [PersonId] ASC),
    CONSTRAINT [FK_BookAuthors_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([BookId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookAuthors_People] FOREIGN KEY ([PersonId]) REFERENCES [dbo].[People] ([PersonId]) ON DELETE CASCADE
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_BookAuthors_PersonId ON [dbo].[BookAuthors] ([PersonId] ASC)
GO
CREATE TABLE [dbo].[BookIllustrators](
    [BookId] [int] NOT NULL,
    [PersonId] [int] NOT NULL,
    [Name] [nvarchar](max) NULL,
    [RoleDesc] [nvarchar](100) NULL,
    [SortOrder] [smallint] NOT NULL CONSTRAINT [DF_BookIllustrators_SortOrder] DEFAULT (0),
    CONSTRAINT [PK_BookIllustrators] PRIMARY KEY CLUSTERED ([BookId] ASC, [PersonId] ASC),
    CONSTRAINT [FK_BookIllustrators_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([BookId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookIllustrators_People] FOREIGN KEY ([PersonId]) REFERENCES [dbo].[People] ([PersonId]) ON DELETE CASCADE
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_BookIllustrators_PersonId ON [dbo].[BookIllustrators] ([PersonId] ASC)
GO
CREATE TABLE [dbo].[BookTranslators](
    [BookId] [int] NOT NULL,
    [PersonId] [int] NOT NULL,
    [Name] [nvarchar](max) NULL,
    [RoleDesc] [nvarchar](100) NULL,
    [SortOrder] [smallint] NOT NULL CONSTRAINT [DF_BookTranslators_SortOrder] DEFAULT (0),
    CONSTRAINT [PK_BookTranslators] PRIMARY KEY CLUSTERED ([BookId] ASC, [PersonId] ASC),
    CONSTRAINT [FK_BookTranslators_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([BookId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookTranslators_People] FOREIGN KEY ([PersonId]) REFERENCES [dbo].[People] ([PersonId]) ON DELETE CASCADE
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_BookTranslators_PersonId ON [dbo].[BookTranslators] ([PersonId] ASC)
GO