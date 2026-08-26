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
                new SystemConfig { Category = ConfigCategory.NotificationTemplates, ConfigKey = "system", Value = "", Description = "系统通知 模板ID" }
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
        if (!await tableExists("ReadingBooks"))
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
    }
}
