<template>
	<page-meta :page-style="pageThemeStyle" />
		<view>
		<view class="quick-actions">
			<view class="action-btn" @click="goFindSeat">
				<image class="action-icon" :src="`/static/icons/search-${season}.png`" mode="aspectFit" />
				<text>找座位</text>
			</view>
			<view class="action-btn" @click="goStudy">
				<image class="action-icon" :src="`/static/icons/study-${season}.png`" mode="aspectFit" />
				<text>开始学习</text>
			</view>
			<view class="action-btn" @click="goReading">
				<image class="action-icon" :src="`/static/icons/books-${season}.png`" mode="aspectFit" />
				<text>我的阅读</text>
			</view>
		</view>

		<view v-if="nearby.length" class="section">
			<text class="section-title">附近场馆</text>
			<view class="card venue-card" v-for="v in nearby" :key="v.id" @click="goVenue(v.id)">
				<view class="venue-main">
					<text class="venue-name">{{v.name}}</text>
					<view class="venue-addr-row">
						<text class="venue-addr">{{v.address}}</text>
						<text class="venue-distance" v-if="v.distanceKm">{{v.distanceKm}}km</text>
					</view>
				</view>
				<view class="venue-meta">
					<text class="venue-count">{{v.seatCount || 0}} 座位</text>
					<text class="venue-sep">丨</text>
					<text class="venue-available" :class="{ none: v.availableCount === 0 }">可预约 {{v.availableCount || 0}}</text>
				</view>
			</view>
		</view>

		<view v-if="shares.length" class="section">
			<text class="section-title">最近分享的座位</text>
			<view class="card share-card" v-for="s in shares" :key="s.id" @click="goSeat(s.seatId)">
				<view class="share-top">
					<text class="share-seat">{{s.displayCode || s.seatCode}}</text>
					<text class="tag" :class="shareTagClass(s.status)">{{statusText(s.status)}}</text>
				</view>
				<text class="share-venue">{{s.venueName}}<text v-if="s.floorName" class="share-floor"> · {{s.floorName}}</text><text v-if="s.areaName" class="share-floor"> · {{s.areaName}}</text></text>
				<view class="share-time">预计释放：{{formatTime(s.endAt)}}</view>
				<view class="share-note" v-if="s.note">{{s.note}}</view>
			</view>
		</view>

		<view class="section">
			<view class="sec-head">
				<text class="section-title">最近换座</text>
				<text class="sec-more" @click="openPublish">＋ 发布换座</text>
			</view>
			<view v-if="swaps.length">
				<view class="card swap-card" v-for="s in swaps" :key="s.id">
					<view class="swap-top">
						<text class="swap-title">{{s.userNickname || '友邻'}} · {{s.venueName}}</text>
						<text class="remain">剩 {{remainMinutes(s.expireAt)}} 分</text>
					</view>
					<view class="swap-line"><text class="sk">当前位置</text><text class="sv">{{locText(s)}}</text></view>
					<view class="swap-line"><text class="sk">想换到</text><text class="sv">{{wantText(s)}}</text></view>
					<view class="reasons">
						<text class="chip" v-for="r in s.reasons" :key="r">{{reasonLabel(r)}}</text>
					</view>
					<view class="swap-foot">
						<text class="swap-hint">平台仅信息撮合，线下自行交换</text>
						<button v-if="s.isMine" class="btn-outline small" disabled>我发布的</button>
						<button v-else-if="s.respondedByMe" class="btn-outline small" disabled>{{myResponseText(s.myResponseStatus)}}</button>
						<button v-else class="btn-primary small" @click="openRespond(s)">我愿意换</button>
					</view>
				</view>
			</view>
			<view v-else class="swap-empty">暂无换座需求</view>
		</view>

		<view v-if="!nearby.length && !shares.length" class="empty">
			<text>正在加载附近的场馆与共享座位…</text>
		</view>

		<!-- 浮动分享入口 -->
		<view class="fab" @click="goShare">
			<view class="share-icon"></view>
			<text>分享座位</text>
		</view>

		<!-- 发布换座 -->
		<view v-if="showPublish" class="mask" @click="showPublish = false">
			<view class="pop" @click.stop>
				<text class="pop-title">发布换座意向</text>
				<text class="pop-hint">免费、无联系方式；平台仅提供信息撮合，以场馆规定为准。</text>

				<text class="lb">场馆</text>
				<picker mode="selector" :range="publishVenues" range-key="name" :value="pubVenueIdx" @change="onVenueChange">
					<view class="pick">{{pubVenue ? pubVenue.name : '选择场馆'}}</view>
				</picker>

				<text class="lb">我当前的位置</text>
				<view class="loc-row">
					<picker mode="selector" :range="floorList('cur')" range-key="name" :value="cur.floor" @change="onLoc('cur', 'floor', $event)">
						<view class="pick">{{pickText('cur', 'floor')}}</view>
					</picker>
					<picker mode="selector" :range="areaOptions('cur')" range-key="name" :value="cur.area" @change="onLoc('cur', 'area', $event)">
						<view class="pick">{{pickText('cur', 'area')}}</view>
					</picker>
					<picker mode="selector" :range="zoneOptions('cur')" range-key="name" :value="cur.zone" @change="onLoc('cur', 'zone', $event)">
						<view class="pick">{{pickText('cur', 'zone')}}</view>
					</picker>
				</view>

				<text class="lb">期望换到（可不限）</text>
				<view class="loc-row">
					<picker mode="selector" :range="floorList('want')" range-key="name" :value="want.floor" @change="onLoc('want', 'floor', $event)">
						<view class="pick">{{pickText('want', 'floor')}}</view>
					</picker>
					<picker mode="selector" :range="areaOptions('want')" range-key="name" :value="want.area" @change="onLoc('want', 'area', $event)">
						<view class="pick">{{pickText('want', 'area')}}</view>
					</picker>
					<picker mode="selector" :range="zoneOptions('want')" range-key="name" :value="want.zone" @change="onLoc('want', 'zone', $event)">
						<view class="pick">{{pickText('want', 'zone')}}</view>
					</picker>
				</view>

				<text class="lb">原因（客观因素，可多选）</text>
				<view class="reasons">
					<text class="chip" :class="{ on: publishReasons.includes(r.code) }" v-for="r in reasonOptions" :key="r.code" @click="toggleReason(r.code)">{{r.label}}</text>
				</view>

				<text class="lb">有效期</text>
				<view class="chips-row">
					<text class="chip" :class="{ on: durationIdx === i }" v-for="(d, i) in durationOptions" :key="d.value" @click="durationIdx = i">{{d.label}}</text>
				</view>

				<view class="pop-actions">
					<button class="btn-outline small" @click="showPublish = false">取消</button>
					<button class="btn-primary small" :loading="submitting" @click="publish">发布</button>
				</view>
			</view>
		</view>

		<!-- 响应换座 -->
		<view v-if="showRespond" class="mask" @click="showRespond = false">
			<view class="pop" @click.stop>
				<text class="pop-title">提交我的位置</text>
				<text class="pop-hint">对方确认后即可线下物理交换</text>
				<view class="loc-row">
					<picker mode="selector" :range="floorList('resp')" range-key="name" :value="resp.floor" @change="onLoc('resp', 'floor', $event)">
						<view class="pick">{{pickText('resp', 'floor')}}</view>
					</picker>
					<picker mode="selector" :range="areaOptions('resp')" range-key="name" :value="resp.area" @change="onLoc('resp', 'area', $event)">
						<view class="pick">{{pickText('resp', 'area')}}</view>
					</picker>
					<picker mode="selector" :range="zoneOptions('resp')" range-key="name" :value="resp.zone" @change="onLoc('resp', 'zone', $event)">
						<view class="pick">{{pickText('resp', 'zone')}}</view>
					</picker>
				</view>
				<view class="pop-actions">
					<button class="btn-outline small" @click="showRespond = false">取消</button>
					<button class="btn-primary small" :loading="submitting" @click="submitRespond">提交</button>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime, statusText } from '../../utils/format.js'
	import { getSeasonKey } from '../../utils/theme.js'

	export default {
		data() {
			return {
				nearby: [],
				shares: [],
				venueShares: [],
				season: getSeasonKey(),
				swaps: [],
				showPublish: false,
				showRespond: false,
				submitting: false,
				currentSwap: null,
				publishVenues: [],
				pubVenueIdx: 0,
				pubVenue: null,
				respondVenue: null,
				publishReasons: [],
				durationIdx: 1,
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
				],
				cur: { floor: 0, area: 0, zone: 0 },
				want: { floor: 0, area: 0, zone: 0 },
				resp: { floor: 0, area: 0, zone: 0 }
			}
		},
		computed: {
			activeVenue() {
				if (this.showPublish) return this.pubVenue
				if (this.showRespond) return this.respondVenue
				return null
			}
		},
		onShow() {
			this.loadData()
		},
		onPullDownRefresh() {
			this.loadData().then(() => uni.stopPullDownRefresh())
		},
		onShareAppMessage() {
			return { title: '友邻座 - 发现身边的共享座位，座位不浪费', path: '/pages/index/index' }
		},
		onShareTimeline() {
			return { title: '友邻座 - 发现身边的共享座位' }
		},
		methods: {
			formatTime,
			statusText,
			shareTagClass(status) {
				const map = {
					Available: 'status-available',
					Reserved: 'status-reserved',
					Active: 'status-active',
					Completed: 'status-completed'
				}
				return map[status] || 'status-completed'
			},
			async loadData() {
				this.loadSwaps()
				try {
					const location = await this.getLocation()
					this.nearby = await api.getVenues({
						lat: location.latitude,
						lng: location.longitude,
						radiusKm: 20,
						page: 1,
						pageSize: 10
					})
					if (!this.nearby.length) {
						this.nearby = await api.getVenues({})
					}
					if (this.nearby.length) {
						const venueId = this.nearby[0].id
						this.venueShares = await api.getVenueShares(venueId)
						this.shares = this.venueShares.slice(0, 5)
					}
				} catch (e) {
					try {
						this.nearby = await api.getVenues({})
					} catch (err) {
						this.nearby = []
					}
				}
				// 同步底部「通知」tab 未读角标
				const token = uni.getStorageSync('token')
				if (token) {
					try {
						const unread = await api.getUnreadCount()
						this.setTabBarBadge(unread)
					} catch (e) {}
				}
			},
			setTabBarBadge(count) {
				try {
					if (count > 0) {
						uni.setTabBarBadge({ index: 2, text: count > 99 ? '99+' : String(count) })
					} else {
						uni.removeTabBarBadge({ index: 2 })
					}
				} catch (e) {}
			},
			getLocation() {
				return new Promise((resolve) => {
					uni.getLocation({
						type: 'gcj02',
						success: (res) => resolve({ latitude: res.latitude, longitude: res.longitude }),
						fail: () => resolve({ latitude: 30.5728, longitude: 104.0668 })
					})
				})
			},
			goFindSeat() {
				uni.navigateTo({ url: '/pages/venues/venues' })
			},
			goShare() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				// 去场馆选一个座位，在座位详情发起分享
				uni.navigateTo({ url: '/pages/venues/venues' })
				setTimeout(() => uni.showToast({ title: '请选择一个座位来分享', icon: 'none' }), 400)
			},
			goStudy() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				uni.navigateTo({ url: '/pages/study/study' })
			},
			goReading() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				uni.navigateTo({ url: '/pages/reading/reading' })
			},
			goReservations() {
				uni.switchTab({ url: '/pages/reservations/reservations' })
			},
			goVenue(id) {
				uni.navigateTo({ url: `/pages/venue/venue?id=${id}` })
			},
			goSeat(id) {
				uni.navigateTo({ url: `/pages/seat/seat?id=${id}` })
			},
			async loadSwaps() {
				if (!uni.getStorageSync('token')) return
				try {
					this.swaps = await api.getRecentSwaps(20)
				} catch (e) {}
			},
			locText(s) {
				const parts = [s.floorName, s.areaName, s.zoneName].filter(Boolean)
				return parts.length ? parts.join(' / ') : '未填写'
			},
			wantText(s) {
				const parts = [s.wantFloorName || '不限楼层', s.wantAreaName, s.wantZoneName].filter(Boolean)
				return parts.join(' / ')
			},
			reasonLabel(code) {
				const o = this.reasonOptions.find(r => r.code === code)
				return o ? o.label : code
			},
			remainMinutes(expireAt) {
				return Math.max(0, Math.round((new Date(expireAt).getTime() - Date.now()) / 60000))
			},
			myResponseText(status) {
				const map = { Pending: '等待对方确认', Accepted: '已同意', Rejected: '未被选中' }
				return map[status] || '已响应'
			},
			floorList(ctx) {
				const base = this.activeVenue && this.activeVenue.floors ? this.activeVenue.floors : []
				return ctx === 'want' ? [{ id: null, name: '不限楼层' }].concat(base) : base
			},
			floorObj(ctx) {
				return this.floorList(ctx)[this[ctx].floor] || null
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
				const area = this.areaOptions(ctx)[this[ctx].area]
				let zones = f.zones
				if (area && area.id) zones = zones.filter(z => z.areaId === area.id)
				return base.concat(zones.map(z => ({ id: z.id, name: z.name })))
			},
			pickText(ctx, level) {
				if (level === 'floor') {
					const o = this.floorList(ctx)[this[ctx].floor]
					return o ? o.name : '选择楼层'
				}
				if (level === 'area') {
					const o = this.areaOptions(ctx)[this[ctx].area]
					return o ? o.name : '不限区域'
				}
				const o = this.zoneOptions(ctx)[this[ctx].zone]
				return o ? o.name : '不限区块'
			},
			onLoc(ctx, level, e) {
				const v = Number(e.detail.value)
				if (level === 'floor') {
					this[ctx].floor = v
					this[ctx].area = 0
					this[ctx].zone = 0
				} else if (level === 'area') {
					this[ctx].area = v
					this[ctx].zone = 0
				} else {
					this[ctx].zone = v
				}
			},
			buildLoc(ctx) {
				const f = this.floorObj(ctx)
				const area = this.areaOptions(ctx)[this[ctx].area]
				const zone = this.zoneOptions(ctx)[this[ctx].zone]
				return {
					floorId: f ? f.id : null,
					areaId: area ? area.id : null,
					zoneId: zone ? zone.id : null
				}
			},
			toggleReason(code) {
				const i = this.publishReasons.indexOf(code)
				if (i >= 0) this.publishReasons.splice(i, 1)
				else this.publishReasons.push(code)
			},
			async openPublish() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				this.publishReasons = []
				this.durationIdx = 1
				this.cur = { floor: 0, area: 0, zone: 0 }
				this.want = { floor: 0, area: 0, zone: 0 }
				this.showPublish = true
				try {
					if (!this.publishVenues.length) {
						this.publishVenues = await api.getVenues({ page: 1, pageSize: 100 })
					}
				} catch (e) {}
				if (this.publishVenues.length) {
					this.pubVenueIdx = 0
					try { this.pubVenue = await api.getVenue(this.publishVenues[0].id) } catch (e) { this.pubVenue = null }
				}
			},
			async onVenueChange(e) {
				this.pubVenueIdx = Number(e.detail.value)
				const v = this.publishVenues[this.pubVenueIdx]
				this.cur = { floor: 0, area: 0, zone: 0 }
				this.want = { floor: 0, area: 0, zone: 0 }
				this.pubVenue = v ? await api.getVenue(v.id).catch(() => null) : null
			},
			async publish() {
				if (this.submitting) return
				if (!this.pubVenue) {
					uni.showToast({ title: '请选择场馆', icon: 'none' })
					return
				}
				if (!this.publishReasons.length) {
					uni.showToast({ title: '请选择换座原因', icon: 'none' })
					return
				}
				this.submitting = true
				try {
					const l = this.buildLoc('cur')
					const w = this.buildLoc('want')
					await api.createSwap({
						venueId: this.pubVenue.id,
						floorId: l.floorId,
						areaId: l.areaId,
						zoneId: l.zoneId,
						wantFloorId: w.floorId,
						wantAreaId: w.areaId,
						wantZoneId: w.zoneId,
						reasons: this.publishReasons,
						durationMinutes: this.durationOptions[this.durationIdx].value
					})
					this.showPublish = false
					uni.showToast({ title: '已发布换座意向', icon: 'none' })
					this.loadSwaps()
				} catch (e) {
					uni.showToast({ title: (e && e.message) || '发布失败', icon: 'none' })
				} finally {
					this.submitting = false
				}
			},
			async openRespond(s) {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				this.currentSwap = s
				this.resp = { floor: 0, area: 0, zone: 0 }
				this.showRespond = true
				try { this.respondVenue = await api.getVenue(s.venueId) } catch (e) { this.respondVenue = null }
			},
			async submitRespond() {
				if (this.submitting || !this.currentSwap) return
				this.submitting = true
				try {
					const l = this.buildLoc('resp')
					await api.respondSwap(this.currentSwap.id, {
						floorId: l.floorId,
						areaId: l.areaId,
						zoneId: l.zoneId
					})
					this.showRespond = false
					uni.showToast({ title: '已提交，等待对方确认', icon: 'none' })
					this.loadSwaps()
				} catch (e) {
					uni.showToast({ title: (e && e.message) || '提交失败', icon: 'none' })
				} finally {
					this.submitting = false
				}
			}
		}
	}
