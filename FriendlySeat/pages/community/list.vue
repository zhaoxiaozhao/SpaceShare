<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
	<view>
		<view class="head">
			<text class="head-title">{{venueName || '场馆交流板'}}</text>
			<text class="head-post" @click="goCreate">＋ 发帖</text>
		</view>
		<VenueFeed ref="feed" :venue-id="venueId" />
		<view class="bottom-space"></view>
	</view>
</template>

<script>
	export default {
		data() {
			return { venueId: 0, venueName: '' }
		},
		onLoad(options) {
			this.venueId = options.venueId ? Number(options.venueId) : 0
			this.venueName = options.venueName ? decodeURIComponent(options.venueName) : ''
		},
		onShow() {
			if (this.$refs.feed) this.$refs.feed.refresh()
		},
		methods: {
			goCreate() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				this.goEdit()
			},
			goEdit() {
				const name = encodeURIComponent(this.venueName || '')
				uni.navigateTo({ url: `/pages/community/edit?venueId=${this.venueId}&venueName=${name}` })
			}
		}
	}
</script>

<style scoped>
	.head { display: flex; align-items: center; justify-content: space-between; margin: 16rpx 20rpx 0; }
	.head-title { font-size: 34rpx; font-weight: 700; color: #2B2B27; }
	.head-post { font-size: 24rpx; color: var(--primary); }
	.bottom-space { height: 60rpx; }
</style>
