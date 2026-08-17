<template>
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
			<view class="seat-features">
				<text class="feature" v-if="seat.window">🪟 靠窗</text>
				<text class="feature" v-if="seat.powerSocket">🔌 插座</text>
				<text class="feature" v-if="seat.quietLevel">🤫 安静</text>
				<text class="feature" v-if="seat.lightLevel === 3">💡 明亮</text>
			</view>
			<text class="seat-desc" v-if="seat.description">{{seat.description}}</text>
		</view>

		<!-- ============ 有分享：预约者视角 ============ -->
		<view v-if="shares.length" class="section">
			<text class="section-title">分享者预计释放时间</text>
			<view class="card share-card" v-for="s in shares" :key="s.id">
				<view class="share-row">
					<view class="share-info">
						<text class="share-time">预计释放：{{formatTime(s.endAt)}}</text>
						<text class="share-owner" v-if="s.ownerNickname">分享者：{{s.ownerNickname}}</text>
						<text class="share-note" v-if="s.note">{{s.note}}</text>
					</view>
					<view class="share-actions">
						<button class="btn-primary small" @click="reserve(s)">预约</button>
					</view>
				</view>
			</view>

			<!-- 有分享才需要举报（针对虚假分享） -->
			<view class="card" style="margin-top:16rpx;">
				<text class="share-note">发现这个分享是虚假的？</text>
				<button class="btn-outline" style="margin-top:16rpx;" @click="report(s)">举报该分享</button>
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
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime, statusText } from '../../utils/format.js'

	export default {
		data() {
			return {
				id: null,
				seat: null,
				shares: [],
				mySession: null,
				loading: false
			}
		},
		onLoad(options) {
			this.id = options.id
		},
		onShow() {
			this.load()
		},
		methods: {
			formatTime,
			statusText,
			async load() {
				try {
					this.seat = await api.getSeat(this.id)
					// 座位状态语义：未知（默认）/ 已预约 / 不可用
					this.seat.statusText = {
						Available: '未知',
						Occupied: '已预约',
						Unavailable: '不可用'
					}[this.seat.status] || '未知'
					this.shares = await api.getShares(this.id)
					const token = uni.getStorageSync('token')
					if (token) {
						try {
							this.mySession = await api.getMySession()
						} catch (e) {}
					}
				} catch (e) {
					uni.showToast({ title: '加载失败', icon: 'none' })
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
								const r = await api.createReservation(share.id)
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
		color: #3A8A7E;
		background: #EAF3F0;
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
</style>
