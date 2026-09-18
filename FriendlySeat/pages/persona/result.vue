<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
	<view v-if="p" class="page">
		<!-- 画像卡 -->
		<view class="hero">
			<text class="hero-label">友邻画像 · 偏好侧写</text>
			<text class="hero-type">{{p.typeName}}</text>
			<text class="hero-desc">{{p.typeDesc}}</text>
			<text class="hero-hint">结果仅供参考，不是专业心理测评</text>
		</view>

		<!-- 标签 -->
		<view class="card">
			<view class="tags">
				<text class="tag" v-for="(t, i) in p.tags" :key="i">{{t}}</text>
			</view>
		</view>

		<!-- 维度 -->
		<view class="card">
			<text class="card-title">偏好维度</text>
			<view class="dim" v-for="d in p.dimensions" :key="d.key">
				<text class="dim-label">{{d.label}}</text>
				<view class="dim-bar">
					<text class="dim-end">{{d.low}}</text>
					<view class="dim-track"><view class="dim-fill" :style="{ width: (d.score / 4 * 100) + '%' }"></view></view>
					<text class="dim-end">{{d.high}}</text>
				</view>
			</view>
		</view>

		<!-- 契合搭子 -->
		<view class="card">
			<text class="card-title">适合的搭子</text>
			<view class="match" v-for="m in p.matches" :key="m.code">
				<text class="match-name">{{m.name}}</text>
				<text class="match-desc">{{m.desc}}</text>
			</view>
			<text class="advice">{{p.advice}}</text>
		</view>

		<!-- 公开设置 -->
		<view class="card row-card">
			<view class="row-info">
				<text class="row-label">公开我的画像</text>
				<text class="row-tip">开启后可在活动、书单等场景展示（默认私密）</text>
			</view>
			<switch :checked="p.isPublic" color="var(--primary)" @change="onPublicChange" />
		</view>

		<view class="actions">
			<button class="btn-primary" @click="genPoster">生成画像卡</button>
			<button class="btn-outline" @click="retake">重新测一次</button>
		</view>

		<text class="foot">一席相邻，善意相续</text>

		<!-- 预览 -->
		<view class="modal-mask" v-if="showPreview" @click="showPreview = false">
			<view class="preview" @click.stop>
				<text class="modal-title">我的友邻画像</text>
				<image class="poster" :src="posterPath" mode="aspectFit" />
				<button class="btn-primary modal-btn" open-type="share">分享给好友</button>
				<button class="btn-outline modal-btn" @click="savePoster">保存图片</button>
			</view>
		</view>

		<canvas type="2d" id="personaCard" class="poster-canvas" :style="{ width: '640px', height: '1120px', position: 'fixed', left: '-99999px', top: '0' }"></canvas>
	</view>
	<view v-else class="empty">加载中…</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { getTheme } from '../../utils/theme.js'

	export default {
		data() {
			return {
				p: null,
				posterPath: '',
				showPreview: false
			}
		},
		onLoad() {
			if (!uni.getStorageSync('token')) {
				uni.redirectTo({ url: '/pages/login/login' })
				return
			}
			this.load()
		},
		onShareAppMessage() {
			return {
				title: this.p ? `我的友邻画像：${this.p.typeName}` : '友邻画像',
				path: '/pages/persona/quiz',
				imageUrl: this.posterPath || undefined
			}
		},
		onShareTimeline() {
			return {
				title: this.p ? `我的友邻画像：${this.p.typeName}` : '友邻画像',
				imageUrl: this.posterPath || undefined
			}
		},
		methods: {
			async load() {
				try {
					this.p = await api.getPersonaMe()
				} catch (e) {}
				if (!this.p) uni.redirectTo({ url: '/pages/persona/quiz' })
			},
			retake() {
				uni.redirectTo({ url: '/pages/persona/quiz' })
			},
			async onPublicChange(e) {
				const isPublic = !!(e.detail && e.detail.value)
				try {
					const res = await api.setPersonaVisibility(isPublic)
					this.p.isPublic = res.isPublic
					uni.showToast({ title: res.isPublic ? '已公开' : '已设为私密', icon: 'none' })
				} catch (err) {
					this.p.isPublic = !isPublic
					uni.showToast({ title: err.message || '操作失败', icon: 'none' })
				}
			},
			rr(ctx, x, y, w, h, r) {
				ctx.beginPath()
				ctx.moveTo(x + r, y)
				ctx.arcTo(x + w, y, x + w, y + h, r)
				ctx.arcTo(x + w, y + h, x, y + h, r)
				ctx.arcTo(x, y + h, x, y, r)
				ctx.arcTo(x, y, x + w, y, r)
				ctx.closePath()
			},
			wrap(ctx, text, x, y, maxWidth, lineHeight, maxLines) {
				const chars = (text || '').split('')
				let line = ''
				let n = 0
				for (const ch of chars) {
					const test = line + ch
					if (ctx.measureText(test).width > maxWidth && line) {
						ctx.fillText(line, x, y + n * lineHeight)
						line = ch
						n++
						if (maxLines && n >= maxLines) return n
					} else {
						line = test
					}
				}
				if (line && (!maxLines || n < maxLines)) {
					ctx.fillText(line, x, y + n * lineHeight)
					n++
				}
				return n
			},
			async genPoster() {
				if (!this.p) return
				uni.showLoading({ title: '生成中', mask: true })
				try {
					await this.drawPoster()
					uni.hideLoading()
					this.showPreview = true
				} catch (e) {
					uni.hideLoading()
					uni.showToast({ title: '生成失败，请重试', icon: 'none' })
				}
			},
			async drawPoster() {
				const p = this.p
				const W = 640
				const pad = 40
				const topY = 36
				const topH = 250
				const tagsH = 44
				const dimsH = 56 + p.dimensions.length * 62 + 16
				const matchH = 56 + p.matches.length * 76 + 16
				const H = topY + topH + 26 + tagsH + 20 + dimsH + 20 + matchH + 110

				await this.$nextTick()
				const node = await new Promise((resolve) => {
					wx.createSelectorQuery().in(this).select('#personaCard').fields({ node: true, size: true }).exec((res) => {
						resolve(res && res[0] ? res[0].node : null)
					})
				})
				if (!node) throw new Error('canvas_not_found')

				const dpr = uni.getSystemInfoSync().pixelRatio || 2
				node.width = W * dpr
				node.height = H * dpr
				const ctx = node.getContext('2d')
				ctx.scale(dpr, dpr)

				const theme = getTheme()
				const primary = theme.primary
				const primaryLight = theme.primaryLight
				const primaryBg = theme.primaryBg
				const dark = '#2B2B27'
				const sub = '#9A9A94'

				ctx.fillStyle = '#F5F3ED'
				ctx.fillRect(0, 0, W, H)

				// 顶部卡片
				ctx.save()
				this.rr(ctx, pad, topY, W - pad * 2, topH, 26)
				ctx.clip()
				const g = ctx.createLinearGradient(pad, topY, W - pad, topY + topH)
				g.addColorStop(0, primary)
				g.addColorStop(1, primaryLight)
				ctx.fillStyle = g
				ctx.fillRect(pad, topY, W - pad * 2, topH)
				ctx.fillStyle = 'rgba(255,255,255,0.12)'
				ctx.beginPath()
				ctx.arc(W - pad - 20, topY + 6, 120, 0, Math.PI * 2)
				ctx.fill()
				ctx.restore()

				ctx.textAlign = 'left'
				ctx.fillStyle = 'rgba(255,255,255,0.85)'
				ctx.font = '22px sans-serif'
				ctx.fillText('友邻画像 · 偏好侧写', pad + 30, topY + 54)

				ctx.fillStyle = '#FFFFFF'
				ctx.font = 'bold 56px sans-serif'
				ctx.fillText(p.typeName, pad + 30, topY + 128)

				ctx.fillStyle = 'rgba(255,255,255,0.92)'
				ctx.font = '24px sans-serif'
				this.wrap(ctx, p.typeDesc, pad + 30, topY + 176, W - pad * 2 - 60, 34, 2)

				// 标签
				let y = topY + topH + 26
				ctx.fillStyle = sub
				ctx.font = '24px sans-serif'
				ctx.fillText(p.tags.join(' · '), pad, y + 26)

				// 维度卡
				y += tagsH + 20
				ctx.fillStyle = '#FFFFFF'
				this.rr(ctx, pad, y, W - pad * 2, dimsH, 18)
				ctx.fill()
				ctx.fillStyle = dark
				ctx.font = 'bold 28px sans-serif'
				ctx.fillText('偏好维度', pad + 24, y + 44)

				p.dimensions.forEach((d, i) => {
					const ry = y + 52 + i * 62
					ctx.fillStyle = dark
					ctx.font = '24px sans-serif'
					ctx.fillText(d.label, pad + 24, ry + 20)

					const trackX = pad + 150
					const trackW = W - pad * 2 - 150 - 90
					ctx.fillStyle = sub
					ctx.font = '20px sans-serif'
					ctx.fillText(d.low, trackX, ry + 20)
					ctx.fillStyle = sub
					ctx.fillText(d.high, trackX + trackW - 46, ry + 20)

					ctx.fillStyle = primaryBg
					this.rr(ctx, trackX, ry + 4, trackW, 12, 6)
					ctx.fill()
					ctx.fillStyle = primary
					this.rr(ctx, trackX, ry + 4, Math.max(12, trackW * d.score / 4), 12, 6)
					ctx.fill()
				})

				// 契合搭子
				y += dimsH + 20
				ctx.fillStyle = '#FFFFFF'
				this.rr(ctx, pad, y, W - pad * 2, matchH, 18)
				ctx.fill()
				ctx.fillStyle = dark
				ctx.font = 'bold 28px sans-serif'
				ctx.fillText('适合的搭子', pad + 24, y + 44)

				p.matches.forEach((m, i) => {
					const ry = y + 56 + i * 76
					ctx.fillStyle = primary
					ctx.font = 'bold 26px sans-serif'
					ctx.fillText(m.name, pad + 24, ry + 20)
					ctx.fillStyle = sub
					ctx.font = '22px sans-serif'
					this.wrap(ctx, m.desc, pad + 24, ry + 50, W - pad * 2 - 48, 28, 1)
				})

				// 页脚
				ctx.textAlign = 'center'
				ctx.fillStyle = primary
				ctx.font = 'bold 24px sans-serif'
				ctx.fillText('一席相邻，善意相续', W / 2, H - 62)
				ctx.fillStyle = sub
				ctx.font = '21px sans-serif'
				ctx.fillText('微信搜索小程序：友邻座', W / 2, H - 30)

				return new Promise((resolve, reject) => {
					setTimeout(() => {
						wx.canvasToTempFilePath({
							canvas: node,
							x: 0,
							y: 0,
							width: W,
							height: H,
							destWidth: W * dpr,
							destHeight: H * dpr,
							success: (res) => {
								this.posterPath = res.tempFilePath
								resolve()
							},
							fail: reject
						}, this)
					}, 200)
				})
			},
			savePoster() {
				if (!this.posterPath) return
				uni.saveImageToPhotosAlbum({
					filePath: this.posterPath,
					success: () => uni.showToast({ title: '已保存到相册', icon: 'success' }),
					fail: () => uni.showToast({ title: '保存失败', icon: 'none' })
				})
			}
		}
	}
