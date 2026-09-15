<template>
	<page-meta :page-style="pageThemeStyle" />
	<view v-if="share">
		<!-- 书单头部 -->
		<view class="hero">
			<view class="hero-top">
				<image class="avatar" :src="share.ownerAvatar || '/static/logo.png'" mode="aspectFill" />
				<view class="owner">
					<text class="owner-name">{{share.ownerName || '书友'}}</text>
					<text class="owner-sub">的书单 · {{share.count}} 本 · {{totalHours}} 小时</text>
				</view>
			</view>
			<text class="hero-title">{{share.title}}</text>
			<text class="hero-remark" v-if="share.remark">{{share.remark}}</text>
			<view class="hero-foot">
				<text>友邻座 · 书单分享</text>
				<text>{{share.viewCount}} 次浏览 · {{share.favoriteCount}} 次收藏</text>
			</view>
		</view>

		<!-- 书籍列表 -->
		<view class="section">
			<view class="card book-card" v-for="(b, i) in share.books" :key="b.bookId">
				<text class="index">{{i + 1}}</text>
				<BookCover :url="b.coverUrl" :title="b.title" :width="84" :height="112" :radius="8" />
				<view class="book-info">
					<text class="book-title">{{b.title}}</text>
					<text class="book-author" v-if="b.author">{{b.author}}</text>
					<view class="book-meta">
						<text class="status" :class="'st-' + b.status">{{statusText(b.status)}}</text>
						<text class="minutes" v-if="b.totalMinutes">累计 {{b.totalMinutes}} 分钟</text>
					</view>
				</view>
			</view>
		</view>

		<view class="cta">
			<view class="cta-row" v-if="!share.isOwner">
				<button class="btn-outline fav-btn" :class="{ on: share.favorited }" @click="toggleFavorite">
					{{share.favorited ? '已收藏' : '收藏'}} {{share.favoriteCount}}
				</button>
				<button class="btn-primary share-btn" open-type="share">分享这个书单</button>
			</view>
			<button v-else class="btn-primary" open-type="share">分享这个书单</button>

			<view class="owner-row" v-if="share.isOwner">
				<text class="owner-label">公开到热门书单榜</text>
				<switch :checked="share.isPublic" color="var(--primary)" @change="onPublicChange" />
			</view>

			<button class="btn-outline" @click="goReading">我也要做书单</button>

			<view class="links">
				<text class="link" @click="goBoard">热门书单</text>
				<text class="link danger" v-if="!share.isOwner" @click="goReport">举报</text>
			</view>
		</view>

		<text class="footer">一席相邻，善意相续 · 友邻座</text>
	</view>

	<view v-else-if="loaded" class="empty-state">
		<text>书单不存在或已被删除</text>
	</view>
	<view v-else class="empty-state">
		<text>加载中…</text>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	const STATUS_LABELS = { WantToRead: '想读', Reading: '在读', Finished: '已读' }

	export default {
		data() {
			return {
				token: '',
				share: null,
				loaded: false,
				favoriting: false
			}
		},
		computed: {
			totalHours() {
				if (!this.share) return 0
				return Math.round((this.share.totalMinutes || 0) / 60)
			}
		},
		onLoad(options) {
			this.token = options.token || ''
			this.load()
		},
		onShareAppMessage() {
			return {
				title: this.share ? `${this.share.ownerName || '书友'}的书单：${this.share.title}` : '友邻座 · 书单分享',
				path: `/pages/reading/list-share-view?token=${this.token}`
			}
		},
		onShareTimeline() {
			return {
				title: this.share ? `${this.share.ownerName || '书友'}的书单：${this.share.title}` : '友邻座 · 书单分享',
				query: `token=${this.token}`
			}
		},
		methods: {
			statusText(s) {
				return STATUS_LABELS[s] || s
			},
			async load() {
				if (!this.token) {
					this.loaded = true
					return
				}
				try {
					this.share = await api.getBookListShare(this.token)
				} catch (e) {}
				this.loaded = true
			},
			goReading() {
				uni.navigateTo({ url: '/pages/reading/reading' })
			},
			goBoard() {
				uni.navigateTo({ url: '/pages/reading/list-share-board' })
			},
			goReport() {
				if (!this.share) return
				uni.navigateTo({ url: `/pages/report/report?targetType=BookListShare&targetId=${this.share.id}` })
			},
			async toggleFavorite() {
				if (!this.token || this.favoriting) return
				this.favoriting = true
				try {
					const res = await api.toggleBookListFavorite(this.token)
					this.share.favorited = res.favorited
					this.share.favoriteCount = res.favoriteCount
					uni.showToast({ title: res.favorited ? '已收藏' : '已取消收藏', icon: 'none' })
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				} finally {
					this.favoriting = false
				}
			},
			async onPublicChange(e) {
				const isPublic = !!(e.detail && e.detail.value)
				try {
					const res = await api.setBookListVisibility(this.token, isPublic)
					this.share.isPublic = res.isPublic
					uni.showToast({ title: res.isPublic ? '已公开到热门书单' : '已设为私密', icon: 'none' })
				} catch (err) {
					this.share.isPublic = !isPublic
					uni.showToast({ title: err.message || '操作失败', icon: 'none' })
				}
			}
		}
	}
