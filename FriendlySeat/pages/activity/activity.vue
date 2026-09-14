<template>
	<page-meta :page-style="pageThemeStyle" />
		<view class="page">
		<view class="tabs">
			<view class="tab" :class="{ active: tab === 'discover' }" @click="tab = 'discover'">发现</view>
			<view class="tab" :class="{ active: tab === 'joined' }" @click="tab = 'joined'">我报名的</view>
			<view class="tab" :class="{ active: tab === 'mine' }" @click="tab = 'mine'">我发布的</view>
		</view>

		<view v-if="tab === 'discover'" class="cats">
			<text class="cat" :class="{ on: category === '' }" @click="setCategory('')">全部</text>
			<text class="cat" :class="{ on: category === c.code }" v-for="c in categoryOptions" :key="c.code" @click="setCategory(c.code)">{{c.label}}</text>
		</view>

		<view v-if="list.length">
			<view class="card act-card" v-for="a in list" :key="a.id" @click="goDetail(a.id)">
				<image v-if="a.coverImage" class="act-cover" :src="a.coverImage" mode="aspectFill" />
				<view class="act-top">
					<text class="act-cat" :class="'cat-' + a.category">{{categoryLabel(a.category)}}</text>
					<text class="act-status" :class="'st-' + a.status.toLowerCase()">{{statusText(a.status)}}</text>
				</view>
				<text class="act-title">{{a.title}}</text>
				<text class="act-time">{{formatTime(a.startAt)}} ~ {{formatTime(a.endAt)}}</text>
				<text class="act-loc" v-if="a.venueName || a.locationText">{{a.venueName || ''}}<text v-if="a.locationText"> · {{a.locationText}}</text></text>
				<view class="act-foot">
					<text class="act-by">{{a.creatorNickname || '友邻'}} 发起</text>
					<text class="act-count">{{a.signupCount}}/{{a.capacity}} 人</text>
				</view>
			</view>
		</view>
		<view v-else class="empty">{{emptyText}}</view>
	</view>
	<view class="fab" @click="goCreate">＋ 发布活动</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'
	import { ACTIVITY_CATEGORIES, activityCategoryLabel } from '../../utils/activity.js'

	export default {
		data() {
			return {
				tab: 'discover',
				category: '',
				discover: [],
				joined: [],
				mine: [],
				categoryOptions: ACTIVITY_CATEGORIES
			}
		},
		computed: {
			list() {
				if (this.tab === 'discover') return this.discover
				if (this.tab === 'joined') return this.joined
				return this.mine
			},
			emptyText() {
				if (this.tab === 'discover') return '暂无进行中的活动'
				if (this.tab === 'joined') return '你还没有报名活动'
				return '你还没有发布活动'
			}
		},
		onShow() {
			this.load()
		},
		onPullDownRefresh() {
			this.load().then(() => uni.stopPullDownRefresh())
		},
		onShareAppMessage() {
			return { title: '友邻座 - 一起参加身边的读书与讲座活动', path: '/pages/activity/activity' }
		},
		methods: {
			formatTime,
			async load() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				try { this.discover = await api.getActivities(this.category) } catch (e) {}
				try { this.joined = await api.getJoinedActivities() } catch (e) {}
				try { this.mine = await api.getMyActivities() } catch (e) {}
			},
			setCategory(code) {
				this.category = code
				this.load()
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
			goDetail(id) {
				uni.navigateTo({ url: `/pages/activity/detail?id=${id}` })
			},
			goCreate() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				uni.navigateTo({ url: '/pages/activity/edit' })
			}
		}
	}
</script>

<style scoped>
	.page {
		padding: 24rpx;
	}
	.tabs {
		display: flex;
		background: #FFFFFF;
		border-radius: 20rpx;
		padding: 10rpx;
	}
	.tab {
		flex: 1;
		text-align: center;
		padding: 16rpx 0;
		font-size: 28rpx;
		color: #55554F;
		border-radius: 14rpx;
	}
	.tab.active {
		background: var(--primary-bg, #EAF3F1);
		color: var(--primary);
		font-weight: 600;
	}
	.cats {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
		margin: 20rpx 0 4rpx;
	}
	.cat {
		font-size: 24rpx;
		padding: 8rpx 22rpx;
		border-radius: 999rpx;
		background: #F1F0EB;
		color: #6B6A64;
	}
	.cat.on {
		background: var(--primary);
		color: #fff;
	}
	.act-card {
		margin-top: 16rpx;
	}
	.act-cover {
		width: 100%;
		height: 300rpx;
		border-radius: 12rpx;
		margin-bottom: 16rpx;
	}
	.act-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 10rpx;
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
		font-size: 32rpx;
		font-weight: 700;
	}
	.act-time, .act-loc {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin-top: 8rpx;
	}
	.act-foot {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-top: 16rpx;
		padding-top: 16rpx;
		border-top: 1rpx solid #F0EEE8;
	}
	.act-by, .act-count {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.act-count {
		color: var(--primary);
		font-weight: 600;
	}
	.empty {
		text-align: center;
		color: #B0AEA8;
		font-size: 26rpx;
		padding: 100rpx 0;
	}
	.fab {
		position: fixed;
		right: 30rpx;
		bottom: 60rpx;
		z-index: 99;
		padding: 20rpx 36rpx;
		border-radius: 44rpx;
		background: var(--primary-gradient, linear-gradient(135deg, #3A8A7E, #5BA99C));
		color: #fff;
		font-size: 28rpx;
		font-weight: 600;
		box-shadow: 0 8rpx 24rpx rgba(0, 0, 0, 0.18);
	}
</style>
