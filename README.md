# 友邻座（FriendlySeat）

> **一席相邻，善意相续。**
> 公共学习空间座位共享与预约平台。

基于 `docs/` 下的需求、设计、品牌与合规文档开发的 MVP，包括：

- **小程序端**（uni-app / Vue3）：找座、预约、分享、到座确认、候补、信用、举报、支持项目
- **后台 API**（ASP.NET Core 10，模块化单体）：用户端 API + 管理端 API
- **Web 管理后台**（Vue3 + Element Plus + ECharts）

## 目录结构

```text
SpaceShare/
├── docs/                        # 需求 / 设计 / 品牌 / 合规文档
├── backend/                     # ASP.NET Core 后端
│   ├── FriendlySeat.slnx
│   ├── docker-compose.yml       # postgres + redis + api + web
│   └── src/
│       ├── FriendlySeat.Domain          # 领域实体
│       ├── FriendlySeat.Application     # 用例 / DTO / 服务（含管理端服务）
│       ├── FriendlySeat.Infrastructure  # EF Core(PG/MySQL) / Redis / 微信 / 通知
│       └── FriendlySeat.Api             # 单一 API（用户端 + 管理端 + 定时任务内嵌）
│       # 保留（结构模板，已整合进 Api，不再部署）：
│       ├── FriendlySeat.Admin.Api       # 管理端 API（已并入 Api）
│       └── FriendlySeat.Worker          # 定时任务（已内嵌 Api 的 HostedService）
├── FriendlySeat/                # 友邻座微信小程序（uni-app）
└── web/
    └── seatshare-admin/         # Web 管理后台（Vue3）
```

## 快速启动

### 1. 基础设施（PostgreSQL + Redis）

```bash
cd backend
docker compose up -d
```

### 2. 后端 API（单进程：用户端 + 管理端 + 定时任务）

```bash
# http://localhost:5000，Swagger 在 /swagger
dotnet run --project src/FriendlySeat.Api

# 定时任务 Worker（自动释放超时预约、爽约扣信用、候补通知）
dotnet run --project src/FriendlySeat.Worker
```

首次启动会自动创建数据库表并写入示例数据（3 个城市场馆 × 3 层 × 2 区 × 10 座）和默认管理员。

默认管理员：`admin / admin123`（生产环境务必修改）。

### 3. Web 管理后台

```bash
cd web/seatshare-admin
npm install
npm run dev        # http://localhost:5173（已配置代理到 :5000 单 API）
```

### 4. 小程序

使用 HBuilderX 打开 `FriendlySeat/` 目录，运行到微信开发者工具。
- 未配置微信 AppId 时，`manifest.json` 的 `appid` 留空即可（开发者工具体验版）；
- 未配置后端微信凭据时，登录接口走 `dev_` 前缀的模拟 code，可直接登录；
- API 地址在 `FriendlySeat/utils/request.js` 中的 `BASE_URL`（默认 `http://localhost:5000`，需在小程序后台配置域名白名单，开发时可在开发者工具中勾选"不校验合法域名"）。

## 核心业务闭环

```text
微信登录 → 选择场馆 → 选择座位 → 签到到座 → 分享空闲时间
    → 第二位友邻预约 → 到座确认 → 使用 → 再次分享 → 信用沉淀
```

## 核心机制

| 机制 | 说明 |
|---|---|
| 真实使用原则 | 只有确认到座（签到）并有 active session 才能分享座位，防止黄牛虚构座位 |
| 并发预约控制 | Redis 分布式锁 + 数据库事务双保险，已验证 10 并发仅 1 成功 |
| 自动释放 | Worker 每分钟检查：超时未到座→no_show 扣信用；过期 share/候补自动清理 |
| 信用体系 | 正常到座 +1、爽约 -5、交易 -20 等，规则全部后台可配置 |
| 联系方式授权 | 预约成功且分享者授权后才可见，预约结束自动隐藏 |
| 赞助与预约隔离 | Donation 不影响 Reservation / Credit / Risk |
| 管理端审计 | 所有管理员敏感操作写入 AdminAuditLog |

## API 概览

用户端（`/api/v1`）：

```text
POST /auth/wechat/login          微信登录
GET  /cities                     城市列表
GET  /venues                     场馆列表（支持定位/距离/关键词）
GET  /venues/{id}                场馆详情（楼层/区域/座位地图）
GET  /seats/{id}                 座位详情
GET  /seats/{id}/shares          座位可用分享
POST /sessions/check-in          签到到座
POST /sessions/end               结束使用
POST /shares                     分享这一席
GET  /shares/my                  我的分享
DELETE /shares/{id}              取消分享
POST /reservations               预约
POST /reservations/{id}/cancel   取消预约
POST /reservations/{id}/arrive   确认到座
POST /reservations/{id}/complete 结束使用
POST /shares/{id}/waitlist       加入候补
GET  /waitlist/my                我的候补
GET  /credit                     友邻信用
POST /reports                    举报
GET  /ads?placement=home_feed    广告位
POST /donations                  支持项目
GET  /me                         我的资料/通知/联系方式
```

管理端（`/api/v1/admin`）：登录、统计、用户、管理员、场馆、预约、举报、系统配置、审计日志。

## 合规要点（详见 docs/上线合规与运营.md）

- 不卖座、不炒座、不占座；座位预约始终免费
- 平台不拥有实体座位，仅做信息撮合
- 赞助不换取任何预约特权，与预约完全解耦
- 最小化收集个人信息，联系方式预约后授权可见
- 管理员操作全程审计留痕
- 正式上线前需完成：主体认证、类目审核、用户协议、隐私政策、备案

## 测试

已通过验证：

- [x] 微信登录（dev 模式）
- [x] 场馆/座位查询
- [x] 签到 → 分享 → 预约 完整闭环
- [x] 并发预约（10 并发仅 1 成功）
- [x] 重复预约拒绝
- [x] 管理端登录 / 统计 / 配置
- [x] 全部项目编译通过
