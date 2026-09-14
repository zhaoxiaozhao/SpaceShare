<template>
	<page-meta :page-style="pageThemeStyle" />
	<view class="page">
		<view v-if="list.length">
			<view class="card swap-card" v-for="s in list" :key="s.id" @click="goSeat(s)">
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
		<view v-else class="empty">暂无换座需求</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { getAppOptions } from '../../utils/options.js'

	export default {
		data() {
			return {
				list: []
			}
		},
		computed: {
			reasonOptions() {
				return getAppOptions().swapReasons
			}
		},
		onLoad() {
			this.load()
		},
		onPullDownRefresh() {
			this.load().then(() => uni.stopPullDownRefresh())
		},
		methods: {
			async load() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				try {
					this.list = await api.getRecentSwaps(50)
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
			goSeat(s) {
				uni.navigateTo({ url: `/pages/seat/seat?id=${s.seatId}&venueId=${s.venueId}` })
			}
		}
	}
</script>

<style scoped>
	.page {
		padding: 24rpx;
	}
	.swap-card {
		margin-bottom: 20rpx;
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
	.empty {
		text-align: center;
		color: #B0AEA8;
		font-size: 26rpx;
		padding: 120rpx 0;
	}
</style>
