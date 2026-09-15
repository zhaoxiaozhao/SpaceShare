<template>
	<page-meta :page-style="pageThemeStyle" />
	<view>
		<!-- 选择书籍 -->
		<view class="card">
			<view class="sec-head">
				<text class="section-label">选择书籍</text>
				<view class="head-actions">
					<text class="link" @click="selectAll">全选</text>
					<text class="link" @click="clearAll">清空</text>
				</view>
			</view>
			<text class="tip">已选 {{selected.length}} / 最多 20 本</text>
			<view v-if="books.length" class="book-list">
				<view
					class="book-row"
					v-for="b in books"
					:key="b.id"
					@click="toggle(b.id)"
				>
					<BookCover :url="b.coverUrl" :title="b.title" :width="72" :height="96" :radius="8" />
					<view class="book-info">
						<text class="book-title">{{b.title}}</text>
						<text class="book-author" v-if="b.author">{{b.author}}</text>
					</view>
					<view class="check" :class="{ on: selected.includes(b.id) }"></view>
				</view>
			</view>
			<view v-else class="empty">还没有书籍，先去「我的阅读」添加吧</view>
		</view>

		<!-- 书单信息 -->
		<view class="card">
			<text class="section-label">书单信息</text>
			<input class="input" v-model="title" placeholder="书单标题" :maxlength="30" />
			<textarea class="textarea" v-model="remark" placeholder="写一句推荐语（可选）" :maxlength="100" />
			<text class="count">{{remark.length}}/100</text>
			<view class="switch-row">
				<view class="switch-info">
					<text class="switch-label">公开到热门书单榜</text>
					<text class="switch-tip">公开后其他用户可看到并收藏（默认私密，仅拿到链接的人可看）</text>
				</view>
				<switch :checked="isPublic" color="var(--primary)" @change="onPublicChange" />
			</view>
		</view>

		<view class="actions">
			<button class="btn-primary" :loading="creating" @click="generate">生成书单海报</button>
		</view>

		<view class="board-entry" @click="goBoard">
			<text class="board-entry-text">看看热门书单榜</text>
			<text class="share-arrow">›</text>
		</view>

		<!-- 我生成的书单 -->
		<view class="card" v-if="myShares.length">
			<text class="section-label">我生成的书单</text>
			<view class="share-row" v-for="s in myShares" :key="s.token" @click="openShare(s.token)">
				<view class="share-info">
					<text class="share-title">{{s.title}}</text>
					<text class="share-sub">{{s.count}} 本 · {{s.viewCount}} 浏览 · {{s.favoriteCount}} 收藏 · {{formatDate(s.createdAt)}}</text>
				</view>
				<text class="pub-tag" :class="{ on: s.isPublic }">{{s.isPublic ? '公开' : '私密'}}</text>
				<text class="share-arrow">›</text>
			</view>
		</view>

		<!-- 预览 -->
		<view class="modal-mask" v-if="showPreview" @click="showPreview = false">
			<view class="preview-modal" @click.stop>
				<text class="modal-title">书单已生成</text>
				<image class="share-image" :src="posterPath" mode="aspectFit" />
				<button class="btn-primary modal-btn" open-type="share">分享给好友</button>
				<button class="btn-outline modal-btn" @click="savePoster">保存图片</button>
				<button class="btn-outline modal-btn" @click="viewList">查看书单页</button>
			</view>
		</view>

		<canvas type="2d" id="posterCard" class="poster-canvas" :style="{ width: '640px', height: canvasH + 'px', position: 'fixed', left: '-99999px', top: '0' }"></canvas>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { getSeasonKey, getTheme, bookCoverColor } from '../../utils/theme.js'
	import { parseDate } from '../../utils/format.js'
	import { getTempFileUrl } from '../../utils/profile.js'
	import { CLOUD_ENV } from '../../utils/config.js'

	const STATUS_LABELS = { WantToRead: '想读', Reading: '在读', Finished: '已读' }

	export default {
		data() {
			return {
				season: getSeasonKey(),
				books: [],
				selected: [],
				title: '我的书单',
				remark: '',
				creating: false,
				showPreview: false,
				posterPath: '',
				token: '',
				canvasH: 1200,
				isPublic: false,
				myShares: []
			}
		},
		onLoad() {
			this.load()
		},
		onShow() {
			this.loadMy()
		},
		onShareAppMessage() {
			return {
				title: this.title || '我的书单',
				path: this.token ? `/pages/reading/list-share-view?token=${this.token}` : '/pages/index/index',
				imageUrl: this.posterPath || ''
			}
		},
		methods: {
			async load() {
				try {
					const res = await api.getReadingBooks('')
					this.books = (res && res.books) || []
				} catch (e) {}
			},
			async loadMy() {
				try {
					this.myShares = await api.getMyBookListShares()
				} catch (e) {}
			},
			openShare(token) {
				uni.navigateTo({ url: `/pages/reading/list-share-view?token=${token}` })
			},
			goBoard() {
				uni.navigateTo({ url: '/pages/reading/list-share-board' })
			},
			onPublicChange(e) {
				this.isPublic = !!(e.detail && e.detail.value)
			},
			formatDate(iso) {
				const d = parseDate(iso)
				if (!d) return ''
				return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
			},
			toggle(id) {
				const i = this.selected.indexOf(id)
				if (i >= 0) {
					this.selected.splice(i, 1)
				} else {
					if (this.selected.length >= 20) {
						uni.showToast({ title: '最多选择 20 本', icon: 'none' })
						return
					}
					this.selected.push(id)
				}
			},
			selectAll() {
				this.selected = this.books.slice(0, 20).map(b => b.id)
			},
			clearAll() {
				this.selected = []
			},
			async generate() {
				if (!this.selected.length) {
					uni.showToast({ title: '请至少选择一本书', icon: 'none' })
					return
				}
				this.creating = true
				uni.showLoading({ title: '生成中', mask: true })
				try {
					// 保持与书籍列表一致的顺序
					const bookIds = this.books.filter(b => this.selected.includes(b.id)).map(b => b.id)
					const share = await api.createBookListShare({
						bookIds,
						title: this.title,
						remark: this.remark,
						isPublic: this.isPublic
					})
					this.token = share.token
					await this.drawPoster(share)
					uni.hideLoading()
					this.showPreview = true
					this.loadMy()
				} catch (e) {
					uni.hideLoading()
					uni.showToast({ title: e.message || '生成失败，请重试', icon: 'none' })
				} finally {
					this.creating = false
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
			// 解析封面为 canvas 可绘制图片（cloud:// 先下载/换 https，再落本地路径）
			async loadCover(canvas, url) {
				try {
					if (!url) return null
					let local = null
					if (/^cloud:\/\//.test(url)) {
						// ① 云存储直接下载
						local = await this.cloudDownload(url)
						// ② 换 https 临时链接后再取本地路径
						if (!local) {
							const https = await getTempFileUrl(url)
							if (https) local = await this.toLocalPath(https)
						}
					} else {
						local = await this.toLocalPath(url)
					}
					if (!local) {
						console.warn('[poster] 封面转本地路径失败', url)
						return null
					}
					return await this.toCanvasImage(canvas, local)
				} catch (e) {
					console.warn('[poster] 封面加载异常', url, e)
					return null
				}
			},
			cloudDownload(fileID) {
				return new Promise((resolve) => {
					if (!wx || !wx.cloud || !wx.cloud.downloadFile) return resolve(null)
					wx.cloud.downloadFile({
						fileID,
						config: { env: CLOUD_ENV },
						success: (r) => resolve(r.tempFilePath),
						fail: (e) => {
							console.warn('[poster] cloud.downloadFile 失败', fileID, e)
							resolve(null)
						}
					})
				})
			},
			toLocalPath(src) {
				return new Promise((resolve) => {
					uni.getImageInfo({
						src,
						success: (info) => resolve(info.path || src),
						fail: () => {
							if (/^https?:/.test(src)) {
								uni.downloadFile({
									url: src,
									success: (r) => resolve(r.statusCode === 200 ? r.tempFilePath : null),
									fail: () => resolve(null)
								})
							} else {
								resolve(null)
							}
						}
					})
				})
			},
			toCanvasImage(canvas, src) {
				return new Promise((resolve) => {
					let img = null
					try {
						img = canvas.createImage()
					} catch (e) {
						console.warn('[poster] canvas.createImage 不可用', e)
					}
					if (!img) return resolve(null)
					uni.getImageInfo({
						src,
						success: (i) => (img.__w = i.width, img.__h = i.height),
						fail: () => {}
					})
					img.onload = () => resolve({ img, width: img.__w || img.width || 0, height: img.__h || img.height || 0 })
					img.onerror = (e) => {
						console.warn('[poster] 图片加载失败', src, e)
						resolve(null)
					}
					img.src = src
				})
			},
			async drawPoster(share) {
				const books = share.books || []
				const show = books.slice(0, 10)
				const more = books.length - show.length
				const hasRemark = !!(share.remark && share.remark.length)

				const W = 640
				const pad = 44
				const headerH = 250
				const cardH = 104
				const gap = 16
				const remarkH = hasRemark ? 96 : 0
				const H = headerH + 36 + remarkH + show.length * (cardH + gap) + (more > 0 ? 46 : 0) + 130
				this.canvasH = H
				await this.$nextTick()

				const node = await new Promise((resolve) => {
					wx.createSelectorQuery().in(this).select('#posterCard').fields({ node: true, size: true }).exec((res) => {
						resolve(res && res[0] ? res[0].node : null)
					})
				})
				if (!node) throw new Error('canvas_not_found')

				const dpr = uni.getSystemInfoSync().pixelRatio || 2
				node.width = W * dpr
				node.height = H * dpr
				const ctx = node.getContext('2d')
				ctx.scale(dpr, dpr)

				// 预加载封面
				const imgs = await Promise.all(show.map((b) => this.loadCover(node, b.coverUrl)))

				const theme = getTheme()
				const primary = theme.primary
				const primaryLight = theme.primaryLight
				const dark = '#2B2B27'
				const sub = '#9A9A94'

				// 背景
				ctx.fillStyle = '#F5F3ED'
				ctx.fillRect(0, 0, W, H)

				// 顶部渐变
				const g = ctx.createLinearGradient(0, 0, W, headerH)
				g.addColorStop(0, primary)
				g.addColorStop(1, primaryLight)
				ctx.fillStyle = g
				ctx.fillRect(0, 0, W, headerH)
				ctx.fillStyle = 'rgba(255,255,255,0.10)'
				ctx.beginPath()
				ctx.arc(562, 24, 160, 0, Math.PI * 2)
				ctx.fill()
				ctx.beginPath()
				ctx.arc(16, 234, 96, 0, Math.PI * 2)
				ctx.fill()

				ctx.textAlign = 'left'
				ctx.fillStyle = 'rgba(255,255,255,0.85)'
				ctx.font = '24px sans-serif'
				ctx.fillText('友邻座 · 书单', pad, 76)

				ctx.fillStyle = '#FFFFFF'
				ctx.font = 'bold 46px sans-serif'
				let title = share.title || '我的书单'
				while (ctx.measureText(title).width > W - pad * 2 && title.length > 1) title = title.slice(0, -1)
				ctx.fillText(title, pad, 142)

				ctx.fillStyle = 'rgba(255,255,255,0.92)'
				ctx.font = '24px sans-serif'
				const totalHours = Math.round((share.totalMinutes || 0) / 60)
				ctx.fillText(`${share.ownerName || '书友'} · 共 ${share.count || books.length} 本 · ${totalHours} 小时阅读`, pad, 198)

				let y = headerH + 36

				// 推荐语
				if (hasRemark) {
					ctx.fillStyle = '#FFFFFF'
					this.rr(ctx, pad, y, W - pad * 2, remarkH - 16, 18)
					ctx.fill()
					ctx.fillStyle = primary
					this.rr(ctx, pad, y, 8, remarkH - 16, 4)
					ctx.fill()
					ctx.fillStyle = dark
					ctx.font = '26px sans-serif'
					this.wrap(ctx, share.remark, pad + 30, y + 34, W - pad * 2 - 56, 34, 2)
					y += remarkH
				}

				// 书籍卡片
				show.forEach((b, i) => {
					const cy = y + i * (cardH + gap)
					ctx.fillStyle = '#FFFFFF'
					this.rr(ctx, pad, cy, W - pad * 2, cardH, 18)
					ctx.fill()

					const cw = 62
					const ch = 88
					const cx = pad + 16
					const ccy = cy + (cardH - ch) / 2
					const img = imgs[i]
					if (img && img.img) {
						this.rr(ctx, cx, ccy, cw, ch, 10)
						ctx.save()
						ctx.clip()
						if (img.width && img.height) {
							const s = Math.min(img.width, img.height)
							const sx = (img.width - s) / 2
							const sy = (img.height - s) / 2
							ctx.drawImage(img.img, sx, sy, s, s, cx, ccy, cw, ch)
						} else {
							ctx.drawImage(img.img, cx, ccy, cw, ch)
						}
						ctx.restore()
					} else {
						ctx.fillStyle = bookCoverColor(b.title)
						this.rr(ctx, cx, ccy, cw, ch, 10)
						ctx.fill()
						ctx.fillStyle = '#FFFFFF'
						ctx.font = 'bold 30px sans-serif'
						ctx.textAlign = 'center'
						ctx.fillText((b.title || '书').slice(0, 1), cx + cw / 2, ccy + ch / 2 + 11)
						ctx.textAlign = 'left'
					}

					const tx = cx + cw + 22
					const maxW = W - pad - tx - 26
					ctx.fillStyle = dark
					ctx.font = 'bold 30px sans-serif'
					let bt = b.title || ''
					while (ctx.measureText(bt).width > maxW && bt.length > 1) bt = bt.slice(0, -1)
					ctx.fillText(bt, tx, cy + 48)

					ctx.fillStyle = sub
					ctx.font = '23px sans-serif'
					let bs = [b.author, STATUS_LABELS[b.status] || ''].filter(Boolean).join(' · ')
					while (ctx.measureText(bs).width > maxW && bs.length > 1) bs = bs.slice(0, -1)
					ctx.fillText(bs, tx, cy + 80)
				})

				y += show.length * (cardH + gap)
				if (more > 0) {
					ctx.fillStyle = sub
					ctx.font = '24px sans-serif'
					ctx.textAlign = 'center'
					ctx.fillText(`等共 ${books.length} 本`, W / 2, y + 12)
					ctx.textAlign = 'left'
				}

				// 页脚
				ctx.textAlign = 'center'
				ctx.fillStyle = primary
				ctx.font = 'bold 24px sans-serif'
				ctx.fillText('一席相邻，善意相续', W / 2, H - 64)
				ctx.fillStyle = sub
				ctx.font = '21px sans-serif'
				ctx.fillText('友邻座 · 书单分享', W / 2, H - 32)

				await new Promise((resolve, reject) => {
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
			},
			viewList() {
				if (!this.token) return
				this.showPreview = false
				uni.navigateTo({ url: `/pages/reading/list-share-view?token=${this.token}` })
			}
		}
	}
</script>

<style scoped>
	.sec-head { display: flex; align-items: center; justify-content: space-between; }
	.head-actions { display: flex; gap: 24rpx; }
	.link { font-size: 26rpx; color: var(--primary); }
	.section-label { font-size: 30rpx; font-weight: 600; }
	.tip { display: block; font-size: 22rpx; color: #B0B0AB; margin: 10rpx 0 16rpx; }
	.book-list { display: flex; flex-direction: column; }
	.book-row { display: flex; align-items: center; gap: 20rpx; padding: 14rpx 0; border-bottom: 1rpx solid #F0EFEA; }
	.book-row:last-child { border-bottom: none; }
	.book-info { flex: 1; min-width: 0; }
	.book-title { font-size: 28rpx; font-weight: 600; display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.book-author { font-size: 22rpx; color: #8A8A86; }
	.check { width: 40rpx; height: 40rpx; border-radius: 50%; border: 2rpx solid #D5D3CC; flex-shrink: 0; }
	.check.on { background: var(--primary); border-color: var(--primary); }
	.input { background: #F7F5EF; border-radius: 12rpx; padding: 16rpx 20rpx; font-size: 28rpx; margin-bottom: 16rpx; }
	.textarea { width: 100%; box-sizing: border-box; background: #F7F5EF; border-radius: 12rpx; padding: 16rpx 20rpx; font-size: 28rpx; height: 140rpx; }
	.count { display: block; text-align: right; font-size: 22rpx; color: #B0B0AB; margin-top: 8rpx; }
	.switch-row { display: flex; align-items: center; justify-content: space-between; gap: 20rpx; margin-top: 20rpx; }
	.switch-info { flex: 1; min-width: 0; }
	.switch-label { display: block; font-size: 28rpx; color: #55554F; }
	.switch-tip { display: block; font-size: 21rpx; color: #B0B0AB; line-height: 1.4; margin-top: 4rpx; }
	.board-entry { display: flex; align-items: center; justify-content: space-between; margin: 0 20rpx 20rpx; padding: 24rpx 28rpx; background: #FFFFFF; border-radius: 20rpx; box-shadow: 0 4rpx 16rpx rgba(0,0,0,0.04); }
	.board-entry-text { font-size: 28rpx; color: var(--primary); font-weight: 500; }
	.pub-tag { font-size: 20rpx; padding: 4rpx 14rpx; border-radius: 8rpx; background: #F1EFE9; color: #8A8A86; margin: 0 12rpx; flex-shrink: 0; }
	.pub-tag.on { background: var(--primary-bg); color: var(--primary); }
	.actions { margin: 20rpx; }
	.share-row { display: flex; align-items: center; justify-content: space-between; padding: 18rpx 0; border-bottom: 1rpx solid #F0EFEA; }
	.share-row:last-child { border-bottom: none; }
	.share-info { flex: 1; min-width: 0; }
	.share-title { font-size: 28rpx; font-weight: 600; display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.share-sub { font-size: 22rpx; color: #8A8A86; }
	.share-arrow { font-size: 36rpx; color: #C4C2BB; margin-left: 16rpx; }
	.modal-mask { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 999; display: flex; align-items: center; justify-content: center; }
	.preview-modal { width: 600rpx; max-height: 84vh; background: #FFFFFF; border-radius: 24rpx; padding: 30rpx; display: flex; flex-direction: column; align-items: center; }
	.modal-title { font-size: 32rpx; font-weight: 600; margin-bottom: 20rpx; }
	.share-image { width: 460rpx; height: 600rpx; border-radius: 12rpx; background: #F7F5EF; }
	.modal-btn { margin-top: 20rpx; width: 100%; }
</style>
