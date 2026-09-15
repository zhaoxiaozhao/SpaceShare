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
					<image class="cover" :src="b.coverUrl || '/static/logo.png'" mode="aspectFill" />
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
		</view>

		<view class="actions">
			<button class="btn-primary" :loading="creating" @click="generate">生成书单海报</button>
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

		<canvas class="poster-canvas" canvas-id="posterCard" :style="{ width: '640px', height: canvasH + 'px', position: 'fixed', left: '-9999px', top: '0' }"></canvas>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { getSeasonKey, getTheme } from '../../utils/theme.js'

	const STATUS_LABELS = { WantToRead: '想读', Reading: '在读', Finished: '已读' }
	const COVER_COLORS = ['#6BAF8B', '#2E8B94', '#C98A3D', '#5B6E8C', '#8CC5A8', '#DBA968', '#7E90AC', '#57AFB8']

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
				canvasH: 1200
			}
		},
		onLoad() {
			this.load()
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
						remark: this.remark
					})
					this.token = share.token
					await this.drawPoster(share)
					uni.hideLoading()
					this.showPreview = true
				} catch (e) {
					uni.hideLoading()
					uni.showToast({ title: e.message || '生成失败，请重试', icon: 'none' })
				} finally {
					this.creating = false
				}
			},
			wrap(ctx, text, x, y, maxWidth, lineHeight) {
				const chars = (text || '').split('')
				let line = ''
				let lines = 0
				for (const ch of chars) {
					const test = line + ch
					if (ctx.measureText(test).width > maxWidth && line) {
						ctx.fillText(line, x, y + lines * lineHeight)
						line = ch
						lines++
					} else {
						line = test
					}
				}
				if (line) {
					ctx.fillText(line, x, y + lines * lineHeight)
					lines++
				}
				return lines
			},
			drawPoster(share) {
				return new Promise((resolve, reject) => {
					const books = share.books || []
					const show = books.slice(0, 12)
					const more = books.length - show.length
					const hasRemark = !!(share.remark && share.remark.length)
					const rowH = 84
					const H = 300 + (hasRemark ? 90 : 0) + show.length * rowH + (more > 0 ? 40 : 0) + 150
					this.canvasH = H

					this.$nextTick(() => {
						const ctx = uni.createCanvasContext('posterCard', this)
						const W = 640
						const theme = getTheme()
						const primary = theme.primary
						const dark = '#2B2B27'
						const gray = '#8A8A86'
						const soft = '#F1EFE9'

						ctx.setFillStyle('#F7F5EF')
						ctx.fillRect(0, 0, W, H)

						// 顶部品牌区
						ctx.setFillStyle(primary)
						ctx.fillRect(0, 0, W, 240)
						ctx.setFillStyle('rgba(255,255,255,0.12)')
						ctx.beginPath()
						ctx.arc(560, 40, 150, 0, 2 * Math.PI)
						ctx.fill()
						ctx.beginPath()
						ctx.arc(30, 210, 90, 0, 2 * Math.PI)
						ctx.fill()

						ctx.setTextAlign('left')
						ctx.setFillStyle('#FFFFFF')
						ctx.setFontSize(40)
						ctx.fillText(share.title || '我的书单', 40, 100)

						ctx.setFontSize(24)
						ctx.setFillStyle('rgba(255,255,255,0.85)')
						const totalHours = Math.round((share.totalMinutes || 0) / 60)
						ctx.fillText(`${share.ownerName || '书友'} · 共 ${share.count || books.length} 本 · ${totalHours} 小时`, 40, 150)
						ctx.fillText(`友邻座 · ${new Date().getFullYear()}`, 40, 190)

						let y = 300
						if (hasRemark) {
							ctx.setFillStyle(dark)
							ctx.setFontSize(26)
							const lines = Math.min(2, this.wrap(ctx, share.remark, 40, y, W - 80, 36))
							y += lines * 36 + 34
						}

						// 书单条目
						show.forEach((b, i) => {
							ctx.setFillStyle('#FFFFFF')
							ctx.fillRect(40, y - 30, W - 80, rowH - 14)

							// 生成封面（书名首字 + 主题色）
							const color = COVER_COLORS[i % COVER_COLORS.length]
							ctx.setFillStyle(color)
							ctx.fillRect(56, y - 16, 52, 52)
							ctx.setFillStyle('#FFFFFF')
							ctx.setFontSize(26)
							ctx.setTextAlign('center')
							ctx.fillText((b.title || '书').slice(0, 1), 82, y + 18)
							ctx.setTextAlign('left')

							ctx.setFillStyle(dark)
							ctx.setFontSize(28)
							let title = b.title || ''
							while (ctx.measureText(title).width > W - 240 && title.length > 1) {
								title = title.slice(0, -1)
							}
							ctx.fillText(title, 128, y + 6)

							ctx.setFillStyle(gray)
							ctx.setFontSize(22)
							const sub = [b.author, STATUS_LABELS[b.status] || ''].filter(Boolean).join(' · ')
							ctx.fillText(sub, 128, y + 36)

							y += rowH
						})

						if (more > 0) {
							ctx.setFillStyle(gray)
							ctx.setFontSize(24)
							ctx.fillText(`等共 ${books.length} 本`, 40, y + 6)
							y += 40
						}

						ctx.setTextAlign('center')
						ctx.setFillStyle(soft)
						ctx.fillRect(0, H - 90, W, 90)
						ctx.setFillStyle(gray)
						ctx.setFontSize(22)
						ctx.fillText('一席相邻，善意相续 · 友邻座', W / 2, H - 48)

						ctx.draw(false, () => {
							setTimeout(() => {
								uni.canvasToTempFilePath({
									canvasId: 'posterCard',
									width: W,
									height: H,
									destWidth: W * 2,
									destHeight: H * 2,
									success: (res) => {
										this.posterPath = res.tempFilePath
										resolve()
									},
									fail: reject
								}, this)
							}, 300)
						})
					})
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
	.cover { width: 72rpx; height: 96rpx; border-radius: 8rpx; background: var(--primary-bg); flex-shrink: 0; }
	.book-info { flex: 1; min-width: 0; }
	.book-title { font-size: 28rpx; font-weight: 600; display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.book-author { font-size: 22rpx; color: #8A8A86; }
	.check { width: 40rpx; height: 40rpx; border-radius: 50%; border: 2rpx solid #D5D3CC; flex-shrink: 0; }
	.check.on { background: var(--primary); border-color: var(--primary); }
	.input { background: #F7F5EF; border-radius: 12rpx; padding: 16rpx 20rpx; font-size: 28rpx; margin-bottom: 16rpx; }
	.textarea { width: 100%; box-sizing: border-box; background: #F7F5EF; border-radius: 12rpx; padding: 16rpx 20rpx; font-size: 28rpx; height: 140rpx; }
	.count { display: block; text-align: right; font-size: 22rpx; color: #B0B0AB; margin-top: 8rpx; }
	.actions { margin: 20rpx; }
	.modal-mask { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 999; display: flex; align-items: center; justify-content: center; }
	.preview-modal { width: 600rpx; max-height: 84vh; background: #FFFFFF; border-radius: 24rpx; padding: 30rpx; display: flex; flex-direction: column; align-items: center; }
	.modal-title { font-size: 32rpx; font-weight: 600; margin-bottom: 20rpx; }
	.share-image { width: 460rpx; height: 600rpx; border-radius: 12rpx; background: #F7F5EF; }
	.modal-btn { margin-top: 20rpx; width: 100%; }
</style>
