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
	import { getTheme } from '../../utils/theme.js'

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
				const s = String(this.report.start).slice(5, 10)
				const e = String(this.report.end).slice(5, 10)
				return `${s} ~ ${e}`
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
				if (min >= 60) return (min / 60).toFixed(1).replace('.0', '') + ' 小时'
				return min + ' 分钟'
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
			rr(ctx, x, y, w, h, r) {
				ctx.beginPath()
				ctx.moveTo(x + r, y)
				ctx.lineTo(x + w - r, y)
				ctx.quadraticCurveTo(x + w, y, x + w, y + r)
				ctx.lineTo(x + w, y + h - r)
				ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h)
				ctx.lineTo(x + r, y + h)
				ctx.quadraticCurveTo(x, y + h, x, y + h - r)
				ctx.lineTo(x, y + r)
				ctx.quadraticCurveTo(x, y, x + r, y)
				ctx.closePath()
			},
			drawCard() {
				return new Promise((resolve, reject) => {
					const W = 640
					const pad = 40
					const topY = 36
					const topH = 236
					const r = this.report
					const periodName = this.period === 'monthly' ? '月报' : '周报'
					const theme = getTheme()
					const primary = theme.primary
					const primaryLight = theme.primaryLight
					const primaryBg = theme.primaryBg
					const dark = '#2B2B27'
					const gray = '#8A8A86'

					const types = r.typeDistribution.slice(0, 4)
					const statH = 120
					const typeCardH = types.length ? types.length * 52 + 56 : 0
					const H = topY + topH + 24 + statH + 24 + typeCardH + 90

					const ctx = uni.createCanvasContext('shareCard', this)

					// 背景
					ctx.setFillStyle('#F5F3ED')
					ctx.fillRect(0, 0, W, H)

					// 顶部圆角渐变卡片
					ctx.save()
					this.rr(ctx, pad, topY, W - pad * 2, topH, 26)
					ctx.clip()
					const g = ctx.createLinearGradient(pad, topY, W - pad, topY + topH)
					g.addColorStop(0, primary)
					g.addColorStop(1, primaryLight)
					ctx.setFillStyle(g)
					ctx.fillRect(pad, topY, W - pad * 2, topH)
					ctx.setFillStyle('rgba(255,255,255,0.12)')
					ctx.beginPath()
					ctx.arc(W - pad - 20, topY + 6, 120, 0, 2 * Math.PI)
					ctx.fill()
					ctx.beginPath()
					ctx.arc(pad + 6, topY + topH - 6, 76, 0, 2 * Math.PI)
					ctx.fill()
					ctx.restore()

					ctx.setTextAlign('left')
					ctx.setFillStyle('rgba(255,255,255,0.85)')
					ctx.setFontSize(24)
					ctx.fillText('友邻座 · 学习' + periodName, pad + 30, topY + 58)

					ctx.setFillStyle('rgba(255,255,255,0.8)')
					ctx.setFontSize(22)
					ctx.fillText(this.periodText, pad + 30, topY + 96)

					ctx.setFillStyle('#FFFFFF')
					ctx.setFontSize(72)
					ctx.fillText(this.formatHours(r.totalMinutes), pad + 30, topY + 178)

					ctx.setFillStyle('rgba(255,255,255,0.85)')
					ctx.setFontSize(22)
					ctx.fillText('总学习时长', pad + 30, topY + 216)

					let y = topY + topH + 24

					// 统计卡片
					const stats = [
						{ label: '学习天数', value: String(r.studyDays) },
						{ label: '学习次数', value: String(r.sessionCount) },
						{ label: '最长连续', value: String(r.longestStreak) + '天' }
					]
					const gap = 20
					const cardW = (W - pad * 2 - gap * 2) / 3
					stats.forEach((s, i) => {
						const x = pad + i * (cardW + gap)
						ctx.setFillStyle('#FFFFFF')
						this.rr(ctx, x, y, cardW, statH, 18)
						ctx.fill()
						ctx.setFillStyle(primary)
						ctx.setFontSize(36)
						ctx.setTextAlign('center')
						ctx.fillText(s.value, x + cardW / 2, y + 52)
						ctx.setFillStyle(gray)
						ctx.setFontSize(22)
						ctx.fillText(s.label, x + cardW / 2, y + 90)
					})
					y += statH + 24

					// 学习类型
					if (types.length) {
						ctx.setFillStyle('#FFFFFF')
						this.rr(ctx, pad, y, W - pad * 2, typeCardH, 18)
						ctx.fill()

						ctx.setTextAlign('left')
						ctx.setFillStyle(dark)
						ctx.setFontSize(28)
						ctx.fillText('学习类型', pad + 24, y + 42)

						const innerX = pad + 24
						const innerW = W - (pad + 24) * 2
						types.forEach((t, i) => {
							const ry = y + 72 + i * 52
							const total = types.reduce((s, x) => s + x.value, 0) || 1
							const pct = Math.round(t.value / total * 100)

							ctx.setFillStyle(gray)
							ctx.setFontSize(24)
							ctx.setTextAlign('left')
							ctx.fillText(this.typeLabel(t.key), innerX, ry)

							ctx.setFillStyle(primary)
							ctx.setFontSize(24)
							ctx.setTextAlign('right')
							ctx.fillText(pct + '%', innerX + innerW, ry)
							ctx.setTextAlign('left')

							ctx.setFillStyle(primaryBg)
							ctx.fillRect(innerX, ry + 12, innerW, 10)
							ctx.setFillStyle(primary)
							ctx.fillRect(innerX, ry + 12, Math.max(4, innerW * pct / 100), 10)
						})
						y += typeCardH
					}

					// 底部
					ctx.setFillStyle(primary)
					ctx.setFontSize(24)
					ctx.setTextAlign('center')
					ctx.fillText('一席相邻，善意相续', W / 2, H - 58)
					ctx.setFillStyle(gray)
					ctx.setFontSize(21)
					ctx.fillText('友邻座 · 学习报告', W / 2, H - 26)

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