</script>

<style scoped>
	.quick-actions {
		display: flex;
		margin: 20rpx;
		background: #FFFFFF;
		border-radius: 20rpx;
		padding: 24rpx 0;
		box-shadow: 0 4rpx 16rpx rgba(0, 0, 0, 0.06);
		position: relative;
		z-index: 1;
	}
	.action-btn {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 8rpx;
		color: #33332E;
		font-size: 26rpx;
	}
	.action-icon {
		width: 56rpx;
		height: 56rpx;
	}
	.fab {
		position: fixed;
		right: 30rpx;
		bottom: 60rpx;
		z-index: 99;
		display: flex;
		align-items: center;
		gap: 12rpx;
		padding: 20rpx 32rpx;
		border-radius: 44rpx;
		background: var(--primary-gradient, linear-gradient(135deg, #C98A3D, #DBA968));
		color: #FFFFFF;
		font-size: 26rpx;
		font-weight: 600;
		box-shadow: 0 8rpx 24rpx rgba(0, 0, 0, 0.18);
	}
	.share-icon {
		width: 30rpx;
		height: 30rpx;
		flex: none;
		background: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'%3E%3Cpath fill='%23FFFFFF' d='M18 16.08c-.76 0-1.44.3-1.96.77L8.91 12.7c.05-.23.09-.46.09-.7s-.04-.47-.09-.7l7.05-4.11c.54.5 1.25.81 2.04.81 1.66 0 3-1.34 3-3s-1.34-3-3-3-3 1.34-3 3c0 .24.04.47.09.7L8.04 9.81C7.5 9.31 6.79 9 6 9c-1.66 0-3 1.34-3 3s1.34 3 3 3c.79 0 1.5-.31 2.04-.81l7.12 4.16c-.05.21-.08.43-.08.65 0 1.61 1.31 2.92 2.92 2.92 1.61 0 2.92-1.31 2.92-2.92s-1.31-2.92-2.92-2.92z'/%3E%3C/svg%3E") no-repeat center / contain;
	}
	.section {
		margin-top: 20rpx;
	}
	.venue-card {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.venue-main {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.venue-name {
		font-size: 30rpx;
		font-weight: 600;
	}
	.venue-addr {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.venue-addr-row {
		display: flex;
		align-items: center;
		gap: 12rpx;
	}
	.venue-distance {
		font-size: 22rpx;
		color: var(--primary);
		flex: none;
	}
	.venue-meta {
		display: flex;
		flex-direction: row;
		align-items: center;
		gap: 12rpx;
	}
	.venue-available {
		font-size: 24rpx;
		color: var(--primary);
	}
	.venue-count {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.venue-sep {
		font-size: 22rpx;
		color: #C9C4B8;
		transform: scaleY(0.8);
	}
	.venue-available.none {
		color: #B85450;
	}
	.share-card {
		display: flex;
		flex-direction: column;
		gap: 10rpx;
	}
	.share-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.share-seat {
		font-size: 30rpx;
		font-weight: 600;
		color: var(--primary);
	}
	.share-venue {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.share-floor {
		color: var(--primary);
	}
	.share-time {
		font-size: 26rpx;
		color: #55554F;
	}
	.share-note {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.sec-head {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.sec-more {
		font-size: 26rpx;
		color: var(--primary);
	}
	.swap-card {
		margin-top: 12rpx;
	}
	.swap-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.swap-title {
		font-size: 28rpx;
		font-weight: 600;
	}
	.remain {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.swap-line {
		display: flex;
		margin-top: 10rpx;
	}
	.sk {
		width: 120rpx;
		font-size: 24rpx;
		color: #8A8A86;
	}
	.sv {
		flex: 1;
		font-size: 26rpx;
	}
	.reasons {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
		margin-top: 12rpx;
	}
	.chip {
		font-size: 22rpx;
		padding: 6rpx 18rpx;
		border-radius: 999rpx;
		background: var(--primary-bg, #EAF3F1);
		color: var(--primary);
	}
	.chip.on {
		background: var(--primary);
		color: #fff;
	}
	.chips-row {
		display: flex;
		gap: 12rpx;
	}
	.swap-foot {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-top: 16rpx;
	}
	.swap-hint {
		font-size: 20rpx;
		color: #B0AEA8;
	}
	.swap-empty {
		text-align: center;
		color: #B0AEA8;
		font-size: 26rpx;
		padding: 30rpx 0;
	}
	.mask {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.4);
		display: flex;
		align-items: flex-end;
		z-index: 999;
	}
	.pop {
		width: 100%;
		background: #fff;
		border-radius: 24rpx 24rpx 0 0;
		padding: 32rpx;
		max-height: 80vh;
		overflow-y: auto;
	}
	.pop-title {
		font-size: 32rpx;
		font-weight: 700;
		display: block;
		margin-bottom: 12rpx;
	}
	.pop-hint {
		display: block;
		font-size: 22rpx;
		color: #8A8A86;
		margin-bottom: 12rpx;
	}
	.lb {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin: 20rpx 0 10rpx;
	}
	.loc-row {
		display: flex;
		gap: 12rpx;
	}
	.pick {
		background: var(--primary-bg, #EAF3F1);
		border-radius: 12rpx;
		padding: 14rpx 16rpx;
		font-size: 24rpx;
		text-align: center;
	}
	.pop-actions {
		display: flex;
		justify-content: flex-end;
		gap: 16rpx;
		margin-top: 30rpx;
	}
</style>
