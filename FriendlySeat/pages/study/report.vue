<template>
	<page-meta :page-style="pageThemeStyle" />
		<view>
		<view class="report-header">
			<view class="period-tabs">
				<view class="period-tab" :class="{ active: period === 'weekly' }" @click="switchPeriod('weekly')">周报</view>
				<view class="period-tab" :class="{ active: period === 'monthly' }" @click="switchPeriod('monthly')">月报</view>
			</view>
		</view>

		<!-- 分享入口 -->
		<view class="share-bar" v-if="report">
			<button class="btn-primary share-btn" @click="generateShare">生成分享卡片</button>
		</view>

		<view class="card hero-card" v-if="report">
			<text class="hero-num">{{formatHours(report.totalMinutes)}}</text>
			<text class="hero-label">共学习</text>
			<text class="hero-period">{{periodText}}</text>
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

		<!-- 分享卡片预览 -->
		<view class="modal-mask" v-if="showSharePreview" @click="showSharePreview = false">
			<view class="preview-modal" @click.stop>
				<text class="modal-title">学习报告</text>
				<image class="share-image" :src="shareImage" mode="aspectFit" />
				<button class="btn-primary share-to-btn" open-type="share">分享给好友</button>
				<button class="btn-outline save-btn" @click="saveShareImage">保存图片</button>
			</view>
		</view>

		<!-- 隐藏画布：生成分享卡片 -->
		<canvas class="share-canvas" canvas-id="shareCard" :style="{ width: '640px', height: '800px', position: 'fixed', left: '-9999px' }"></canvas>
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
				report: null,
				showSharePreview: false,
				shareImage: ''
			}
		},
		onLoad(options) {
			if (options.period === 'monthly') this.period = 'monthly'
			this.load()
		},
		onShareAppMessage() {
			return {
				title: `我的学习${this.period === 'monthly' ? '月报' : '周报'}：共学习${this.formatHours(this.report ? this.report.totalMinutes : 0)}`,
				path: '/pages/index/index',
				imageUrl: this.shareImage || ''
			}
		},
		onShareTimeline() {
			return {
				title: `我的学习${this.period === 'monthly' ? '月报' : '周报'}：共学习${this.formatHours(this.report ? this.report.totalMinutes : 0)}`,
				...(this.shareImage ? { imageUrl: this.shareImage } : {})
			}
		},
		computed: {
			periodText() {
				if (!this.report) return ''
				return `${this.report.start.slice(5)}. ~ ${this.report.end.slice(5)}.`
			}
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
			},
			async generateShare() {
				if (!this.report) return
				uni.showLoading({ title: '生成中', mask: true })
				try {
					await this.drawCard()
					this.showSharePreview = true
					uni.hideLoading()
				} catch (e) {
					uni.hideLoading()
					uni.showToast({ title: '生成失败，请重试', icon: 'none' })
				}
			},
			drawCard() {
				return new Promise((resolve, reject) => {
					const ctx = uni.createCanvasContext('shareCard', this)
					const W = 640
					const H = 800
					const r = this.report
					const periodName = this.period === 'monthly' ? '月报' : '周报'
					const primary = 'var(--primary)'
					const dark = '#2B2B27'
					const gray = '#8A8A86'

					// 背景
					ctx.setFillStyle('#F7F5EF')
					ctx.fillRect(0, 0, W, H)

					// 顶部渐变区（品牌色）
					ctx.setFillStyle(primary)
					ctx.fillRect(0, 0, W, 320)
					ctx.setFillStyle('rgba(255,255,255,0.15)')
					ctx.beginPath()
					ctx.arc(560, 60, 160, 0, 2 * Math.PI)
					ctx.fill()
					ctx.beginPath()
					ctx.arc(40, 260, 100, 0, 2 * Math.PI)
					ctx.fill()

					// 标题
					ctx.setFillStyle('#FFFFFF')
					ctx.setFontSize(36)
					ctx.setTextAlign('left')
					ctx.fillText('友邻座·学习' + periodName, 40, 90)

					ctx.setFontSize(22)
					ctx.setFillStyle('rgba(255,255,255,0.85)')
					ctx.fillText(this.periodText, 40, 130)

					// 总时长
					ctx.setFontSize(80)
					ctx.setFillStyle('#FFFFFF')
					ctx.fillText(this.formatHours(r.totalMinutes), 40, 230)

					ctx.setFontSize(24)
					ctx.setFillStyle('rgba(255,255,255,0.85)')
					ctx.fillText('总学习时长', 40, 270)

					// 统计卡片
					const stats = [
						{ label: '学习天数', value: String(r.studyDays) },
						{ label: '学习次数', value: String(r.sessionCount) },
						{ label: '最长连续', value: String(r.longestStreak) + '天' }
					]
					const cardW = (W - 40 * 2 - 20 * 2) / 3
					stats.forEach((s, i) => {
						const x = 40 + i * (cardW + 20)
						ctx.setFillStyle('#FFFFFF')
						ctx.fillRect(x, 360, cardW, 120)
						ctx.setFillStyle(primary)
						ctx.setFontSize(36)
						ctx.setTextAlign('center')
						ctx.fillText(s.value, x + cardW / 2, 410)
						ctx.setFillStyle(gray)
						ctx.setFontSize(22)
						ctx.fillText(s.label, x + cardW / 2, 450)
					})

					// 类型分布
					const types = r.typeDistribution.slice(0, 4)
					if (types.length) {
						ctx.setFillStyle(dark)
						ctx.setFontSize(28)
						ctx.setTextAlign('left')
						ctx.fillText('学习类型', 40, 540)

						types.forEach((t, i) => {
							const y = 575 + i * 48
							const total = types.reduce((s, x) => s + x.value, 0) || 1
							const pct = Math.round(t.value / total * 100)

							ctx.setFillStyle(gray)
							ctx.setFontSize(24)
							ctx.fillText(this.typeLabel(t.key), 40, y + 6)

							ctx.setFillStyle(primary)
							ctx.setFontSize(24)
							ctx.setTextAlign('right')
							ctx.fillText(pct + '%', W - 40, y + 6)
							ctx.setTextAlign('left')

							// 进度条
							ctx.setFillStyle('var(--primary-bg)')
							ctx.fillRect(40, y + 20, W - 80, 10)
							ctx.setFillStyle(primary)
							ctx.fillRect(40, y + 20, Math.max(4, (W - 80) * pct / 100), 10)
						})
					}

					// 底部
					ctx.setFillStyle(gray)
					ctx.setFontSize(22)
					ctx.setTextAlign('center')
					ctx.fillText('一席相邻，善意相续 · 友邻座', W / 2, 760)

					ctx.draw(false, () => {
						setTimeout(() => {
							uni.canvasToTempFilePath({
								canvasId: 'shareCard',
								width: W,
								height: H,
								destWidth: W * 2,
								destHeight: H * 2,
								success: (res) => {
									this.shareImage = res.tempFilePath
									resolve()
								},
								fail: reject
							}, this)
						}, 300)
					})
				})
			},
			saveShareImage() {
				if (!this.shareImage) return
				uni.saveImageToPhotosAlbum({
					filePath: this.shareImage,
					success: () => uni.showToast({ title: '已保存到相册', icon: 'success' }),
					fail: (e) => {
						if (e.errMsg && e.errMsg.indexOf('auth deny') > -1) {
							uni.showModal({
								title: '需要相册权限',
								content: '请在设置中允许保存图片到相册',
								showCancel: false
							})
						} else {
							uni.showToast({ title: '保存失败', icon: 'none' })
						}
					}
				})
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
		color: var(--primary);
		font-weight: 600;
		box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);
	}
	.hero-card {
		background: linear-gradient(160deg, var(--primary), var(--primary-light));
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
		color: var(--primary);
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
		background: linear-gradient(180deg, var(--primary-light), var(--primary));
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
		color: var(--primary);
		font-weight: 600;
	}
	.share-bar {
		margin: 20rpx 20rpx 0;
	}
	.share-btn {
		font-size: 28rpx;
	}
	.modal-mask {
		position: fixed;
		top: 0; left: 0; right: 0; bottom: 0;
		background: rgba(0, 0, 0, 0.4);
		z-index: 100;
		display: flex;
		align-items: center;
		justify-content: center;
	}
	.preview-modal {
		background: #FFFFFF;
		border-radius: 24rpx;
		padding: 30rpx;
		width: 560rpx;
		display: flex;
		flex-direction: column;
		align-items: center;
	}
	.modal-title {
		font-size: 32rpx;
		font-weight: 600;
		display: block;
		margin-bottom: 20rpx;
	}
	.share-image {
		width: 480rpx;
		height: 600rpx;
		border-radius: 12rpx;
		background: #F7F5EF;
	}
	.share-to-btn {
		margin-top: 30rpx;
		width: 100%;
	}
	.save-btn {
		margin-top: 16rpx;
		width: 100%;
	}
	.share-canvas {
		width: 640px;
		height: 800px;
		position: fixed;
		left: -9999px;
		top: 0;
	}
</style>
