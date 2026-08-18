<template>
	<view>
		<view class="report-header">
			<view class="period-tabs">
				<view class="period-tab" :class="{ active: period === 'weekly' }" @click="switchPeriod('weekly')">周报</view>
				<view class="period-tab" :class="{ active: period === 'monthly' }" @click="switchPeriod('monthly')">月报</view>
			</view>
		</view>

		<view class="card hero-card" v-if="report">
			<text class="hero-num">{{formatHours(report.totalMinutes)}}</text>
			<text class="hero-label">共学习</text>
			<text class="hero-period">{{report.start.slice(5)}. ~ {{report.end.slice(5)}}.</text>
		</view>

		<view class="stats-grid" v-if="report">
			<view class="stat-cell">
				<text class="stat-num">{{report.studyDays}}</text>
				<text class="stat-label">学习天数</text>
			</view>
			<view class="stat-cell">
				<text class="stat-num">{{report.sessionCount}}</text>
				<text class="stat-label">学习次数</text>
			</view>
			<view class="stat-cell">
				<text class="stat-num">{{report.longestStreak}}</text>
				<text class="stat-label">最长连续</text>
			</view>
			<view class="stat-cell">
				<text class="stat-num">{{formatShortHours(report.maxDailyMinutes)}}</text>
				<text class="stat-label">单日最高</text>
			</view>
		</view>

		<view class="card" v-if="report && report.dailyMinutes.length">
			<text class="section-title">每日学习时长</text>
			<view class="bar-chart">
				<view class="bar-col" v-for="(d, i) in report.dailyMinutes" :key="i">
					<view class="bar" :style="{ height: barHeight(d.value) }"></view>
					<text class="bar-date">{{d.key}}</text>
				</view>
			</view>
		</view>

		<view class="card" v-if="report && report.typeDistribution.length">
			<text class="section-title">学习类型分布</text>
			<view class="type-row" v-for="t in report.typeDistribution" :key="t.key">
				<text class="type-name">{{typeLabel(t.key)}}</text>
				<text class="type-min">{{formatMinutes(t.value)}}</text>
			</view>
		</view>

		<view v-if="!report" class="empty">加载中…</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	const TYPE_LABELS = {
		Reading: '阅读', Programming: '编程', English: '英语', Exam: '考研',
		Postgraduate: '考公', Papers: '论文', Other: '其他'
	}

	export default {
		data() {
			return {
				period: 'weekly',
				report: null
			}
		},
		onLoad(options) {
			if (options.period === 'monthly') this.period = 'monthly'
			this.load()
		},
		methods: {
			async load() {
				try {
					this.report = await api.getStudyReport(this.period)
				} catch (e) {}
			},
			switchPeriod(p) {
				this.period = p
				this.report = null
				this.load()
			},
			barHeight(value) {
				const max = Math.max(...this.report.dailyMinutes.map(d => d.value), 1)
				return Math.max(4, Math.round(value / max * 120)) + 'rpx'
			},
			typeLabel(v) {
				return TYPE_LABELS[v] || v
			},
			formatHours(min) {
				const h = Math.floor(min / 60)
				return h > 0 ? `${h} 小时` : '0'
			},
			formatShortHours(min) {
				if (min >= 60) return (min / 60).toFixed(1).replace('.0', '') + 'h'
				return min + 'm'
			},
			formatMinutes(min) {
				if (min >= 60) {
					const h = Math.floor(min / 60)
					const m = min % 60
					return m ? `${h}小时${m}分` : `${h}小时`
				}
				return `${min}分钟`
			}
		}
	}
</script>

<style scoped>
	.report-header {
		margin: 20rpx;
	}
	.period-tabs {
		display: flex;
		background: #EFEEE9;
		border-radius: 40rpx;
		padding: 6rpx;
	}
	.period-tab {
		flex: 1;
		text-align: center;
		padding: 14rpx 0;
		border-radius: 34rpx;
		font-size: 28rpx;
		color: #8A8A86;
	}
	.period-tab.active {
		background: #FFFFFF;
		color: #3A8A7E;
		font-weight: 600;
		box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);
	}
	.hero-card {
		background: linear-gradient(160deg, #3A8A7E, #5BA48D);
		color: #FFFFFF;
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 50rpx 30rpx;
		gap: 6rpx;
	}
	.hero-num {
		font-size: 72rpx;
		font-weight: 700;
	}
	.hero-label {
		font-size: 26rpx;
		opacity: 0.9;
	}
	.hero-period {
		font-size: 22rpx;
		opacity: 0.75;
	}
	.stats-grid {
		display: flex;
		margin: 20rpx;
		gap: 20rpx;
	}
	.stat-cell {
		flex: 1;
		background: #FFFFFF;
		border-radius: 20rpx;
		padding: 24rpx 0;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 6rpx;
		box-shadow: 0 4rpx 16rpx rgba(0, 0, 0, 0.04);
	}
	.stat-num {
		font-size: 40rpx;
		font-weight: 700;
		color: #3A8A7E;
	}
	.stat-label {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.bar-chart {
		display: flex;
		align-items: flex-end;
		gap: 8rpx;
		height: 180rpx;
		margin-top: 20rpx;
	}
	.bar-col {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		height: 100%;
		justify-content: flex-end;
	}
	.bar {
		width: 100%;
		background: linear-gradient(180deg, #5BA48D, #3A8A7E);
		border-radius: 6rpx 6rpx 0 0;
		min-height: 4rpx;
	}
	.bar-date {
		font-size: 18rpx;
		color: #B0B0AB;
		margin-top: 8rpx;
	}
	.type-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 16rpx 0;
		border-bottom: 1rpx solid #F0EFEA;
	}
	.type-row:last-child {
		border-bottom: none;
	}
	.type-name {
		font-size: 28rpx;
	}
	.type-min {
		font-size: 26rpx;
		color: #3A8A7E;
		font-weight: 600;
	}
</style>
