<template>
	<view>
		<view class="hero">
			<text class="hero-title">友邻座</text>
			<text class="hero-slogan">一席相邻，善意相续</text>
			<text class="hero-desc">让公共空间里每一个空闲座位，继续被需要</text>
		</view>

		<view class="quick-actions">
			<view class="action-btn" @click="goFindSeat">
				<text class="action-icon">🔍</text>
				<text>找座位</text>
			</view>
			<view class="action-btn" @click="goStudy">
				<text class="action-icon">📖</text>
				<text>开始学习</text>
			</view>
			<view class="action-btn" @click="goReservations">
				<text class="action-icon">📅</text>
				<text>我的预约</text>
			</view>
		</view>

		<view v-if="nearby.length" class="section">
			<text class="section-title">附近场馆</text>
			<view class="card venue-card" v-for="v in nearby" :key="v.id" @click="goVenue(v.id)">
				<view class="venue-main">
					<text class="venue-name">{{v.name}}</text>
					<text class="venue-addr">{{v.address}}</text>
				</view>
				<view class="venue-meta">
					<text class="venue-available" v-if="v.availableCount > 0">可预约 {{v.availableCount}}</text>
					<text class="venue-available none" v-else>暂无分享</text>
					<text class="venue-distance" v-if="v.distanceKm">{{v.distanceKm}}km</text>
				</view>
			</view>
		</view>

		<view v-if="shares.length" class="section">
			<text class="section-title">最近分享的座位</text>
			<view class="card share-card" v-for="s in shares" :key="s.id" @click="goSeat(s.seatId)">
				<view class="share-top">
					<text class="share-seat">{{s.seatCode}}</text>
					<text class="share-venue">{{s.venueName}}</text>
				</view>
				<view class="share-time">{{formatTime(s.startAt)}} ~ {{formatTime(s.endAt)}}</view>
				<view class="share-note" v-if="s.note">{{s.note}}</view>
			</view>
		</view>

		<view v-if="!nearby.length && !shares.length" class="empty">
			<text>正在加载附近的场馆与共享座位…</text>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'

	export default {
		data() {
			return {
				nearby: [],
				shares: [],
				venueShares: []
			}
		},
		onShow() {
			this.loadData()
		},
		onPullDownRefresh() {
			this.loadData().then(() => uni.stopPullDownRefresh())
		},
		methods: {
			formatTime,
			async loadData() {
				try {
					const location = await this.getLocation()
					this.nearby = await api.getVenues({
						lat: location.latitude,
						lng: location.longitude,
						radiusKm: 20
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
				uni.switchTab({ url: '/pages/reservations/reservations' })
				setTimeout(() => {
					uni.navigateTo({ url: '/pages/venues/venues' })
				}, 300)
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
	.hero {
		padding: 60rpx 40rpx 40rpx;
		background: linear-gradient(160deg, #3A8A7E 0%, #5BA48D 60%, #F7F5EF 100%);
		display: flex;
		flex-direction: column;
	}
	.hero-title {
		color: #FFFFFF;
		font-size: 56rpx;
		font-weight: 700;
		letter-spacing: 8rpx;
	}
	.hero-slogan {
		color: #FFFFFF;
		font-size: 32rpx;
		margin-top: 12rpx;
		opacity: 0.95;
	}
	.hero-desc {
		color: #FFFFFF;
		font-size: 24rpx;
		margin-top: 16rpx;
		opacity: 0.8;
	}
	.quick-actions {
		display: flex;
		margin: -30rpx 20rpx 0;
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
		font-size: 40rpx;
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
	.venue-meta {
		display: flex;
		flex-direction: column;
		align-items: flex-end;
		gap: 8rpx;
	}
	.venue-available {
		font-size: 24rpx;
		color: #3A8A7E;
	}
	.venue-available.none {
		color: #B85450;
	}
	.venue-distance {
		font-size: 22rpx;
		color: #8A8A86;
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
		color: #3A8A7E;
	}
	.share-venue {
		font-size: 24rpx;
		color: #8A8A86;
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
