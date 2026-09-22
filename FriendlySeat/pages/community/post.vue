<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
	<view v-if="post" class="page">
		<view class="card">
			<view class="p-top">
				<text class="p-cat">{{post.categoryLabel}}</text>
				<text class="p-pin" v-if="post.isPinned">置顶</text>
			</view>
			<text class="p-title">{{post.title}}</text>
			<view class="p-by" @click="openUser(post.ownerId)">
				<Avatar :url="post.ownerAvatar" :name="post.ownerName" :size="48" />
				<text class="p-name">{{post.ownerName}}</text>
				<text class="p-time">{{timeText(post.createdAt)}}</text>
			</view>
			<text class="p-content" v-if="post.content">{{post.content}}</text>
			<image v-if="post.coverImage" class="p-cover" :src="post.coverImage" mode="widthFix" @click="previewImage(post.coverImage)" />

			<view class="p-meta-row">
				<view class="p-meta">
					<image class="ico" src="/static/icons/eye-gray.png" mode="aspectFit" />
					<text>{{post.viewCount || 0}}</text>
				</view>
				<view class="p-meta">
					<image class="ico" src="/static/icons/chat-gray.png" mode="aspectFit" />
					<text>{{post.commentCount || 0}}</text>
				</view>
			</view>

			<view class="p-actions">
				<view class="act act-icon" :class="{ on: post.liked }" @click="like">
					<image class="ico-sm" :src="post.liked ? `/static/icons/thumb-up-filled-${season}.png` : '/static/icons/thumb-up-gray.png'" mode="aspectFit" />
					<text>赞 {{post.likeCount || 0}}</text>
				</view>
				<text class="act" v-if="post.isOwner" @click="editPost">编辑</text>
				<button class="act act-btn" open-type="share">分享</button>
				<text class="act" v-if="post.isOwner" @click="removePost">删除</text>
				<text class="act danger" v-else @click="reportPost">举报</text>
			</view>
		</view>

		<view v-if="isHidden" class="card hidden-tip">
			<text class="hidden-title">该内容已被举报并自动隐藏</text>
			<text class="hidden-sub">正在等待人工审核，审核通过后将恢复展示。</text>
		</view>

		<!-- 评论 -->
		<view class="card">
			<text class="section-label">评论 {{post.commentCount || 0}}</text>
			<view v-if="comments.length" class="c-list">
				<view class="c-item" v-for="c in comments" :key="c.id">
					<view class="c-main">
						<view @click.stop="openUser(c.ownerId)">
							<Avatar :url="c.ownerAvatar" :name="c.ownerName" :size="44" />
						</view>
						<view class="c-body">
							<text class="c-name">{{c.ownerName}}</text>
							<text class="c-text" v-if="c.content">{{c.content}}</text>
							<image v-if="c.imageUrl" class="c-img" :src="c.imageUrl" mode="widthFix" @click.stop="previewImage(c.imageUrl)" />
							<view class="c-foot">
								<text class="c-time">{{timeText(c.createdAt)}}</text>
								<view class="c-reply" @click.stop="startReply(c)">
									<image class="ico-sm" :src="`/static/icons/reply-${season}.png`" mode="aspectFit" />
									<text>回复</text>
								</view>
								<view class="c-like" :class="{ on: c.liked }" @click.stop="likeComment(c)">
									<image class="ico-sm" :src="c.liked ? `/static/icons/thumb-up-filled-${season}.png` : '/static/icons/thumb-up-gray.png'" mode="aspectFit" />
									<text>{{c.likeCount || 0}}</text>
								</view>
							</view>
						</view>
						<image v-if="c.isOwner" class="c-icon" src="/static/icons/trash.png" mode="aspectFit" @click.stop="removeComment(c)" />
						<image v-else class="c-icon" src="/static/icons/flag.png" mode="aspectFit" @click.stop="reportComment(c)" />
					</view>

					<!-- 一级回复：默认显示 2 条，可展开 -->
					<view v-if="c.replies && c.replies.length" class="r-list">
						<view class="r-item" v-for="r in visibleReplies(c)" :key="r.id">
							<view @click.stop="openUser(r.ownerId)">
								<Avatar :url="r.ownerAvatar" :name="r.ownerName" :size="36" />
							</view>
							<view class="c-body">
								<text class="c-name">{{r.ownerName}}<text v-if="r.replyToName" class="r-to"> 回复 {{r.replyToName}}</text></text>
								<text class="c-text" v-if="r.content">{{r.content}}</text>
								<image v-if="r.imageUrl" class="c-img" :src="r.imageUrl" mode="widthFix" @click.stop="previewImage(r.imageUrl)" />
								<view class="c-foot">
									<text class="c-time">{{timeText(r.createdAt)}}</text>
									<view class="c-reply" @click.stop="startReply(r)">
										<image class="ico-sm" :src="`/static/icons/reply-${season}.png`" mode="aspectFit" />
										<text>回复</text>
									</view>
									<view class="c-like" :class="{ on: r.liked }" @click.stop="likeComment(r)">
										<image class="ico-sm" :src="r.liked ? `/static/icons/thumb-up-filled-${season}.png` : '/static/icons/thumb-up-gray.png'" mode="aspectFit" />
										<text>{{r.likeCount || 0}}</text>
									</view>
								</view>
							</view>
							<image v-if="r.isOwner" class="c-icon" src="/static/icons/trash.png" mode="aspectFit" @click.stop="removeComment(r)" />
							<image v-else class="c-icon" src="/static/icons/flag.png" mode="aspectFit" @click.stop="reportComment(r)" />
						</view>
						<text v-if="c.replies.length > 2 && !expanded[c.id]" class="r-more" @click.stop="expand(c.id)">查看全部 {{c.replies.length}} 条回复</text>
					</view>
				</view>
			</view>
			<text v-else class="c-empty">还没有评论，来说两句吧～</text>
		</view>

		<view class="bottom-space"></view>
	</view>
	<view v-else-if="loaded" class="empty">帖子不存在或已下架</view>
	<view v-else class="empty">加载中…</view>

	<!-- 吸底评论输入条 -->
	<view v-if="post" class="c-bar">
		<view class="c-bar-inner">
			<view v-if="replyTo" class="c-quote">
				<text class="c-quote-name">回复 {{replyTo.name}}</text>
				<text class="c-quote-x" @click="cancelReply">×</text>
			</view>
			<view v-if="commentImage" class="c-img-preview">
				<image class="c-img-preview-img" :src="commentImage" mode="aspectFill" @click="previewImage(commentImage)" />
				<text class="c-img-preview-x" @click="commentImage = ''">×</text>
			</view>
			<view class="c-input-row">
				<input class="c-input" v-model="commentInput" :maxlength="200" :placeholder="replyTo ? `回复 ${replyTo.name}…` : '友善发言（最多 200 字）'" confirm-type="send" :adjust-position="true" @confirm="sendComment" />
				<view class="c-attach" @click="chooseCommentImage">
					<image class="ico-sm" :src="`/static/icons/add-photo-${season}.png`" mode="aspectFit" />
					<text>图片</text>
				</view>
				<button class="c-send" :loading="sending" @click="sendComment">发送</button>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { parseDate } from '../../utils/format.js'
	import { uploadImage, getTempFileUrl } from '../../utils/profile.js'
	import { getSeasonKey } from '../../utils/theme.js'

	export default {
		data() {
			return {
				id: 0,
				post: null,
				comments: [],
				commentInput: '',
				commentImage: '',
				sending: false,
				loaded: false,
				replyTo: null,
				expanded: {},
				loadedOnce: false,
				season: getSeasonKey()
			}
		},
		computed: {
			isHidden() {
				return this.post && this.post.status === 'Hidden'
			}
		},
		onLoad(options) {
			this.id = options.id ? Number(options.id) : 0
		},
		onShow() {
			const first = !this.loadedOnce
			this.loadedOnce = true
			this.load(first)
		},
		onShareAppMessage() {
			const p = this.post
			return {
				title: p ? p.title : '友邻座 · 场馆交流',
				path: `/pages/community/post?id=${this.id}`
			}
		},
		onShareTimeline() {
			const p = this.post
			return { title: p ? p.title : '友邻座 · 场馆交流', query: `id=${this.id}` }
		},
		methods: {
			async load(countView) {
				if (!this.id) return
				try {
					const d = await api.getVenuePost(this.id, countView)
					this.post = d.post
					this.comments = d.comments || []
				} catch (e) {
					this.post = null
				}
				this.loaded = true
			},
			timeText(s) {
				const d = parseDate(s)
				if (!d) return ''
				const p = (x) => (x < 10 ? '0' + x : x)
				return `${d.getMonth() + 1}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}`
			},
			requireLogin() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return false
				}
				return true
			},
			async like() {
				if (!this.requireLogin()) return
				try {
					const res = await api.likeVenuePost(this.id)
					this.post.liked = res.liked
					this.post.likeCount = res.likeCount
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			},
			async likeComment(c) {
				if (!this.requireLogin()) return
				try {
					const res = await api.likeVenuePostComment(c.id)
					c.liked = res.liked
					c.likeCount = res.likeCount
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			},
			editPost() {
				const name = encodeURIComponent(this.post.venueName || '')
				uni.navigateTo({ url: `/pages/community/edit?id=${this.id}&venueId=${this.post.venueId}&venueName=${name}` })
			},
			removePost() {
				uni.showModal({
					title: '删除帖子',
					content: '确定删除这条帖子吗？删除后不可恢复。',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.deleteVenuePost(this.id)
							uni.showToast({ title: '已删除', icon: 'success' })
							setTimeout(() => uni.navigateBack(), 500)
						} catch (e) {
							uni.showToast({ title: e.message || '删除失败', icon: 'none' })
						}
					}
				})
			},
			reportPost() {
				if (!this.requireLogin()) return
				uni.navigateTo({ url: `/pages/report/report?targetType=VenuePost&targetId=${this.id}` })
			},
			previewImage(url) {
				if (!url) return
				uni.previewImage({ urls: Array.isArray(url) ? url : [url] })
			},
			openUser(id) {
				if (!id) return
				uni.navigateTo({ url: `/pages/user/profile?id=${id}` })
			},
			chooseCommentImage() {
				if (!this.requireLogin()) return
				uni.chooseImage({
					count: 1,
					sizeType: ['compressed'],
					sourceType: ['album', 'camera'],
					success: async (res) => {
						const filePath = res.tempFilePaths && res.tempFilePaths[0]
						if (!filePath) return
						uni.showLoading({ title: '上传中', mask: true })
						try {
							this.commentImage = await uploadImage(filePath, 'posts')
						} catch (e) {
							uni.showToast({ title: '上传失败，请重试', icon: 'none' })
						} finally {
							uni.hideLoading()
						}
					}
				})
			},
			async sendComment() {
				if (this.sending) return
				if (!this.requireLogin()) return
				const content = (this.commentInput || '').trim()
				if (!content && !this.commentImage) {
					uni.showToast({ title: '请先写点什么', icon: 'none' })
					return
				}
				this.sending = true
				try {
					let imageUrlTemp = null
					if (this.commentImage) {
						try { imageUrlTemp = await getTempFileUrl(this.commentImage) } catch (e) {}
					}
					await api.commentVenuePost(this.id, content, this.replyTo ? this.replyTo.id : null, this.commentImage || null, imageUrlTemp)
					this.commentInput = ''
					this.commentImage = ''
					this.replyTo = null
					uni.showToast({ title: '已发送', icon: 'success' })
					this.load()
				} catch (e) {
					uni.showToast({ title: e.message || '发送失败', icon: 'none' })
				} finally {
					this.sending = false
				}
			},
			startReply(c) {
				if (!this.requireLogin()) return
				this.replyTo = { id: c.id, name: c.ownerName }
			},
			cancelReply() {
				this.replyTo = null
			},
			expand(id) {
				this.$set(this.expanded, id, true)
			},
			visibleReplies(c) {
				const list = c.replies || []
				return this.expanded[c.id] ? list : list.slice(0, 2)
			},
			removeComment(c) {
				uni.showModal({
					title: '删除评论',
					content: c.parentCommentId ? '确定删除这条回复吗？' : '确定删除这条评论及其下全部回复吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.deleteVenuePostComment(c.id)
							uni.showToast({ title: '已删除', icon: 'success' })
							this.load()
						} catch (e) {
							uni.showToast({ title: e.message || '删除失败', icon: 'none' })
						}
					}
				})
			},
			reportComment(c) {
				if (!this.requireLogin()) return
				uni.navigateTo({ url: `/pages/report/report?targetType=VenuePostComment&targetId=${c.id}` })
			}
		}
	}
