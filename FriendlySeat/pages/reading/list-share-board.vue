<template>
	<page-meta :page-style="pageThemeStyle" />
	<view>
		<view class="tabs">
			<view class="tab" :class="{ active: tab === 'hot' }" @click="switchTab('hot')">热门</view>
			<view class="tab" :class="{ active: tab === 'mine' }" @click="switchTab('mine')">我的</view>
			<view class="tab" :class="{ active: tab === 'favorites' }" @click="switchTab('favorites')">收藏</view>
		</view>

		<!-- 热门 -->
		<block v-if="tab === 'hot'">
			<view v-if="hot.length" class="list">
				<view class="card board-card" v-for="(s, i) in hot" :key="s.id" @click="open(s.token)">
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
			<view v-else-if="loaded" class="empty">还没有公开的书单</view>
			<view v-else class="empty">加载中…</view>
		</block>

		<!-- 我的 -->
		<block v-else-if="tab === 'mine'">
			<view v-if="mine.length" class="list">
				<view class="card mine-card" v-for="s in mine" :key="s.token" @click="open(s.token)">
					<view class="info">
						<text class="title">{{s.title}}</text>
						<text class="remark" v-if="s.remark">{{s.remark}}</text>
						<view class="meta">
							<text>{{s.count}} 本</text>
							<text class="dot">·</text>
							<text class="fav">{{s.favoriteCount}} 收藏</text>
							<text class="dot">·</text>
							<text>{{s.viewCount}} 浏览</text>
						</view>
					</view>
					<text class="pub-tag" :class="{ on: s.isPublic }">{{s.isPublic ? '公开' : '私密'}}</text>
					<image class="del" src="/static/icons/trash.png" mode="aspectFit" @click.stop="removeShare(s)" />
				</view>
			</view>
			<view v-else-if="loaded" class="empty">还没有生成过书单，点右下角「生成书单」试试</view>
			<view v-else class="empty">加载中…</view>
		</block>

		<!-- 收藏 -->
		<block v-else>
			<view v-if="favorites.length" class="list">
				<view class="card board-card" v-for="s in favorites" :key="s.id" @click="open(s.token)">
					<view class="info">
						<text class="title">{{s.title}}</text>
						<text class="remark" v-if="s.remark">{{s.remark}}</text>
						<view class="meta">
							<text class="owner">{{s.ownerName}}</text>
							<text class="dot">·</text>
							<text>{{s.count}} 本</text>
							<text class="dot">·</text>
							<text class="fav">{{s.favoriteCount}} 收藏</text>
						</view>
					</view>
				</view>
			</view>
			<view v-else-if="loaded" class="empty">还没有收藏的书单</view>
			<view v-else class="empty">加载中…</view>
		</block>

		<view class="bottom-space"></view>
		<view class="fab" @click="goCreate">＋ 生成书单</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				tab: 'hot',
				hot: [],
				mine: [],
				favorites: [],
				loaded: false
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				this.loaded = false
				try {
					if (this.tab === 'hot') {
						this.hot = (await api.getBookListBoard()) || []
					} else if (this.tab === 'favorites') {
						this.favorites = (await api.getMyBookListFavorites()) || []
					} else {
						this.mine = (await api.getMyBookListShares()) || []
					}
				} catch (e) {}
				this.loaded = true
			},
			switchTab(t) {
				if (this.tab === t) return
				this.tab = t
				this.load()
			},
			open(token) {
				uni.navigateTo({ url: `/pages/reading/list-share-view?token=${token}` })
			},
			goCreate() {
				uni.navigateTo({ url: '/pages/reading/list-share-create' })
			},
			removeShare(s) {
				uni.showModal({
					title: '删除书单',
					content: `确定删除书单「${s.title}」吗？`,
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.deleteBookListShare(s.token)
							uni.showToast({ title: '已删除', icon: 'success' })
							this.load()
						} catch (e) {
							uni.showToast({ title: e.message || '删除失败', icon: 'none' })
						}
					}
				})
			}
		}
	}
</script>

<style scoped>
	.tabs { display: flex; gap: 12rpx; margin: 20rpx; padding: 6rpx; background: #EFEEE9; border-radius: 40rpx; }
	.tab { flex: 1; text-align: center; padding: 14rpx 0; border-radius: 34rpx; font-size: 28rpx; color: #8A8A86; }
	.tab.active { background: #FFFFFF; color: var(--primary); font-weight: 600; box-shadow: 0 2rpx 8rpx rgba(0,0,0,0.06); }
	.list { padding: 0 0 20rpx; }
	.board-card { display: flex; align-items: flex-start; gap: 20rpx; }
	.rank { width: 48rpx; font-size: 32rpx; font-weight: 700; color: #C4C2BB; text-align: center; flex-shrink: 0; line-height: 1.2; }
	.rank.top { color: var(--primary); }
	.mine-card { display: flex; align-items: center; gap: 16rpx; }
	.info { flex: 1; min-width: 0; }
	.title { font-size: 30rpx; font-weight: 600; display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.remark { font-size: 24rpx; color: #55554F; display: block; margin-top: 6rpx; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.meta { display: flex; align-items: center; gap: 8rpx; font-size: 22rpx; color: #8A8A86; margin-top: 12rpx; }
	.owner { color: #55554F; }
	.fav { color: var(--primary); }
	.dot { color: #C4C2BB; }
	.pub-tag { font-size: 20rpx; padding: 4rpx 14rpx; border-radius: 8rpx; background: #F1EFE9; color: #8A8A86; flex-shrink: 0; }
	.pub-tag.on { background: var(--primary-bg); color: var(--primary); }
	.del { width: 36rpx; height: 36rpx; flex-shrink: 0; }
	.bottom-space { height: 140rpx; }
	.fab { position: fixed; right: 40rpx; bottom: 60rpx; padding: 0 36rpx; height: 88rpx; line-height: 88rpx; border-radius: 44rpx; background: var(--primary); color: #FFFFFF; font-size: 28rpx; font-weight: 600; box-shadow: 0 8rpx 24rpx rgba(0,0,0,0.18); }
</style>
