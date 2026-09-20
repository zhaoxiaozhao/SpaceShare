<template>
	<page-meta :page-style="pageThemeStyle" />
	<view class="page" v-if="a">
		<view class="card">
			<image v-if="a.coverImage" class="cover" :src="a.coverImage" mode="aspectFill" @click="previewCover" />
			<view class="act-top">
				<text class="act-cat">{{categoryLabel(a.category)}}</text>
				<text class="act-status" :class="'st-' + a.status.toLowerCase()">{{statusText(a.status)}}</text>
			</view>
			<text class="act-title">{{a.title}}</text>
			<text class="act-time">{{formatTime(a.startAt)}} ~ {{formatTime(a.endAt)}}</text>
			<text class="act-loc" v-if="a.venueName || a.locationText">{{a.venueName || ''}}<text v-if="a.locationText"> · {{a.locationText}}</text></text>
			<view class="meta">
				<text class="meta-item">发起人：{{a.creatorNickname || '友邻'}}</text>
				<text class="meta-item">报名 {{a.signupCount}}/{{a.capacity}} 人</text>
				<text class="meta-item" v-if="a.signupDeadline">报名截止：{{formatTime(a.signupDeadline)}}</text>
			</view>
			<text class="reject" v-if="a.status === 'Rejected' && a.reviewRemark">未通过原因：{{a.reviewRemark}}</text>
		</view>

		<view class="card" v-if="a.description">
			<text class="section-title">活动介绍</text>
			<text class="desc">{{a.description}}</text>
		</view>

		<!-- 已报名（头像+昵称） -->
		<view class="card" v-if="a.signups && a.signups.length">
			<view class="part-head">
				<text class="section-title">已报名 {{a.signups.length}} 人</text>
				<text class="part-all" v-if="a.signups.length > 8" @click="showParticipants = true">查看全部</text>
			</view>
			<view class="part-list">
				<view class="part-item" v-for="p in a.signups.slice(0, 8)" :key="p.id" @click="openUser(p.userId)">
					<Avatar :url="p.userAvatar" :name="p.userNickname" :size="72" />
					<text class="part-name">{{p.userNickname || '友邻'}}</text>
				</view>
			</view>
		</view>

		<!-- 全部参与者 -->
		<view v-if="showParticipants" class="p-mask" @click="showParticipants = false">
			<view class="p-pop" @click.stop>
				<text class="p-title">已报名（{{a.signups.length}}）</text>
				<scroll-view scroll-y class="p-scroll">
					<view class="p-item" v-for="p in a.signups" :key="p.id" @click="openUser(p.userId)">
						<Avatar :url="p.userAvatar" :name="p.userNickname" :size="64" />
						<text class="p-name">{{p.userNickname || '友邻'}}</text>
					</view>
				</scroll-view>
				<button class="btn-outline" @click="showParticipants = false">关闭</button>
			</view>
		</view>

		<!-- 操作 -->
		<view class="actions">
			<template v-if="a.isMine">
				<button class="btn-outline" @click="edit" v-if="a.status === 'PendingReview' || a.status === 'Rejected'">编辑</button>
				<button class="btn-outline" @click="cancel" v-if="a.status !== 'Cancelled' && a.status !== 'Finished'">取消活动</button>
			</template>
			<template v-else>
				<button v-if="a.isSignedUp" class="btn-outline" @click="cancelSignup">取消报名</button>
				<button v-else-if="a.signupOpen" class="btn-primary" @click="signup">立即报名</button>
				<button v-else class="btn-outline" disabled>{{signupClosedText}}</button>
			</template>
		</view>

		<button class="btn-outline share-btn" open-type="share">分享给好友 / 朋友圈</button>
		<text class="report-link" v-if="!a.isMine" @click="report">举报该活动</text>

		<!-- 留言鼓励 -->
		<view class="card cm-card">
			<view class="cm-head">
				<text class="cm-title">留言鼓励</text>
				<text class="cm-count" v-if="comments.length">{{comments.length}} 条</text>
			</view>

			<view v-if="comments.length" class="cm-list">
				<view class="cm-item" v-for="c in comments" :key="c.id">
					<view @click.stop="openUser(c.ownerId)">
						<Avatar :url="c.ownerAvatar" :name="c.ownerName" :size="56" />
					</view>
					<view class="cm-body">
						<text class="cm-name">{{c.ownerName}}</text>
						<text class="cm-content">{{c.content}}</text>
						<text class="cm-time">{{commentTime(c.createdAt)}}</text>
					</view>
					<image v-if="c.isOwner" class="cm-icon" src="/static/icons/trash.png" mode="aspectFit" @click.stop="removeComment(c)" />
					<image v-else class="cm-icon" src="/static/icons/flag.png" mode="aspectFit" @click.stop="reportComment(c)" />
				</view>
			</view>
			<text v-else class="cm-empty">还没有留言，来给这场活动加一句鼓励吧～</text>

			<view class="cm-input-row">
				<input class="cm-input" v-model="commentInput" :maxlength="50" placeholder="说点鼓励的话（最多 50 字）" confirm-type="send" @confirm="sendComment" />
				<button class="cm-send" :loading="commentSending" @click="sendComment">发送</button>
			</view>
		</view>
	</view>
	<view v-else class="empty">活动不存在或已结束</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime, parseDate } from '../../utils/format.js'
	import { getTempFileUrl } from '../../utils/profile.js'
	import { activityCategoryLabel } from '../../utils/activity.js'
	import { subscribeFor } from '../../utils/subscribe.js'

	export default {
		data() {
			return { id: null, a: null, shareImage: '', showParticipants: false,
				comments: [], commentInput: '', commentSending: false }
		},
		computed: {
			signupClosedText() {
				if (!this.a) return ''
				if (this.a.status !== 'Published') return this.statusText(this.a.status)
				if (this.a.isFull) return '名额已满'
				return '报名已截止'
			}
		},
		onLoad(options) {
			this.id = options.id
		},
		onShow() {
			this.load()
		},
		onShareAppMessage() {
			const a = this.a
			return {
				title: a ? a.title : '友邻座活动',
				path: `/pages/activity/detail?id=${this.id}`,
				imageUrl: this.shareImage || undefined
			}
		},
		onShareTimeline() {
			const a = this.a
			return {
				title: a ? a.title : '友邻座活动',
				query: `id=${this.id}`,
				imageUrl: this.shareImage || undefined
			}
		},
		methods: {
			formatTime,
			async load() {
				try {
					this.a = await api.getActivity(this.id)
					this.shareImage = await this.resolveShareImage()
				} catch (e) {
					this.a = null
				}
				try { this.comments = (await api.getActivityComments(this.id)) || [] } catch (e) { this.comments = [] }
			},
			// 分享图：活动海报 -> 云存储 fileID 转临时 https -> 下载为本地文件（分享卡片用本地图最稳）
			async resolveShareImage() {
				const fileId = this.a && this.a.coverImage
				if (!fileId) return ''
				let url = fileId
				if (String(fileId).startsWith('cloud://')) {
					url = await getTempFileUrl(fileId)
				}
				if (!url) return ''
				try {
					const local = await new Promise((resolve) => {
						uni.downloadFile({
							url,
							success: (r) => resolve(r.statusCode === 200 ? r.tempFilePath : ''),
							fail: () => resolve('')
						})
					})
					return local || url
				} catch (e) {
					return url
				}
			},
			categoryLabel(code) {
				return activityCategoryLabel(code)
			},
			statusText(s) {
				const map = {
					PendingReview: '审核中', Published: '已发布', Rejected: '未通过',
					Cancelled: '已取消', Finished: '已结束'
				}
				return map[s] || s
			},
			edit() {
				uni.navigateTo({ url: `/pages/activity/edit?id=${this.id}` })
			},
			previewCover() {
				if (this.a && this.a.coverImage) {
					uni.previewImage({ urls: [this.a.coverImage] })
				}
			},
			commentTime(s) {
				const d = parseDate(s)
				if (!d) return ''
				const p = (x) => (x < 10 ? '0' + x : x)
				return `${d.getMonth() + 1}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}`
			},
			async sendComment() {
				if (this.commentSending) return
				const content = (this.commentInput || '').trim()
				if (!content) {
					uni.showToast({ title: '请先写点什么', icon: 'none' })
					return
				}
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				this.commentSending = true
				try {
					await api.createActivityComment({ activityId: Number(this.id), content })
					this.commentInput = ''
					uni.showToast({ title: '已发送', icon: 'success' })
					this.comments = (await api.getActivityComments(this.id)) || []
				} catch (e) {
					uni.showToast({ title: e.message || '发送失败', icon: 'none' })
				} finally {
					this.commentSending = false
				}
			},
			removeComment(c) {
				uni.showModal({
					title: '删除留言',
					content: '确定删除这条留言吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.deleteActivityComment(c.id)
							uni.showToast({ title: '已删除', icon: 'success' })
							this.comments = (await api.getActivityComments(this.id)) || []
						} catch (e) {
							uni.showToast({ title: e.message || '删除失败', icon: 'none' })
						}
					}
				})
			},
			reportComment(c) {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				uni.navigateTo({ url: `/pages/report/report?targetType=ActivityComment&targetId=${c.id}` })
			},
			openUser(id) {
				if (!id) return
				uni.navigateTo({ url: `/pages/user/profile?id=${id}` })
			},
			report() {
				const nick = encodeURIComponent(this.a.creatorNickname || '')
				uni.navigateTo({ url: `/pages/report/report?targetType=Activity&targetId=${this.id}&targetUserId=${this.a.creatorUserId}&targetNickname=${nick}` })
			},
			async signup() {
				try {
					await api.signupActivity(this.id)
					uni.showToast({ title: '报名成功', icon: 'success' })
					// 订阅：活动开始提醒 / 取消变更通知
					subscribeFor(['activity_starting', 'system'])
					this.load()
				} catch (e) {
					uni.showToast({ title: e.message || '报名失败', icon: 'none' })
				}
			},
			cancelSignup() {
				uni.showModal({
					title: '取消报名',
					content: '确定取消报名吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.cancelActivitySignup(this.id)
							uni.showToast({ title: '已取消', icon: 'none' })
							this.load()
						} catch (e) {
							uni.showToast({ title: e.message || '操作失败', icon: 'none' })
						}
					}
				})
			},
			cancel() {
				uni.showModal({
					title: '取消活动',
					content: '确定取消你发布的活动吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.cancelActivity(this.id)
							uni.showToast({ title: '已取消', icon: 'none' })
							this.load()
						} catch (e) {
							uni.showToast({ title: e.message || '操作失败', icon: 'none' })
						}
					}
				})
			}
		}
	}
