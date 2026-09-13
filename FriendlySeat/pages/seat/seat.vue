<template>
	<page-meta :page-style="pageThemeStyle" />
		<view v-if="seat">
		<view class="card seat-header">
			<view class="seat-top">
				<text class="seat-code">{{seat.displayCode || seat.code}}</text>
				<text class="tag" v-if="seat.statusText">{{seat.statusText}}</text>
			</view>
			<view class="seat-loc" v-if="seat.floorName || seat.areaName || seat.venueName">
				<text class="loc-floor" v-if="seat.floorName">{{seat.floorName}}</text>
				<text class="loc-area" v-if="seat.areaName">{{seat.areaName}}</text>
				<text class="loc-venue" v-if="seat.venueName">{{seat.venueName}}</text>
			</view>
			<!-- 属性标签：有分享时显示分享标签（与主页/场馆详情一致），无分享时显示座位属性 -->
			<view class="seat-features" v-if="shares.length">
				<text class="feature" v-for="(t, i) in shareTags" :key="i">{{t}}</text>
			</view>
			<view class="seat-features" v-else>
				<text class="feature" v-if="seat.window">靠窗</text>
				<text class="feature" v-if="seat.powerSocket">有插座</text>
				<text class="feature" v-if="seat.quietLevel">安静</text>
				<text class="feature" v-if="seat.lightLevel === 3">光线好</text>
			</view>
			<text class="seat-desc" v-if="seat.description">{{seat.description}}</text>
		</view>

		<!-- ============ 有分享：预约者/候补视角 ============ -->
		<view v-if="shares.length" class="section">
			<view class="card share-card" v-for="s in shares" :key="s.id">
				<view class="share-row">
					<view class="share-info">
						<text class="share-time">预计释放：{{formatTime(s.endAt)}}</text>
						<text class="share-owner" v-if="s.ownerNickname">分享者：{{s.ownerNickname}}</text>
					</view>
					<view class="share-actions">
						<button v-if="canReserve(s)" class="btn-primary small" @click="reserve(s)">{{yourTurn(s) ? '去预约' : '预约'}}</button>
						<button
							v-else-if="!isMine(s) && s.status !== 'Available'"
							class="btn-outline small"
							:disabled="waiting(s)"
							@click="waitlist(s)"
						>{{waiting(s) ? '候补中' : '加入候补'}}</button>
					</view>
				</view>
			</view>

			<!-- 有分享才需要举报（针对虚假分享） -->
			<view class="card" style="margin-top:16rpx;">
				<text class="share-note">发现这个分享是虚假的？</text>
				<button class="btn-outline" style="margin-top:16rpx;" @click="report(shares[0])">举报该分享</button>
			</view>
		</view>

		<!-- ============ 无分享：分享者视角 ============ -->
		<view v-else class="section">
			<text class="section-title">分享这个座位</text>
			<view class="card">
				<text class="share-note">把这个座位的空闲时间留给下一位友邻，让他们预约使用。</text>
				<button class="btn-primary" style="margin-top:20rpx;" @click="goShare">分享这一席</button>
			</view>
		</view>

		<!-- 换座 -->
		<view class="section">
			<view class="card">
				<text class="share-note">想换个座位？标记你当前所在的座位发起换座，或响应同场馆其他友邻的换座需求。</text>
				<view class="swap-btns">
					<button class="btn-primary small" @click="openSwap('publish')">发起换座</button>
					<button class="btn-outline small" @click="openSwap('respond')">响应换座</button>
				</view>
			</view>
		</view>

		<!-- 换座弹窗 -->
		<view v-if="showSwap" class="swap-mask" @click="showSwap = false">
			<view class="swap-pop" @click.stop>
				<text class="swap-pop-title">{{swapMode === 'publish' ? '发起换座' : '响应换座'}}</text>
				<text class="swap-pop-hint">平台仅提供信息撮合，免费自愿、线下自行交换，以场馆规定为准。</text>
				<text class="swap-pop-sub">我的座位：{{seat.displayCode || seat.code}}</text>

				<block v-if="swapMode === 'publish'">
					<text class="swap-lb">期望换到（可不限）</text>
					<view class="swap-loc-row">
						<picker mode="selector" :range="floorList('want')" range-key="name" :value="want.floor" @change="onLoc('want', 'floor', $event)">
							<view class="swap-pick">{{pickText('want', 'floor')}}</view>
						</picker>
						<picker mode="selector" :range="areaOptions('want')" range-key="name" :value="want.area" @change="onLoc('want', 'area', $event)">
							<view class="swap-pick">{{pickText('want', 'area')}}</view>
						</picker>
						<picker mode="selector" :range="zoneOptions('want')" range-key="name" :value="want.zone" @change="onLoc('want', 'zone', $event)">
							<view class="swap-pick">{{pickText('want', 'zone')}}</view>
						</picker>
					</view>
					<text class="swap-lb">原因（客观因素，可多选）</text>
					<view class="swap-reasons">
						<text class="swap-chip" :class="{ on: publishReasons.includes(r.code) }" v-for="r in reasonOptions" :key="r.code" @click="toggleReason(r.code)">{{r.label}}</text>
					</view>
					<text class="swap-lb">有效期</text>
					<view class="swap-reasons">
						<text class="swap-chip" :class="{ on: durationIdx === i }" v-for="(d, i) in durationOptions" :key="d.value" @click="durationIdx = i">{{d.label}}</text>
					</view>
					<view class="swap-pop-actions">
						<button class="btn-outline small" @click="showSwap = false">取消</button>
						<button class="btn-primary small" :loading="swapping" @click="submitPublish">发布</button>
					</view>
				</block>

				<block v-else>
					<view v-if="openSwaps.length">
						<view class="swap-req" v-for="s in openSwaps" :key="s.id">
							<view class="swap-req-info">
								<text class="swap-req-seat">TA 的座位：{{s.seatCode}}</text>
								<text class="swap-req-want">想换到：{{wantText(s)}}</text>
								<view class="swap-reasons"><text class="swap-chip" v-for="r in s.reasons" :key="r">{{reasonLabel(r)}}</text></view>
							</view>
							<button class="btn-primary small" @click="submitRespond(s)">和TA换</button>
						</view>
					</view>
					<view v-else class="swap-empty">该场馆暂无换座需求</view>
					<view class="swap-pop-actions">
						<button class="btn-outline small" @click="showSwap = false">关闭</button>
					</view>
				</block>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime, statusText } from '../../utils/format.js'
	import { subscribeFor } from '../../utils/subscribe.js'

	export default {
		data() {
			return {
				id: null,
				seat: null,
				shares: [],
				mySession: null,
				myWaitlist: [],
				loading: false,
				showSwap: false,
				swapMode: 'publish',
				swapVenue: null,
				openSwaps: [],
				publishReasons: [],
				durationIdx: 1,
				swapping: false,
				want: { floor: 0, area: 0, zone: 0 },
				reasonOptions: [
					{ code: 'light', label: '光线问题' },
					{ code: 'cold', label: '位置偏冷' },
					{ code: 'hot', label: '位置偏热' },
					{ code: 'noise', label: '附近有人交谈' },
					{ code: 'together', label: '想与同伴相邻' },
					{ code: 'window', label: '想靠窗' },
					{ code: 'socket', label: '需要插座' },
					{ code: 'other', label: '其他' }
				],
				durationOptions: [
					{ label: '30分钟', value: 30 },
					{ label: '1小时', value: 60 },
					{ label: '2小时', value: 120 }
				]
			}
		},
		onLoad(options) {
			this.id = options.id
		},
		onShareAppMessage() {
			const s = this.seat
			const hasShares = this.shares.length > 0
			const title = s
				? `${s.displayCode || s.code}${hasShares ? ' 可预约' : ''} · ${s.venueName || '友邻座'}`
				: '友邻座 - 发现身边的共享座位'
			return { title, path: `/pages/seat/seat?id=${this.id}` }
		},
		onShareTimeline() {
			const s = this.seat
			const hasShares = this.shares.length > 0
			const title = s
				? `${s.displayCode || s.code}${hasShares ? ' 可预约' : ''} · ${s.venueName || '友邻座'}`
				: '友邻座 - 发现身边的共享座位'
			return { title, query: `id=${this.id}` }
		},
		onShow() {
			this.load()
		},
		computed: {
			// 分享标签（与主页/场馆详情显示的备注一致）：取第一条分享的 note 拆分为标签
			shareTags() {
				const note = this.shares.length ? (this.shares[0].note || '') : ''
				return note ? note.split(/\s+/).filter(Boolean) : []
			}
		},
		methods: {
			formatTime,
			statusText,
			async load() {
				try {
					this.seat = await api.getSeat(this.id)
					// 座位状态语义：不可用 > 使用中(已入座) > 已预约 > 可预约(有可用分享) > 待分享
					if (this.seat.status === 'Unavailable') {
						this.seat.statusText = '不可用'
					} else if (this.seat.status === 'Occupied') {
						this.seat.statusText = '使用中'
					} else if (this.seat.currentReservedCount > 0) {
						this.seat.statusText = '已预约'
					} else if (this.seat.currentShareCount > 0) {
						this.seat.statusText = '可预约'
					} else {
						this.seat.statusText = '待分享'
					}
					this.shares = await api.getShares(this.id)
					const token = uni.getStorageSync('token')
					if (token) {
						try {
							this.mySession = await api.getMySession()
						} catch (e) {}
						try {
							this.myWaitlist = await api.getMyWaitlist()
						} catch (e) {}
					}
				} catch (e) {
					uni.showToast({ title: '加载失败', icon: 'none' })
				}
			},
			isMine(s) {
				return s.ownerUserId === (uni.getStorageSync('user') || {}).id
			},
			waiting(s) {
				return this.myWaitlist.some(w => w.shareId === s.id && (w.status === 'Waiting' || w.status === 'Notified'))
			},
			// 候补优先预约权：我的候补已被通知（座位预留给本人）时，显示「去预约」
			yourTurn(s) {
				return this.myWaitlist.some(w => w.shareId === s.id && w.status === 'Notified')
			},
			canReserve(s) {
				return s.isReservable || this.yourTurn(s)
			},
			async waitlist(s) {
				if (!this.checkLogin()) return
				try {
					await api.joinWaitlist(s.id)
					this.myWaitlist = await api.getMyWaitlist()
					uni.showToast({ title: '已加入候补，有空位会通知你', icon: 'none' })
					// 加入候补后请求订阅：有空位时能收到微信订阅消息
					subscribeFor(['waitlist_available'])
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			},
			checkLogin() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return false
				}
				return true
			},
			async reserve(share) {
				if (!this.checkLogin() || this.loading) return
				this.loading = true
				uni.showModal({
					title: '确认预约',
					content: `预约「${share.displayCode || this.seat.code}」预计释放：${formatTime(share.endAt)}？`,
					success: async (res) => {
						if (res.confirm) {
							try {
								// 先请求订阅授权（预约成功/即将开始/到座提醒），再创建预约，确保能收到推送
								await subscribeFor(['reservation_created', 'reservation_starting', 'arrival_required'])
								await api.createReservation(share.id)
								uni.showToast({ title: '预约成功', icon: 'success' })
								setTimeout(() => uni.switchTab({ url: '/pages/reservations/reservations' }), 600)
							} catch (e) {
								uni.showToast({ title: e.message || '预约失败', icon: 'none' })
							}
						}
						this.loading = false
					}
				})
			},
			goShare() {
				uni.navigateTo({ url: `/pages/share/share?seatId=${this.id}` })
			},
			report(share) {
				if (!this.checkLogin()) return
				if (share) {
					const nick = encodeURIComponent(share.ownerNickname || '')
					uni.navigateTo({
						url: `/pages/report/report?targetType=Share&targetId=${share.id}&targetUserId=${share.ownerUserId || ''}&targetNickname=${nick}`
					})
				} else {
					uni.navigateTo({ url: `/pages/report/report?targetType=Seat&targetId=${this.id}` })
				}
			},
			openSwap(mode) {
				if (!this.checkLogin()) return
				this.swapMode = mode
				this.publishReasons = []
				this.durationIdx = 1
				this.want = { floor: 0, area: 0, zone: 0 }
				this.showSwap = true
				this.loadSwapContext()
			},
			async loadSwapContext() {
				try {
					if (!this.swapVenue && this.seat && this.seat.venueId) {
						this.swapVenue = await api.getVenue(this.seat.venueId)
					}
				} catch (e) {}
				if (this.swapMode === 'respond') {
					try {
						const list = await api.getSwaps(this.seat.venueId)
						this.openSwaps = list.filter(x => !x.isMine && !x.respondedByMe)
					} catch (e) {
						this.openSwaps = []
					}
				}
			},
			floorList(ctx) {
				const base = this.swapVenue && this.swapVenue.floors ? this.swapVenue.floors : []
				return ctx === 'want' ? [{ id: null, name: '不限楼层' }].concat(base) : base
			},
			floorObj(ctx) {
				return this.floorList(ctx)[this.want.floor] || null
			},
			areaOptions(ctx) {
				const f = this.floorObj(ctx)
				const base = [{ id: null, name: '不限区域' }]
				if (!f || !f.areas) return base
				return base.concat(f.areas.map(a => ({ id: a.id, name: a.name })))
			},
			zoneOptions(ctx) {
				const f = this.floorObj(ctx)
				const base = [{ id: null, name: '不限区块' }]
				if (!f || !f.zones) return base
				const area = this.areaOptions(ctx)[this.want.area]
				let zones = f.zones
				if (area && area.id) zones = zones.filter(z => z.areaId === area.id)
				return base.concat(zones.map(z => ({ id: z.id, name: z.name })))
			},
			pickText(ctx, level) {
				if (level === 'floor') {
					const o = this.floorList(ctx)[this.want.floor]
					return o ? o.name : '选择楼层'
				}
				if (level === 'area') {
					const o = this.areaOptions(ctx)[this.want.area]
					return o ? o.name : '不限区域'
				}
				const o = this.zoneOptions(ctx)[this.want.zone]
				return o ? o.name : '不限区块'
			},
			onLoc(ctx, level, e) {
				const v = Number(e.detail.value)
				if (level === 'floor') {
					this.want.floor = v
					this.want.area = 0
					this.want.zone = 0
				} else if (level === 'area') {
					this.want.area = v
					this.want.zone = 0
				} else {
					this.want.zone = v
				}
			},
			toggleReason(code) {
				const i = this.publishReasons.indexOf(code)
				if (i >= 0) this.publishReasons.splice(i, 1)
				else this.publishReasons.push(code)
			},
			wantText(s) {
				const parts = [s.wantFloorName || '不限楼层', s.wantAreaName, s.wantZoneName].filter(Boolean)
				return parts.join(' / ')
			},
			reasonLabel(code) {
				const o = this.reasonOptions.find(r => r.code === code)
				return o ? o.label : code
			},
			async submitPublish() {
				if (this.swapping) return
				if (!this.publishReasons.length) {
					uni.showToast({ title: '请选择换座原因', icon: 'none' })
					return
				}
				this.swapping = true
				try {
					const f = this.floorObj('want')
					const area = this.areaOptions('want')[this.want.area]
					const zone = this.zoneOptions('want')[this.want.zone]
					await api.createSwap({
						seatId: Number(this.id),
						wantFloorId: f ? f.id : null,
						wantAreaId: area ? area.id : null,
						wantZoneId: zone ? zone.id : null,
						reasons: this.publishReasons,
						durationMinutes: this.durationOptions[this.durationIdx].value
					})
					this.showSwap = false
					uni.showToast({ title: '已发布换座意向', icon: 'none' })
				} catch (e) {
					uni.showToast({ title: (e && e.message) || '发布失败', icon: 'none' })
				} finally {
					this.swapping = false
				}
			},
			async submitRespond(s) {
				if (this.swapping) return
				this.swapping = true
				try {
					await api.respondSwap(s.id, { seatId: Number(this.id) })
					this.showSwap = false
					uni.showToast({ title: '已提交，等待对方确认', icon: 'none' })
				} catch (e) {
					uni.showToast({ title: (e && e.message) || '提交失败', icon: 'none' })
				} finally {
					this.swapping = false
				}
			}
		}
	}