</script>

<style scoped>
	.hero { background: linear-gradient(160deg, var(--primary), var(--primary-light)); color: #FFFFFF; padding: 40rpx 30rpx; }
	.hero-top { display: flex; align-items: center; gap: 20rpx; }
	.avatar { width: 72rpx; height: 72rpx; border-radius: 50%; background: rgba(255,255,255,0.3); flex-shrink: 0; }
	.owner { display: flex; flex-direction: column; }
	.owner-name { font-size: 30rpx; font-weight: 600; }
	.owner-sub { font-size: 22rpx; opacity: 0.85; }
	.hero-title { display: block; font-size: 44rpx; font-weight: 700; margin-top: 30rpx; }
	.hero-remark { display: block; font-size: 26rpx; line-height: 1.6; opacity: 0.92; margin-top: 16rpx; }
	.hero-foot { display: flex; justify-content: space-between; font-size: 22rpx; opacity: 0.75; margin-top: 30rpx; }
	.section { padding: 20rpx; }
	.book-card { display: flex; align-items: center; gap: 20rpx; }
	.index { width: 40rpx; font-size: 28rpx; font-weight: 700; color: var(--primary); text-align: center; flex-shrink: 0; }
	.book-info { flex: 1; min-width: 0; }
	.book-title { font-size: 30rpx; font-weight: 600; display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.book-author { font-size: 24rpx; color: #8A8A86; }
	.book-meta { display: flex; align-items: center; gap: 16rpx; margin-top: 8rpx; }
	.status { font-size: 20rpx; padding: 4rpx 14rpx; border-radius: 8rpx; }
	.st-WantToRead { background: #F1EFE9; color: #8A8A86; }
	.st-Reading { background: var(--primary-bg); color: var(--primary); }
	.st-Finished { background: #E8F1E8; color: #4A7A4A; }
	.minutes { font-size: 22rpx; color: var(--primary); }
	.cta { padding: 10rpx 20rpx 0; display: flex; flex-direction: column; gap: 20rpx; }
	.cta-row { display: flex; gap: 20rpx; }
	.fav-btn { flex: 1; }
	.fav-btn.on { color: var(--primary); border-color: var(--primary); }
	.share-btn { flex: 2; }
	.owner-row { display: flex; align-items: center; justify-content: space-between; background: #FFFFFF; border-radius: 16rpx; padding: 16rpx 24rpx; }
	.owner-label { font-size: 28rpx; color: #55554F; }
	.links { display: flex; justify-content: center; gap: 60rpx; padding: 6rpx 0 20rpx; }
	.link { font-size: 26rpx; color: var(--primary); }
	.link.danger { color: #B85450; }
	.footer { display: block; text-align: center; font-size: 22rpx; color: #B0B0AB; padding: 30rpx 0 40rpx; }
	.empty-state { display: flex; align-items: center; justify-content: center; min-height: 60vh; color: #B0B0AB; font-size: 26rpx; }
</style>
