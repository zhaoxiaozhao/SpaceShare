<template>
	<view>
		<scroll-view scroll-x class="feed-tabs" :show-scrollbar="false">
			<text class="ft" :class="{ on: category === '' }" @click="switchCategory('')">全部</text>
			<text class="ft" v-for="c in cats" :key="c.code" :class="{ on: category === c.code }" @click="switchCategory(c.code)">{{c.label}}</text>
		</scroll-view>

		<view class="feed-sort">
			<text class="fs" :class="{ on: sort === 'new' }" @click="switchSort('new')">最新</text>
			<text class="fs" :class="{ on: sort === 'hot' }" @click="switchSort('hot')">最热</text>
		</view>

		<view v-if="posts.length">
			<view class="card post" v-for="p in posts" :key="p.id" @click="open(p.id)">
				<view class="p-top">
					<text class="p-cat">{{p.categoryLabel}}</text>
					<text class="p-pin" v-if="p.isPinned">置顶</text>
					<text class="p-time">{{timeText(p.createdAt)}}</text>
				</view>
				<view class="p-main">
					<view class="p-main-body">
						<text class="p-title">{{p.title}}</text>
						<text class="p-excerpt">{{p.content}}</text>
					</view>
					<image v-if="p.coverImage" class="p-thumb" :src="p.coverImage" mode="aspectFill" />
				</view>
				<view class="p-foot">
					<Avatar :url="p.ownerAvatar" :name="p.ownerName" :size="44" />
					<text class="p-owner">{{p.ownerName}}</text>
					<view class="p-stats">
						<text class="p-stat" :class="{ on: p.liked }" @click.stop="like(p)">赞 {{p.likeCount}}</text>
						<text class="p-stat">评 {{p.commentCount}}</text>
					</view>
				</view>
			</view>
		</view>
		<view v-else-if="loaded" class="empty">还没有帖子，来发第一条吧</view>
		<view v-else class="empty">加载中…</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { parseDate } from '../../utils/format.js'
	import { getAppOptions } from '../../utils/options.js'

	export default {
		name: 'VenueFeed',
		props: {
			venueId: { type: [Number, String], default: 0 }
		},
		data() {
			return {
				category: '',
				sort: 'new',
				posts: [],
				loaded: false
			}
		},
		computed: {
			cats() {
				return getAppOptions().venuePostCategories
			}
		},
		watch: {
			venueId() {
				this.load()
			}
		},
		mounted() {
			this.load()
		},
		methods: {
			async load() {
				if (!this.venueId) return
				this.loaded = false
				try {
					this.posts = (await api.getVenuePosts(this.venueId, this.category, this.sort)) || []
				} catch (e) {
					this.posts = []
				}
				this.loaded = true
			},
			refresh() {
				this.load()
			},
			switchCategory(c) {
				if (this.category === c) return
				this.category = c
				this.load()
			},
			switchSort(s) {
				if (this.sort === s) return
				this.sort = s
				this.load()
			},
			open(id) {
				uni.navigateTo({ url: `/pages/community/post?id=${id}` })
			},
			timeText(s) {
				const d = parseDate(s)
				if (!d) return ''
				const p = (x) => (x < 10 ? '0' + x : x)
				return `${d.getMonth() + 1}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}`
			},
			async like(p) {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				try {
					const res = await api.likeVenuePost(p.id)
					p.liked = res.liked
					p.likeCount = res.likeCount
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			}
		}
	}
</script>

<style scoped>
	.feed-tabs { white-space: nowrap; margin: 16rpx 20rpx 0; }
	.ft { display: inline-block; padding: 8rpx 26rpx; margin-right: 14rpx; border-radius: 28rpx; font-size: 24rpx; background: #F1EFE9; color: #8A8A86; }
	.ft.on { background: var(--primary); color: #FFFFFF; }
	.feed-sort { display: flex; gap: 26rpx; margin: 14rpx 24rpx 6rpx; }
	.fs { font-size: 24rpx; color: #8A8A86; }
	.fs.on { color: var(--primary); font-weight: 600; }
	.post { display: flex; flex-direction: column; }
	.p-top { display: flex; align-items: center; gap: 10rpx; }
	.p-cat { font-size: 20rpx; color: var(--primary); background: var(--primary-bg); border-radius: 8rpx; padding: 4rpx 14rpx; }
	.p-pin { font-size: 20rpx; color: #B85450; background: #FBEDEC; border-radius: 8rpx; padding: 4rpx 14rpx; }
	.p-time { font-size: 20rpx; color: #B0B0AB; margin-left: auto; }
	.p-main { display: flex; align-items: flex-start; gap: 16rpx; margin-top: 14rpx; }
	.p-main-body { flex: 1; min-width: 0; }
	.p-thumb { width: 160rpx; height: 120rpx; border-radius: 12rpx; flex-shrink: 0; background: #F1EFE9; }
	.p-title { display: block; font-size: 28rpx; font-weight: 600; }
	.p-excerpt { display: block; font-size: 24rpx; color: #55554F; line-height: 1.4; margin-top: 8rpx; overflow: hidden; text-overflow: ellipsis; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
	.p-foot { display: flex; align-items: center; gap: 10rpx; margin-top: 16rpx; }
	.p-owner { font-size: 24rpx; color: #8A8A86; }
	.p-stats { margin-left: auto; display: flex; gap: 21rpx; }
	.p-stat { font-size: 24rpx; color: #8A8A86; }
	.p-stat.on { color: var(--primary); }
	.empty { display: flex; align-items: center; justify-content: center; padding: 64rpx 0; color: #B0B0AB; font-size: 24rpx; }
</style>