</script>

<style scoped>
	.page {
		padding: 19rpx;
	}
	.card {
		margin-bottom: 20rpx;
	}
	.cover {
		width: 100%;
		height: 340rpx;
		border-radius: 14rpx;
		margin-bottom: 20rpx;
	}
	.share-btn {
		width: 100%;
		margin-top: 8rpx;
	}
	.report-link {
		display: block;
		text-align: center;
		font-size: 24rpx;
		color: #B0AEA8;
		margin-top: 28rpx;
	}
	.part-head {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 16rpx;
	}
	.part-all {
		font-size: 24rpx;
		color: var(--primary);
	}
	.part-list {
		display: flex;
		flex-wrap: wrap;
		gap: 16rpx;
	}
	.part-item {
		width: 100rpx;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 6rpx;
	}
	.part-avatar {
		width: 72rpx;
		height: 72rpx;
		border-radius: 50%;
		background: #F0EEE8;
	}
	.part-name {
		max-width: 100rpx;
		font-size: 20rpx;
		color: #6B6A64;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.p-mask {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.4);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 999;
	}
	.p-pop {
		width: 600rpx;
		max-height: 70vh;
		background: #fff;
		border-radius: 20rpx;
		padding: 26rpx;
		display: flex;
		flex-direction: column;
	}
	.p-title {
		font-size: 28rpx;
		font-weight: 700;
		margin-bottom: 20rpx;
	}
	.p-scroll {
		flex: 1;
		max-height: 50vh;
	}
	.p-item {
		display: flex;
		align-items: center;
		gap: 13rpx;
		padding: 10rpx 0;
	}
	.p-avatar {
		width: 64rpx;
		height: 64rpx;
		border-radius: 50%;
		background: #F0EEE8;
	}
	.p-name {
		font-size: 24rpx;
	}
	.act-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.act-cat {
		font-size: 22rpx;
		padding: 4rpx 16rpx;
		border-radius: 8rpx;
		background: var(--primary-bg, #EAF3F1);
		color: var(--primary);
	}
	.act-status {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.st-pendingreview { color: #C78A2B; }
	.st-rejected { color: #C0533F; }
	.st-cancelled, .st-finished { color: #B0AEA8; }
	.st-published { color: var(--primary); }
	.act-title {
		display: block;
		font-size: 36rpx;
		font-weight: 700;
		margin-top: 12rpx;
	}
	.act-time, .act-loc {
		display: block;
		font-size: 24rpx;
		color: #55554F;
		margin-top: 10rpx;
	}
	.meta {
		display: flex;
		flex-direction: column;
		gap: 5rpx;
		margin-top: 16rpx;
		padding-top: 16rpx;
		border-top: 1rpx solid #F0EEE8;
	}
	.meta-item {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.reject {
		display: block;
		font-size: 24rpx;
		color: #C0533F;
		margin-top: 12rpx;
	}
	.section-title {
		display: block;
		font-size: 26rpx;
		font-weight: 700;
		margin-bottom: 12rpx;
	}
	.desc {
		font-size: 24rpx;
		color: #4A4945;
		line-height: 1.4;
	}
	.signup {
		display: block;
		font-size: 24rpx;
		color: #4A4945;
		padding: 8rpx 0;
	}
	.actions {
		display: flex;
		gap: 13rpx;
		margin-top: 24rpx;
	}
	.actions button {
		flex: 1;
	}
	.empty {
		text-align: center;
		color: #B0AEA8;
		font-size: 24rpx;
		padding: 96rpx 0;
	}

	/* 活动留言 */
	.cm-card { margin-top: 20rpx; }
	.cm-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12rpx; }
	.cm-title { font-size: 28rpx; font-weight: 600; }
	.cm-count { font-size: 22rpx; color: #B0B0AB; }
	.cm-list { display: flex; flex-direction: column; }
	.cm-item { display: flex; align-items: flex-start; gap: 13rpx; padding: 14rpx 0; border-bottom: 1rpx solid #F0EFEA; }
	.cm-item:last-child { border-bottom: none; }
	.cm-body { flex: 1; min-width: 0; }
	.cm-name { display: block; font-size: 24rpx; color: #8A8A86; }
	.cm-content { display: block; font-size: 26rpx; color: #33332E; line-height: 1.4; margin-top: 4rpx; }
	.cm-time { display: block; font-size: 20rpx; color: #C4C2BB; margin-top: 6rpx; }
	.cm-icon { width: 34rpx; height: 34rpx; flex-shrink: 0; margin-top: 6rpx; }
	.cm-empty { display: block; font-size: 24rpx; color: #B0B0AB; padding: 10rpx 0 14rpx; }
	.cm-input-row { display: flex; align-items: center; gap: 13rpx; margin-top: 16rpx; }
	.cm-input { flex: 1; background: #F7F5EF; border-radius: 36rpx; padding: 13rpx 26rpx; font-size: 26rpx; }
	.cm-send { flex-shrink: 0; margin: 0; padding: 0 34rpx; height: 72rpx; line-height: 72rpx; border-radius: 36rpx; background: var(--primary); color: #FFFFFF; font-size: 26rpx; }
	.cm-send::after { border: none; }
</style>
