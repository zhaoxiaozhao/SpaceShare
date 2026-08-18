<template>
	<view>
		<view class="card summary-card">
			<text class="summary-num">{{earnedCount}} / {{achievements.length}}</text>
			<text class="summary-label">已获得成就</text>
		</view>

		<view class="section">
			<text class="section-title">全部成就</text>
			<view class="card achievement" v-for="a in achievements" :key="a.code" :class="{ locked: !a.earned }">
				<text class="ach-icon">{{a.earned ? a.icon : '🔒'}}</text>
				<view class="ach-info">
					<text class="ach-title">{{a.title}}</text>
					<text class="ach-desc">{{a.description}}</text>
				</view>
				<text class="ach-status">{{a.earned ? (formatDate(a.earnedAt)) : '未获得'}}</text>
			</view>
		</view>

		<view v-if="!achievements.length" class="empty">加载中…</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				achievements: []
			}
		},
		computed: {
			earnedCount() {
				return this.achievements.filter(a => a.earned).length
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				try {
					this.achievements = await api.getStudyAchievements()
				} catch (e) {}
			},
			formatDate(s) {
				if (!s) return ''
				const d = new Date(s)
				return `${d.getMonth() + 1}.${d.getDate()}`
			}
		}
	}
</script>

<style scoped>
	.summary-card {
		background: linear-gradient(160deg, #3A8A7E, #5BA48D);
		color: #FFFFFF;
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 50rpx 30rpx;
		gap: 8rpx;
	}
	.summary-num {
		font-size: 64rpx;
		font-weight: 700;
	}
	.summary-label {
		font-size: 26rpx;
		opacity: 0.9;
	}
	.achievement {
		display: flex;
		align-items: center;
		gap: 24rpx;
		padding: 28rpx;
	}
	.achievement.locked {
		opacity: 0.55;
	}
	.ach-icon {
		font-size: 48rpx;
	}
	.ach-info {
		flex: 1;
	}
	.ach-title {
		font-size: 30rpx;
		font-weight: 600;
		display: block;
		margin-bottom: 6rpx;
	}
	.ach-desc {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.ach-status {
		font-size: 22rpx;
		color: #3A8A7E;
	}
	.achievement.locked .ach-status {
		color: #B0B0AB;
	}
</style>
