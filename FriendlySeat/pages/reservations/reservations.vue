<template>
	<page-meta :page-style="pageThemeStyle" />
		<view>
		<view class="tabs">
			<view class="tab" :class="{ active: tab === 'upcoming' }" @click="tab = 'upcoming'">待使用</view>
			<view class="tab" :class="{ active: tab === 'shares' }" @click="tab = 'shares'">我的分享</view>
			<view class="tab" :class="{ active: tab === 'waitlist' }" @click="tab = 'waitlist'">我的候补</view>
			<view class="tab" :class="{ active: tab === 'history' }" @click="tab = 'history'">历史</view>
		</view>

		<view v-if="tab === 'upcoming'">
			<view v-if="summary.upcoming.length">
				<view class="card res-card" v-for="r in summary.upcoming" :key="r.id">
					<view class="res-top">
						<text class="res-seat">{{r.displayCode || r.seatCode}}</text>
						<text class="tag" :class="'status-' + r.status.toLowerCase()">{{statusText(r.status)}}</text>
					</view>
					<text class="res-venue">{{r.venueName}}<text v-if="r.floorName" class="res-floor"> · {{r.floorName}}</text><text v-if="r.areaName" class="res-floor"> · {{r.areaName}}</text></text>
					<text class="res-time">{{formatTime(r.startAt)}} ~ {{formatTime(r.endAt)}}</text>
					<view class="res-actions">
						<button
							v-if="r.status === 'Reserved'"
							class="btn-outline small"
							@click="cancel(r)"
						>取消预约</button>
						<button
							v-if="r.status === 'Reserved'"
							class="btn-primary small"
							@click="arrive(r)"
						>确认到座</button>
						<button
							v-if="r.status === 'Arrived'"
							class="btn-primary small"
							@click="complete(r)"
						>结束使用</button>
					</view>
				</view>
			</view>
			<view v-else class="empty">暂无待使用预约</view>
		</view>

		<view v-if="tab === 'shares'">
			<view v-if="summary.myShares.length">
				<view class="card res-card" v-for="s in summary.myShares" :key="s.id">
					<view class="res-top">
						<text class="res-seat">{{s.displayCode || s.seatCode}}</text>
						<text class="tag" :class="'status-' + s.status.toLowerCase()">{{statusText(s.status)}}</text>
					</view>
					<text class="res-venue">{{s.venueName}}<text v-if="s.floorName" class="res-floor"> · {{s.floorName}}</text><text v-if="s.areaName" class="res-floor"> · {{s.areaName}}</text></text>
					<text class="res-time">{{formatTime(s.startAt)}} ~ {{formatTime(s.endAt)}}</text>
					<view class="res-actions">
						<button
							v-if="s.status === 'Available'"
							class="btn-outline small"
							@click="cancelShare(s)"
						>取消分享</button>
					</view>
				</view>
			</view>
			<view v-else class="empty">还没有分享过座位</view>
		</view>

		<view v-if="tab === 'waitlist'">
			<view v-if="waitlist.length">
				<view class="card res-card" v-for="w in waitlist" :key="w.id" @click="goSeat(w)">
					<view class="res-top">
						<text class="res-seat">{{w.seatCode}}</text>
						<text class="tag" :class="'status-' + w.status.toLowerCase()">{{statusText(w.status)}}</text>
					</view>
					<text class="res-venue">{{w.venueName}}</text>
					<text class="res-time">{{formatTime(w.startAt)}} ~ {{formatTime(w.endAt)}}</text>
					<view class="res-actions">
						<button
							v-if="w.status === 'Waiting' || w.status === 'Notified'"
							class="btn-outline small"
							@click.stop="cancelWaitlist(w)"
						>取消候补</button>
					</view>
				</view>
			</view>
			<view v-else class="empty">暂无候补</view>
		</view>

		<view v-if="tab === 'history'">
			<view v-if="summary.history.length">
				<view class="card res-card" v-for="r in summary.history" :key="r.id">
					<view class="res-top">
						<text class="res-seat">{{r.displayCode || r.seatCode}}</text>
						<text class="tag" :class="'status-' + r.status.toLowerCase()">{{statusText(r.status)}}</text>
					</view>
					<text class="res-venue">{{r.venueName}}<text v-if="r.floorName" class="res-floor"> · {{r.floorName}}</text><text v-if="r.areaName" class="res-floor"> · {{r.areaName}}</text></text>
					<text class="res-time">{{formatTime(r.startAt)}} ~ {{formatTime(r.endAt)}}</text>
				</view>
			</view>
			<view v-else class="empty">暂无历史记录</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime, statusText } from '../../utils/format.js'

	export default {
		data() {
			return {
				tab: 'upcoming',
				summary: { upcoming: [], history: [], myShares: [] },
				waitlist: []
			}
		},
		onShow() {
			this.load()
		},
		onPullDownRefresh() {
			this.load().then(() => uni.stopPullDownRefresh())
		},
		methods: {
			formatTime,
			statusText,
			async load() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				try {
					this.summary = await api.getMyReservations()
				} catch (e) {
					uni.showToast({ title: '加载失败', icon: 'none' })
				}
				try {
					this.waitlist = await api.getMyWaitlist()
				} catch (e) {}
			},
			async cancel(r) {
				uni.showModal({
					title: '取消预约',
					content: '确定取消这个预约吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.cancelReservation(r.id)
							uni.showToast({ title: '已取消', icon: 'success' })
							this.load()
						} catch (e) {
							uni.showToast({ title: e.message || '取消失败', icon: 'none' })
						}
					}
				})
			},
			arrive(r) {
				uni.getLocation({
					type: 'gcj02',
					success: (loc) => {
						this.doArrive(r, loc.latitude, loc.longitude)
					},
					fail: () => {
						this.doArrive(r, 31.22, 121.528)
					}
				})
			},
			async doArrive(r, lat, lng) {
				uni.showLoading({ title: '确认中', mask: true })
				try {
					const res = await api.arrive(r.id, lat, lng)
					uni.hideLoading()
					uni.showToast({ title: res.message || '已到座', icon: 'success' })
					this.load()
				} catch (e) {
					uni.hideLoading()
					uni.showToast({ title: e.message || '确认失败', icon: 'none' })
				}
			},
			async complete(r) {
				try {
					await api.complete(r.id)
					uni.showToast({ title: '已结束', icon: 'success' })
					uni.showModal({
						title: '分享这一席',
						content: '感谢你守约。要把接下来的空闲时间留给下一位友邻吗？',
						confirmText: '分享',
						cancelText: '不用了',
						success: (res) => {
							if (res.confirm) {
								uni.navigateTo({ url: `/pages/share/share?seatId=${r.seatId}` })
							} else {
								this.load()
							}
						}
					})
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			},
			async cancelShare(s) {
				uni.showModal({
					title: '取消分享',
					content: '确定取消这条分享吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.cancelShare(s.id)
							uni.showToast({ title: '已取消', icon: 'success' })
							this.load()
						} catch (e) {
							uni.showToast({ title: e.message || '操作失败', icon: 'none' })
						}
					}
				})
			},
			async cancelWaitlist(w) {
				uni.showModal({
					title: '取消候补',
					content: '确定取消这条候补吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.cancelWaitlist(w.id)
							uni.showToast({ title: '已取消', icon: 'success' })
							this.load()
						} catch (e) {
							uni.showToast({ title: e.message || '操作失败', icon: 'none' })
						}
					}
				})
			},
			goSeat(w) {
				uni.navigateTo({ url: `/pages/seat/seat?id=${w.seatId}` })
			},
		}
	}
</script>

<style scoped>
	.tabs {
		display: flex;
		background: #FFFFFF;
		padding: 10rpx;
		border-radius: 20rpx;
		margin: 20rpx;
	}
	.tab {
		flex: 1;
		text-align: center;
		padding: 16rpx 0;
		font-size: 28rpx;
		color: #55554F;
		border-radius: 14rpx;
	}
	.tab.active {
		background: var(--primary);
		color: #FFFFFF;
		font-weight: 600;
	}
	.res-card {
		display: flex;
		flex-direction: column;
		gap: 10rpx;
	}
	.res-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.res-seat {
		font-size: 32rpx;
		font-weight: 600;
	}
	.res-venue {
		font-size: 26rpx;
		color: #55554F;
	}
	.res-floor {
		color: var(--primary);
	}
	.res-time {
		font-size: 26rpx;
		color: #8A8A86;
	}
	.res-actions {
		display: flex;
		gap: 16rpx;
		margin-top: 12rpx;
	}
	.btn-primary.small, .btn-outline.small {
		font-size: 26rpx;
		line-height: 2;
		padding: 0 24rpx;
		margin: 0;
	}
</style>
