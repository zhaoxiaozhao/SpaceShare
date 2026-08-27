<template>
	<page-meta :page-style="pageThemeStyle" />
		<view>
		<!-- 今日阅读卡片 -->
		<view class="card today-card" :class="{ active: stats.activeSession }">
			<text class="today-label">{{stats.activeSession ? '正在阅读' : '今日阅读'}}</text>
			<text class="today-time">{{formatMinutes(stats.activeSession ? activeElapsed : stats.todayMinutes)}}</text>
			<text class="today-sub">
				{{stats.activeSession ? '本次已读' : '今天已读'}}
				<text v-if="stats.consecutiveDays > 0"> · 连续阅读 {{stats.consecutiveDays}} 天</text>
				<text v-else> · 本周 {{stats.weekMinutes}} 分钟</text>
			</text>
			<text class="today-book" v-if="stats.activeSession">📖 {{stats.activeSession.bookTitle}}</text>
		</view>

		<!-- 正在阅读的书（计时中） -->
		<view class="card" v-if="stats.activeSession">
			<view class="active-info">
				<text class="active-book">📖 {{stats.activeSession.bookTitle}}</text>
				<text class="active-time">{{activeElapsedText}}</text>
			</view>
			<button class="btn-outline end-btn" @click="endReading">结束阅读</button>
		</view>

		<!-- 统计概览 -->
		<view class="stat-row">
			<view class="stat">
				<text class="stat-num">{{stats.todayMinutes}}</text>
				<text class="stat-label">今日阅读</text>
			</view>
			<view class="stat">
				<text class="stat-num">{{stats.weekMinutes}}</text>
				<text class="stat-label">本周阅读</text>
			</view>
			<view class="stat">
				<text class="stat-num">{{stats.totalMinutes}}</text>
				<text class="stat-label">累计分钟</text>
			</view>
		</view>

		<!-- 快捷入口 -->
		<view class="quick-row">
			<view class="quick-btn" @click="goStats">
				<text class="quick-icon">📅</text>
				<text>阅读日历</text>
			</view>
			<view class="quick-btn" @click="goHistory">
				<text class="quick-icon">🕐</text>
				<text>阅读历史</text>
			</view>
			<view class="quick-btn" @click="goYearly">
				<text class="quick-icon">📊</text>
				<text>年度报告</text>
			</view>
		</view>

		<!-- 书籍列表 -->
		<view class="section-head">
			<text class="section-title">我的书籍</text>
			<text class="add-btn" @click="openEdit()">＋ 添加书籍</text>
		</view>
		<view class="filter-row">
			<text class="filter-tab" :class="{ active: filter === '' }" @click="switchFilter('')">全部</text>
			<text class="filter-tab" :class="{ active: filter === 'Reading' }" @click="switchFilter('Reading')">在读</text>
			<text class="filter-tab" :class="{ active: filter === 'WantToRead' }" @click="switchFilter('WantToRead')">想读</text>
			<text class="filter-tab" :class="{ active: filter === 'Finished' }" @click="switchFilter('Finished')">已读</text>
		</view>

		<view v-if="list.books.length">
			<view class="card book-card" v-for="b in list.books" :key="b.id" @click="goBook(b.id)">
				<image class="book-cover" :src="b.coverUrl || '/static/logo.png'" mode="aspectFill" />
				<view class="book-info">
					<view class="book-top">
						<text class="book-title">{{b.title}}</text>
						<text class="book-status" :class="'st-' + b.status">{{statusText(b.status)}}</text>
					</view>
					<text class="book-author" v-if="b.author">{{b.author}}</text>
					<template v-if="b.totalPages">
						<view class="progress-bar">
							<view class="progress-fill" :style="{ width: Math.min(b.progressPercent, 100) + '%' }"></view>
						</view>
						<view class="book-meta-row">
							<text class="book-progress">{{b.currentProgress}}/{{b.totalPages}}页 · {{b.progressPercent}}%</text>
							<text class="book-minutes" v-if="b.totalMinutes">已读 {{b.totalMinutes}} 分钟</text>
						</view>
					</template>
					<text class="book-minutes" v-else-if="b.totalMinutes">累计阅读 {{b.totalMinutes}} 分钟</text>
				</view>
			</view>
		</view>
		<view v-else class="empty">还没有书籍，点右上角「添加书籍」开始记录吧</view>

		<!-- 添加/编辑书籍弹窗 -->
		<view class="modal-mask" v-if="editVisible" @click="editVisible = false">
			<view class="modal" @click.stop>
				<text class="modal-title">{{editingId ? '编辑书籍' : '添加书籍'}}</text>
				<input class="modal-input" v-model="editForm.title" placeholder="书名（必填）" />
				<input class="modal-input" v-model="editForm.author" placeholder="作者" />
				<view class="cover-row" @click="chooseCover">
					<image class="cover-preview" :src="editForm.coverUrl || '/static/logo.png'" mode="aspectFill" />
					<text class="cover-btn">{{editForm.coverUrl ? '更换封面' : '选择封面'}}</text>
				</view>
				<view class="status-row">
					<text
						class="status-chip"
						:class="{ active: editForm.status === s }"
						v-for="s in ['WantToRead', 'Reading', 'Finished']"
						:key="s"
						@click="editForm.status = s"
					>{{statusText(s)}}</text>
				</view>
				<view class="num-row">
					<text class="num-label">当前进度（页）</text>
					<input class="num-input" type="number" v-model="editForm.currentProgress" placeholder="0" />
					<text class="num-label">总页数</text>
					<input class="num-input" type="number" v-model="editForm.totalPages" placeholder="可选" />
				</view>
				<input class="modal-input" v-model="editForm.lastPosition" placeholder="上次阅读位置，如：第3章" />
				<picker class="venue-picker" :range="venueNames" @change="onVenueChange">
					<view class="venue-picker-box">
						<text class="venue-picker-label">常阅读场馆</text>
						<text class="venue-picker-value" :class="{ none: !editForm.venueName }">{{editForm.venueName || '选择（可选）'}}</text>
						<text class="venue-picker-clear" v-if="editForm.venueId" @click.stop="clearVenue">清除</text>
					</view>
				</picker>
				<view class="modal-actions">
					<button class="btn-outline small" @click="editVisible = false">取消</button>
					<button class="btn-primary small" @click="saveBook">保存</button>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { uploadAvatar } from '../../utils/profile.js'

	export default {
		data() {
			return {
				list: { books: [], readingCount: 0, finishedCount: 0, wantToReadCount: 0, todayMinutes: 0 },
				stats: { todayMinutes: 0, weekMinutes: 0, totalMinutes: 0, consecutiveDays: 0, activeSession: null },
				filter: '',
				editVisible: false,
				editingId: null,
				editForm: { title: '', author: '', coverUrl: '', venueId: null, venueName: '', status: 'WantToRead', currentProgress: 0, totalPages: '', lastPosition: '' },
				venues: [],
				timer: null,
				activeElapsed: 0,
				activeStartedAt: null
			}
		},
		computed: {
			venueNames() {
				return this.venues.map(v => v.name)
			}
		},
		onShow() {
			this.load()
		},
		onUnload() {
			if (this.timer) clearInterval(this.timer)
		},
		methods: {
			formatMinutes(min) {
				if (min >= 60) return `${Math.floor(min / 60)}h${min % 60 ? (min % 60) + 'm' : ''}`
				return `${min}m`
			},
			statusText(s) {
				const map = { WantToRead: '想读', Reading: '在读', Finished: '已读' }
				return map[s] || s
			},
			async load() {
				try {
					const [listRes, statsRes] = await Promise.all([
						api.getReadingBooks(this.filter),
						api.getReadingStats()
					])
					this.list = listRes
					this.stats = statsRes
					if (statsRes.activeSession) {
						this.activeStartedAt = new Date(statsRes.activeSession.startedAt)
						this.startTimer()
					} else {
						this.stopTimer()
					}
				} catch (e) {}
				if (!this.venues.length) {
					try {
						this.venues = await api.getVenues({})
					} catch (e) {}
				}
			},
			startTimer() {
				if (this.timer) clearInterval(this.timer)
				this.timer = setInterval(() => {
					if (this.activeStartedAt) {
						this.activeElapsed = Math.floor((Date.now() - this.activeStartedAt.getTime()) / 60000)
					}
				}, 60000)
			},
			stopTimer() {
				if (this.timer) clearInterval(this.timer)
				this.timer = null
				this.activeElapsed = 0
			},
			get activeElapsedText() {
				return this.formatMinutes(this.activeElapsed || 0)
			},
			switchFilter(f) {
				if (this.filter === f) return
				this.filter = f
				this.load()
			},
			async endReading() {
				try {
					await api.endActiveReading({})
					uni.showToast({ title: '已结束阅读', icon: 'success' })
					this.load()
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			},
			goBook(id) {
				uni.navigateTo({ url: `/pages/reading/book?id=${id}` })
			},
			goStats() {
				uni.navigateTo({ url: '/pages/reading/stats' })
			},
			goHistory() {
				uni.navigateTo({ url: '/pages/reading/history' })
			},
			goYearly() {
				uni.navigateTo({ url: '/pages/reading/yearly' })
			},
			openEdit(book) {
				this.editingId = book ? book.id : null
				this.editForm = book
					? { title: book.title, author: book.author || '', coverUrl: book.coverUrl || '', venueId: book.venueId || null, venueName: book.venueName || '', status: book.status, currentProgress: book.currentProgress, totalPages: book.totalPages || '', lastPosition: book.lastPosition || '' }
					: { title: '', author: '', coverUrl: '', venueId: null, venueName: '', status: 'WantToRead', currentProgress: 0, totalPages: '', lastPosition: '' }
				this.editVisible = true
			},
			onVenueChange(e) {
				const idx = Number(e.detail.value)
				const v = this.venues[idx]
				if (v) {
					this.editForm.venueId = v.id
					this.editForm.venueName = v.name
				}
			},
			clearVenue() {
				this.editForm.venueId = null
				this.editForm.venueName = ''
			},
			chooseCover() {
				uni.chooseImage({
					count: 1,
					sizeType: ['compressed'],
					success: async (res) => {
						const filePath = res.tempFilePaths[0]
						if (!filePath) return
						uni.showLoading({ title: '上传中', mask: true })
						try {
							this.editForm.coverUrl = await uploadAvatar(filePath)
							uni.hideLoading()
						} catch (e) {
							uni.hideLoading()
							uni.showToast({ title: '封面上传失败', icon: 'none' })
						}
					}
				})
			},
			async saveBook() {
				const title = (this.editForm.title || '').trim()
				if (!title) {
					uni.showToast({ title: '请输入书名', icon: 'none' })
					return
				}
				const data = {
					title,
					author: this.editForm.author,
					coverUrl: this.editForm.coverUrl,
					venueId: this.editForm.venueId,
					status: this.editForm.status,
					currentProgress: Number(this.editForm.currentProgress) || 0,
					totalPages: Number(this.editForm.totalPages) || null,
					lastPosition: this.editForm.lastPosition
				}
				try {
					if (this.editingId) {
						await api.updateReadingBook(this.editingId, data)
					} else {
						await api.addReadingBook(data)
					}
					this.editVisible = false
					uni.showToast({ title: '已保存', icon: 'success' })
					this.load()
				} catch (e) {
					uni.showToast({ title: e.message || '保存失败', icon: 'none' })
				}
			}
		}
	}
</script>

<style scoped>
	/* 今日阅读卡片 */
	.today-card { background: linear-gradient(135deg, var(--primary) 0%, var(--primary-light) 100%); color: #FFFFFF; }
	.today-label { font-size: 26rpx; opacity: 0.9; display: block; }
	.today-time { font-size: 64rpx; font-weight: 700; margin: 8rpx 0; display: block; }
	.today-sub { font-size: 24rpx; opacity: 0.9; display: block; }
	.today-book { display: block; margin-top: 12rpx; font-size: 26rpx; font-weight: 500; }

	/* 正在阅读（计时中） */
	.active-info { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20rpx; }
	.active-book { font-size: 30rpx; font-weight: 600; }
	.active-time { font-size: 32rpx; font-weight: 700; color: var(--primary); }
	.end-btn { margin-top: 10rpx; }

	/* 统计概览（无背景卡片，紧凑三列） */
	.stat-row { display: flex; background: #FFFFFF; border-radius: 20rpx; margin: 20rpx; padding: 24rpx 0; box-shadow: 0 4rpx 16rpx rgba(0,0,0,0.04); }
	.stat { flex: 1; display: flex; flex-direction: column; align-items: center; gap: 4rpx; }
	.stat-num { font-size: 40rpx; font-weight: 700; color: var(--primary); }
	.stat-label { font-size: 24rpx; color: #8A8A86; }

	/* 快捷入口 */
	.quick-row { display: flex; gap: 16rpx; margin: 0 20rpx; }
	.quick-btn { flex: 1; background: #FFFFFF; border-radius: 16rpx; padding: 24rpx 0; display: flex; flex-direction: column; align-items: center; gap: 10rpx; font-size: 26rpx; color: #55554F; box-shadow: 0 4rpx 16rpx rgba(0,0,0,0.04); }
	.quick-icon { font-size: 36rpx; }

	/* 书籍列表标题 + 添加 */
	.section-head { display: flex; align-items: center; justify-content: space-between; margin: 30rpx 20rpx 6rpx; }
	.section-title { font-size: 32rpx; font-weight: 700; color: #2B2B27; }
	.add-btn { font-size: 26rpx; color: var(--primary); font-weight: 500; }

	/* 状态筛选 */
	.filter-row { display: flex; align-items: center; gap: 12rpx; margin: 10rpx 20rpx 0; }
	.filter-tab { font-size: 26rpx; color: #8A8A86; padding: 10rpx 24rpx; border-radius: 24rpx; background: #F1EFE9; }
	.filter-tab.active { background: var(--primary); color: #FFFFFF; }

	/* 书籍卡片 */
	.book-card { display: flex; gap: 24rpx; align-items: stretch; }
	.book-cover { width: 110rpx; height: 150rpx; border-radius: 10rpx; background: var(--primary-bg); flex-shrink: 0; }
	.book-info { flex: 1; display: flex; flex-direction: column; justify-content: center; gap: 8rpx; min-width: 0; }
	.book-top { display: flex; align-items: center; justify-content: space-between; gap: 12rpx; }
	.book-title { font-size: 30rpx; font-weight: 600; flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.book-author { font-size: 24rpx; color: #8A8A86; }
	.book-status { font-size: 20rpx; flex-shrink: 0; padding: 4rpx 16rpx; border-radius: 8rpx; }
	.st-WantToRead { background: #F1EFE9; color: #8A8A86; }
	.st-Reading { background: var(--primary-bg); color: var(--primary); }
	.st-Finished { background: #E8F1E8; color: #4A7A4A; }
	.progress-bar { height: 8rpx; background: #F1EFE9; border-radius: 4rpx; overflow: hidden; }
	.progress-fill { height: 100%; background: var(--primary); border-radius: 4rpx; }
	.book-meta-row { display: flex; justify-content: space-between; align-items: center; }
	.book-progress { font-size: 22rpx; color: #8A8A86; }
	.book-minutes { font-size: 22rpx; color: var(--primary); }

	/* 弹窗 */
	.modal-mask { position: fixed; inset: 0; background: rgba(0,0,0,0.45); z-index: 999; display: flex; align-items: center; justify-content: center; }
	.modal { width: 640rpx; background: #FFFFFF; border-radius: 20rpx; padding: 30rpx; }
	.modal-title { font-size: 32rpx; font-weight: 700; margin-bottom: 20rpx; display: block; }
	.modal-input { background: #F7F5EF; border-radius: 12rpx; padding: 16rpx 20rpx; font-size: 28rpx; margin-bottom: 16rpx; }
	.cover-row { display: flex; align-items: center; gap: 20rpx; margin-bottom: 16rpx; }
	.cover-preview { width: 100rpx; height: 130rpx; border-radius: 8rpx; background: var(--primary-bg); }
	.cover-btn { font-size: 26rpx; color: var(--primary); }
	.status-row { display: flex; gap: 12rpx; margin-bottom: 16rpx; }
	.status-chip { padding: 8rpx 20rpx; border-radius: 20rpx; font-size: 24rpx; background: #F1EFE9; color: #8A8A86; }
	.status-chip.active { background: var(--primary); color: #FFFFFF; }
	.num-row { display: flex; align-items: center; gap: 12rpx; margin-bottom: 16rpx; }
	.num-label { font-size: 24rpx; color: #8A8A86; }
	.num-input { width: 110rpx; background: #F7F5EF; border-radius: 8rpx; padding: 10rpx; text-align: center; font-size: 26rpx; }
	.modal-actions { display: flex; gap: 16rpx; justify-content: flex-end; margin-top: 10rpx; }
	.venue-picker { margin-bottom: 16rpx; }
	.venue-picker-box { background: #F7F5EF; border-radius: 12rpx; padding: 16rpx 20rpx; display: flex; align-items: center; gap: 12rpx; }
	.venue-picker-label { font-size: 26rpx; color: #55554F; }
	.venue-picker-value { flex: 1; font-size: 26rpx; color: var(--primary); text-align: right; }
	.venue-picker-value.none { color: #B0B0AB; }
	.venue-picker-clear { font-size: 22rpx; color: #B85450; }
</style>