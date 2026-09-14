<template>
	<page-meta :page-style="pageThemeStyle" />
	<view class="page">
		<view v-if="list.length">
			<view class="card share-card" v-for="s in list" :key="s.id" @click="goSeat(s.seatId)">
				<view class="share-top">
					<text class="share-seat">{{s.displayCode || s.seatCode}}</text>
					<text class="tag" :class="shareTagClass(s.status)">{{statusText(s.status)}}</text>
				</view>
				<text class="share-venue">{{s.venueName}}<text v-if="s.floorName" class="share-floor"> · {{s.floorName}}</text><text v-if="s.areaName" class="share-floor"> · {{s.areaName}}</text></text>
				<view class="share-time">预计释放：{{formatTime(s.endAt)}}</view>
				<view class="share-note" v-if="s.note">{{s.note}}</view>
			</view>
		</view>
		<view v-else class="empty">暂无分享的座位</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime, statusText } from '../../utils/format.js'

	export default {
		data() {
			return { list: [] }
		},
		onLoad() {
			this.load()
		},
		onPullDownRefresh() {
			this.load().then(() => uni.stopPullDownRefresh())
		},
		methods: {
			formatTime,
			statusText,
			async load() {
				try {
					this.list = await api.getRecentShares(50)
				} catch (e) {}
			},
			shareTagClass(status) {
				const map = {
					Available: 'status-available',
					Reserved: 'status-reserved',
					Active: 'status-active',
					Completed: 'status-completed'
				}
				return map[status] || 'status-completed'
			},
			goSeat(id) {
				uni.navigateTo({ url: `/pages/seat/seat?id=${id}` })
			}
		}
	}
</script>

<style scoped>
	.page {
		padding: 24rpx;
	}
	.share-card {
		margin-bottom: 20rpx;
	}
	.share-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.share-seat {
		font-size: 32rpx;
		font-weight: 700;
	}
	.share-venue {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin-top: 8rpx;
	}
	.share-floor {
		color: var(--primary);
	}
	.share-time {
		display: block;
		font-size: 24rpx;
		color: #55554F;
		margin-top: 8rpx;
	}
	.share-note {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin-top: 6rpx;
	}
	.tag {
		font-size: 22rpx;
		padding: 4rpx 14rpx;
		border-radius: 8rpx;
	}
	.status-available { color: var(--primary); background: var(--primary-bg, #EAF3F1); }
	.status-reserved { color: #C78A2B; background: #FBF3E3; }
	.status-active { color: #2E8B94; background: #E8F2F3; }
	.status-completed { color: #B0AEA8; background: #F1F0EB; }
	.empty {
		text-align: center;
		color: #B0AEA8;
		font-size: 26rpx;
		padding: 120rpx 0;
	}
</style>
