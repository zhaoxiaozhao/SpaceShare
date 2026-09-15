<template>
	<page-meta :page-style="pageThemeStyle" />
	<view>
		<view v-if="items.length" class="list">
			<view class="card board-card" v-for="(s, i) in items" :key="s.id" @click="open(s.token)">
				<text class="rank" :class="{ top: i < 3 }">{{i + 1}}</text>
				<view class="info">
					<text class="title">{{s.title}}</text>
					<text class="remark" v-if="s.remark">{{s.remark}}</text>
					<view class="meta">
						<text class="owner">{{s.ownerName}}</text>
						<text class="dot">·</text>
						<text>{{s.count}} 本</text>
						<text class="dot">·</text>
						<text class="fav">{{s.favoriteCount}} 收藏</text>
						<text class="dot">·</text>
						<text>{{s.viewCount}} 浏览</text>
					</view>
				</view>
			</view>
		</view>
		<view v-else-if="loaded" class="empty">还没有公开的书单，去生成一个吧</view>
		<view v-else class="empty">加载中…</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				items: [],
				loaded: false
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				try {
					this.items = (await api.getBookListBoard()) || []
				} catch (e) {}
				this.loaded = true
			},
			open(token) {
				uni.navigateTo({ url: `/pages/reading/list-share-view?token=${token}` })
			}
		}
	}
</script>

<style scoped>
	.list { padding: 20rpx 20rpx 30rpx; }
	.board-card { display: flex; align-items: flex-start; gap: 20rpx; }
	.rank { width: 48rpx; font-size: 32rpx; font-weight: 700; color: #C4C2BB; text-align: center; flex-shrink: 0; line-height: 1.2; }
	.rank.top { color: var(--primary); }
	.info { flex: 1; min-width: 0; }
	.title { font-size: 30rpx; font-weight: 600; display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.remark { font-size: 24rpx; color: #55554F; display: block; margin-top: 6rpx; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.meta { display: flex; align-items: center; gap: 8rpx; font-size: 22rpx; color: #8A8A86; margin-top: 12rpx; }
	.owner { color: #55554F; }
	.fav { color: var(--primary); }
	.dot { color: #C4C2BB; }
</style>
