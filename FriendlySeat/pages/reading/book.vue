<template>
	<view v-if="book" class="page">
		<!-- 书籍头部 -->
		<view class="book-hero">
			<image class="cover" :src="book.coverUrl || '/static/logo.png'" mode="aspectFill" />
			<view class="book-main">
				<text class="title">{{book.title}}</text>
				<text class="author" v-if="book.author">{{book.author}}</text>
				<view class="status-row">
					<text class="status" :class="'st-' + book.status">{{statusText(book.status)}}</text>
					<text class="meta" v-if="book.totalMinutes">已读 {{book.totalMinutes}} 分钟</text>
				</view>
				<picker :range="venueNames" @change="onVenueChange">
					<text class="venue" :class="{ 'venue-editable': true }">📍 {{book.venueName || '选择阅读场馆'}} ›</text>
				</picker>
			</view>
		</view>

		<!-- 阅读进度概览 -->
		<view class="card progress-overview" v-if="book.totalPages">
			<view class="po-head">
				<text class="po-label">阅读进度</text>
				<text class="po-percent">{{book.progressPercent}}%</text>
			</view>
			<view class="progress-bar">
				<view class="progress-fill" :style="{ width: Math.min(book.progressPercent, 100) + '%' }"></view>
			</view>
			<text class="po-pages">{{book.currentProgress}} / {{book.totalPages}} 页</text>
		</view>

		<!-- 阅读操作 -->
		<view class="card action-card">
			<template v-if="isReadingNow">
				<view class="reading-banner">
					<text class="banner-icon">📖</text>
					<view class="banner-info">
						<text class="banner-title">正在阅读</text>
						<text class="banner-sub" v-if="activeSession">开始于 {{formatTime(activeSession.startedAt)}}</text>
					</view>
				</view>
				<button class="btn-outline" @click="endReading">结束阅读</button>
			</template>
			<template v-else>
				<button class="btn-primary" @click="startReading">{{book.status === 'WantToRead' ? '开始阅读' : '继续阅读'}}</button>
				<button v-if="book.status !== 'Finished'" class="btn-outline" @click="markFinished">标记读完</button>
			</template>
		</view>

		<!-- 记录进度 -->
		<view class="card">
			<text class="card-title">记录进度</text>
			<view class="field-row">
				<text class="field-label">读到</text>
				<input class="field-input number" type="number" v-model="progressInput" placeholder="0" />
				<text class="field-unit">/</text>
				<input class="field-input number" type="number" v-model="totalPagesInput" placeholder="总页数" />
				<text class="field-unit">页</text>
			</view>
			<view class="field-row">
				<text class="field-label">位置</text>
				<input class="field-input" v-model="positionInput" placeholder="如：第三章" />
			</view>
			<button class="btn-outline save" @click="saveProgress">保存进度</button>
		</view>

		<!-- 摘抄与笔记 -->
		<view class="card">
			<text class="card-title">摘抄与笔记</text>
			<view class="seg-tabs">
				<text class="seg-tab" :class="{ active: noteType === 'Highlight' }" @click="noteType = 'Highlight'">摘抄</text>
				<text class="seg-tab" :class="{ active: noteType === 'Note' }" @click="noteType = 'Note'">笔记</text>
			</view>
			<textarea
				class="note-textarea"
				v-model="noteContent"
				:placeholder="noteType === 'Highlight' ? '摘抄书中让你心动的句子…' : '记录你的阅读心得…'"
				:maxlength="2000"
			/>
			<view class="note-actions">
				<input class="note-pos-input" v-model="notePosition" placeholder="位置（可选）" />
				<button class="btn-primary small" @click="addNote">添加</button>
			</view>
		</view>

		<!-- 笔记列表 -->
		<view class="card" v-if="notes.length">
			<view class="list-tabs">
				<text class="list-tab" :class="{ active: noteFilter === '' }" @click="noteFilter = ''; loadNotes()">全部</text>
				<text class="list-tab" :class="{ active: noteFilter === 'Highlight' }" @click="noteFilter = 'Highlight'; loadNotes()">摘抄</text>
				<text class="list-tab" :class="{ active: noteFilter === 'Note' }" @click="noteFilter = 'Note'; loadNotes()">笔记</text>
			</view>
			<view class="note-item" v-for="n in notes" :key="n.id">
				<view class="note-item-head">
					<text class="note-tag" :class="'nt-' + n.type.toLowerCase()">{{n.type === 'Highlight' ? '摘抄' : '笔记'}}</text>
					<text class="note-time">{{formatTime(n.createdAt)}}</text>
					<text class="note-del" @click="removeNote(n.id)">删除</text>
				</view>
				<text class="note-content" :class="{ 'is-highlight': n.type === 'Highlight' }">{{n.content}}</text>
				<text class="note-pos" v-if="n.position">{{n.position}}</text>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'

	export default {
		data() {
			return {
				id: null,
				book: null,
				notes: [],
				noteFilter: '',
				noteType: 'Highlight',
				noteContent: '',
				notePosition: '',
				progressInput: 0,
				totalPagesInput: '',
				positionInput: '',
				venues: [],
				activeSession: null
			}
		},
		computed: {
			venueNames() {
				return this.venues.map(v => v.name)
			},
			isReadingNow() {
				return !!(this.activeSession && this.activeSession.bookId === Number(this.id))
			}
		},
		onLoad(options) {
			this.id = options.id
		},
		onShow() {
			this.load()
		},
		methods: {
			formatTime,
			statusText(s) {
				const map = { WantToRead: '想读', Reading: '在读', Finished: '已读' }
				return map[s] || s
			},
			async load() {
				try {
					const [bookRes, statsRes] = await Promise.all([
						api.getReadingBook(this.id),
						api.getReadingStats()
					])
					this.book = bookRes
					this.activeSession = statsRes.activeSession || null
					this.progressInput = this.book.currentProgress
					this.totalPagesInput = this.book.totalPages || ''
					this.positionInput = this.book.lastPosition || ''
					await this.loadNotes()
				} catch (e) {}
				if (!this.venues.length) {
					try {
						this.venues = await api.getVenues({})
					} catch (e) {}
				}
			},
			async onVenueChange(e) {
				const idx = Number(e.detail.value)
				const v = this.venues[idx]
				if (!v) return
				try {
					await api.updateReadingBook(this.id, {
						title: this.book.title,
						author: this.book.author,
						coverUrl: this.book.coverUrl,
						venueId: v.id,
						status: this.book.status,
						currentProgress: this.book.currentProgress,
						totalPages: this.book.totalPages,
						lastPosition: this.book.lastPosition
					})
					this.book.venueId = v.id
					this.book.venueName = v.name
					uni.showToast({ title: '已更新场馆', icon: 'success' })
				} catch (e) {
					uni.showToast({ title: e.message || '更新失败', icon: 'none' })
				}
			},
			async loadNotes() {
				try {
					this.notes = await api.getReadingNotes(this.id, this.noteFilter)
				} catch (e) {}
			},
			async startReading() {
				try {
					await api.startReading(this.id, { venueId: this.book.venueId })
					uni.showToast({ title: '开始阅读', icon: 'success' })
					this.load()
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			},
			async endReading() {
				try {
					await api.endReading(this.id, {
						progress: Number(this.progressInput) || 0,
						lastPosition: this.positionInput
					})
					uni.showToast({ title: '已结束阅读', icon: 'success' })
					this.load()
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			},
			async saveProgress() {
				try {
					await api.updateReadingBook(this.id, {
						title: this.book.title,
						author: this.book.author,
						coverUrl: this.book.coverUrl,
						venueId: this.book.venueId,
						status: this.book.status,
						currentProgress: Number(this.progressInput) || 0,
						totalPages: Number(this.totalPagesInput) || null,
						lastPosition: this.positionInput
					})
					uni.showToast({ title: '已保存', icon: 'success' })
					this.load()
				} catch (e) {
					uni.showToast({ title: e.message || '保存失败', icon: 'none' })
				}
			},
			async markFinished() {
				uni.showModal({
					title: '标记读完',
					content: '确定把这本书标记为「已读」吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.updateReadingBook(this.id, {
								title: this.book.title,
								author: this.book.author,
								coverUrl: this.book.coverUrl,
								venueId: this.book.venueId,
								status: 'Finished',
								currentProgress: this.book.totalPages || this.book.currentProgress,
								totalPages: this.book.totalPages,
								lastPosition: this.book.lastPosition
							})
							uni.showToast({ title: '已标记读完', icon: 'success' })
							this.load()
						} catch (e) {
							uni.showToast({ title: e.message || '操作失败', icon: 'none' })
						}
					}
				})
			},
			async addNote() {
				const content = (this.noteContent || '').trim()
				if (!content) {
					uni.showToast({ title: '请输入内容', icon: 'none' })
					return
				}
				try {
					await api.addReadingNote(this.id, { type: this.noteType, content, position: this.notePosition })
					this.noteContent = ''
					this.notePosition = ''
					uni.showToast({ title: '已添加', icon: 'success' })
					this.loadNotes()
				} catch (e) {
					uni.showToast({ title: e.message || '添加失败', icon: 'none' })
				}
			},
			removeNote(noteId) {
				uni.showModal({
					title: '删除',
					content: '确定删除这条记录吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.deleteReadingNote(noteId)
							uni.showToast({ title: '已删除', icon: 'success' })
							this.loadNotes()
						} catch (e) {}
					}
				})
			}
		}
	}
