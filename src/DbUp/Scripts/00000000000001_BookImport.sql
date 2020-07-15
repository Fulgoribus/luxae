CREATE TABLE [dbo].[Books](
    [BookId] [int] IDENTITY(1,1) NOT NULL,
    [Title] [nvarchar](250) NOT NULL,
    [Author] [nvarchar](max) NULL,
    [ReleaseDate] [date] NULL,
    [Label] [nvarchar](250) NULL,
    CONSTRAINT [PK_Books] PRIMARY KEY CLUSTERED ([BookId] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE TABLE [dbo].[Retailers](
    [RetailerId] [nvarchar](10) NOT NULL,
    [ShortName] [nvarchar](20) NOT NULL,
    [LongName] [nvarchar](64) NOT NULL,
    CONSTRAINT [PK_Retailers] PRIMARY KEY CLUSTERED ([RetailerId] ASC)
)
GO
INSERT INTO [dbo].[Retailers] ([RetailerId], [ShortName], [LongName]) VALUES ('BW', 'Bookwalker', 'Bookwalker')
GO
CREATE TABLE [dbo].[BookRetailers](
    [BookId] [int] NOT NULL,
    [RetailerId] [nvarchar](10) NOT NULL,
    [RetailerKey] [nvarchar](64) NOT NULL,
    CONSTRAINT [FK_BookRetailers_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[books] ([BookId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookRetailers_Retailers] FOREIGN KEY ([RetailerId]) REFERENCES [dbo].[Retailers] ([RetailerId])
)
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_BookRetailers_Retailer ON [dbo].[BookRetailers] ([RetailerId] ASC, [RetailerKey] ASC)
GO
CREATE TABLE [dbo].[Series](
    [SeriesId] [int] IDENTITY(1,1) NOT NULL,
    [Title] [nvarchar](250) NOT NULL,
    CONSTRAINT [PK_Series] PRIMARY KEY CLUSTERED ([SeriesId] ASC)
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX IX_Series_Title ON [dbo].[Series] ([Title] ASC)
GO
CREATE TABLE [dbo].[SeriesBooks](
    [SeriesId] [int] NOT NULL,
    [BookId] [int] NOT NULL,
    [Volume] [nvarchar](25) NULL,
    [SortOrder] DECIMAL(6,2) NOT NULL,
    CONSTRAINT [FK_SeriesBooks_Series] FOREIGN KEY ([SeriesId]) REFERENCES [dbo].[Series]([SeriesId]) ON DELETE CASCADE,
    CONSTRAINT [FK_SeriesBooks_Book] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books]([BookId]) ON DELETE CASCADE
) ON [PRIMARY]
GO
CREATE CLUSTERED INDEX CDX_SeriesBooks_Series ON [dbo].[SeriesBooks] ([SeriesId] ASC, [SortOrder] ASC)
GO
CREATE NONCLUSTERED INDEX IX_SeriesBooks_Book ON [dbo].[SeriesBooks] ([BookId] ASC)
GO
