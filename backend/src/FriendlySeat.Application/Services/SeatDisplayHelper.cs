using FriendlySeat.Application.Common;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

/// <summary>座位展示信息（区块字母/展示编号/楼层/区域）统一计算，供分享/预约列表复用</summary>
public static class SeatDisplayHelper
{
    // zoneId -> (区块字母, 楼层名, 区域名)
    public static async Task<Dictionary<long, (char Letter, string FloorName, string? AreaName)>> BuildZoneMapAsync(
        IAppDbContext db, IEnumerable<long> zoneIds, CancellationToken ct)
    {
        var result = new Dictionary<long, (char, string, string?)>();
        var ids = zoneIds.Distinct().ToList();
        if (ids.Count == 0) return result;

        var zones = await db.Zones
            .Where(z => ids.Contains(z.Id))
            .Select(z => new { z.Id, z.FloorId, z.AreaId, z.SortOrder, z.OffsetX })
            .ToListAsync(ct);
        var floorIds = zones.Select(z => z.FloorId).Distinct().ToList();
        var floors = await db.Floors
            .Where(f => floorIds.Contains(f.Id))
            .Include(f => f.Areas)
            .Include(f => f.Zones)
            .ToListAsync(ct);

        static IOrderedEnumerable<Zone> OrderZones(IEnumerable<Zone> zones) =>
            zones.OrderBy(z => z.SortOrder).ThenBy(z => z.OffsetX).ThenBy(z => z.Id);

        foreach (var floor in floors.OrderBy(f => f.SortOrder))
        {
            var ordered = new List<Zone>();
            foreach (var area in floor.Areas.OrderBy(a => a.SortOrder))
            {
                var zg = OrderZones(floor.Zones.Where(z => z.AreaId == area.Id)).ToList();
                foreach (var zone in zg) result[zone.Id] = ('A', floor.Name, area.Name);
                ordered.AddRange(zg);
            }
            var unassigned = OrderZones(floor.Zones.Where(z => z.AreaId == null)).ToList();
            foreach (var zone in unassigned) result[zone.Id] = ('A', floor.Name, null);
            ordered.AddRange(unassigned);

            for (var i = 0; i < ordered.Count; i++)
            {
                var (_, floorName, areaName) = result[ordered[i].Id];
                result[ordered[i].Id] = ((char)('A' + i), floorName, areaName);
            }
        }
        return result;
    }

    /// <summary>由座位原始编号生成展示编号：区块字母 + 尾部序号，如 "B区-002"</summary>
    public static string DisplayCode(char letter, string seatCode)
    {
        var seatNo = seatCode.Split('-').LastOrDefault() ?? seatCode;
        return $"{letter}区-{seatNo}";
    }
}