</script>

<style scoped>
	.page { padding-bottom: 40rpx; }
	.hero { margin: 20rpx; border-radius: 28rpx; background: linear-gradient(160deg, var(--primary), var(--primary-light)); color: #FFFFFF; padding: 44rpx 36rpx; box-shadow: 0 10rpx 30rpx rgba(0,0,0,0.08); }
	.hero-label { display: block; font-size: 24rpx; opacity: 0.85; }
	.hero-type { display: block; font-size: 60rpx; font-weight: 700; margin-top: 16rpx; }
	.hero-desc { display: block; font-size: 26rpx; line-height: 1.6; opacity: 0.92; margin-top: 14rpx; }
	.hero-hint { display: block; font-size: 20rpx; opacity: 0.7; margin-top: 20rpx; }
	.card-title { display: block; font-size: 30rpx; font-weight: 600; margin-bottom: 20rpx; }
	.tags { display: flex; flex-wrap: wrap; gap: 14rpx; }
	.tag { font-size: 24rpx; color: var(--primary); background: var(--primary-bg); border-radius: 24rpx; padding: 8rpx 24rpx; }
	.dim { display: flex; align-items: center; margin-bottom: 22rpx; }
	.dim-label { width: 130rpx; font-size: 26rpx; color: #33332E; }
	.dim-bar { flex: 1; display: flex; align-items: center; gap: 10rpx; }
	.dim-end { font-size: 20rpx; color: #B0B0AB; }
	.dim-track { flex: 1; height: 12rpx; background: var(--primary-bg); border-radius: 6rpx; overflow: hidden; }
	.dim-fill { height: 100%; background: var(--primary); border-radius: 6rpx; }
	.match { margin-bottom: 18rpx; }
	.match-name { display: block; font-size: 28rpx; font-weight: 600; color: var(--primary); }
	.match-desc { display: block; font-size: 24rpx; color: #8A8A86; margin-top: 4rpx; }
	.advice { display: block; font-size: 24rpx; color: #55554F; margin-top: 8rpx; }
	.row-card { display: flex; align-items: center; justify-content: space-between; gap: 20rpx; }
	.row-info { flex: 1; min-width: 0; }
	.row-label { display: block; font-size: 28rpx; color: #33332E; font-weight: 600; }
	.row-tip { display: block; font-size: 21rpx; color: #B0B0AB; margin-top: 4rpx; }
	.actions { margin: 20rpx; display: flex; flex-direction: column; gap: 20rpx; }
	.foot { display: block; text-align: center; font-size: 22rpx; color: #B0B0AB; padding: 10rpx 0 30rpx; }
	.modal-mask { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 999; display: flex; align-items: center; justify-content: center; }
	.preview { width: 600rpx; max-height: 86vh; background: #FFFFFF; border-radius: 24rpx; padding: 30rpx; display: flex; flex-direction: column; align-items: center; }
	.modal-title { font-size: 32rpx; font-weight: 600; margin-bottom: 20rpx; }
	.poster { width: 460rpx; height: 700rpx; border-radius: 12rpx; background: #F5F3ED; }
	.modal-btn { margin-top: 20rpx; width: 100%; }
	.empty { display: flex; align-items: center; justify-content: center; min-height: 60vh; color: #B0B0AB; font-size: 26rpx; }
</style>
