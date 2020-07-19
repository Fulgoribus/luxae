CREATE TABLE [dbo].[UserBooks](
    [UserId] [nvarchar](450) NOT NULL,
    [BookId] [int] NOT NULL,
    CONSTRAINT [PK_UserBooks] PRIMARY KEY CLUSTERED ([UserId] ASC, [BookId] ASC),
    CONSTRAINT [FK_UserBooks_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserBooks_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([BookId]) ON DELETE CASCADE    
) ON [PRIMARY]
GO
