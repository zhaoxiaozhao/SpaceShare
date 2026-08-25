<template>
	<view>
		<view class="notif-toolbar" v-if="list.length">
			<view class="tabs">
				<text class="tab" :class="{ active: filter === '' }" @click="switchFilter('')">全部</text>
				<text class="tab" :class="{ active: filter === 'unread' }" @click="switchFilter('unread')">未读</text>
			</view>
			<text class="read-all" @click="readAll">全部已读</text>
		</view>
		<view v-if="list.length">
			<view class="card notif-card" v-for="n in list" :key="n.id">
				<view class="notif-top">
					<text class="notif-title" :class="{ unread: !n.isRead }">{{n.title}}</text>
					<text class="notif-dot" v-if="!n.isRead"></text>
				</view>
				<text class="notif-content" v-if="n.content">{{n.content}}</text>
				<text class="notif-time">{{formatTime(n.createdAt)}}</text>
			</view>
		</view>
		<view v-else class="empty">{{ filter === 'unread' ? '暂无未读消息' : '暂无消息' }}</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'

	export default {
		data() {
			return {
				list: [],
				filter: ''
			}
		},
		onShow() {
			this.load()
		},
		onHide() {
			if (this.list.some(n => !n.isRead)) {
				api.markNotificationsRead().catch(() => {})
			}
			this.setTabBarBadge(0)
		},
		methods: {
			formatTime,
			async load() {
				try {
					this.list = await api.getNotifications(this.filter === 'unread')
					const unread = this.list.filter(n => !n.isRead).length
					uni.setStorageSync('unreadCount', unread)
					this.setTabBarBadge(unread)
				} catch (e) {}
			},
			switchFilter(f) {
				if (this.filter === f) return
				this.filter = f
				this.load()
			},
			async readAll() {
				try {
					await api.markNotificationsRead()
					this.list.forEach(n => { n.isRead = true })
					uni.setStorageSync('unreadCount', 0)
					this.setTabBarBadge(0)
					uni.showToast({ title: '已全部标为已读', icon: 'none' })
				} catch (e) {}
			},
			// 设置底部导航「通知」tab 的未读角标（index=2）
			setTabBarBadge(count) {
				try {
					if (count > 0) {
						uni.setTabBarBadge({ index: 2, text: count > 99 ? '99+' : String(count) })
					} else {
						uni.removeTabBarBadge({ index: 2 })
					}
				} catch (e) {}
			}
		}
	}
</script>

<style scoped>
	.notif-toolbar {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 0 20rpx 16rpx;
	}
	.tabs {
		display: flex;
		gap: 12rpx;
	}
	.tab {
		padding: 8rpx 24rpx;
		border-radius: 24rpx;
		font-size: 24rpx;
		color: #55554F;
		background: #FFFFFF;
		border: 1rpx solid #E0DED6;
	}
	.tab.active {
		background: #3A8A7E;
		color: #FFFFFF;
		border-color: #3A8A7E;
	}
	.read-all {
		font-size: 24rpx;
		color: #3A8A7E;
		padding: 8rpx 12rpx;
	}
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
		color: #B0B0AB;
	}
	.notif-title.unread {
		color: #33332E;
		font-weight: 700;
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