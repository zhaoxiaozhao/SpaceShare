<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
	<view v-if="p" class="page">
		<!-- 画像卡 -->
		<view class="hero">
			<view class="hero-top">
				<view class="hero-left">
					<view class="type-dot" :style="{ background: p.color }"></view>
					<text class="hero-label">友邻画像 · 偏好侧写</text>
				</view>
				<view class="hero-user">
					<text class="hero-name">{{userName}}</text>
					<Avatar :url="userAvatar" :name="userName" :size="56" />
				</view>
			</view>
			<text class="hero-type">{{p.typeName}}</text>
			<text class="hero-desc">{{p.typeDesc}}</text>
			<text class="hero-quote" v-if="p.quote">「{{p.quote}}」</text>
			<text class="hero-hint">结果仅供参考，不是专业心理测评</text>
		</view>

		<!-- 文人气质 -->
		<view class="card poet-card" v-if="p.poet">
			<view class="poet-top">
				<text class="poet-label">学习气质 · 像</text>
				<text class="poet-name">{{p.poet}}</text>
			</view>
			<text class="poet-line" v-if="p.poetLine">「{{p.poetLine}}」</text>
			<text class="poet-why" v-if="p.poetWhy">{{p.poetWhy}}</text>
			<text class="poet-hint">与你气质相近的古代文人，仅供娱乐参考</text>
		</view>

		<!-- 标签 & 角色 -->
		<view class="card">
			<view class="tags">
				<text class="tag" v-for="(t, i) in p.tags" :key="i">{{t}}</text>
			</view>
			<view class="roles" v-if="p.roles && p.roles.length">
				<text class="roles-label">搭子角色</text>
				<text class="role" v-for="(r, i) in p.roles" :key="i">{{r}}</text>
			</view>
			<text class="scene" v-if="p.scene">适合场景：{{p.scene}}</text>
		</view>

		<!-- 维度 -->
		<view class="card">
			<text class="card-title">偏好维度</text>
			<canvas type="2d" id="radarChart" class="radar"></canvas>
			<view class="dim-legend">
				<view class="legend-item" v-for="d in p.dimensions" :key="d.key">
					<text class="legend-label">{{d.label}}</text>
					<text class="legend-score">{{d.low}} {{d.score}}/4 {{d.high}}</text>
				</view>
			</view>
		</view>

		<!-- 搭子配型 -->
		<view class="card" v-for="(g, gi) in p.pairings" :key="gi">
			<text class="card-title">{{g.title}}</text>
			<text class="pair-reason">{{g.reason}}</text>
			<view class="pair-item" v-for="m in g.items" :key="m.code">
				<text class="pair-name">{{m.name}}</text>
				<text class="pair-desc">{{m.desc}}</text>
			</view>
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
				<image class="poster" :src="posterPath" mode="aspectFit" show-menu-by-longpress />
				<button class="btn-primary modal-btn" open-type="share">分享给好友</button>
				<button class="btn-outline modal-btn" @click="savePoster">保存图片</button>
				<text class="modal-tip">也可长按上方图片直接保存或转发</text>
			</view>
		</view>

		<canvas type="2d" id="personaCard" class="poster-canvas" :style="{ width: '640px', height: '1500px', position: 'fixed', left: '-99999px', top: '0' }"></canvas>
	</view>
	<view v-else class="empty">加载中…</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { drawRadar, animateRadar } from '../../utils/radar.js'
	import { getTheme } from '../../utils/theme.js'

	export default {
		data() {
			return {
				p: null,
				avatarInfo: { name: '', dataUrl: '' },
				user: uni.getStorageSync('user') || {},
				avatarImg: null,
				posterPath: '',
				showPreview: false
			}
		},
		computed: {
			userName() {
				return this.avatarInfo.name || this.user.nickname || '友邻'
			},
			userAvatar() {
				return this.avatarInfo.dataUrl || this.user.avatarUrl || ''
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
				if (!this.p) {
					uni.redirectTo({ url: '/pages/persona/quiz' })
					return
				}
				try {
					const info = await api.getPersonaAvatar()
					if (info) this.avatarInfo = info
				} catch (e) {}
				await this.$nextTick()
				this.drawRadarChart()
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
			async drawRadarChart() {
				if (!this.p) return
				const W = 320
				const H = 300
				const node = await new Promise((resolve) => {
					wx.createSelectorQuery().in(this).select('#radarChart').fields({ node: true, size: true }).exec((res) => {
						resolve(res && res[0] ? res[0].node : null)
					})
				})
				if (!node) return
				const dpr = uni.getSystemInfoSync().pixelRatio || 2
				node.width = W * dpr
				node.height = H * dpr
				const ctx = node.getContext('2d')
				ctx.scale(dpr, dpr)
				await animateRadar(node, ctx, {
					cx: W / 2,
					cy: 150,
					radius: 100,
					width: W,
					height: H,
					dimensions: this.p.dimensions,
					color: getTheme().primary,
					labelColor: '#8A8A86',
					labelFont: '13px sans-serif',
					showScale: true,
					showScore: true
				}, 700)
			},
			saveDataUrl(dataUrl) {
				return new Promise((resolve) => {
					try {
						const i = String(dataUrl).indexOf(',')
						const b64 = i >= 0 ? dataUrl.slice(i + 1) : dataUrl
						const m = /^data:image\/(\w+)/.exec(String(dataUrl))
						const ext = m ? m[1] : 'jpg'
						const fs = wx.getFileSystemManager()
						const pth = `${wx.env.USER_DATA_PATH}/persona_avatar.${ext}`
						fs.writeFileSync(pth, b64, 'base64')
						resolve(pth)
					} catch (e) {
						resolve(null)
					}
				})
			},
			loadAvatarImage(canvas) {
				return new Promise((resolve) => {
					const dataUrl = this.avatarInfo && this.avatarInfo.dataUrl
					if (!dataUrl) return resolve(null)
					this.saveDataUrl(dataUrl).then((pth) => {
						if (!pth) return resolve(null)
						const img = canvas.createImage()
						img.onload = () => resolve(img)
						img.onerror = () => resolve(null)
						img.src = pth
					})
				})
			},
			lighten(hex, ratio) {
				const h = String(hex || '#6BAF8B').replace('#', '')
				const r = parseInt(h.slice(0, 2), 16)
				const g = parseInt(h.slice(2, 4), 16)
				const b = parseInt(h.slice(4, 6), 16)
				const mix = (c) => Math.round(c + (255 - c) * ratio)
				return `rgb(${mix(r)},${mix(g)},${mix(b)})`
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
				const topH = 340
				const tagsH = 84
				const dimsH = 396
				const pairH = 56 + (p.pairings || []).length * 96 + 16
				const H = topY + topH + 26 + tagsH + 20 + dimsH + 20 + pairH + 110

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

				this.avatarImg = await this.loadAvatarImage(node)

				const theme = getTheme()
				const primary = theme.primary
				const primaryLight = theme.primaryLight
				const typeColor = p.color || primary
				const dark = '#2B2B27'
				const sub = '#9A9A94'

				ctx.fillStyle = '#F5F3ED'
				ctx.fillRect(0, 0, W, H)

				// 顶部卡片（用画像专属色）
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
				ctx.arc(W - pad - 20, topY + 6, 130, 0, Math.PI * 2)
				ctx.fill()
				ctx.restore()

				ctx.textAlign = 'left'
				ctx.fillStyle = typeColor
				this.rr(ctx, pad + 30, topY + 36, 16, 16, 8)
				ctx.fill()
				ctx.fillStyle = 'rgba(255,255,255,0.85)'
				ctx.font = '22px sans-serif'
				ctx.fillText('友邻画像 · 偏好侧写', pad + 56, topY + 52)

				// 右上角：头像 + 昵称
				const nick = (this.userName || '友邻').slice(0, 6)
				const avSize = 46
				const avX = W - pad - 30 - avSize
				const avY = topY + 28
				if (this.avatarImg) {
					ctx.save()
					ctx.beginPath()
					ctx.arc(avX + avSize / 2, avY + avSize / 2, avSize / 2, 0, Math.PI * 2)
					ctx.clip()
					ctx.drawImage(this.avatarImg, avX, avY, avSize, avSize)
					ctx.restore()
				} else {
					ctx.fillStyle = 'rgba(255,255,255,0.35)'
					ctx.beginPath()
					ctx.arc(avX + avSize / 2, avY + avSize / 2, avSize / 2, 0, Math.PI * 2)
					ctx.fill()
					ctx.fillStyle = '#FFFFFF'
					ctx.font = 'bold 22px sans-serif'
					ctx.textAlign = 'center'
					ctx.textBaseline = 'middle'
					ctx.fillText(nick.slice(0, 1) || '友', avX + avSize / 2, avY + avSize / 2)
					ctx.textAlign = 'left'
					ctx.textBaseline = 'alphabetic'
				}
				ctx.textAlign = 'right'
				ctx.fillStyle = 'rgba(255,255,255,0.95)'
				ctx.font = '22px sans-serif'
				ctx.fillText(nick, avX - 12, avY + avSize / 2 + 7)
				ctx.textAlign = 'left'

				ctx.fillStyle = '#FFFFFF'
				ctx.font = 'bold 56px sans-serif'
				ctx.fillText(p.typeName, pad + 30, topY + 124)

				if (p.poet) {
					ctx.fillStyle = 'rgba(255,255,255,0.9)'
					ctx.font = '24px sans-serif'
					ctx.fillText('学习气质 · 像 ' + p.poet, pad + 30, topY + 166)
				}

				ctx.fillStyle = 'rgba(255,255,255,0.92)'
				ctx.font = '24px sans-serif'
				this.wrap(ctx, p.typeDesc, pad + 30, topY + 208, W - pad * 2 - 60, 34, 2)

				if (p.quote) {
					ctx.fillStyle = 'rgba(255,255,255,0.8)'
					ctx.font = 'italic 22px sans-serif'
					this.wrap(ctx, '「' + p.quote + '」', pad + 30, topY + 300, W - pad * 2 - 60, 30, 1)
				}

				// 标签（两行内）
				let y = topY + topH + 26
				ctx.fillStyle = sub
				ctx.font = '24px sans-serif'
				this.wrap(ctx, (p.tags || []).join(' · '), pad, y + 26, W - pad * 2, 34, 2)

				// 维度卡
				y += tagsH + 20
				ctx.fillStyle = '#FFFFFF'
				this.rr(ctx, pad, y, W - pad * 2, dimsH, 18)
				ctx.fill()
				ctx.fillStyle = dark
				ctx.font = 'bold 28px sans-serif'
				ctx.textAlign = 'left'
				ctx.fillText('偏好维度', pad + 24, y + 44)
				drawRadar(ctx, {
					cx: W / 2,
					cy: y + 52 + 156,
					radius: 112,
					dimensions: p.dimensions,
					color: primary,
					labelColor: sub,
					labelFont: '22px sans-serif',
					showScale: true,
					showScore: true,
					scaleFont: '16px sans-serif',
					scoreFont: '20px sans-serif'
				})

				// 配型卡
				y += dimsH + 20
				ctx.fillStyle = '#FFFFFF'
				this.rr(ctx, pad, y, W - pad * 2, pairH, 18)
				ctx.fill()
				ctx.fillStyle = dark
				ctx.font = 'bold 28px sans-serif'
				ctx.fillText('搭子配型', pad + 24, y + 44)

				;(p.pairings || []).forEach((grp, gi) => {
					const gy = y + 56 + gi * 96
					ctx.fillStyle = primary
					ctx.font = 'bold 24px sans-serif'
					ctx.fillText(grp.title, pad + 24, gy + 20)
					ctx.fillStyle = dark
					ctx.font = '23px sans-serif'
					const names = (grp.items || []).map((x) => x.name).join('、')
					this.wrap(ctx, names, pad + 24, gy + 50, W - pad * 2 - 48, 30, 1)
					ctx.fillStyle = sub
					ctx.font = '20px sans-serif'
					this.wrap(ctx, grp.reason, pad + 24, gy + 78, W - pad * 2 - 48, 26, 1)
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
				if (!this.posterPath) {
					uni.showToast({ title: '请先生成画像卡', icon: 'none' })
					return
				}
				uni.saveImageToPhotosAlbum({
					filePath: this.posterPath,
					success: () => uni.showToast({ title: '已保存到相册', icon: 'success' }),
					fail: (e) => {
						const msg = (e && e.errMsg) || ''
						if (msg.indexOf('auth') > -1 || msg.indexOf('authorize') > -1) {
							uni.showModal({
								title: '需要相册权限',
								content: '请允许保存到相册（或长按图片直接保存/转发）',
								confirmText: '去设置',
								success: (r) => { if (r.confirm) uni.openSetting({}) }
							})
						} else {
							uni.showToast({ title: '保存失败，可长按图片保存', icon: 'none' })
						}
					}
				})
			}
		}
	}
</script>

<style scoped>
	.page { padding-bottom: 40rpx; }
	.hero { margin: 20rpx; border-radius: 28rpx; color: #FFFFFF; padding: 44rpx 36rpx; box-shadow: 0 10rpx 30rpx rgba(0,0,0,0.08); background: linear-gradient(160deg, var(--primary), var(--primary-light)); }
	.hero-top { display: flex; align-items: center; justify-content: space-between; gap: 16rpx; }
	.hero-left { display: flex; align-items: center; gap: 10rpx; min-width: 0; }
	.hero-user { display: flex; align-items: center; gap: 12rpx; flex-shrink: 0; }
	.hero-name { font-size: 24rpx; opacity: 0.9; max-width: 220rpx; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.type-dot { width: 16rpx; height: 16rpx; border-radius: 50%; flex-shrink: 0; }
	.hero-label { display: block; font-size: 24rpx; opacity: 0.85; }
	.hero-type { display: block; font-size: 60rpx; font-weight: 700; margin-top: 16rpx; }
	.hero-desc { display: block; font-size: 26rpx; line-height: 1.6; opacity: 0.92; margin-top: 14rpx; }
	.hero-quote { display: block; font-size: 26rpx; opacity: 0.9; margin-top: 18rpx; }
	.hero-hint { display: block; font-size: 20rpx; opacity: 0.7; margin-top: 20rpx; }
	.poet-card { background: linear-gradient(160deg, var(--primary-bg), #FFFFFF); }
	.poet-top { display: flex; align-items: baseline; gap: 12rpx; }
	.poet-label { font-size: 24rpx; color: #8A8A86; }
	.poet-name { font-size: 44rpx; font-weight: 700; color: var(--primary); }
	.poet-line { display: block; font-size: 28rpx; color: #55554F; margin-top: 16rpx; }
	.poet-why { display: block; font-size: 26rpx; color: #33332E; line-height: 1.6; margin-top: 12rpx; }
	.poet-hint { display: block; font-size: 20rpx; color: #B0B0AB; margin-top: 14rpx; }
	.card-title { display: block; font-size: 30rpx; font-weight: 600; margin-bottom: 16rpx; }
	.tags { display: flex; flex-wrap: wrap; gap: 14rpx; }
	.tag { font-size: 24rpx; color: var(--primary); background: var(--primary-bg); border-radius: 24rpx; padding: 8rpx 24rpx; }
	.roles { display: flex; align-items: center; flex-wrap: wrap; gap: 12rpx; margin-top: 20rpx; }
	.roles-label { font-size: 24rpx; color: #8A8A86; margin-right: 4rpx; }
	.role { font-size: 24rpx; color: var(--primary); background: var(--primary-bg); border-radius: 20rpx; padding: 6rpx 20rpx; }
	.scene { display: block; font-size: 24rpx; color: #55554F; margin-top: 18rpx; }
	.radar { width: 320px; height: 300px; margin: 0 auto; display: block; }
	.dim-legend { display: flex; flex-wrap: wrap; gap: 10rpx 18rpx; margin-top: 16rpx; }
	.legend-item { display: flex; align-items: center; gap: 8rpx; }
	.legend-label { font-size: 22rpx; color: #33332E; }
	.legend-score { font-size: 20rpx; color: #B0B0AB; }
	.dim { display: flex; align-items: center; margin-bottom: 20rpx; }
	.dim-label { width: 130rpx; font-size: 26rpx; color: #33332E; }
	.dim-bar { flex: 1; display: flex; align-items: center; gap: 10rpx; }
	.dim-end { font-size: 20rpx; color: #B0B0AB; }
	.dim-track { flex: 1; height: 12rpx; background: #EFEEE9; border-radius: 6rpx; overflow: hidden; }
	.dim-fill { height: 100%; border-radius: 6rpx; }
	.pair-reason { display: block; font-size: 22rpx; color: #B0B0AB; margin: -6rpx 0 16rpx; }
	.pair-item { margin-bottom: 14rpx; }
	.pair-name { display: block; font-size: 28rpx; font-weight: 600; color: var(--primary); }
	.pair-desc { display: block; font-size: 24rpx; color: #8A8A86; margin-top: 4rpx; }
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
	.modal-tip { font-size: 20rpx; color: #B0B0AB; margin-top: 14rpx; }
	.modal-btn { margin-top: 20rpx; width: 100%; }
	.empty { display: flex; align-items: center; justify-content: center; min-height: 60vh; color: #B0B0AB; font-size: 26rpx; }
</style>