</script>

<style scoped>
	.seat-header {
		display: flex;
		flex-direction: column;
		gap: 12rpx;
	}
	.seat-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.seat-code {
		font-size: 40rpx;
		font-weight: 700;
	}
	.seat-features {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
	}
	.feature {
		font-size: 24rpx;
		color: #55554F;
	}
	.seat-desc {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.seat-loc {
		display: flex;
		align-items: center;
		gap: 12rpx;
	}
	.loc-floor {
		font-size: 24rpx;
		color: var(--primary);
		background: var(--primary-bg);
		padding: 2rpx 14rpx;
		border-radius: 8rpx;
	}
	.loc-area {
		font-size: 24rpx;
		color: #55554F;
	}
	.loc-venue {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.share-card {
		padding: 24rpx;
	}
	.share-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
		gap: 16rpx;
	}
	.share-info {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.share-time {
		font-size: 28rpx;
		font-weight: 500;
	}
	.share-owner, .share-note {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.share-actions {
		display: flex;
		flex-direction: column;
		gap: 12rpx;
	}
	.btn-primary.small, .btn-outline.small {
		font-size: 26rpx;
		line-height: 2;
		padding: 0 24rpx;
		margin: 0;
	}
	.swap-btns {
		display: flex;
		gap: 16rpx;
		margin-top: 20rpx;
	}
	.swap-mask {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.4);
		display: flex;
		align-items: flex-end;
		z-index: 999;
	}
	.swap-pop {
		width: 100%;
		background: #fff;
		border-radius: 24rpx 24rpx 0 0;
		padding: 32rpx;
		max-height: 80vh;
		overflow-y: auto;
	}
	.swap-pop-title {
		font-size: 32rpx;
		font-weight: 700;
		display: block;
		margin-bottom: 12rpx;
	}
	.swap-pop-hint {
		display: block;
		font-size: 22rpx;
		color: #8A8A86;
		margin-bottom: 12rpx;
	}
	.swap-pop-sub {
		display: block;
		font-size: 26rpx;
		font-weight: 600;
		color: var(--primary);
		margin-bottom: 8rpx;
	}
	.swap-lb {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin: 20rpx 0 10rpx;
	}
	.swap-loc-row {
		display: flex;
		gap: 12rpx;
	}
	.swap-pick {
		background: var(--primary-bg, #EAF3F1);
		border-radius: 12rpx;
		padding: 14rpx 16rpx;
		font-size: 24rpx;
		text-align: center;
	}
	.swap-reasons {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
	}
	.swap-chip {
		font-size: 22rpx;
		padding: 6rpx 18rpx;
		border-radius: 999rpx;
		background: var(--primary-bg, #EAF3F1);
		color: var(--primary);
	}
	.swap-chip.on {
		background: var(--primary);
		color: #fff;
	}
	.swap-req {
		display: flex;
		justify-content: space-between;
		align-items: center;
		gap: 16rpx;
		padding: 18rpx 0;
		border-top: 1rpx solid #F0EEE8;
	}
	.swap-req-info {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 6rpx;
	}
	.swap-req-seat {
		font-size: 26rpx;
		font-weight: 600;
	}
	.swap-req-want {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.swap-empty {
		text-align: center;
		color: #B0AEA8;
		font-size: 26rpx;
		padding: 40rpx 0;
	}
	.swap-pop-actions {
		display: flex;
		justify-content: flex-end;
		gap: 16rpx;
		margin-top: 30rpx;
	}
</style>
