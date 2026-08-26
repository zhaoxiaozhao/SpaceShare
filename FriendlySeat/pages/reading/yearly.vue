<template>
	<view>
		<view class="card year-switch">
			<text class="year-btn" v-for="y in yearList" :key="y" :class="{ active: y === year }" @click="switchYear(y)">{{y}}年</text>
		</view>

		<view class="card report-card">
			<text class="report-title">{{year}} 年度阅读报告</text>
			<view class="r-grid">
				<view class="r-item">
					<text class="r-num">{{formatHours(report.totalMinutes)}}</text>
					<text class="r-label">总阅读时长</text>
				</view>
				<view class="r-item">
					<text class="r-num">{{report.readingDays}}</text>
					<text class="r-label">阅读天数</text>
				</view>
				<view class="r-item">
					<text class="r-num">{{report.sessionsCount}}</text>
					<text class="r-label">阅读次数</text>
				</view>
				<view class="r-item">
					<text class="r-num">{{report.booksFinished}}</text>
					<text class="r-label">读完书籍</text>
				</view>
				<view class="r-item">
					<text class="r-num">{{report.longestStreak}}</text>
					<text class="r-label">最长连续(天)</text>
				</view>
			</view>
		</view>

		<view class="card">
			<text class="section-label">月度阅读分布</text>
			<view class="bar-chart">
				<view class="bar-col" v-for="m in monthly" :key="m.date">
					<view class="bar" :style="{ height: barHeight(m.minutes) + '%' }"></view>
					<text class="bar-label">{{m.date.slice(5)}}</text>
				</view>
			</view>
		</view>

		<view class="card">
			<text class="section-label">每日阅读(分钟)</text>
			<view v-if="report.dailyMinutes.length" class="daily-list">
				<view class="daily-item" v-for="d in report.dailyMinutes" :key="d.key">
					<text class="d-date">{{d.key}}</text>
					<text class="d-min">{{d.value}}m</text>
				</view>
			</view>
			<text v-else class="empty">今年还没有阅读记录</text>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				year: new Date().getFullYear(),
				report: { totalMinutes: 0, readingDays: 0, sessionsCount: 0, booksFinished: 0, longestStreak: 0, monthlyMinutes: [], dailyMinutes: [] }
			}
		},
		computed: {
			yearList() {
				const y = new Date().getFullYear()
				return [y - 1, y, y + 1]
			},
			monthly() {
				// 补全 12 个月
				const months = []
				for (let m = 1; m <= 12; m++) {
					const key = `${this.year}-${String(m).padStart(2, '0')}`
					const found = this.report.monthlyMinutes.find(x => x.date === key)
					months.push({ date: key, minutes: found ? found.minutes : 0 })
				}
				return months
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				try {
					this.report = await api.getReadingYearlyReport(this.year)
				} catch (e) {}
			},
			switchYear(y) {
				this.year = y
				this.load()
			},
			formatHours(min) {
				if (min >= 60) return `${Math.floor(min / 60)}h${min % 60 ? (min % 60) + 'm' : ''}`
				return `${min}m`
			},
			barHeight(min) {
				const max = Math.max(...this.monthly.map(m => m.minutes), 1)
				return Math.round((min / max) * 100)
			}
		}
	}
</script>

<style scoped>
	.year-switch { display: flex; gap: 16rpx; }
	.year-btn { padding: 10rpx 30rpx; border-radius: 24rpx; font-size: 26rpx; background: #F1EFE9; color: #55554F; }
	.year-btn.active { background: #3A8A7E; color: #FFFFFF; }
	.report-card { background: #3A8A7E; }
	.report-title { display: block; color: #FFFFFF; font-size: 32rpx; font-weight: 700; text-align: center; margin-bottom: 24rpx; }
	.r-grid { display: flex; flex-wrap: wrap; gap: 20rpx; }
	.r-item { flex: 1; min-width: 30%; display: flex; flex-direction: column; align-items: center; gap: 6rpx; }
	.r-num { color: #FFFFFF; font-size: 40rpx; font-weight: 700; }
	.r-label { color: rgba(255,255,255,0.85); font-size: 22rpx; }
	.bar-chart { display: flex; align-items: flex-end; gap: 8rpx; height: 240rpx; padding: 10rpx 0; }
	.bar-col { flex: 1; display: flex; flex-direction: column; align-items: center; gap: 6rpx; height: 100%; justify-content: flex-end; }
	.bar { width: 100%; max-width: 36rpx; background: #3A8A7E; border-radius: 4rpx 4rpx 0 0; }
	.bar-label { font-size: 18rpx; color: #8A8A86; }
	.daily-list { max-height: 400rpx; overflow: auto; }
	.daily-item { display: flex; justify-content: space-between; padding: 10rpx 0; border-bottom: 1rpx solid #F0EFEA; }
	.d-date { font-size: 26rpx; color: #55554F; }
	.d-min { font-size: 26rpx; color: #3A8A7E; }
</style>