</script>

<style scoped>
	.page { padding-bottom: 40rpx; }
	.p-top { display: flex; align-items: center; gap: 10rpx; }
	.p-cat { font-size: 20rpx; color: var(--primary); background: var(--primary-bg); border-radius: 8rpx; padding: 4rpx 14rpx; }
	.p-pin { font-size: 20rpx; color: #B85450; background: #FBEDEC; border-radius: 8rpx; padding: 4rpx 14rpx; }
	.p-title { display: block; font-size: 36rpx; font-weight: 700; margin-top: 16rpx; }
	.p-by { display: flex; align-items: center; gap: 10rpx; margin-top: 18rpx; }
	.p-name { font-size: 24rpx; color: #55554F; }
	.p-time { font-size: 22rpx; color: #B0B0AB; margin-left: auto; }
	.p-content { display: block; font-size: 28rpx; color: #33332E; line-height: 1.4; margin-top: 20rpx; white-space: pre-wrap; }
	.p-cover { width: 100%; border-radius: 12rpx; margin-top: 18rpx; background: #F1EFE9; }
	.p-meta-row { display: flex; gap: 24rpx; margin-top: 18rpx; }
	.p-meta { display: flex; align-items: center; gap: 5rpx; font-size: 20rpx; color: #B0B0AB; }
	.ico { width: 26rpx; height: 26rpx; }
	.ico-sm { width: 24rpx; height: 24rpx; }
	.p-actions { display: flex; align-items: center; gap: 32rpx; margin-top: 16rpx; padding-top: 18rpx; border-top: 1rpx solid #F0EFEA; }
	.act { display: flex; align-items: center; gap: 5rpx; font-size: 24rpx; color: #8A8A86; }
	.act-icon { font-size: 24rpx; }
	.act.on { color: var(--primary); font-weight: 600; }
	.act.danger { color: #B85450; }
	.act-btn { margin: 0; padding: 0; line-height: 1.4; background: transparent; color: #8A8A86; font-size: 24rpx; }
	.act-btn::after { border: none; }
	.hidden-tip { border-left: 6rpx solid #B85450; }
	.hidden-title { display: block; font-size: 26rpx; font-weight: 600; color: #B85450; }
	.hidden-sub { display: block; font-size: 22rpx; color: #8A8A86; margin-top: 6rpx; }
	.section-label { display: block; font-size: 26rpx; font-weight: 600; margin-bottom: 16rpx; }
	.c-list { display: flex; flex-direction: column; }
	.c-item { display: flex; flex-direction: column; padding: 13rpx 0; border-bottom: 1rpx solid #F0EFEA; }
	.c-item:last-child { border-bottom: none; }
	.c-main { display: flex; align-items: flex-start; gap: 11rpx; }
	.c-body { flex: 1; min-width: 0; }
	.c-name { display: block; font-size: 22rpx; color: #8A8A86; }
	.c-text { display: block; font-size: 26rpx; color: #33332E; line-height: 1.4; margin-top: 4rpx; }
	.c-img { width: 320rpx; border-radius: 10rpx; margin-top: 8rpx; background: #F1EFE9; }
	.c-time { font-size: 20rpx; color: #C4C2BB; }
	.c-foot { display: flex; align-items: center; gap: 24rpx; margin-top: 6rpx; }
	.c-reply { display: flex; align-items: center; gap: 4rpx; font-size: 20rpx; color: var(--primary); }
	.c-like { display: flex; align-items: center; gap: 4rpx; font-size: 20rpx; color: #8A8A86; }
	.c-like.on { color: var(--primary); }
	.r-list { margin: 10rpx 0 0 55rpx; padding-left: 16rpx; border-left: 3rpx solid #F0EFEA; }
	.r-item { display: flex; align-items: flex-start; gap: 11rpx; padding: 10rpx 0; }
	.r-to { color: var(--primary); }
	.r-more { display: block; font-size: 22rpx; color: var(--primary); padding: 8rpx 0; }
	.c-icon { width: 32rpx; height: 32rpx; flex-shrink: 0; margin-top: 6rpx; }
	.c-empty { display: block; font-size: 24rpx; color: #B0B0AB; padding: 10rpx 0 14rpx; }
	.bottom-space { height: 140rpx; }
	/* 吸底输入条 */
	.c-bar { position: fixed; left: 0; right: 0; bottom: 0; z-index: 20; background: #FFFFFF; border-top: 1rpx solid #F0EFEA; padding: 12rpx 20rpx calc(12rpx + env(safe-area-inset-bottom)); }
	.c-bar-inner { display: flex; flex-direction: column; gap: 8rpx; }
	.c-quote { display: flex; align-items: center; justify-content: space-between; background: #F7F5EF; border-radius: 10rpx; padding: 8rpx 16rpx; }
	.c-quote-name { font-size: 22rpx; color: var(--primary); }
	.c-quote-x { font-size: 30rpx; color: #B0B0AB; padding: 0 8rpx; }
	.c-img-preview { position: relative; width: 120rpx; }
	.c-img-preview-img { width: 120rpx; height: 120rpx; border-radius: 10rpx; }
	.c-img-preview-x { position: absolute; top: -12rpx; right: -12rpx; width: 36rpx; height: 36rpx; line-height: 32rpx; text-align: center; border-radius: 50%; background: rgba(0,0,0,0.55); color: #FFFFFF; font-size: 24rpx; }
	.c-input-row { display: flex; align-items: center; gap: 14rpx; }
	.c-input { flex: 1; min-width: 0; background: #F7F5EF; border-radius: 36rpx; padding: 12rpx 24rpx; font-size: 26rpx; }
	.c-attach { flex-shrink: 0; display: flex; align-items: center; gap: 4rpx; font-size: 24rpx; color: var(--primary); }
	.c-send { flex-shrink: 0; margin: 0; padding: 0 32rpx; height: 68rpx; line-height: 68rpx; border-radius: 34rpx; background: var(--primary); color: #FFFFFF; font-size: 26rpx; }
	.c-send::after { border: none; }
	.empty { display: flex; align-items: center; justify-content: center; min-height: 60vh; color: #B0B0AB; font-size: 24rpx; }
</style>
