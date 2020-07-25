ALTER TABLE [dbo].[Books] ADD [OriginalCultureCode] VARCHAR(5) NULL
GO
ALTER TABLE [dbo].[Books] DROP COLUMN [ReleaseDate]
GO
ALTER TABLE [dbo].[Cultures] DROP COLUMN [IsDefault]
GO
CREATE VIEW [dbo].[BooksByCulture] AS
-- Get the books specific to a culture.
SELECT b.BookId, br.ReleaseId, br.CultureCode, br.Title, br.ReleaseDate, brOriginal.Title AS OriginalTitle, CAST(1 AS BIT) AS IsLocalized
    FROM [dbo].[BookReleases] br
        JOIN [dbo].[Books] b ON b.BookId = br.BookId
        LEFT JOIN [dbo].[BookReleases] brOriginal ON brOriginal.BookId = br.BookId AND brOriginal.CultureCode = b.OriginalCultureCode
UNION
-- Get books from the default culture that do not have a localized release in the specific culture.
SELECT b.BookId, brOriginal.ReleaseId, c.CultureCode, brOriginal.Title, NULL AS ReleaseDate, brOriginal.Title AS OriginalTitle, CAST(0 AS BIT) AS IsLocalized
    FROM [dbo].[Books] b
        CROSS APPLY [dbo].[Cultures] c
        LEFT JOIN [dbo].[BookReleases] br ON br.BookId = b.BookId AND br.CultureCode = c.CultureCode
        LEFT JOIN [dbo].[BookReleases] brOriginal ON brOriginal.BookId = b.BookId AND brOriginal.CultureCode = b.OriginalCultureCode
    WHERE c.CultureCode <> b.OriginalCultureCode AND br.ReleaseId IS NULL
GO
UPDATE [dbo].[Books] SET [OriginalCultureCode] = 'jp-JP' WHERE [OriginalCultureCode] IS NULL