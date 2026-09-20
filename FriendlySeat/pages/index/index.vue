<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
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

		<!-- 当前图书馆（切换） -->
		<view class="venue-bar">
			<view class="vb-left" @click="showVenuePicker = true">
				<text class="vb-label">当前图书馆</text>
				<text class="vb-name">{{currentVenueName || '选择图书馆'}}</text>
				<text class="vb-switch">切换 ›</text>
			</view>
			<text class="vb-post" @click.stop="goCreatePost">＋ 发帖</text>
		</view>

		<!-- 场馆交流板 -->
		<VenueFeed ref="feed" :venue-id="currentVenueId" />

		<view v-if="shares.length" class="section">
			<view class="sec-head">
				<text class="section-title">最近分享的座位</text>
				<text class="sec-more" @click="goSharesList">更多</text>
			</view>
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

		<view v-if="swaps.length" class="section">
			<view class="sec-head">
				<text class="section-title">最近换座</text>
				<text class="sec-more" @click="goSwapList">更多</text>
			</view>
			<view class="card swap-card" v-for="s in swaps.slice(0, 3)" :key="s.id" @click="goSwapSeat(s)">
				<view class="swap-head">
					<text class="swap-title">{{s.venueName}}</text>
					<text class="remain">剩{{remainMinutes(s.expireAt)}}分钟</text>
				</view>
				<view class="swap-route">
					<text class="route-seat">{{s.seatCode}}</text>
					<text class="route-arrow">→</text>
					<text class="route-want">{{wantText(s)}}</text>
				</view>
				<view class="reasons" v-if="s.reasons && s.reasons.length">
					<text class="chip" v-for="r in s.reasons" :key="r">{{reasonLabel(r)}}</text>
				</view>
			</view>
		</view>

		<view v-if="activities.length" class="section">
			<view class="sec-head">
				<text class="section-title">最近活动</text>
				<text class="sec-more" @click="goActivities">更多</text>
			</view>
			<view class="card act-card" v-for="a in activities" :key="a.id" @click="goActivity(a.id)">
				<image v-if="a.coverImage" class="act-cover" :src="a.coverImage" mode="aspectFill" />
				<text class="act-title">{{a.title}}</text>
				<text class="act-meta">{{formatTime(a.startAt)}}<text v-if="a.venueName"> · {{a.venueName}}</text></text>
				<view class="act-top">
					<text class="act-cat">{{categoryLabel(a.category)}}</text>
					<text class="act-count">{{a.signupCount}}/{{a.capacity}} 人</text>
				</view>
			</view>
		</view>

		<view v-if="!nearby.length && !shares.length" class="empty">
			<text>正在加载附近的场馆与共享座位…</text>
		</view>

		<!-- 选择图书馆 -->
		<view v-if="showVenuePicker" class="vk-mask" @click="showVenuePicker = false">
			<view class="vk-sheet" @click.stop>
				<text class="vk-title">选择图书馆</text>
				<scroll-view scroll-y class="vk-list">
					<view class="vk-item" :class="{ on: v.id === currentVenueId }" v-for="v in nearby" :key="v.id" @click="pickVenue(v)">
						<text class="vk-name">{{v.name}}</text>
						<text class="vk-meta">{{v.seatCount || 0}} 座位 · 可预约 {{v.availableCount || 0}}</text>
					</view>
				</scroll-view>
				<button class="btn-outline" @click="goVenues">查看全部场馆</button>
			</view>
		</view>

		<!-- 浮动分享入口 -->
		<view class="fab" @click="goShare">
			<view class="share-icon"></view>
			<text>分享座位</text>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime, statusText } from '../../utils/format.js'
	import { getSeasonKey } from '../../utils/theme.js'
	import { activityCategoryLabel } from '../../utils/activity.js'
	import { getAppOptions } from '../../utils/options.js'

	export default {
		data() {
			return {
				nearby: [],
				currentVenueId: null,
				currentVenueName: '',
				showVenuePicker: false,
				shares: [],
				venueShares: [],
				sharesVenueId: null,
				season: getSeasonKey(),
				swaps: [],
				activities: []
			}
		},
		computed: {
			reasonOptions() {
				return getAppOptions().swapReasons
			}
		},
		onShow() {
			this.loadData()
			if (this.$refs.feed) this.$refs.feed.refresh()
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
				this.loadActivities()
				try {
					const location = await this.getAuthorizedLocation()
					this.nearby = location
						? await api.getVenues({ lat: location.latitude, lng: location.longitude, radiusKm: 20, page: 1, pageSize: 10 })
						: await api.getVenues({})
					if (!this.nearby.length) {
						this.nearby = await api.getVenues({})
					}
					if (this.nearby.length && !this.currentVenueId) {
						this.currentVenueId = this.nearby[0].id
						this.currentVenueName = this.nearby[0].name
					}
					await this.loadSharesForCurrent()
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
						uni.setTabBarBadge({ index: 3, text: count > 99 ? '99+' : String(count) })
					} else {
						uni.removeTabBarBadge({ index: 3 })
					}
				} catch (e) {}
			},
			// 仅在已授权定位时才使用位置，避免启动即弹权限（合规）
			async getAuthorizedLocation() {
				const setting = await new Promise((resolve) => {
					uni.getSetting({ success: (r) => resolve(r), fail: () => resolve(null) })
				})
				if (!setting || !setting.authSetting || !setting.authSetting['scope.userLocation']) return null
				return await this.getLocation()
			},
			getLocation() {
				return new Promise((resolve) => {
					uni.getLocation({
						type: 'gcj02',
						success: (res) => resolve({ latitude: res.latitude, longitude: res.longitude }),
						fail: () => resolve(null)
					})
				})
			},
			goFindSeat() {
				uni.navigateTo({ url: '/pages/venues/venues' })
			},
			async loadSharesForCurrent() {
				if (!this.currentVenueId) return
				try {
					this.venueShares = await api.getVenueShares(this.currentVenueId)
					this.shares = this.venueShares.slice(0, 3)
					this.sharesVenueId = this.currentVenueId
				} catch (e) {}
			},
			pickVenue(v) {
				this.currentVenueId = v.id
				this.currentVenueName = v.name
				this.showVenuePicker = false
				this.loadSharesForCurrent()
			},
			goCreatePost() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				if (!this.currentVenueId) {
					uni.showToast({ title: '请先选择图书馆', icon: 'none' })
					return
				}
				uni.navigateTo({ url: `/pages/community/edit?venueId=${this.currentVenueId}&venueName=${encodeURIComponent(this.currentVenueName)}` })
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
				const vid = this.sharesVenueId ? `&venueId=${this.sharesVenueId}` : ''
				uni.navigateTo({ url: `/pages/seat/seat?id=${id}${vid}` })
			},
			async loadSwaps() {
				if (!uni.getStorageSync('token')) return
				try {
					this.swaps = await api.getRecentSwaps(20)
				} catch (e) {}
			},
			wantText(s) {
				const parts = [s.wantFloorName, s.wantAreaName, s.wantZoneName].filter(Boolean)
				return parts.length ? parts.join(' / ') : '不限'
			},
			reasonLabel(code) {
				const o = this.reasonOptions.find(r => r.code === code)
				return o ? o.label : code
			},
			remainMinutes(expireAt) {
				return Math.max(0, Math.round((new Date(expireAt).getTime() - Date.now()) / 60000))
			},
			goSwapSeat(s) {
				uni.navigateTo({ url: `/pages/seat/seat?id=${s.seatId}&venueId=${s.venueId}` })
			},
			async loadActivities() {
				if (!uni.getStorageSync('token')) return
				try {
					const list = await api.getActivities()
					this.activities = (list || []).slice(0, 3)
				} catch (e) {}
			},
			categoryLabel(code) {
				return activityCategoryLabel(code)
			},
			goActivity(id) {
				uni.navigateTo({ url: `/pages/activity/detail?id=${id}` })
			},
			goActivities() {
				uni.switchTab({ url: '/pages/activity/activity' })
			},
			goVenues() {
				uni.navigateTo({ url: '/pages/venues/venues' })
			},
			goSharesList() {
				uni.navigateTo({ url: '/pages/shares/list' })
			},
			goSwapList() {
				uni.navigateTo({ url: '/pages/swap/list' })
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
		margin-top: 14rpx;
	}
	.section-title {
		font-size: 30rpx;
		font-weight: 500;
		margin: 0;
		color: #2B2B27;
	}
	.card {
		margin: 14rpx 20rpx;
	}
	.venue-card {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.venue-name-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12rpx;
	}
	.venue-main {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.venue-name {
		flex: 1;
		min-width: 0;
		font-size: 30rpx;
		font-weight: 600;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
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
		flex-shrink: 0;
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
		align-items: baseline;
		gap: 8rpx;
		margin: 4rpx 20rpx 0;
	}
	.sec-more {
		font-size: 24rpx;
		color: var(--primary);
	}
	.act-card {
		margin-top: 12rpx;
	}
	.act-cover {
		width: 100%;
		height: 260rpx;
		border-radius: 12rpx;
		margin-bottom: 14rpx;
	}
	.act-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-top: 14rpx;
	}
	.act-cat {
		font-size: 22rpx;
		padding: 4rpx 16rpx;
		border-radius: 8rpx;
		background: var(--primary-bg, #EAF3F1);
		color: var(--primary);
	}
	.act-count {
		font-size: 22rpx;
		color: var(--primary);
		font-weight: 600;
	}
	.act-title {
		display: block;
		font-size: 30rpx;
		font-weight: 600;
	}
	.act-meta {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin-top: 8rpx;
	}
	.swap-card {
		margin-top: 12rpx;
	}
	.swap-head {
		display: flex;
		justify-content: space-between;
		align-items: center;
		gap: 16rpx;
	}
	.swap-title {
		flex: 1;
		min-width: 0;
		font-size: 28rpx;
		font-weight: 600;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.remain {
		flex-shrink: 0;
		font-size: 22rpx;
		color: #8A8A86;
		background: #F5F4EF;
		padding: 6rpx 14rpx;
		border-radius: 999rpx;
	}
	.swap-route {
		display: flex;
		align-items: center;
		gap: 12rpx;
		margin-top: 16rpx;
	}
	.route-seat {
		flex-shrink: 0;
		font-size: 30rpx;
		font-weight: 700;
		color: #33332E;
	}
	.route-arrow {
		flex-shrink: 0;
		font-size: 26rpx;
		font-weight: 700;
		color: var(--primary);
	}
	.route-want {
		flex: 1;
		min-width: 0;
		font-size: 26rpx;
		color: #55554F;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
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

	/* 当前图书馆栏 */
	.venue-bar { display: flex; align-items: center; justify-content: space-between; margin: 20rpx; padding: 22rpx 26rpx; background: #FFFFFF; border-radius: 20rpx; box-shadow: 0 4rpx 16rpx rgba(0,0,0,0.04); }
	.vb-left { display: flex; align-items: baseline; gap: 14rpx; min-width: 0; }
	.vb-label { font-size: 22rpx; color: #B0B0AB; }
	.vb-name { font-size: 32rpx; font-weight: 700; color: #2B2B27; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 320rpx; }
	.vb-switch { font-size: 24rpx; color: var(--primary); }
	.vb-post { font-size: 26rpx; color: var(--primary); flex-shrink: 0; }
	.vk-mask { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 999; display: flex; align-items: flex-end; }
	.vk-sheet { width: 100%; background: #FFFFFF; border-radius: 24rpx 24rpx 0 0; padding: 30rpx; }
	.vk-title { display: block; font-size: 32rpx; font-weight: 700; margin-bottom: 20rpx; }
	.vk-list { max-height: 60vh; }
	.vk-item { padding: 22rpx 0; border-bottom: 1rpx solid #F0EFEA; }
	.vk-item.on .vk-name { color: var(--primary); }
	.vk-name { display: block; font-size: 30rpx; font-weight: 600; }
	.vk-meta { display: block; font-size: 22rpx; color: #8A8A86; margin-top: 4rpx; }
</style>
