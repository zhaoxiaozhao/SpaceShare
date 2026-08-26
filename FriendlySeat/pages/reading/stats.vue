<template>
	<view>
		<view class="card stat-summary">
			<view class="s-item">
				<text class="s-num">{{stats.totalMinutes}}</text>
				<text class="s-label">累计阅读(分钟)</text>
			</view>
			<view class="s-item">
				<text class="s-num">{{stats.consecutiveDays}}</text>
				<text class="s-label">连续阅读(天)</text>
			</view>
			<view class="s-item">
				<text class="s-num">{{stats.finishedBooks}}</text>
				<text class="s-label">读完书籍</text>
			</view>
		</view>

		<view class="card">
			<text class="section-label">{{year}}年阅读日历</text>
			<view class="heatmap">
				<view
					class="hm-cell"
					v-for="(d, i) in calendarDays"
					:key="i"
					:style="{ background: heatColor(d.minutes) }"
				>
					<text class="hm-date" v-if="d.minutes > 0">{{d.minutes}}</text>
				</view>
			</view>
			<view class="heat-legend">
				<text class="hl-item">少</text>
				<view class="hl-box" style="background:#F1EFE9"></view>
				<view class="hl-box" style="background:#D8E8E4"></view>
				<view class="hl-box" style="background:#9CC5BC"></view>
				<view class="hl-box" style="background:#3A8A7E"></view>
				<text class="hl-item">多</text>
			</view>
			<text class="heat-note">有阅读的 {{readingDays}} 天 · 本月 {{currentMonthMinutes}} 分钟</text>
		</view>

		<view class="card">
			<text class="section-label">选择年份</text>
			<view class="year-row">
				<text class="year-btn" v-for="y in yearList" :key="y" :class="{ active: y === year }" @click="switchYear(y)">{{y}}</text>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				stats: { totalMinutes: 0, consecutiveDays: 0, finishedBooks: 0, todayMinutes: 0 },
				calendar: [],
				year: new Date().getFullYear(),
				readingDays: 0,
				currentMonthMinutes: 0
			}
		},
		computed: {
			yearList() {
				const y = new Date().getFullYear()
				return [y - 1, y, y + 1]
			},
			calendarDays() {
				// 按日期填充一年 366 格（约每行一周）
				const days = []
				const start = new Date(this.year, 0, 1)
				const dayMap = {}
				this.calendar.forEach(c => { dayMap[c.date] = c.minutes })
				for (let i = 0; i < 366; i++) {
					const d = new Date(start)
					d.setDate(start.getDate() + i)
					if (d.getFullYear() !== this.year) break
					const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
					days.push({ minutes: dayMap[key] || 0 })
				}
				return days
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				try {
					const [s, c] = await Promise.all([
						api.getReadingStats(),
						api.getReadingCalendar(this.year)
					])
					this.stats = s
					this.calendar = c
					this.readingDays = c.length
					const now = new Date()
					const monthKey = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
					this.currentMonthMinutes = c.filter(d => d.date.startsWith(monthKey)).reduce((sum, d) => sum + d.minutes, 0)
				} catch (e) {}
			},
			switchYear(y) {
				this.year = y
				this.load()
			},
			heatColor(min) {
				if (min <= 0) return '#F1EFE9'
				if (min < 30) return '#D8E8E4'
				if (min < 60) return '#9CC5BC'
				return '#3A8A7E'
			}
		}
	}
</script>

<style scoped>
	.stat-summary { display: flex; }
	.s-item { flex: 1; display: flex; flex-direction: column; align-items: center; gap: 6rpx; }
	.s-num { font-size: 44rpx; font-weight: 700; color: #3A8A7E; }
	.s-label { font-size: 22rpx; color: #8A8A86; }
	.heatmap { display: flex; flex-wrap: wrap; gap: 6rpx; margin: 20rpx 0; }
	.hm-cell { width: 30rpx; height: 30rpx; border-radius: 4rpx; display: flex; align-items: center; justify-content: center; font-size: 16rpx; color: #FFFFFF; }
	.hm-date { font-size: 14rpx; }
	.heat-legend { display: flex; align-items: center; gap: 8rpx; margin-bottom: 10rpx; }
	.hl-item { font-size: 20rpx; color: #B0B0AB; }
	.hl-box { width: 24rpx; height: 24rpx; border-radius: 4rpx; }
	.heat-note { display: block; font-size: 22rpx; color: #8A8A86; }
	.year-row { display: flex; gap: 16rpx; }
	.year-btn { padding: 10rpx 30rpx; border-radius: 24rpx; font-size: 26rpx; background: #F1EFE9; color: #55554F; }
	.year-btn.active { background: #3A8A7E; color: #FFFFFF; }
</style>