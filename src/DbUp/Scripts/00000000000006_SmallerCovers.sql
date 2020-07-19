﻿ALTER TABLE [BookCovers] ADD [IsFullResolution] BIT NOT NULL CONSTRAINT [DF_BookCovers_IsFullResolution] DEFAULT (1)
GO
ALTER TABLE [BookCovers] DROP CONSTRAINT [PK_Covers]
GO
ALTER TABLE [BookCovers] ADD CONSTRAINT [PK_Covers] PRIMARY KEY NONCLUSTERED ([BookId] ASC, [IsFullResolution])