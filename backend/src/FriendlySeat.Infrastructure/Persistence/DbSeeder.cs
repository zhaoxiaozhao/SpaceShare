using FriendlySeat.Application.Services;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FriendlySeat.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(FriendlySeatDbContext db, ILogger logger, DbProvider provider = DbProvider.Postgres)
    {
        // PostgreSQL 用迁移；MySQL 用 EnsureCreated（新部署直接建表）
        if (provider == DbProvider.MySql)
        {
            await db.Database.EnsureCreatedAsync();
            // MySQL 无迁移历史，EnsureCreated 不会为已有库补建新表，需手动补缺失表
            await EnsureMySqlTablesAsync(db, logger);
        }
        else
        {
            await db.Database.MigrateAsync();
        }

        // 系统配置
        if (!await db.SystemConfigs.AnyAsync())
        {
            db.SystemConfigs.AddRange(new[]
            {
                new SystemConfig { Category = ConfigCategory.ReservationRules, ConfigKey = "min_minutes", Value = "30", Description = "最短预约(分钟)" },
                new SystemConfig { Category = ConfigCategory.ReservationRules, ConfigKey = "max_minutes", Value = "240", Description = "最长预约(分钟)" },
                new SystemConfig { Category = ConfigCategory.ReservationRules, ConfigKey = "max_advance_hours", Value = "24", Description = "最多提前(小时)" },
                new SystemConfig { Category = ConfigCategory.ReservationRules, ConfigKey = "max_active_reservations", Value = "1", Description = "同时有效预约数" },
                new SystemConfig { Category = ConfigCategory.ReservationRules, ConfigKey = "daily_reservation_limit", Value = "5", Description = "每日预约上限" },
                new SystemConfig { Category = ConfigCategory.ReservationRules, ConfigKey = "arrival_grace_minutes", Value = "30", Description = "到座宽限(分钟)" },
                new SystemConfig { Category = ConfigCategory.ReservationRules, ConfigKey = "arrival_warning_minutes", Value = "15", Description = "到座预警(分钟)" },
                new SystemConfig { Category = ConfigCategory.ReservationRules, ConfigKey = "waitlist_window_minutes", Value = "10", Description = "候补预约窗口(分钟)" },
                new SystemConfig { Category = ConfigCategory.CreditRules, ConfigKey = "arrival_bonus", Value = "1" },
                new SystemConfig { Category = ConfigCategory.CreditRules, ConfigKey = "completion_bonus", Value = "1" },
                new SystemConfig { Category = ConfigCategory.CreditRules, ConfigKey = "no_show_penalty", Value = "-5" },
                new SystemConfig { Category = ConfigCategory.CreditRules, ConfigKey = "fake_seat_penalty", Value = "-10" },
                new SystemConfig { Category = ConfigCategory.CreditRules, ConfigKey = "malicious_hold_penalty", Value = "-10" },
                new SystemConfig { Category = ConfigCategory.CreditRules, ConfigKey = "transaction_penalty", Value = "-20" },
                new SystemConfig { Category = ConfigCategory.CreditRules, ConfigKey = "malicious_report_penalty", Value = "-10" },
                new SystemConfig { Category = ConfigCategory.CreditRules, ConfigKey = "max_score", Value = "100" },
                new SystemConfig { Category = ConfigCategory.RiskRules, ConfigKey = "rapid_reservation_threshold", Value = "5" },
                new SystemConfig { Category = ConfigCategory.RiskRules, ConfigKey = "rapid_reservation_window_minutes", Value = "30" },
                new SystemConfig { Category = ConfigCategory.RiskRules, ConfigKey = "no_show_threshold", Value = "2" },
                new SystemConfig { Category = ConfigCategory.RiskRules, ConfigKey = "cancel_threshold", Value = "5" },
                new SystemConfig { Category = ConfigCategory.ImageRules, ConfigKey = "max_size_mb", Value = "10" },
                new SystemConfig { Category = ConfigCategory.ImageRules, ConfigKey = "daily_upload_limit", Value = "20" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "reservation_created", Value = "", Description = "预约成功通知 订阅消息模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "reservation_starting", Value = "", Description = "预约即将开始通知 模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "arrival_required", Value = "", Description = "到座提醒通知 模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "reservation_expired", Value = "", Description = "预约过期/爽约通知 模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "reservation_cancelled", Value = "", Description = "预约取消通知 模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "waitlist_available", Value = "", Description = "候补成功通知 模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "credit_changed", Value = "", Description = "信用变更通知 模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "report_result", Value = "", Description = "举报处理结果通知 模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "system", Value = "", Description = "系统通知 模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "activity_review", Value = "", Description = "活动审核结果通知 模板ID" },
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "activity_starting", Value = "", Description = "活动开始前提醒 模板ID" },
                new SystemConfig { Category = ConfigCategory.ActivityCategories, ConfigKey = "list", Value = ConfigOptionsService.DefaultActivityCategories, Description = "活动分类（JSON 数组）" },
                new SystemConfig { Category = ConfigCategory.SwapReasons, ConfigKey = "list", Value = ConfigOptionsService.DefaultSwapReasons, Description = "换座原因（JSON 数组）" },
                new SystemConfig { Category = ConfigCategory.SeatTags, ConfigKey = "list", Value = ConfigOptionsService.DefaultSeatTags, Description = "座位标签（JSON 数组）" },
                new SystemConfig { Category = ConfigCategory.VenuePostCategories, ConfigKey = "list", Value = ConfigOptionsService.DefaultVenuePostCategories, Description = "交流板板块（JSON 数组）" }
            });
            await db.SaveChangesAsync();
        }

        // 默认管理员
        if (!await db.AdminUsers.AnyAsync())
        {
            db.AdminUsers.Add(new AdminUser
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                DisplayName = "超级管理员",
                Role = AdminRole.SuperAdmin,
                Status = EntityStatus.Active,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // 示例城市与场馆
        if (!await db.Cities.AnyAsync())
        {
            var city = new City
            {
                Name = "上海市",
                Province = "上海",
                CountryCode = "CN",
                Longitude = 121.4737,
                Latitude = 31.2304,
                Status = EntityStatus.Active
            };
            db.Cities.Add(city);
            await db.SaveChangesAsync();

            var venues = new[]
            {
                new Venue { CityId = city.Id, Name = "上海图书馆（东馆）", Type = VenueType.Library, Address = "浦东新区迎春路300号", Longitude = 121.5280, Latitude = 31.2200, OpeningTime = TimeSpan.FromHours(9), ClosingTime = TimeSpan.FromHours(21), Description = "上海市中心图书馆，含自习区、阅览区、报告厅。", Status = EntityStatus.Active },
                new Venue { CityId = city.Id, Name = "徐汇区图书馆", Type = VenueType.Library, Address = "徐汇区南丹东路80号", Longitude = 121.4453, Latitude = 31.1896, OpeningTime = TimeSpan.FromHours(9), ClosingTime = new TimeSpan(20,30,0), Description = "区级公共图书馆，读者自习空间充足。", Status = EntityStatus.Active },
                new Venue { CityId = city.Id, Name = "浦东图书馆", Type = VenueType.Library, Address = "浦东新区前程路88号", Longitude = 121.5300, Latitude = 31.2000, OpeningTime = TimeSpan.FromHours(9), ClosingTime = TimeSpan.FromHours(21), Description = "浦东新区地标性公共图书馆。", Status = EntityStatus.Active }
            };
            db.Venues.AddRange(venues);
            await db.SaveChangesAsync();

            foreach (var venue in venues)
            {
                for (var f = 1; f <= 3; f++)
                {
                    var floor = new Floor { VenueId = venue.Id, Name = $"{f}F", SortOrder = f };
                    db.Floors.Add(floor);
                    await db.SaveChangesAsync();

                    for (var z = 1; z <= 2; z++)
                    {
                        var zone = new Zone { FloorId = floor.Id, Name = $"{f}F-{z}区", SortOrder = z, GridRows = 2, GridCols = 5 };
                        db.Zones.Add(zone);
                        await db.SaveChangesAsync();

                        for (var s = 1; s <= 10; s++)
                        {
                            db.Seats.Add(new Seat
                            {
                                ZoneId = zone.Id,
                                Code = $"{zone.Name}-{s:D2}",
                                Type = SeatType.Normal,
                                PositionX = (s - 1) % 5,
                                PositionY = (s - 1) / 5,
                                Window = s % 3 == 0,
                                PowerSocket = s % 2 == 0,
                                QuietLevel = 3,
                                LightLevel = s % 2 == 0 ? 3 : 2,
                                Status = SeatStatus.Available,
                                Verified = true
                            });
                        }
                    }
                }
            }
            await db.SaveChangesAsync();
            logger.LogInformation("已生成示例城市、场馆与座位数据");
        }

        // 敏感词兜底配置：确保存在一行（空值=使用代码内置默认词表）
        if (!await db.SystemConfigs.AnyAsync(c => c.Category == ConfigCategory.SensitiveWords))
        {
            db.SystemConfigs.Add(new SystemConfig
            {
                Category = ConfigCategory.SensitiveWords,
                ConfigKey = "words",
                Value = "",
                Description = "敏感词（英文逗号或换行分隔，留空则用内置默认词表）"
            });
            await db.SaveChangesAsync();
        }

        // 可配置选项（活动分类/换座原因/座位标签）：确保存在
        await EnsureConfigRowAsync(db, ConfigCategory.ActivityCategories, ConfigOptionsService.DefaultActivityCategories, "活动分类（JSON 数组）");
        await EnsureConfigRowAsync(db, ConfigCategory.SwapReasons, ConfigOptionsService.DefaultSwapReasons, "换座原因（JSON 数组）");
        await EnsureConfigRowAsync(db, ConfigCategory.SeatTags, ConfigOptionsService.DefaultSeatTags, "座位标签（JSON 数组）");
            await EnsureConfigRowAsync(db, ConfigCategory.VenuePostCategories, ConfigOptionsService.DefaultVenuePostCategories, "交流板板块（JSON 数组）");

        // 通知模板配置键：确保全部存在（供后台配置模板ID；历史库会自动补齐缺失项）
        var notificationTemplateKeys = new (string Key, string Desc)[]
        {
            ("reservation_created", "预约成功通知 模板ID"),
            ("reservation_starting", "预约即将开始通知 模板ID"),
            ("arrival_required", "到座提醒通知 模板ID"),
            ("reservation_expired", "预约过期/爽约通知 模板ID"),
            ("reservation_cancelled", "预约取消通知 模板ID"),
            ("waitlist_available", "候补成功通知 模板ID"),
            ("credit_changed", "信用变更通知 模板ID"),
            ("report_result", "举报处理结果通知 模板ID"),
            ("system", "系统通知 模板ID"),
            ("activity_review", "活动审核结果通知 模板ID"),
            ("activity_starting", "活动开始前提醒 模板ID"),
            ("swap_request", "换座申请通知 模板ID"),
            ("swap_confirmed", "换座确认通知 模板ID")
        };
        foreach (var (key, desc) in notificationTemplateKeys)
        {
            if (!await db.SystemConfigs.AnyAsync(c => c.Category == ConfigCategory.NotificationTemplates && c.ConfigKey == key))
            {
                db.SystemConfigs.Add(new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = key, Value = "", Description = desc });
                await db.SaveChangesAsync();
            }
        }

        logger.LogInformation("数据库初始化完成");
    }

    // MySQL 使用 EnsureCreated，无迁移历史，EnsureCreated 不会为已有库补建新表。
    // 这里检查缺失表并自动补建（当前含阅读管理 V1.5 三张表，后续新增表在此追加）。
    private static async Task EnsureMySqlTablesAsync(FriendlySeatDbContext db, ILogger logger)
    {
        var tableExists = async (string table) =>
        {
            var conn = db.Database.GetDbConnection();
            var open = conn.State != System.Data.ConnectionState.Open;
            if (open) await conn.OpenAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{table}'";
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            finally
            {
                if (open) await conn.CloseAsync();
            }
        };

        // 阅读管理（V1.5）
        var readingBooksExists = await tableExists("ReadingBooks");
        logger.LogInformation("MySQL 阅读表检测：ReadingBooks exists={ReadingBooksExists}", readingBooksExists);
        if (!readingBooksExists)
        {
            logger.LogInformation("MySQL 补建阅读管理表（ReadingBooks/ReadingNotes/ReadingSessions）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `ReadingBooks` (
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `ReadingNotes` (
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `ReadingSessions` (
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // 书单分享表 BookListShares（已有库补建表）
        if (!await tableExists("BookListShares"))
        {
            logger.LogInformation("MySQL 补建书单分享表（BookListShares）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `BookListShares` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserId` bigint NOT NULL,
  `Token` varchar(64) NOT NULL,
  `Title` longtext NOT NULL,
  `Remark` longtext NULL,
  `ItemsJson` longtext NOT NULL,
  `IsPublic` tinyint(1) NOT NULL DEFAULT 0,
  `ViewCount` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_BookListShares_Token` (`Token`),
  KEY `IX_BookListShares_UserId` (`UserId`),
  CONSTRAINT `FK_BookListShares_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // BookListShares.IsPublic 列（公开到热门书单榜）：已有表补列
        if (await tableExists("BookListShares") && !await ColumnExistsAsync(db, "BookListShares", "IsPublic"))
        {
            logger.LogInformation("MySQL 补充 BookListShares.IsPublic 列（公开书单）");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `BookListShares` ADD COLUMN `IsPublic` tinyint(1) NOT NULL DEFAULT 0;");
        }

        // 书单收藏表 BookListShareFavorites（已有库补建表）
        if (!await tableExists("BookListShareFavorites"))
        {
            logger.LogInformation("MySQL 补建书单收藏表（BookListShareFavorites）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `BookListShareFavorites` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ShareId` bigint NOT NULL,
  `UserId` bigint NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_BookListShareFavorites_ShareId_UserId` (`ShareId`, `UserId`),
  KEY `IX_BookListShareFavorites_UserId` (`UserId`),
  CONSTRAINT `FK_BookListShareFavorites_BookListShares_ShareId` FOREIGN KEY (`ShareId`) REFERENCES `BookListShares` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_BookListShareFavorites_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // 座位便签表 SeatNotes（已有库补建表）
        if (!await tableExists("SeatNotes"))
        {
            logger.LogInformation("MySQL 补建座位便签表（SeatNotes）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `SeatNotes` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `SeatId` bigint NOT NULL,
  `UserId` bigint NOT NULL,
  `Content` longtext NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_SeatNotes_SeatId_UserId` (`SeatId`, `UserId`),
  KEY `IX_SeatNotes_SeatId_Status` (`SeatId`, `Status`),
  KEY `IX_SeatNotes_UserId` (`UserId`),
  CONSTRAINT `FK_SeatNotes_Seats_SeatId` FOREIGN KEY (`SeatId`) REFERENCES `Seats` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_SeatNotes_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // 活动留言表 ActivityComments（已有库补建表）
        if (!await tableExists("ActivityComments"))
        {
            logger.LogInformation("MySQL 补建活动留言表（ActivityComments）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `ActivityComments` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ActivityId` bigint NOT NULL,
  `UserId` bigint NOT NULL,
  `Content` longtext NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ActivityComments_ActivityId_Status` (`ActivityId`, `Status`),
  KEY `IX_ActivityComments_UserId` (`UserId`),
  CONSTRAINT `FK_ActivityComments_Activities_ActivityId` FOREIGN KEY (`ActivityId`) REFERENCES `Activities` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ActivityComments_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // 友邻画像表 PersonaProfiles（已有库补建表）
        if (!await tableExists("PersonaProfiles"))
        {
            logger.LogInformation("MySQL 补建友邻画像表（PersonaProfiles）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `PersonaProfiles` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserId` bigint NOT NULL,
  `Social` int NOT NULL,
  `Rhythm` int NOT NULL,
  `Style` int NOT NULL,
  `Plan` int NOT NULL,
  `Interest` int NOT NULL,
  `Focus` int NOT NULL,
  `Motive` int NOT NULL,
  `TypeCode` varchar(64) NOT NULL,
  `IsPublic` tinyint(1) NOT NULL DEFAULT 0,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_PersonaProfiles_UserId` (`UserId`),
  CONSTRAINT `FK_PersonaProfiles_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // 场馆交流板（VenuePosts / VenuePostComments / VenuePostLikes，已有库补建表）
        if (!await tableExists("VenuePosts"))
        {
            logger.LogInformation("MySQL 补建场馆交流板表（VenuePosts/VenuePostComments/VenuePostLikes）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `VenuePosts` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `VenueId` bigint NOT NULL,
  `UserId` bigint NOT NULL,
  `Category` longtext NOT NULL,
  `Title` longtext NOT NULL,
  `Content` longtext NOT NULL,
  `Status` int NOT NULL,
  `IsPinned` tinyint(1) NOT NULL DEFAULT 0,
  `LikeCount` int NOT NULL,
  `CommentCount` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_VenuePosts_VenueId_Status_IsPinned` (`VenueId`, `Status`, `IsPinned`),
  KEY `IX_VenuePosts_UserId` (`UserId`),
  CONSTRAINT `FK_VenuePosts_Venues_VenueId` FOREIGN KEY (`VenueId`) REFERENCES `Venues` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_VenuePosts_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `VenuePostComments` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `PostId` bigint NOT NULL,
  `UserId` bigint NOT NULL,
  `Content` longtext NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `ParentCommentId` bigint NULL,
  `ReplyToUserId` bigint NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_VenuePostComments_PostId_Status` (`PostId`, `Status`),
  KEY `IX_VenuePostComments_UserId` (`UserId`),
  KEY `IX_VenuePostComments_ParentCommentId` (`ParentCommentId`),
  CONSTRAINT `FK_VenuePostComments_VenuePosts_PostId` FOREIGN KEY (`PostId`) REFERENCES `VenuePosts` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_VenuePostComments_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `VenuePostLikes` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `PostId` bigint NOT NULL,
  `UserId` bigint NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_VenuePostLikes_PostId_UserId` (`PostId`, `UserId`),
  KEY `IX_VenuePostLikes_UserId` (`UserId`),
  CONSTRAINT `FK_VenuePostLikes_VenuePosts_PostId` FOREIGN KEY (`PostId`) REFERENCES `VenuePosts` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_VenuePostLikes_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // VenuePostComments 楼中楼列（ParentCommentId / ReplyToUserId）：已有表补列
        if (await tableExists("VenuePostComments") && !await ColumnExistsAsync(db, "VenuePostComments", "ParentCommentId"))
        {
            logger.LogInformation("MySQL 补充 VenuePostComments.ParentCommentId/ReplyToUserId 列（楼中楼回复）");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `VenuePostComments` ADD COLUMN `ParentCommentId` bigint NULL;");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `VenuePostComments` ADD COLUMN `ReplyToUserId` bigint NULL;");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `VenuePostComments` ADD KEY `IX_VenuePostComments_ParentCommentId` (`ParentCommentId`);");
        }

        // PersonaProfiles.Focus / Motive 列（画像扩展维度）：已有表补列
        if (await tableExists("PersonaProfiles") && !await ColumnExistsAsync(db, "PersonaProfiles", "Focus"))
        {
            logger.LogInformation("MySQL 补充 PersonaProfiles.Focus/Motive 列");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `PersonaProfiles` ADD COLUMN `Focus` int NOT NULL DEFAULT 0;");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `PersonaProfiles` ADD COLUMN `Motive` int NOT NULL DEFAULT 0;");
        }

        // SeatShares.CheckInCode 列（到座核销码）：已有表补列
        // 注意：MySQL 的 TEXT 列不允许 DEFAULT 值，非空约束由应用层保证
        if (await tableExists("SeatShares") && !await ColumnExistsAsync(db, "SeatShares", "CheckInCode"))
        {
            logger.LogInformation("MySQL 补充 SeatShares.CheckInCode 列（到座核销码）");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `SeatShares` ADD COLUMN `CheckInCode` longtext NOT NULL;");
        }

        // FloorPois.AreaId 列（标志物归属空间区域）：已有表补列
        if (await tableExists("FloorPois") && !await ColumnExistsAsync(db, "FloorPois", "AreaId"))
        {
            logger.LogInformation("MySQL 补充 FloorPois.AreaId 列（标志物区域归属）");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `FloorPois` ADD COLUMN `AreaId` bigint NULL;");
        }

        // SeatShares.HoldForUserId 列（候补预留归属用户）：已有表补列
        if (await tableExists("SeatShares") && !await ColumnExistsAsync(db, "SeatShares", "HoldForUserId"))
        {
            logger.LogInformation("MySQL 补充 SeatShares.HoldForUserId 列（候补预留归属）");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `SeatShares` ADD COLUMN `HoldForUserId` bigint NULL;");
        }

        // 范围候补偏好表 WaitlistPreferences（系统自动代约）：已有库补建表
        if (!await tableExists("WaitlistPreferences"))
        {
            logger.LogInformation("MySQL 补建范围候补偏好表（WaitlistPreferences）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `WaitlistPreferences` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserId` bigint NOT NULL,
  `VenueId` bigint NOT NULL,
  `FloorId` bigint NULL,
  `AreaId` bigint NULL,
  `Preference` longtext NOT NULL,
  `ExpireAt` datetime(6) NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `BookedAt` datetime(6) NULL,
  `ReservationId` bigint NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_WaitlistPreferences_AreaId` (`AreaId`),
  KEY `IX_WaitlistPreferences_FloorId` (`FloorId`),
  KEY `IX_WaitlistPreferences_ReservationId` (`ReservationId`),
  KEY `IX_WaitlistPreferences_UserId_Status` (`UserId`, `Status`),
  KEY `IX_WaitlistPreferences_VenueId_Status_CreatedAt` (`VenueId`, `Status`, `CreatedAt`),
  CONSTRAINT `FK_WaitlistPreferences_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_WaitlistPreferences_Venues_VenueId` FOREIGN KEY (`VenueId`) REFERENCES `Venues` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_WaitlistPreferences_Floors_FloorId` FOREIGN KEY (`FloorId`) REFERENCES `Floors` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_WaitlistPreferences_Areas_AreaId` FOREIGN KEY (`AreaId`) REFERENCES `Areas` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_WaitlistPreferences_Reservations_ReservationId` FOREIGN KEY (`ReservationId`) REFERENCES `Reservations` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // WaitlistPreferences.ExpireAt 列（候补截止时间）：已有表补列
        if (await tableExists("WaitlistPreferences") && !await ColumnExistsAsync(db, "WaitlistPreferences", "ExpireAt"))
        {
            logger.LogInformation("MySQL 补充 WaitlistPreferences.ExpireAt 列（候补截止时间）");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `WaitlistPreferences` ADD COLUMN `ExpireAt` datetime(6) NULL;");
        }

        // 换座意向表 SeatSwapRequests / SeatSwapResponses：已有库补建表
        if (!await tableExists("SeatSwapRequests"))
        {
            logger.LogInformation("MySQL 补建换座意向表（SeatSwapRequests/SeatSwapResponses）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `SeatSwapRequests` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserId` bigint NOT NULL,
  `VenueId` bigint NOT NULL,
  `SeatId` bigint NOT NULL,
  `WantFloorId` bigint NULL,
  `WantAreaId` bigint NULL,
  `WantZoneId` bigint NULL,
  `Reasons` longtext NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `ExpireAt` datetime(6) NOT NULL,
  `MatchedAt` datetime(6) NULL,
  `MatchedResponseId` bigint NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_SeatSwapRequests_SeatId` (`SeatId`),
  KEY `IX_SeatSwapRequests_UserId_Status` (`UserId`, `Status`),
  KEY `IX_SeatSwapRequests_VenueId_Status_CreatedAt` (`VenueId`, `Status`, `CreatedAt`),
  CONSTRAINT `FK_SeatSwapRequests_Seats_SeatId` FOREIGN KEY (`SeatId`) REFERENCES `Seats` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_SeatSwapRequests_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_SeatSwapRequests_Venues_VenueId` FOREIGN KEY (`VenueId`) REFERENCES `Venues` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `SeatSwapResponses` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `RequestId` bigint NOT NULL,
  `UserId` bigint NOT NULL,
  `SeatId` bigint NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_SeatSwapResponses_RequestId_UserId` (`RequestId`, `UserId`),
  KEY `IX_SeatSwapResponses_SeatId` (`SeatId`),
  KEY `IX_SeatSwapResponses_UserId` (`UserId`),
  CONSTRAINT `FK_SeatSwapResponses_SeatSwapRequests_RequestId` FOREIGN KEY (`RequestId`) REFERENCES `SeatSwapRequests` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_SeatSwapResponses_Seats_SeatId` FOREIGN KEY (`SeatId`) REFERENCES `Seats` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_SeatSwapResponses_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // 活动表 Activities / ActivitySignups：已有库补建表
        if (!await tableExists("Activities"))
        {
            logger.LogInformation("MySQL 补建活动表（Activities/ActivitySignups）");
            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `Activities` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CreatorUserId` bigint NOT NULL,
  `Title` longtext NOT NULL,
  `Category` longtext NOT NULL,
  `VenueId` bigint NULL,
  `LocationText` longtext NULL,
  `StartAt` datetime(6) NOT NULL,
  `EndAt` datetime(6) NOT NULL,
  `SignupDeadline` datetime(6) NULL,
  `Capacity` int NOT NULL,
  `Description` longtext NOT NULL,
  `CoverImage` longtext NULL,
  `Status` int NOT NULL,
  `ReviewRemark` longtext NULL,
  `ReviewedAt` datetime(6) NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Activities_CreatorUserId_Status` (`CreatorUserId`, `Status`),
  KEY `IX_Activities_Status_StartAt` (`Status`, `StartAt`),
  KEY `IX_Activities_VenueId` (`VenueId`),
  CONSTRAINT `FK_Activities_Users_CreatorUserId` FOREIGN KEY (`CreatorUserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Activities_Venues_VenueId` FOREIGN KEY (`VenueId`) REFERENCES `Venues` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE `ActivitySignups` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ActivityId` bigint NOT NULL,
  `UserId` bigint NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ActivitySignups_ActivityId_Status` (`ActivityId`, `Status`),
  KEY `IX_ActivitySignups_UserId_Status` (`UserId`, `Status`),
  CONSTRAINT `FK_ActivitySignups_Activities_ActivityId` FOREIGN KEY (`ActivityId`) REFERENCES `Activities` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ActivitySignups_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        // Activities.CoverImage 列（活动海报）：已有表补列
        if (await tableExists("Activities") && !await ColumnExistsAsync(db, "Activities", "CoverImage"))
        {
            logger.LogInformation("MySQL 补充 Activities.CoverImage 列（活动海报）");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE `Activities` ADD COLUMN `CoverImage` longtext NULL;");
        }
    }

    // 检测某表中是否存在某列
    private static async Task<bool> ColumnExistsAsync(FriendlySeatDbContext db, string table, string column)
    {
        var conn = db.Database.GetDbConnection();
        var open = conn.State != System.Data.ConnectionState.Open;
        if (open) await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = '{table}' AND column_name = '{column}'";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            if (open) await conn.CloseAsync();
        }
    }

    // 确保某配置分类存在一行默认配置（ConfigKey=list）
    private static async Task EnsureConfigRowAsync(FriendlySeatDbContext db, ConfigCategory category, string value, string description)
    {
        if (await db.SystemConfigs.AnyAsync(c => c.Category == category)) return;
        db.SystemConfigs.Add(new SystemConfig
        {
            Category = category,
            ConfigKey = "list",
            Value = value,
            Description = description
        });
        await db.SaveChangesAsync();
    }
}
