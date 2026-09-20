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
			<view class="p-by">
				<Avatar :url="post.ownerAvatar" :name="post.ownerName" :size="48" />
				<text class="p-name">{{post.ownerName}}</text>
				<text class="p-time">{{timeText(post.createdAt)}}</text>
			</view>
			<text class="p-content">{{post.content}}</text>
			<image v-if="post.coverImage" class="p-cover" :src="post.coverImage" mode="widthFix" @click="previewCover" />

			<view class="p-actions">
				<text class="act" :class="{ on: post.liked }" @click="like">赞 {{post.likeCount}}</text>
				<text class="act" v-if="post.isOwner" @click="removePost">删除</text>
				<text class="act danger" v-else @click="reportPost">举报</text>
			</view>
		</view>

		<!-- 评论 -->
		<view class="card">
			<text class="section-label">评论 {{post.commentCount}}</text>
			<view v-if="comments.length" class="c-list">
				<view class="c-item" v-for="c in comments" :key="c.id">
					<view class="c-main">
						<Avatar :url="c.ownerAvatar" :name="c.ownerName" :size="44" />
						<view class="c-body">
							<text class="c-name">{{c.ownerName}}</text>
							<text class="c-text">{{c.content}}</text>
							<view class="c-foot">
								<text class="c-time">{{timeText(c.createdAt)}}</text>
								<text class="c-reply" @click.stop="startReply(c)">回复</text>
							</view>
						</view>
						<image v-if="c.isOwner" class="c-icon" src="/static/icons/trash.png" mode="aspectFit" @click.stop="removeComment(c)" />
						<image v-else class="c-icon" src="/static/icons/flag.png" mode="aspectFit" @click.stop="reportComment(c)" />
					</view>

					<!-- 一级回复：默认显示 2 条，可展开 -->
					<view v-if="c.replies && c.replies.length" class="r-list">
						<view class="r-item" v-for="r in visibleReplies(c)" :key="r.id">
							<Avatar :url="r.ownerAvatar" :name="r.ownerName" :size="36" />
							<view class="c-body">
								<text class="c-name">{{r.ownerName}}<text v-if="r.replyToName" class="r-to"> 回复 {{r.replyToName}}</text></text>
								<text class="c-text">{{r.content}}</text>
								<view class="c-foot">
									<text class="c-time">{{timeText(r.createdAt)}}</text>
									<text class="c-reply" @click.stop="startReply(r)">回复</text>
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

			<view class="c-input-row">
				<view class="c-input-wrap">
					<view v-if="replyTo" class="c-quote">
						<text class="c-quote-name">回复 {{replyTo.name}}</text>
						<text class="c-quote-x" @click="cancelReply">×</text>
					</view>
					<input class="c-input" v-model="commentInput" :maxlength="200" :placeholder="replyTo ? `回复 ${replyTo.name}…` : '友善发言（最多 200 字）'" confirm-type="send" @confirm="sendComment" />
				</view>
				<button class="c-send" :loading="sending" @click="sendComment">发送</button>
			</view>
		</view>
	</view>
	<view v-else-if="loaded" class="empty">帖子不存在或已下架</view>
	<view v-else class="empty">加载中…</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { parseDate } from '../../utils/format.js'

	export default {
		data() {
			return {
				id: 0,
				post: null,
				comments: [],
				commentInput: '',
				sending: false,
				loaded: false,
				replyTo: null,
				expanded: {}
			}
		},
		onLoad(options) {
			this.id = options.id ? Number(options.id) : 0
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				if (!this.id) return
				try {
					const d = await api.getVenuePost(this.id)
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
			removePost() {
				uni.showModal({
					title: '删除帖子',
					content: '确定删除这条帖子吗？',
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
			previewCover() {
				if (this.post && this.post.coverImage) {
					uni.previewImage({ urls: [this.post.coverImage] })
				}
			},
			async sendComment() {
				if (this.sending) return
				if (!this.requireLogin()) return
				const content = (this.commentInput || '').trim()
				if (!content) {
					uni.showToast({ title: '请先写点什么', icon: 'none' })
					return
				}
				this.sending = true
				try {
					await api.commentVenuePost(this.id, content, this.replyTo ? this.replyTo.id : null)
					this.commentInput = ''
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
					content: '确定删除这条评论吗？',
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
	.p-actions { display: flex; gap: 32rpx; margin-top: 26rpx; padding-top: 20rpx; border-top: 1rpx solid #F0EFEA; }
	.act { font-size: 24rpx; color: #8A8A86; }
	.act.on { color: var(--primary); font-weight: 600; }
	.act.danger { color: #B85450; }
	.section-label { display: block; font-size: 26rpx; font-weight: 600; margin-bottom: 16rpx; }
	.c-list { display: flex; flex-direction: column; }
	.c-item { display: flex; flex-direction: column; padding: 13rpx 0; border-bottom: 1rpx solid #F0EFEA; }
	.c-item:last-child { border-bottom: none; }
	.c-main { display: flex; align-items: flex-start; gap: 11rpx; }
	.c-body { flex: 1; min-width: 0; }
	.c-name { display: block; font-size: 22rpx; color: #8A8A86; }
	.c-text { display: block; font-size: 26rpx; color: #33332E; line-height: 1.4; margin-top: 4rpx; }
	.c-time { font-size: 20rpx; color: #C4C2BB; }
	.c-foot { display: flex; align-items: center; gap: 24rpx; margin-top: 6rpx; }
	.c-reply { font-size: 20rpx; color: var(--primary); }
	/* 一级回复（楼中楼） */
	.r-list { margin: 10rpx 0 0 55rpx; padding-left: 16rpx; border-left: 3rpx solid #F0EFEA; }
	.r-item { display: flex; align-items: flex-start; gap: 11rpx; padding: 10rpx 0; }
	.r-to { color: var(--primary); }
	.r-more { display: block; font-size: 22rpx; color: var(--primary); padding: 8rpx 0; }
	.c-icon { width: 32rpx; height: 32rpx; flex-shrink: 0; margin-top: 6rpx; }
	.c-empty { display: block; font-size: 24rpx; color: #B0B0AB; padding: 10rpx 0 14rpx; }
	.c-input-row { display: flex; align-items: flex-end; gap: 13rpx; margin-top: 16rpx; }
	.c-input-wrap { flex: 1; min-width: 0; background: #F7F5EF; border-radius: 24rpx; padding: 10rpx 20rpx; }
	.c-quote { display: flex; align-items: center; justify-content: space-between; margin-bottom: 6rpx; }
	.c-quote-name { font-size: 22rpx; color: var(--primary); }
	.c-quote-x { font-size: 30rpx; color: #B0B0AB; padding: 0 8rpx; }
	.c-input { width: 100%; background: transparent; font-size: 26rpx; padding: 6rpx 0; }
	.c-send { flex-shrink: 0; margin: 0; padding: 0 34rpx; height: 72rpx; line-height: 72rpx; border-radius: 36rpx; background: var(--primary); color: #FFFFFF; font-size: 26rpx; }
	.c-send::after { border: none; }
	.empty { display: flex; align-items: center; justify-content: center; min-height: 60vh; color: #B0B0AB; font-size: 24rpx; }
</style>
