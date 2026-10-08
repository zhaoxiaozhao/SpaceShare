<template>
	<view>
		<scroll-view scroll-x class="feed-tabs" :show-scrollbar="false">
			<text class="ft" :class="{ on: category === '' }" @click="switchCategory('')">全部</text>
			<text class="ft" v-for="c in cats" :key="c.code" :class="{ on: category === c.code }" @click="switchCategory(c.code)">{{c.label}}</text>
			<text class="ft" :class="{ on: isMoments }" @click="switchCategory(MOMENTS)">动态</text>
		</scroll-view>

		<view class="feed-sort" v-if="!isMoments">
			<text class="fs" :class="{ on: sort === 'new' }" @click="switchSort('new')">最新</text>
			<text class="fs" :class="{ on: sort === 'hot' }" @click="switchSort('hot')">最热</text>
			<text class="fs-post" @click="$emit('create')">＋ 发帖</text>
		</view>
		<view class="feed-sort" v-else>
			<text class="fs muted">友邻们正在做的事</text>
		</view>

		<template v-if="!isMoments">
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
						<view class="p-user" @click.stop="openUser(p.ownerId)">
							<Avatar :url="p.ownerAvatar" :name="p.ownerName" :size="44" />
							<text class="p-owner">{{p.ownerName}}</text>
						</view>
						<view class="p-stats">
							<view class="p-stat">
								<image class="ico" src="/static/icons/eye-gray.png" mode="aspectFit" />
								<text>{{p.viewCount || 0}}</text>
							</view>
							<view class="p-stat" :class="{ on: p.liked }" @click.stop="like(p)">
								<image class="ico" :src="p.liked ? `/static/icons/thumb-up-filled-${season}.png` : '/static/icons/thumb-up-gray.png'" mode="aspectFit" />
								<text>{{p.likeCount || 0}}</text>
							</view>
							<view class="p-stat">
								<image class="ico" src="/static/icons/chat-gray.png" mode="aspectFit" />
								<text>{{p.commentCount || 0}}</text>
							</view>
							<view class="p-stat danger" v-if="!p.isOwner" @click.stop="report(p)">
								<image class="ico" src="/static/icons/flag.png" mode="aspectFit" />
								<text>举报</text>
							</view>
						</view>
					</view>
				</view>

				<view class="feed-more" v-if="hasMore || loadingMore">
					<text v-if="loadingMore" class="more-text muted">加载中…</text>
					<text v-else class="more-text" @click="loadMore">加载更多</text>
				</view>
			</view>
			<view v-else-if="loaded" class="empty">还没有帖子，来发第一条吧</view>
			<view v-else class="empty">加载中…</view>
		</template>

		<template v-else>
			<view v-if="moments.length">
				<view class="card moment" v-for="m in moments" :key="m.id">
					<view class="m-top">
						<view class="m-user">
							<Avatar :url="m.ownerAvatar" :name="m.ownerName" :size="44" />
							<view class="m-user-body">
								<text class="m-owner">{{m.ownerName || '友邻'}}</text>
								<text class="m-venue" v-if="m.venueName">在 {{m.venueName}}</text>
							</view>
						</view>
						<text class="m-type">{{momentLabel(m.type)}}</text>
						<text class="m-time">{{timeText(m.createdAt)}}</text>
					</view>
					<text class="m-content">{{m.content}}</text>
					<view class="m-foot" v-if="m.isOwner">
						<text class="m-del" @click="removeMoment(m)">删除</text>
					</view>
				</view>

				<view class="feed-more" v-if="hasMore || loadingMore">
					<text v-if="loadingMore" class="more-text muted">加载中…</text>
					<text v-else class="more-text" @click="loadMore">加载更多</text>
				</view>
			</view>
			<view v-else-if="loaded" class="empty">还没有动态，分享座位或留个便签吧</view>
			<view v-else class="empty">加载中…</view>
		</template>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { parseDate } from '../../utils/format.js'
	import { getAppOptions } from '../../utils/options.js'
	import { getSeasonKey } from '../../utils/theme.js'

	const PAGE_SIZE = 20
	const MOMENTS = '__moments__'

	const MOMENT_LABELS = {
		seat_share: '分享座位',
		swap: '换座',
		seat_note: '便签',
		reading: '阅读',
		check_in: '打卡'
	}

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
				moments: [],
				season: getSeasonKey(),
				loaded: false,
				loadedOnce: false,
				loadingMore: false,
				hasMore: false
			}
		},
		computed: {
			cats() {
				return getAppOptions().venuePostCategories
			},
			isMoments() {
				return this.category === MOMENTS
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
					if (this.isMoments) {
						const list = (await api.getMoments(this.venueId, PAGE_SIZE)) || []
						this.moments = list
						this.hasMore = list.length >= PAGE_SIZE
					} else {
						const list = (await api.getVenuePosts(this.venueId, this.category, this.sort)) || []
						this.posts = list
						this.hasMore = list.length >= PAGE_SIZE
					}
				} catch (e) {
					this.posts = []
					this.moments = []
					this.hasMore = false
				}
				this.loaded = true
				this.loadedOnce = true
			},
			async loadMore() {
				if (this.loadingMore || !this.hasMore) return
				this.loadingMore = true
				const list = this.isMoments ? this.moments : this.posts
				if (!list.length) return
				const beforeId = list[list.length - 1].id
				try {
					if (this.isMoments) {
						const more = (await api.getMoments(this.venueId, PAGE_SIZE, beforeId)) || []
						if (more.length) {
							const seen = new Set(this.moments.map(m => m.id))
							this.moments = this.moments.concat(more.filter(m => !seen.has(m.id)))
						}
						this.hasMore = more.length >= PAGE_SIZE
					} else {
						const more = (await api.getVenuePosts(this.venueId, this.category, this.sort, beforeId)) || []
						if (more.length) {
							const seen = new Set(this.posts.map(p => p.id))
							this.posts = this.posts.concat(more.filter(p => !seen.has(p.id)))
						}
						this.hasMore = more.length >= PAGE_SIZE
					}
				} catch (e) {
					this.hasMore = false
				} finally {
					this.loadingMore = false
				}
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
			momentLabel(t) {
				return MOMENT_LABELS[t] || '动态'
			},
			async removeMoment(m) {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				const res = await new Promise(resolve => {
					uni.showModal({
						title: '删除动态',
						content: '删除后不可恢复，确定删除这条动态吗？',
						success: r => resolve(r.confirm)
					})
				})
				if (!res) return
				try {
					await api.deleteMoment(m.id)
					this.moments = this.moments.filter(x => x.id !== m.id)
					uni.showToast({ title: '已删除', icon: 'none' })
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			},
			open(id) {
				uni.navigateTo({ url: `/pages/community/post?id=${id}` })
			},
			openUser(id) {
				if (!id) return
				uni.navigateTo({ url: `/pages/user/profile?id=${id}` })
			},
			report(p) {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				uni.navigateTo({ url: `/pages/report/report?targetType=VenuePost&targetId=${p.id}` })
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
	.fs.muted { color: #B0B0AB; }
	.fs-post { margin-left: auto; font-size: 24rpx; color: var(--primary); }
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
	.p-user { display: flex; align-items: center; gap: 10rpx; min-width: 0; }
	.p-owner { font-size: 24rpx; color: #8A8A86; }
	.p-stats { margin-left: auto; display: flex; gap: 21rpx; }
	.p-stat { display: flex; align-items: center; gap: 5rpx; font-size: 24rpx; color: #8A8A86; }
	.ico { width: 26rpx; height: 26rpx; }
	.p-stat.on { color: var(--primary); }
	.p-stat.danger { color: #B85450; }
	.feed-more { display: flex; justify-content: center; padding: 24rpx 0 40rpx; }
	.more-text { font-size: 24rpx; color: var(--primary); }
	.more-text.muted { color: #C4C2BB; }
	.empty { display: flex; align-items: center; justify-content: center; padding: 64rpx 0; color: #B0B0AB; font-size: 24rpx; }
	.moment { display: flex; flex-direction: column; }
	.m-top { display: flex; align-items: center; gap: 10rpx; }
	.m-user { display: flex; align-items: center; gap: 12rpx; min-width: 0; flex: 1; }
	.m-user-body { display: flex; flex-direction: column; min-width: 0; }
	.m-owner { font-size: 24rpx; color: #2E2E2B; font-weight: 600; }
	.m-venue { font-size: 20rpx; color: #B0B0AB; margin-top: 2rpx; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.m-type { font-size: 20rpx; color: var(--primary); background: var(--primary-bg); border-radius: 8rpx; padding: 4rpx 14rpx; flex-shrink: 0; }
	.m-time { font-size: 20rpx; color: #B0B0AB; margin-left: 6rpx; }
	.m-content { display: block; font-size: 26rpx; color: #55554F; line-height: 1.4; margin-top: 14rpx; }
	.m-foot { display: flex; justify-content: flex-end; margin-top: 10rpx; }
	.m-del { font-size: 22rpx; color: #B85450; }
</style>