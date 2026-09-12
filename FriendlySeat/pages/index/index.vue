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

		<view v-if="!nearby.length && !shares.length" class="empty">
			<text>正在加载附近的场馆与共享座位…</text>
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

	export default {
		data() {
			return {
				nearby: [],
				shares: [],
				venueShares: [],
				season: getSeasonKey()
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
</style>
