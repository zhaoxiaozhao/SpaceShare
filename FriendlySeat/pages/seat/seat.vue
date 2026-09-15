<template>
	<page-meta :page-style="pageThemeStyle" />
		<view v-if="seat">
		<view class="card seat-header">
			<view class="seat-top">
				<text class="seat-code">{{seatCodeText}}</text>
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
				<template v-if="seatSwap && seatSwap.status === 'Matched'">
					<text class="share-note">该座位近期已匹配换座，暂不可再发布或回应。</text>
				</template>
				<template v-else-if="seatSwap && seatSwap.isMine">
					<text class="share-note">你已发布换座意向，等待其他友邻回应。</text>
					<button class="btn-outline" style="margin-top:20rpx;" @click="cancelSwap">取消发布</button>
				</template>
				<template v-else-if="seatSwap">
					<text class="share-note">有友邻发布了换座意向。如你愿意，可提交自己的座位作为回应，由对方决定是否交换；请双方自行线下协商并遵守场馆规定。</text>
					<button class="btn-primary" style="margin-top:20rpx;" @click="openRespondSwap">回应换座</button>
				</template>
				<template v-else>
					<text class="share-note">如需调整座位，可发布换座意向，同场馆友邻可自愿回应，双方自行线下协商。请遵守场馆规定。</text>
					<button class="btn-primary" style="margin-top:20rpx;" @click="openSwap">发布换座意向</button>
				</template>
			</view>
		</view>

		<!-- 响应换座：选自己的座位 -->
		<view v-if="showRespondSwap" class="swap-mask" @click="showRespondSwap = false">
			<view class="swap-pop" @click.stop>
				<text class="swap-pop-title">回应换座</text>
				<text class="swap-pop-hint">对方座位 {{seatSwap.seatCode}} · 期望位置 {{wantText(seatSwap)}}</text>
				<text class="swap-lb">选择我的座位</text>
				<view class="swap-loc-row">
					<OptionPicker :range="respFloors" range-key="name" :value="resp.floor" title="选择楼层" @change="onResp('floor', $event)" />
					<OptionPicker :range="respAreas" range-key="name" :value="resp.area" title="选择区域" @change="onResp('area', $event)" />
				</view>
				<view class="swap-loc-row" style="margin-top: 14rpx;">
					<OptionPicker :range="respZones" range-key="name" :value="resp.zone" title="选择区块" @change="onResp('zone', $event)" />
					<OptionPicker :range="respSeats" range-key="name" :value="resp.seat" title="选择座位" @change="onResp('seat', $event)" />
				</view>
				<view class="swap-pop-actions">
					<button class="btn-outline action-btn" @click="showRespondSwap = false">取消</button>
					<button class="btn-primary action-btn" :loading="responding" @click="submitRespond">提交</button>
				</view>
			</view>
		</view>

		<!-- 换座弹窗 -->
		<view v-if="showSwap" class="swap-mask" @click="showSwap = false">
			<view class="swap-pop" @click.stop>
				<text class="swap-pop-title">发布换座意向</text>
				<view class="swap-mine">
					<text class="swap-mine-label">我的座位</text>
					<text class="swap-mine-value">{{seatCodeText}}</text>
				</view>

				<text class="swap-lb">期望换到（可不限）</text>
				<view class="swap-loc-row">
					<OptionPicker :range="floorList('want')" range-key="name" :value="want.floor" title="选择楼层" @change="onLoc('want', 'floor', $event)" />
					<OptionPicker :range="areaOptions('want')" range-key="name" :value="want.area" title="选择区域" @change="onLoc('want', 'area', $event)" />
					<OptionPicker :range="zoneOptions('want')" range-key="name" :value="want.zone" title="选择区块" @change="onLoc('want', 'zone', $event)" />
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
					<button class="btn-outline action-btn" @click="showSwap = false">取消</button>
					<button class="btn-primary action-btn" :loading="swapping" @click="submitPublish">发布</button>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { getAppOptions } from '../../utils/options.js'
	import { formatTime, statusText } from '../../utils/format.js'
	import { subscribeFor } from '../../utils/subscribe.js'

	export default {
		data() {
			return {
				id: null,
				venueId: null,
				seat: null,
				shares: [],
				mySession: null,
				myWaitlist: [],
				loading: false,
				showSwap: false,
				swapVenue: null,
				publishReasons: [],
				durationIdx: 1,
				swapping: false,
				seatSwap: null,
				showRespondSwap: false,
				responding: false,
				resp: { floor: 0, area: 0, zone: 0, seat: 0 },
				want: { floor: 0, area: 0, zone: 0 },
				durationOptions: [
					{ label: '30分钟', value: 30 },
					{ label: '1小时', value: 60 },
					{ label: '2小时', value: 120 }
				]
			}
		},
		onLoad(options) {
			this.id = options.id
			this.venueId = options.venueId ? Number(options.venueId) : null
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
			// 换座原因（后台可配置）
			reasonOptions() {
				return getAppOptions().swapReasons
			},
			// 统一的座位编号展示：楼层-区块区-序号（如 3F-A区-001）
			seatCodeText() {
				const s = this.seat
				if (!s) return ''
				const code = s.displayCode || s.code || ''
				return s.floorName ? `${s.floorName}-${code}` : code
			},
			// 分享标签（与主页/场馆详情显示的备注一致）：取第一条分享的 note 拆分为标签
			shareTags() {
				const note = this.shares.length ? (this.shares[0].note || '') : ''
				return note ? note.split(/\s+/).filter(Boolean) : []
			},
			respFloors() {
				return this.swapVenue && this.swapVenue.floors ? this.swapVenue.floors : []
			},
			respFloor() {
				return this.respFloors[this.resp.floor] || null
			},
			respAreas() {
				const f = this.respFloor
				if (!f || !f.areas || !f.areas.length) return [{ id: null, name: '全部区域' }]
				return f.areas.map(a => ({ id: a.id, name: a.name }))
			},
			respArea() {
				return this.respAreas[this.resp.area] || null
			},
			respZones() {
				const f = this.respFloor
				if (!f || !f.zones) return []
				let zones = f.zones
				if (this.respArea && this.respArea.id) zones = zones.filter(z => z.areaId === this.respArea.id)
				const labels = this.zoneLabels()
				return zones.map(z => ({ id: z.id, name: labels[z.id] || z.label || z.name }))
			},
			respZone() {
				return this.respZones[this.resp.zone] || null
			},
			respSeats() {
				if (!this.respZone || !this.respZone.id) return []
				const f = this.respFloor
				const z = f && f.zones ? f.zones.find(x => x.id === this.respZone.id) : null
				if (!z || !z.seats) return []
				const floorName = f ? f.name : ''
				const letter = (this.respZone.name || '').replace('区', '')
				const prefix = floorName ? `${floorName}-` : ''
				return z.seats.map(s => ({ id: s.id, name: `${prefix}${letter}区-${(s.code || '').split('-').pop()}` }))
			},
			respFloorText() {
				const o = this.respFloors[this.resp.floor]
				return o ? o.name : '楼层'
			},
			respAreaText() {
				const o = this.respAreas[this.resp.area]
				return o ? o.name : '区域'
			},
			respZoneText() {
				const o = this.respZones[this.resp.zone]
				return o ? o.name : '区块'
			},
			respSeatText() {
				const o = this.respSeats[this.resp.seat]
				return o ? o.name : '座位'
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
						try {
							this.seatSwap = await api.getSwapBySeat(this.id)
						} catch (e) {
							this.seatSwap = null
						}
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
								// 先请求订阅授权（预约/到座/过期/取消/信用/完成交接），再创建预约，确保能收到推送
								await subscribeFor(['reservation_created', 'reservation_starting', 'arrival_required', 'reservation_expired', 'reservation_cancelled', 'credit_changed', 'system'])
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
			openSwap() {
				if (!this.checkLogin()) return
				this.publishReasons = []
				this.durationIdx = 1
				this.want = { floor: 0, area: 0, zone: 0 }
				this.showSwap = true
				this.loadSwapContext()
			},
			async loadSwapContext() {
				const vid = this.venueId || (this.seat && this.seat.venueId) || null
				try {
					if (!this.swapVenue && vid) {
						this.swapVenue = await api.getVenue(vid)
					}
				} catch (e) {}
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
			zoneLabels() {
				const map = {}
				const v = this.swapVenue
				if (!v || !v.floors) return map
				const orderZones = (zs) => zs.slice().sort((a, b) =>
					(a.sortOrder - b.sortOrder) || ((a.offsetX || 0) - (b.offsetX || 0)) || (a.id - b.id))
				for (const f of v.floors) {
					const ordered = []
					const areas = (f.areas || []).slice().sort((a, b) => a.sortOrder - b.sortOrder)
					for (const a of areas) {
						ordered.push(...orderZones((f.zones || []).filter(z => z.areaId === a.id)))
					}
					ordered.push(...orderZones((f.zones || []).filter(z => !z.areaId)))
					ordered.forEach((z, i) => { map[z.id] = String.fromCharCode(65 + i) + '区' })
				}
				return map
			},
			zoneOptions(ctx) {
				const f = this.floorObj(ctx)
				const base = [{ id: null, name: '不限区块' }]
				if (!f || !f.zones) return base
				const area = this.areaOptions(ctx)[this.want.area]
				let zones = f.zones
				if (area && area.id) zones = zones.filter(z => z.areaId === area.id)
				const labels = this.zoneLabels()
				return base.concat(zones.map(z => ({ id: z.id, name: labels[z.id] || z.label || z.name })))
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
			onLoc(ctx, level, idx) {
				const v = Number(idx)
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
				const parts = [s.wantFloorName, s.wantAreaName, s.wantZoneName].filter(Boolean)
				return parts.length ? parts.join(' / ') : '不限'
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
					subscribeFor(['swap_request', 'system'])
					try { this.seatSwap = await api.getSwapBySeat(this.id) } catch (e) {}
				} catch (e) {
					uni.showToast({ title: (e && e.message) || '发布失败', icon: 'none' })
				} finally {
					this.swapping = false
				}
			},
			async openRespondSwap() {
				if (!this.checkLogin()) return
				this.resp = { floor: 0, area: 0, zone: 0, seat: 0 }
				this.showRespondSwap = true
				this.loadSwapContext()
			},
			onResp(level, idx) {
				const v = Number(idx)
				if (level === 'floor') {
					this.resp.floor = v
					this.resp.area = 0
					this.resp.zone = 0
					this.resp.seat = 0
				} else if (level === 'area') {
					this.resp.area = v
					this.resp.zone = 0
					this.resp.seat = 0
				} else if (level === 'zone') {
					this.resp.zone = v
					this.resp.seat = 0
				} else {
					this.resp.seat = v
				}
			},
			async submitRespond() {
				if (this.responding || !this.seatSwap) return
				const seat = this.respSeats[this.resp.seat]
				if (!seat) {
					uni.showToast({ title: '请选择你的座位', icon: 'none' })
					return
				}
				this.responding = true
				try {
					await api.respondSwap(this.seatSwap.id, { seatId: seat.id })
					this.showRespondSwap = false
					subscribeFor(['system'])
					uni.showModal({ title: '已提交', content: '回应已提交，等待对方确认。', showCancel: false })
				} catch (e) {
					uni.showToast({ title: (e && e.message) || '提交失败', icon: 'none' })
				} finally {
					this.responding = false
				}
			},
			cancelSwap() {
				if (!this.seatSwap) return
				uni.showModal({
					title: '取消发布',
					content: '确定取消你发布的换座意向吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.cancelSwap(this.seatSwap.id)
							this.seatSwap = null
							uni.showToast({ title: '已取消', icon: 'none' })
						} catch (e) {
							uni.showToast({ title: (e && e.message) || '操作失败', icon: 'none' })
						}
					}
				})
			},
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
	.swap-mine {
		display: flex;
		align-items: center;
		justify-content: space-between;
		background: var(--primary-bg, #EAF3F1);
		border-radius: 14rpx;
		padding: 18rpx 22rpx;
		margin-top: 8rpx;
	}
	.swap-mine-label {
		font-size: 22rpx;
		color: var(--primary);
	}
	.swap-mine-value {
		font-size: 28rpx;
		font-weight: 700;
		color: var(--primary);
	}
	.swap-lb {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin: 24rpx 0 12rpx;
	}
	.swap-loc-row {
		display: flex;
		gap: 14rpx;
	}
	.swap-pick {
		flex: 1;
		min-width: 0;
		background: #F8F7F3;
		border: 1rpx solid #ECEAE3;
		border-radius: 12rpx;
		padding: 16rpx 12rpx;
		font-size: 24rpx;
		text-align: center;
		color: #4A4945;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
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
		gap: 16rpx;
		margin-top: 36rpx;
	}
	.action-btn {
		flex: 1;
		margin: 0;
		font-size: 28rpx;
		line-height: 2.4;
		padding: 0;
	}
</style>
