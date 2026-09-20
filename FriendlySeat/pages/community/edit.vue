<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
	<view class="page">
		<view class="card">
			<text class="label">板块</text>
			<scroll-view scroll-x class="cats" :show-scrollbar="false">
				<text class="cat" v-for="c in cats" :key="c.code" :class="{ on: category === c.code }" @click="category = c.code">{{c.label}}</text>
			</scroll-view>

			<text class="label">标题</text>
			<input class="input" v-model="title" :maxlength="50" placeholder="一句话说清楚（最多 50 字）" />

			<text class="label">内容</text>
			<textarea class="textarea" v-model="content" :maxlength="1000" placeholder="友善发言，请勿留联系方式或发布交易信息（最多 1000 字）" />
			<text class="count">{{content.length}}/1000</text>

			<view class="venue-row" v-if="venueName">
				<text class="venue-label">发布到</text>
				<text class="venue-name">{{venueName}}</text>
			</view>
		</view>

		<view class="actions">
			<button class="btn-primary" :loading="submitting" @click="submit">发布</button>
			<text class="hint">发布后内容将对其他人可见；被举报会先隐藏并由人工审核。</text>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { getAppOptions } from '../../utils/options.js'

	export default {
		data() {
			return {
				venueId: 0,
				venueName: '',
				category: 'chat',
				title: '',
				content: '',
				submitting: false
			}
		},
		computed: {
			cats() {
				return getAppOptions().venuePostCategories
			}
		},
		onLoad(options) {
			if (!uni.getStorageSync('token')) {
				uni.redirectTo({ url: '/pages/login/login' })
				return
			}
			this.venueId = options.venueId ? Number(options.venueId) : 0
			this.venueName = options.venueName ? decodeURIComponent(options.venueName) : ''
		},
		methods: {
			async submit() {
				if (this.submitting) return
				if (!this.venueId) {
					uni.showToast({ title: '缺少场馆信息', icon: 'none' })
					return
				}
				const title = (this.title || '').trim()
				const content = (this.content || '').trim()
				if (!title) {
					uni.showToast({ title: '请填写标题', icon: 'none' })
					return
				}
				if (!content) {
					uni.showToast({ title: '请填写内容', icon: 'none' })
					return
				}
				this.submitting = true
				uni.showLoading({ title: '发布中', mask: true })
				try {
					const post = await api.createVenuePost({ venueId: this.venueId, category: this.category, title, content })
					uni.hideLoading()
					uni.showToast({ title: '已发布', icon: 'success' })
					setTimeout(() => {
						uni.redirectTo({ url: `/pages/community/post?id=${post.id}` })
					}, 500)
				} catch (e) {
					uni.hideLoading()
					this.submitting = false
					uni.showToast({ title: e.message || '发布失败', icon: 'none' })
				}
			}
		}
	}
</script>

<style scoped>
	.page { padding-bottom: 40rpx; }
	.label { display: block; font-size: 26rpx; color: #8A8A86; margin: 24rpx 0 12rpx; }
	.label:first-child { margin-top: 0; }
	.cats { white-space: nowrap; }
	.cat { display: inline-block; padding: 10rpx 26rpx; margin-right: 14rpx; border-radius: 28rpx; font-size: 26rpx; background: #F1EFE9; color: #8A8A86; }
	.cat.on { background: var(--primary); color: #FFFFFF; }
	.input { background: #F7F5EF; border-radius: 12rpx; padding: 18rpx 22rpx; font-size: 28rpx; }
	.textarea { width: 100%; box-sizing: border-box; height: 320rpx; background: #F7F5EF; border-radius: 12rpx; padding: 18rpx 22rpx; font-size: 28rpx; }
	.count { display: block; text-align: right; font-size: 22rpx; color: #B0B0AB; margin-top: 8rpx; }
	.venue-row { display: flex; align-items: center; gap: 12rpx; margin-top: 20rpx; }
	.venue-label { font-size: 26rpx; color: #8A8A86; }
	.venue-name { font-size: 28rpx; color: var(--primary); font-weight: 600; }
	.actions { margin: 24rpx 20rpx 0; }
	.hint { display: block; font-size: 20rpx; color: #B0B0AB; margin-top: 16rpx; line-height: 1.5; }
</style>
