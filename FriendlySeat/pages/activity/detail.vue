<template>
	<page-meta :page-style="pageThemeStyle" />
	<view class="page" v-if="a">
		<view class="card">
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

		<!-- 报名名单（发起人可见） -->
		<view class="card" v-if="a.isMine && a.signups && a.signups.length">
			<text class="section-title">报名名单（{{a.signups.length}}）</text>
			<text class="signup" v-for="(s, i) in a.signups" :key="s.id">{{i + 1}}. {{s.userNickname || '友邻'}}</text>
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
	</view>
	<view v-else class="empty">活动不存在或已结束</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'
	import { getPageStyle } from '../../utils/theme.js'

	export default {
		data() {
			return { id: null, a: null, pageThemeStyle: getPageStyle() }
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
			return { title: a ? a.title : '友邻座活动', path: `/pages/activity/detail?id=${this.id}` }
		},
		methods: {
			formatTime,
			async load() {
				try {
					this.a = await api.getActivity(this.id)
				} catch (e) {
					this.a = null
				}
			},
			categoryLabel(code) {
				const map = { reading: '读书', lecture: '讲座', exhibition: '展览', study: '自习', other: '其他' }
				return map[code] || '其他'
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
			async signup() {
				try {
					await api.signupActivity(this.id)
					uni.showToast({ title: '报名成功', icon: 'success' })
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
		padding: 24rpx;
	}
	.card {
		margin-bottom: 20rpx;
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
		font-size: 26rpx;
		color: #55554F;
		margin-top: 10rpx;
	}
	.meta {
		display: flex;
		flex-direction: column;
		gap: 6rpx;
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
		font-size: 28rpx;
		font-weight: 700;
		margin-bottom: 12rpx;
	}
	.desc {
		font-size: 26rpx;
		color: #4A4945;
		line-height: 1.7;
	}
	.signup {
		display: block;
		font-size: 26rpx;
		color: #4A4945;
		padding: 8rpx 0;
	}
	.actions {
		display: flex;
		gap: 16rpx;
		margin-top: 24rpx;
	}
	.actions button {
		flex: 1;
	}
	.empty {
		text-align: center;
		color: #B0AEA8;
		font-size: 26rpx;
		padding: 120rpx 0;
	}
</style>
