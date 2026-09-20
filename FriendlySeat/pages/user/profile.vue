<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
	<view v-if="profile" class="page">
		<!-- 基本信息 -->
		<view class="card head">
			<Avatar :url="profile.avatarUrl" :name="profile.nickname" :size="120" />
			<text class="nick">{{profile.nickname}}</text>
			<text class="meta">{{joinText}}<text v-if="profile.postCount"> · 公开帖子 {{profile.postCount}}</text></text>
		</view>

		<!-- 友邻画像 -->
		<view class="card">
			<text class="section-label">友邻画像</text>
			<block v-if="profile.personaPublic && profile.persona">
				<view class="pa-head">
					<text class="pa-type" :style="{ color: profile.persona.color || 'var(--primary)' }">{{profile.persona.typeName}}</text>
					<text class="pa-tag">已公开</text>
				</view>
				<text class="pa-scene" v-if="profile.persona.scene">{{profile.persona.scene}}</text>
				<view class="tags" v-if="profile.persona.tags && profile.persona.tags.length">
					<text class="tag" v-for="t in profile.persona.tags" :key="t">{{t}}</text>
				</view>
				<text class="pa-line" v-if="profile.persona.poetLine">「{{profile.persona.poetLine}}」</text>
			</block>
			<text v-else class="muted">TA 未公开友邻画像</text>
		</view>

		<!-- 公开帖子 -->
		<view class="card">
			<text class="section-label">TA 的公开帖子</text>
			<view v-if="profile.posts.length">
				<view class="p-item" v-for="p in profile.posts" :key="p.id" @click="openPost(p.id)">
					<view class="p-top">
						<text class="p-title">{{p.title}}</text>
						<text class="p-cat">{{p.categoryLabel}}</text>
					</view>
					<text class="p-ex">{{p.content}}</text>
					<text class="p-meta">{{metaText(p)}}</text>
				</view>
			</view>
			<text v-else class="muted">暂无公开帖子</text>
		</view>

		<view class="actions" v-if="!profile.isSelf">
			<button class="btn-outline" @click="report">举报该用户</button>
		</view>
	</view>
	<view v-else-if="loaded" class="empty">用户不存在</view>
	<view v-else class="empty">加载中…</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { parseDate } from '../../utils/format.js'

	export default {
		data() {
			return {
				id: 0,
				profile: null,
				loaded: false
			}
		},
		computed: {
			joinText() {
				const d = this.profile && parseDate(this.profile.joinedAt)
				if (!d) return '友邻座书友'
				return `${d.getFullYear()} 年 ${d.getMonth() + 1} 月加入`
			}
		},
		onLoad(options) {
			this.id = options && options.id ? Number(options.id) : 0
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				if (!this.id) {
					this.loaded = true
					return
				}
				try {
					this.profile = await api.getUserProfile(this.id)
					if (this.profile && this.profile.nickname) {
						uni.setNavigationBarTitle({ title: this.profile.nickname })
					}
				} catch (e) {
					this.profile = null
				}
				this.loaded = true
			},
			metaText(p) {
				const parts = []
				if (p.venueName) parts.push(p.venueName)
				parts.push(`赞 ${p.likeCount || 0}`)
				parts.push(`评 ${p.commentCount || 0}`)
				return parts.join(' · ')
			},
			openPost(id) {
				uni.navigateTo({ url: `/pages/community/post?id=${id}` })
			},
			report() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				const nick = encodeURIComponent((this.profile && this.profile.nickname) || '')
				uni.navigateTo({ url: `/pages/report/report?targetType=User&targetId=${this.id}&targetUserId=${this.id}&targetNickname=${nick}` })
			}
		}
	}
</script>

<style scoped>
	.page { padding-bottom: 40rpx; }
	.head { display: flex; flex-direction: column; align-items: center; gap: 12rpx; padding: 32rpx 22rpx; }
	.nick { font-size: 32rpx; font-weight: 700; color: #2B2B27; }
	.meta { font-size: 22rpx; color: #B0B0AB; }
	.section-label { display: block; font-size: 26rpx; font-weight: 600; margin-bottom: 14rpx; }
	.muted { display: block; font-size: 24rpx; color: #B0B0AB; padding: 8rpx 0; }
	.pa-head { display: flex; align-items: center; gap: 12rpx; }
	.pa-type { font-size: 30rpx; font-weight: 700; }
	.pa-tag { font-size: 20rpx; color: var(--primary); background: var(--primary-bg); border-radius: 8rpx; padding: 4rpx 12rpx; }
	.pa-scene { display: block; font-size: 24rpx; color: #55554F; margin-top: 10rpx; line-height: 1.4; }
	.tags { display: flex; flex-wrap: wrap; gap: 12rpx; margin-top: 14rpx; }
	.tag { font-size: 22rpx; padding: 4rpx 16rpx; border-radius: 999rpx; background: var(--primary-bg); color: var(--primary); margin-right: 0; }
	.pa-line { display: block; font-size: 24rpx; color: #8A8A86; margin-top: 14rpx; line-height: 1.4; }
	.p-item { padding: 16rpx 0; border-bottom: 1rpx solid #F0EFEA; }
	.p-item:last-child { border-bottom: none; }
	.p-top { display: flex; align-items: center; gap: 12rpx; }
	.p-title { flex: 1; min-width: 0; font-size: 28rpx; font-weight: 600; color: #2B2B27; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.p-cat { flex-shrink: 0; font-size: 20rpx; color: var(--primary); background: var(--primary-bg); border-radius: 8rpx; padding: 4rpx 12rpx; }
	.p-ex { display: block; font-size: 24rpx; color: #55554F; margin-top: 8rpx; overflow: hidden; text-overflow: ellipsis; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
	.p-meta { display: block; font-size: 20rpx; color: #B0B0AB; margin-top: 8rpx; }
	.actions { margin: 20rpx; }
	.empty { display: flex; align-items: center; justify-content: center; min-height: 60vh; color: #B0B0AB; font-size: 24rpx; }
</style>
