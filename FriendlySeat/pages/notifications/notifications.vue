<template>
	<view>
		<view v-if="list.length">
			<view class="card notif-card" v-for="n in list" :key="n.id">
				<view class="notif-top">
					<text class="notif-title">{{n.title}}</text>
					<text class="notif-dot" v-if="!n.isRead"></text>
				</view>
				<text class="notif-content" v-if="n.content">{{n.content}}</text>
				<text class="notif-time">{{formatTime(n.createdAt)}}</text>
			</view>
		</view>
		<view v-else class="empty">暂无消息</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'

	export default {
		data() {
			return {
				list: []
			}
		},
		onShow() {
			this.load()
		},
		onHide() {
			api.markNotificationsRead().catch(() => {})
		},
		methods: {
			formatTime,
			async load() {
				try {
					this.list = await api.getNotifications()
					uni.setStorageSync('unreadCount', 0)
				} catch (e) {}
			}
		}
	}
</script>

<style scoped>
	.notif-card {
		padding: 24rpx;
	}
	.notif-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 8rpx;
	}
	.notif-title {
		font-size: 30rpx;
		font-weight: 600;
	}
	.notif-dot {
		width: 16rpx;
		height: 16rpx;
		border-radius: 50%;
		background: #D9822B;
	}
	.notif-content {
		display: block;
		font-size: 26rpx;
		color: #55554F;
		margin-bottom: 8rpx;
	}
	.notif-time {
		font-size: 22rpx;
		color: #B0B0AB;
	}
</style>
