<template>
	<page-meta :page-style="pageThemeStyle" />
	<view v-if="share">
		<!-- 书单头部 -->
		<view class="hero">
			<view class="hero-top">
				<Avatar :url="share.ownerAvatar" :name="share.ownerName" :size="72" />
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

		<!-- 作者设置 -->
		<view class="card owner-card" v-if="share.isOwner">
			<view class="owner-info">
				<text class="owner-label">公开到热门书单榜</text>
				<text class="owner-tip">公开后其他用户可看到并收藏</text>
			</view>
			<switch :checked="share.isPublic" color="var(--primary)" @change="onPublicChange" />
		</view>

		<!-- 操作 -->
		<view class="actions">
			<view class="act-row">
				<view class="act-btn outline" :class="{ on: share.favorited }" v-if="!share.isOwner" @click="toggleFavorite">
					<image class="act-icon" :src="share.favorited ? `/static/icons/bookmark-filled-${season}.png` : `/static/icons/bookmark-${season}.png`" mode="aspectFit" />
					<text>{{share.favoriteCount}}</text>
				</view>
				<button class="act-btn solid" open-type="share">
					<image class="act-icon" src="/static/icons/share-white.png" mode="aspectFit" />
					<text>分享书单</text>
				</button>
			</view>
			<button class="btn-primary make-btn" @click="goReading">我也要做书单</button>
			<view class="links">
				<text class="link" @click="goBoard">更多书单</text>
				<view class="link-report" v-if="!share.isOwner" @click="goReport">
					<image class="link-icon" src="/static/icons/flag.png" mode="aspectFit" />
					<text>举报</text>
				</view>
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
	import { getSeasonKey } from '../../utils/theme.js'

	const STATUS_LABELS = { WantToRead: '想读', Reading: '在读', Finished: '已读' }

	export default {
		data() {
			return {
				token: '',
				season: getSeasonKey(),
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
	.hero { margin: 20rpx; border-radius: 28rpx; background: linear-gradient(160deg, var(--primary), var(--primary-light)); color: #FFFFFF; padding: 36rpx 32rpx; box-shadow: 0 10rpx 30rpx rgba(0,0,0,0.08); overflow: hidden; }
	.hero-top { display: flex; align-items: center; gap: 20rpx; }
	.avatar { width: 72rpx; height: 72rpx; border-radius: 50%; background: rgba(255,255,255,0.3); flex-shrink: 0; }
	.owner { display: flex; flex-direction: column; }
	.owner-name { font-size: 30rpx; font-weight: 600; }
	.owner-sub { font-size: 22rpx; opacity: 0.85; }
	.hero-title { display: block; font-size: 44rpx; font-weight: 700; margin-top: 30rpx; }
	.hero-remark { display: block; font-size: 26rpx; line-height: 1.6; opacity: 0.92; margin-top: 16rpx; }
	.hero-foot { display: flex; justify-content: space-between; font-size: 22rpx; opacity: 0.75; margin-top: 30rpx; }
	.section { padding: 0; }
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
	.owner-card { display: flex; align-items: center; justify-content: space-between; gap: 20rpx; }
	.owner-info { flex: 1; min-width: 0; }
	.owner-label { display: block; font-size: 28rpx; color: #33332E; font-weight: 600; }
	.owner-tip { display: block; font-size: 21rpx; color: #B0B0AB; margin-top: 4rpx; }
	.actions { margin: 0 20rpx; display: flex; flex-direction: column; gap: 18rpx; padding: 4rpx 0 10rpx; }
	.act-row { display: flex; gap: 18rpx; }
	.act-btn { flex: 1; height: 84rpx; display: flex; align-items: center; justify-content: center; gap: 10rpx; border-radius: 42rpx; font-size: 28rpx; line-height: 1; padding: 0; margin: 0; }
	.act-btn::after { border: none; }
	.act-btn.outline { background: #FFFFFF; color: var(--primary); border: 2rpx solid var(--primary); }
	.act-btn.outline.on { background: var(--primary-bg); }
	.act-btn.solid { background: var(--primary); color: #FFFFFF; }
	.act-icon { width: 34rpx; height: 34rpx; }
	.make-btn { margin: 0; }
	.links { display: flex; align-items: center; justify-content: center; gap: 48rpx; padding: 4rpx 0 10rpx; }
	.link { font-size: 26rpx; color: var(--primary); }
	.link-report { display: flex; align-items: center; gap: 6rpx; font-size: 26rpx; color: #B85450; }
	.link-icon { width: 26rpx; height: 26rpx; }
	.footer { display: block; text-align: center; font-size: 22rpx; color: #B0B0AB; padding: 30rpx 0 40rpx; }
	.empty-state { display: flex; align-items: center; justify-content: center; min-height: 60vh; color: #B0B0AB; font-size: 26rpx; }
</style>
