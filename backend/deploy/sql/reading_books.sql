-- ============================================================
-- 友邻座 阅读管理功能（V1.5）生产库手动建表脚本
-- 适用：MySQL（云托管环境，EnsureCreated 不会为已有库补建新表）
-- 执行方式：在 friendlyseat 数据库执行本脚本
-- ============================================================

-- 1. 书籍表
CREATE TABLE IF NOT EXISTS `ReadingBooks` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserId` bigint NOT NULL,
  `Title` longtext NOT NULL,
  `Author` longtext NULL,
  `CoverUrl` longtext NULL,
  `VenueId` bigint NULL,
  `VenueName` longtext NULL,
  `Status` int NOT NULL,
  `CurrentProgress` int NOT NULL,
  `TotalPages` int NULL,
  `LastPosition` longtext NULL,
  `TotalMinutes` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ReadingBooks_UserId_Status` (`UserId`, `Status`),
  KEY `IX_ReadingBooks_VenueId` (`VenueId`),
  CONSTRAINT `FK_ReadingBooks_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReadingBooks_Venues_VenueId` FOREIGN KEY (`VenueId`) REFERENCES `Venues` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. 摘抄/笔记表
CREATE TABLE IF NOT EXISTS `ReadingNotes` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserId` bigint NOT NULL,
  `BookId` bigint NOT NULL,
  `Type` int NOT NULL,
  `Content` longtext NOT NULL,
  `Position` longtext NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ReadingNotes_BookId` (`BookId`),
  KEY `IX_ReadingNotes_UserId_BookId_Type` (`UserId`, `BookId`, `Type`),
  CONSTRAINT `FK_ReadingNotes_ReadingBooks_BookId` FOREIGN KEY (`BookId`) REFERENCES `ReadingBooks` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReadingNotes_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. 阅读会话表
CREATE TABLE IF NOT EXISTS `ReadingSessions` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserId` bigint NOT NULL,
  `BookId` bigint NOT NULL,
  `VenueId` bigint NULL,
  `VenueName` longtext NULL,
  `StartedAt` datetime(6) NOT NULL,
  `EndedAt` datetime(6) NULL,
  `DurationMinutes` int NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ReadingSessions_UserId_StartedAt` (`UserId`, `StartedAt`),
  KEY `IX_ReadingSessions_UserId_Status` (`UserId`, `Status`),
  KEY `IX_ReadingSessions_BookId` (`BookId`),
  KEY `IX_ReadingSessions_VenueId` (`VenueId`),
  CONSTRAINT `FK_ReadingSessions_ReadingBooks_BookId` FOREIGN KEY (`BookId`) REFERENCES `ReadingBooks` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReadingSessions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReadingSessions_Venues_VenueId` FOREIGN KEY (`VenueId`) REFERENCES `Venues` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
