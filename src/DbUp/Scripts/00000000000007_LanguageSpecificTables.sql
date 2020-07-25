-- Make the Cultures table.
CREATE TABLE [dbo].[Cultures](
    [CultureCode] VARCHAR(5) NOT NULL,
    [IsDefault] BIT NOT NULL,
    CONSTRAINT [PK_Cultures] PRIMARY KEY CLUSTERED ([CultureCode] ASC)
)
GO
INSERT INTO [dbo].[Cultures] ([CultureCode], [IsDefault]) VALUES ('jp-JP', 1), ('en-US', 0)
GO

-- Make the Releases table.
CREATE TABLE [dbo].[BookReleases](
    [ReleaseId] [int] IDENTITY(1,1) NOT NULL,
    [BookId] [int] NOT NULL,
    [CultureCode] VARCHAR(5) NOT NULL,
    [Title] [nvarchar](250) NOT NULL,
    [ReleaseDate] [date] NULL,
    CONSTRAINT [PK_BookReleases] PRIMARY KEY CLUSTERED ([ReleaseId] ASC),
    CONSTRAINT [FK_BookReleases_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([BookId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookReleases_Cultures] FOREIGN KEY ([CultureCode]) REFERENCES [dbo].[Cultures] ([CultureCode]) ON DELETE CASCADE ON UPDATE CASCADE
)
GO
CREATE UNIQUE NONCLUSTERED INDEX [AK_BookReleases_BookId_CultureCode] ON [dbo].[BookReleases] ([BookId] ASC, [CultureCode] ASC)
GO

-- Seed it with existing data.
INSERT INTO [BookReleases] (BookId, CultureCode, Title, ReleaseDate)
SELECT BookId, 'en-US', Title, ReleaseDate
FROM [dbo].[Books]
GO

-- Change BookTranslators to use ReleaseId instead of BookId.
ALTER TABLE [dbo].[BookTranslators] DROP CONSTRAINT [FK_BookTranslators_Books]
GO
EXEC sp_rename 'dbo.BookTranslators.BookId', 'ReleaseId'
GO
UPDATE bt SET ReleaseId = br.ReleaseId FROM [dbo].[BookTranslators] bt
JOIN [dbo].[BookReleases] br ON br.BookId = bt.ReleaseId AND br.CultureCode = 'en-US'
GO
ALTER TABLE [dbo].[BookTranslators] ADD CONSTRAINT [FK_BookTranslators_BookReleases] FOREIGN KEY ([ReleaseId]) REFERENCES [dbo].[BookReleases] ([ReleaseId]) ON DELETE CASCADE
GO

-- Change BookRetailers to use ReleaseId instead of BookId.
ALTER TABLE [dbo].[BookRetailers] DROP CONSTRAINT [FK_BookRetailers_Books]
GO
EXEC sp_rename 'dbo.BookRetailers.BookId', 'ReleaseId'
GO
UPDATE brt SET ReleaseId = br.ReleaseId FROM [dbo].[BookRetailers] brt
JOIN [dbo].[BookReleases] br ON br.BookId = brt.ReleaseId AND br.CultureCode = 'en-US'
GO
ALTER TABLE [dbo].[BookRetailers] ADD CONSTRAINT [FK_BookRetailers_BookReleases] FOREIGN KEY ([ReleaseId]) REFERENCES [dbo].[BookReleases] ([ReleaseId]) ON DELETE CASCADE
GO

-- Change BookCovers to use ReleaseId instead of BookId.
ALTER TABLE [dbo].[BookCovers] DROP CONSTRAINT [FK_Covers_Books]
GO
EXEC sp_rename 'dbo.BookCovers.BookId', 'ReleaseId'
GO
UPDATE bc SET ReleaseId = br.ReleaseId FROM [dbo].[BookCovers] bc
JOIN [dbo].[BookReleases] br ON br.BookId = bc.ReleaseId AND br.CultureCode = 'en-US'
GO
ALTER TABLE [dbo].[BookCovers] ADD CONSTRAINT [FK_BookCovers_BookReleases] FOREIGN KEY ([ReleaseId]) REFERENCES [dbo].[BookReleases] ([ReleaseId]) ON DELETE CASCADE
GO

-- For now people own books, not releases, May update later.

-- Remove now-unused fields from Books.
ALTER TABLE [dbo].[Books] DROP COLUMN [Title]
GO
ALTER TABLE [dbo].[Books] DROP COLUMN [Label]
GO
