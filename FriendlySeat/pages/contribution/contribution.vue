<template>
	<view>
		<view class="card hero">
			<text class="hero-title">友邻贡献</text>
			<text class="hero-desc">你每一次分享，都在让善意相续</text>
		</view>

		<view class="card stats">
			<view class="stat">
				<text class="stat-num">{{c.shareCount || 0}}</text>
				<text class="stat-label">分享次数</text>
			</view>
			<view class="stat">
				<text class="stat-num">{{c.shareHours || 0}}</text>
				<text class="stat-label">分享小时</text>
			</view>
			<view class="stat">
				<text class="stat-num">{{c.helpedCount || 0}}</text>
				<text class="stat-label">帮助友邻</text>
			</view>
			<view class="stat">
				<text class="stat-num">{{c.onTimeCount || 0}}</text>
				<text class="stat-label">守约次数</text>
			</view>
		</view>

		<view class="card badges">
			<text class="badges-title">公益勋章</text>
			<view class="badge-row">
				<view class="badge" :class="{ locked: c.shareCount < 1 }">
					<text class="badge-icon">🌱</text>
					<text class="badge-name">初识友邻</text>
					<text class="badge-req">首次分享</text>
				</view>
				<view class="badge" :class="{ locked: c.shareCount < 5 }">
					<text class="badge-icon">🤝</text>
					<text class="badge-name">乐于相助</text>
					<text class="badge-req">分享5次</text>
				</view>
				<view class="badge" :class="{ locked: c.shareCount < 20 }">
					<text class="badge-icon">💚</text>
					<text class="badge-name">常予一席</text>
					<text class="badge-req">分享20次</text>
				</view>
				<view class="badge" :class="{ locked: c.helpedCount < 50 }">
					<text class="badge-icon">✨</text>
					<text class="badge-name">善意相续</text>
					<text class="badge-req">帮助50人</text>
				</view>
			</view>
			<text class="badges-note">勋章仅为荣誉，不可兑换座位、金钱或任何预约权益。</text>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				c: {}
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				try {
					this.c = await api.getContribution()
				} catch (e) {}
			}
		}
	}
</script>

<style scoped>
	.hero {
		background: linear-gradient(160deg, #3A8A7E, #5BA48D);
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 60rpx 40rpx;
		gap: 12rpx;
	}
	.hero-title {
		font-size: 40rpx;
		font-weight: 700;
		color: #FFFFFF;
	}
	.hero-desc {
		font-size: 26rpx;
		color: #FFFFFF;
		opacity: 0.9;
	}
	.stats {
		display: flex;
		flex-wrap: wrap;
	}
	.stat {
		width: 50%;
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 20rpx 0;
		gap: 6rpx;
	}
	.stat-num {
		font-size: 44rpx;
		font-weight: 700;
		color: #3A8A7E;
	}
	.stat-label {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.badges-title {
		font-size: 30rpx;
		font-weight: 600;
		display: block;
		margin-bottom: 24rpx;
	}
	.badge-row {
		display: flex;
		justify-content: space-between;
	}
	.badge {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 6rpx;
		width: 24%;
	}
	.badge.locked {
		opacity: 0.35;
	}
	.badge-icon {
		font-size: 52rpx;
	}
	.badge-name {
		font-size: 22rpx;
		color: #33332E;
	}
	.badge-req {
		font-size: 18rpx;
		color: #B0B0AB;
	}
	.badges-note {
		display: block;
		margin-top: 24rpx;
		font-size: 22rpx;
		color: #B0B0AB;
	}
</style>
