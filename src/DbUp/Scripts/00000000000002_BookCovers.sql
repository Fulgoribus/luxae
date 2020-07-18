CREATE TABLE [dbo].[BookCovers](
    [BookId] [int] NOT NULL,
    [Image] [varbinary](max) NOT NULL
    CONSTRAINT [PK_Covers] PRIMARY KEY NONCLUSTERED ([BookId] ASC),
    CONSTRAINT [FK_Covers_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[books] ([BookId]) ON DELETE CASCADE,
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
