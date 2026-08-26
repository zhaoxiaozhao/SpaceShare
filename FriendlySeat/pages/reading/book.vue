<template>
	<view v-if="book">
		<!-- 书籍信息 -->
		<view class="card book-header">
			<image class="cover" :src="book.coverUrl || '/static/logo.png'" mode="aspectFill" />
			<view class="info">
				<text class="title">{{book.title}}</text>
				<text class="author" v-if="book.author">{{book.author}}</text>
				<text class="status" :class="'st-' + book.status">{{statusText(book.status)}}</text>
				<text class="minutes" v-if="book.totalMinutes">累计阅读 {{book.totalMinutes}} 分钟</text>
				<text class="venue" v-if="book.venueName">📍 {{book.venueName}}</text>
			</view>
		</view>

		<!-- 阅读操作 -->
		<view class="card">
			<template v-if="book.hasActiveSession">
				<text class="reading-now">📖 正在阅读中…</text>
				<text class="reading-venue" v-if="activeVenueName">📍 {{activeVenueName}}</text>
				<button class="btn-outline end-btn" @click="endReading">结束阅读</button>
			</template>
			<template v-else>
				<picker class="venue-picker" :range="venueNames" @change="onVenueChange">
					<view class="venue-picker-box">
						<text class="venue-picker-label">本次阅读场馆</text>
						<text class="venue-picker-value" :class="{ none: !selectedVenueName }">{{selectedVenueName || (book.venueName || '选择（可选）')}}</text>
					</view>
				</picker>
				<button class="btn-primary start-btn" @click="startReading">开始阅读</button>
			</template>
		</view>

		<!-- 进度 -->
		<view class="card">
			<text class="section-label">阅读进度</text>
			<view class="progress-row">
				<text class="plabel">当前进度</text>
				<input class="pinput" type="number" v-model="progressInput" placeholder="页数" />
				<text class="ptotal" v-if="book.totalPages">/ {{book.totalPages}}页</text>
			</view>
			<input class="pos-input" v-model="positionInput" placeholder="上次阅读位置，如：第3章" />
			<button class="btn-outline save-progress" @click="saveProgress">保存进度</button>
			<text class="progress-percent" v-if="book.totalPages">已读 {{book.progressPercent}}%</text>
		</view>

		<!-- 摘抄与笔记 -->
		<view class="card">
			<view class="note-head">
				<text class="section-label">摘抄与笔记</text>
				<text class="add-note" @click="noteType = 'Highlight'">＋ 摘抄</text>
				<text class="add-note" @click="noteType = 'Note'">＋ 笔记</text>
			</view>
			<view class="note-input-row">
				<input class="note-input" v-model="noteContent" :placeholder="noteType === 'Highlight' ? '摘抄书中句子' : '记录你的阅读笔记'" />
				<input class="note-pos" v-model="notePosition" placeholder="位置(可选)" />
				<button class="btn-primary small note-add" @click="addNote">添加</button>
			</view>
			<view class="note-tabs">
				<text class="ntab" :class="{ active: noteFilter === '' }" @click="noteFilter = ''; loadNotes()">全部</text>
				<text class="ntab" :class="{ active: noteFilter === 'Highlight' }" @click="noteFilter = 'Highlight'; loadNotes()">摘抄</text>
				<text class="ntab" :class="{ active: noteFilter === 'Note' }" @click="noteFilter = 'Note'; loadNotes()">笔记</text>
			</view>
			<view v-if="notes.length">
				<view class="note-item" v-for="n in notes" :key="n.id">
					<view class="note-top">
						<text class="note-type">{{n.type === 'Highlight' ? '摘抄' : '笔记'}}</text>
						<text class="note-del" @click="removeNote(n.id)">删除</text>
					</view>
					<text class="note-content">{{n.content}}</text>
					<text class="note-pos-text" v-if="n.position">{{n.position}}</text>
				</view>
			</view>
			<text v-else class="empty">暂无摘抄或笔记</text>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

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
				positionInput: '',
				venues: [],
				selectedVenueId: null,
				selectedVenueName: '',
				activeVenueName: ''
			}
		},
		computed: {
			venueNames() {
				return this.venues.map(v => v.name)
			}
		},
		onLoad(options) {
			this.id = options.id
		},
		onShow() {
			this.load()
		},
		methods: {
			statusText(s) {
				const map = { WantToRead: '想读', Reading: '在读', Finished: '已读' }
				return map[s] || s
			},
			async load() {
				try {
					this.book = await api.getReadingBook(this.id)
					this.progressInput = this.book.currentProgress
					this.positionInput = this.book.lastPosition || ''
					if (this.book.hasActiveSession) {
						// 从 stats 获取当前会话场馆
						try {
							const stats = await api.getReadingStats()
							if (stats.activeSession && stats.activeSession.venueName) {
								this.activeVenueName = stats.activeSession.venueName
							}
						} catch (e) {}
					}
					await this.loadNotes()
				} catch (e) {}
				if (!this.venues.length) {
					try {
						this.venues = await api.getVenues({})
					} catch (e) {}
				}
			},
			onVenueChange(e) {
				const idx = Number(e.detail.value)
				const v = this.venues[idx]
				if (v) {
					this.selectedVenueId = v.id
					this.selectedVenueName = v.name
				}
			},
			async loadNotes() {
				try {
					this.notes = await api.getReadingNotes(this.id, this.noteFilter)
				} catch (e) {}
			},
			async startReading() {
				try {
					await api.startReading(this.id, { venueId: this.selectedVenueId })
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
						status: this.book.status,
						currentProgress: Number(this.progressInput) || 0,
						totalPages: this.book.totalPages,
						lastPosition: this.positionInput
					})
					uni.showToast({ title: '已保存', icon: 'success' })
					this.load()
				} catch (e) {
					uni.showToast({ title: e.message || '保存失败', icon: 'none' })
				}
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
	.book-header { display: flex; gap: 24rpx; }
	.cover { width: 160rpx; height: 220rpx; border-radius: 10rpx; background: #EAF3F0; flex-shrink: 0; }
	.info { flex: 1; display: flex; flex-direction: column; gap: 8rpx; }
	.title { font-size: 34rpx; font-weight: 700; }
	.author { font-size: 26rpx; color: #8A8A86; }
	.status { align-self: flex-start; font-size: 22rpx; padding: 2rpx 14rpx; border-radius: 8rpx; }
	.st-WantToRead { background: #F1EFE9; color: #8A8A86; }
	.st-Reading { background: #EAF3F0; color: #3A8A7E; }
	.st-Finished { background: #E8F1E8; color: #4A7A4A; }
	.minutes { font-size: 24rpx; color: #3A8A7E; }
	.venue { font-size: 24rpx; color: #55554F; }
	.reading-now { display: block; text-align: center; font-size: 30rpx; font-weight: 600; color: #3A8A7E; margin-bottom: 20rpx; }
	.reading-venue { display: block; text-align: center; font-size: 26rpx; color: #8A8A86; margin-bottom: 16rpx; }
	.venue-picker { margin-bottom: 16rpx; }
	.venue-picker-box { background: #F7F5EF; border-radius: 12rpx; padding: 16rpx 20rpx; display: flex; align-items: center; }
	.venue-picker-label { font-size: 26rpx; color: #55554F; }
	.venue-picker-value { flex: 1; font-size: 26rpx; color: #3A8A7E; text-align: right; }
	.venue-picker-value.none { color: #B0B0AB; }
	.start-btn, .end-btn { margin-top: 10rpx; }
	.progress-row { display: flex; align-items: center; gap: 12rpx; margin-bottom: 12rpx; }
	.plabel { font-size: 26rpx; color: #55554F; }
	.pinput { width: 120rpx; background: #F7F5EF; border-radius: 8rpx; padding: 10rpx; text-align: center; font-size: 28rpx; }
	.ptotal { font-size: 24rpx; color: #8A8A86; }
	.pos-input { background: #F7F5EF; border-radius: 8rpx; padding: 14rpx 18rpx; font-size: 26rpx; width: 100%; box-sizing: border-box; margin-bottom: 12rpx; }
	.save-progress { margin-top: 6rpx; }
	.progress-percent { display: block; text-align: center; font-size: 24rpx; color: #3A8A7E; margin-top: 12rpx; }
	.note-head { display: flex; align-items: center; gap: 16rpx; margin-bottom: 16rpx; }
	.add-note { font-size: 24rpx; color: #3A8A7E; }
	.note-input-row { display: flex; gap: 8rpx; align-items: center; margin-bottom: 16rpx; flex-wrap: wrap; }
	.note-input { flex: 1; min-width: 200rpx; background: #F7F5EF; border-radius: 8rpx; padding: 12rpx 16rpx; font-size: 26rpx; }
	.note-pos { width: 140rpx; background: #F7F5EF; border-radius: 8rpx; padding: 12rpx; font-size: 22rpx; }
	.note-add { margin: 0; }
	.note-tabs { display: flex; gap: 12rpx; margin-bottom: 12rpx; }
	.ntab { font-size: 24rpx; color: #8A8A86; padding: 4rpx 14rpx; border-radius: 16rpx; }
	.ntab.active { background: #3A8A7E; color: #FFFFFF; }
	.note-item { border-top: 1rpx solid #F0EFEA; padding: 16rpx 0; }
	.note-top { display: flex; justify-content: space-between; margin-bottom: 6rpx; }
	.note-type { font-size: 22rpx; color: #3A8A7E; }
	.note-del { font-size: 22rpx; color: #B85450; }
	.note-content { display: block; font-size: 28rpx; line-height: 1.6; }
	.note-pos-text { display: block; font-size: 22rpx; color: #B0B0AB; margin-top: 6rpx; }
</style>