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

			<text class="label">封面图（可选）</text>
			<view class="cover-wrap">
				<image v-if="coverImage" class="cover-img" :src="coverImage" mode="aspectFill" @click="chooseCover" />
				<view v-else class="cover-add" @click="chooseCover">
					<text class="cover-plus">＋</text>
					<text class="cover-tip">添加封面</text>
				</view>
				<text v-if="coverImage" class="cover-remove" @click="removeCover">删除</text>
			</view>

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
	import { uploadImage, getTempFileUrl } from '../../utils/profile.js'

	export default {
		data() {
			return {
				venueId: 0,
				venueName: '',
				category: 'chat',
				title: '',
				content: '',
				coverImage: '',
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
			chooseCover() {
				uni.chooseImage({
					count: 1,
					sizeType: ['compressed'],
					sourceType: ['album', 'camera'],
					success: async (res) => {
						const filePath = res.tempFilePaths && res.tempFilePaths[0]
						if (!filePath) return
						uni.showLoading({ title: '上传中', mask: true })
						try {
							this.coverImage = await uploadImage(filePath, 'posts')
						} catch (e) {
							uni.showToast({ title: '上传失败，请重试', icon: 'none' })
						} finally {
							uni.hideLoading()
						}
					}
				})
			},
			removeCover() {
				this.coverImage = ''
			},
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
					let coverImageUrl = null
					if (this.coverImage) {
						try { coverImageUrl = await getTempFileUrl(this.coverImage) } catch (e) {}
					}
					const post = await api.createVenuePost({
						venueId: this.venueId,
						category: this.category,
						title,
						content,
						coverImage: this.coverImage || null,
						coverImageUrl
					})
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
	.label { display: block; font-size: 24rpx; color: #8A8A86; margin: 19rpx 0 10rpx; }
	.label:first-child { margin-top: 0; }
	.cats { white-space: nowrap; }
	.cat { display: inline-block; padding: 8rpx 26rpx; margin-right: 14rpx; border-radius: 28rpx; font-size: 24rpx; background: #F1EFE9; color: #8A8A86; }
	.cat.on { background: var(--primary); color: #FFFFFF; }
	.input { background: #F7F5EF; border-radius: 12rpx; padding: 14rpx 22rpx; font-size: 26rpx; }
	.textarea { width: 100%; box-sizing: border-box; height: 320rpx; background: #F7F5EF; border-radius: 12rpx; padding: 14rpx 22rpx; font-size: 26rpx; }
	.count { display: block; text-align: right; font-size: 22rpx; color: #B0B0AB; margin-top: 8rpx; }
	.cover-wrap { display: flex; align-items: center; gap: 16rpx; margin-top: 4rpx; }
	.cover-img { width: 200rpx; height: 140rpx; border-radius: 12rpx; background: #F1EFE9; }
	.cover-add { width: 200rpx; height: 140rpx; border-radius: 12rpx; border: 2rpx dashed #D8D4C8; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 4rpx; }
	.cover-plus { font-size: 40rpx; color: #C4C2BB; line-height: 1; }
	.cover-tip { font-size: 22rpx; color: #B0B0AB; }
	.cover-remove { font-size: 24rpx; color: #B85450; }
	.venue-row { display: flex; align-items: center; gap: 10rpx; margin-top: 20rpx; }
	.venue-label { font-size: 24rpx; color: #8A8A86; }
	.venue-name { font-size: 26rpx; color: var(--primary); font-weight: 600; }
	.actions { margin: 19rpx 20rpx 0; }
	.hint { display: block; font-size: 20rpx; color: #B0B0AB; margin-top: 16rpx; line-height: 1.4; }
</style>
