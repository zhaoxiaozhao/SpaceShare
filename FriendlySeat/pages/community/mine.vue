<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
	<view class="page">
		<view v-if="list.length">
			<view class="card item" v-for="p in list" :key="p.id" @click="open(p.id)">
				<view class="i-top">
					<text class="i-title">{{p.title}}</text>
					<text class="i-status" :class="statusClass(p.status)">{{statusText(p.status)}}</text>
				</view>
				<text class="i-ex">{{p.content}}</text>
				<view class="i-meta">
					<text class="i-m">{{p.venueName || '—'}}<text v-if="p.categoryLabel"> · {{p.categoryLabel}}</text></text>
				</view>
				<view class="i-stats">
					<text class="i-s">浏览 {{p.viewCount || 0}}</text>
					<text class="i-s">赞 {{p.likeCount || 0}}</text>
					<text class="i-s">评 {{p.commentCount || 0}}</text>
					<text class="i-s i-time">{{timeText(p.createdAt)}}</text>
				</view>
				<view class="i-actions">
					<text class="i-act" @click.stop="edit(p)">编辑</text>
					<text class="i-act danger" @click.stop="remove(p)">删除</text>
				</view>
			</view>
		</view>
		<view v-else-if="loaded" class="empty">还没有发布过帖子</view>
		<view v-else class="empty">加载中…</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { parseDate } from '../../utils/format.js'

	export default {
		data() {
			return {
				list: [],
				loaded: false
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				try {
					this.list = (await api.getMyVenuePosts()) || []
				} catch (e) {
					this.list = []
				}
				this.loaded = true
			},
			statusText(s) {
				return s === 'Hidden' ? '待审核（已隐藏）' : '正常'
			},
			statusClass(s) {
				return s === 'Hidden' ? 'warn' : 'ok'
			},
			timeText(s) {
				const d = parseDate(s)
				if (!d) return ''
				const p = (x) => (x < 10 ? '0' + x : x)
				return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`
			},
			open(id) {
				uni.navigateTo({ url: `/pages/community/post?id=${id}` })
			},
			edit(p) {
				const name = encodeURIComponent(p.venueName || '')
				uni.navigateTo({ url: `/pages/community/edit?id=${p.id}&venueId=${p.venueId}&venueName=${name}` })
			},
			remove(p) {
				uni.showModal({
					title: '删除帖子',
					content: '确定删除这条帖子吗？删除后不可恢复。',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.deleteVenuePost(p.id)
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
	.page { padding-bottom: 40rpx; }
	.item { display: flex; flex-direction: column; gap: 8rpx; }
	.i-top { display: flex; align-items: center; gap: 12rpx; }
	.i-title { flex: 1; min-width: 0; font-size: 28rpx; font-weight: 600; color: #2B2B27; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.i-status { flex-shrink: 0; font-size: 20rpx; border-radius: 8rpx; padding: 4rpx 12rpx; }
	.i-status.ok { color: var(--primary); background: var(--primary-bg); }
	.i-status.warn { color: #B85450; background: #FBEDEC; }
	.i-ex { display: block; font-size: 24rpx; color: #55554F; line-height: 1.4; overflow: hidden; text-overflow: ellipsis; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
	.i-meta { display: block; }
	.i-m { font-size: 22rpx; color: #B0B0AB; }
	.i-stats { display: flex; align-items: center; gap: 20rpx; }
	.i-s { font-size: 20rpx; color: #B0B0AB; }
	.i-time { margin-left: auto; }
	.i-actions { display: flex; gap: 32rpx; margin-top: 6rpx; padding-top: 14rpx; border-top: 1rpx solid #F0EFEA; }
	.i-act { font-size: 24rpx; color: var(--primary); }
	.i-act.danger { color: #B85450; }
	.empty { display: flex; align-items: center; justify-content: center; min-height: 60vh; color: #B0B0AB; font-size: 24rpx; }
</style>