</script>

<style scoped>
	.page { padding-bottom: 40rpx; }

	/* 书籍头部（沉浸式） */
	.book-hero {
		display: flex;
		gap: 28rpx;
		padding: 30rpx;
		background: linear-gradient(135deg, #3A8A7E 0%, #5BA48D 100%);
	}
	.cover {
		width: 150rpx;
		height: 210rpx;
		border-radius: 12rpx;
		background: rgba(255,255,255,0.2);
		flex-shrink: 0;
		box-shadow: 0 8rpx 24rpx rgba(0,0,0,0.2);
	}
	.book-main { flex: 1; display: flex; flex-direction: column; justify-content: center; gap: 10rpx; min-width: 0; }
	.title { font-size: 36rpx; font-weight: 700; color: #FFFFFF; line-height: 1.3; }
	.author { font-size: 26rpx; color: rgba(255,255,255,0.85); }
	.status-row { display: flex; align-items: center; gap: 16rpx; }
	.status { font-size: 22rpx; padding: 4rpx 16rpx; border-radius: 8rpx; }
	.st-WantToRead { background: rgba(255,255,255,0.25); color: #FFFFFF; }
	.st-Reading { background: #FFFFFF; color: #3A8A7E; }
	.st-Finished { background: rgba(255,255,255,0.25); color: #FFFFFF; }
	.meta { font-size: 24rpx; color: rgba(255,255,255,0.85); }
	.venue { font-size: 24rpx; color: rgba(255,255,255,0.9); align-self: flex-start; border-bottom: 1rpx dashed rgba(255,255,255,0.6); padding-bottom: 2rpx; }

	/* 进度概览 */
	.progress-overview { margin-top: -20rpx; }
	.po-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14rpx; }
	.po-label { font-size: 26rpx; color: #55554F; }
	.po-percent { font-size: 32rpx; font-weight: 700; color: #3A8A7E; }
	.progress-bar { height: 12rpx; background: #F1EFE9; border-radius: 6rpx; overflow: hidden; }
	.progress-fill { height: 100%; background: linear-gradient(90deg, #3A8A7E, #5BA48D); border-radius: 6rpx; }
	.po-pages { display: block; margin-top: 12rpx; font-size: 24rpx; color: #8A8A86; }

	/* 阅读操作 */
	.action-card { display: flex; flex-direction: column; align-items: center; gap: 18rpx; }
	.action-card .btn-primary { width: 70%; margin: 0; }
	.action-card .btn-outline { width: 70%; margin: 0; }
	.reading-banner { display: flex; align-items: center; gap: 20rpx; background: #EAF3F0; border-radius: 14rpx; padding: 22rpx; width: 100%; box-sizing: border-box; }
	.banner-icon { font-size: 44rpx; }
	.banner-info { flex: 1; display: flex; flex-direction: column; gap: 4rpx; }
	.banner-title { font-size: 30rpx; font-weight: 600; color: #3A8A7E; }
	.banner-sub { font-size: 22rpx; color: #8A8A86; }

	/* 通用卡片标题 */
	.card-title { font-size: 30rpx; font-weight: 700; color: #2B2B27; display: block; margin-bottom: 20rpx; }

	/* 记录进度 */
	.field-row { display: flex; align-items: center; gap: 14rpx; margin-bottom: 18rpx; }
	.field-label { font-size: 26rpx; color: #55554F; white-space: nowrap; }
	.field-input { flex: 1; min-width: 0; background: #F7F5EF; border-radius: 12rpx; padding: 18rpx 22rpx; font-size: 28rpx; }
	.field-input.number { max-width: 150rpx; }
	.field-unit { font-size: 26rpx; color: #8A8A86; white-space: nowrap; }
	.save { width: 70%; margin: 0 auto; display: block; }

	/* 摘抄与笔记 */
	.seg-tabs { display: inline-flex; background: #F1EFE9; border-radius: 24rpx; padding: 4rpx; margin-bottom: 18rpx; }
	.seg-tab { font-size: 26rpx; color: #8A8A86; padding: 10rpx 36rpx; border-radius: 20rpx; }
	.seg-tab.active { background: #3A8A7E; color: #FFFFFF; font-weight: 600; }
	.note-textarea { width: 100%; min-height: 220rpx; box-sizing: border-box; background: #F7F5EF; border-radius: 14rpx; padding: 22rpx; font-size: 28rpx; line-height: 1.7; margin-bottom: 16rpx; }
	.note-actions { display: flex; align-items: center; gap: 14rpx; }
	.note-pos-input { flex: 1; min-width: 0; background: #F7F5EF; border-radius: 12rpx; padding: 16rpx 20rpx; font-size: 26rpx; }
	.note-actions .btn-primary.small { margin: 0; width: 150rpx; flex-shrink: 0; }

	/* 笔记列表 */
	.list-tabs { display: flex; gap: 12rpx; margin-bottom: 16rpx; }
	.list-tab { font-size: 24rpx; color: #8A8A86; padding: 8rpx 26rpx; border-radius: 20rpx; background: #F1EFE9; }
	.list-tab.active { background: #3A8A7E; color: #FFFFFF; }
	.note-item { padding: 22rpx 0; border-bottom: 1rpx solid #F0EFEA; }
	.note-item:last-child { border-bottom: none; }
	.note-item-head { display: flex; align-items: center; gap: 16rpx; margin-bottom: 10rpx; }
	.note-tag { font-size: 20rpx; padding: 4rpx 14rpx; border-radius: 6rpx; }
	.nt-highlight { background: #FFF4E0; color: #B8860B; }
	.nt-note { background: #EAF3F0; color: #3A8A7E; }
	.note-time { flex: 1; font-size: 22rpx; color: #B0B0AB; }
	.note-del { font-size: 24rpx; color: #B85450; }
	.note-content { display: block; font-size: 28rpx; line-height: 1.7; color: #33332E; }
	.note-content.is-highlight { border-left: 4rpx solid #E6C36A; padding-left: 20rpx; color: #5A5A52; }
	.note-pos { display: block; font-size: 22rpx; color: #B0B0AB; margin-top: 8rpx; }
</style>