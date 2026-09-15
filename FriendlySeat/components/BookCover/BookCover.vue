<template>
	<image
		v-if="url"
		:src="url"
		mode="aspectFill"
		:style="boxStyle"
	/>
	<view v-else class="bc-gen" :style="[boxStyle, { backgroundColor: bg }]">
		<text class="bc-text" :style="{ fontSize: fontRpx + 'rpx' }">{{ initial }}</text>
	</view>
</template>

<script>
	import { bookCoverColor } from '../../utils/theme.js'

	export default {
		name: 'BookCover',
		props: {
			url: { type: String, default: '' },
			title: { type: String, default: '' },
			width: { type: Number, default: 110 },
			height: { type: Number, default: 150 },
			radius: { type: Number, default: 10 }
		},
		computed: {
			boxStyle() {
				return {
					width: this.width + 'rpx',
					height: this.height + 'rpx',
					borderRadius: this.radius + 'rpx',
					flexShrink: 0
				}
			},
			initial() {
				const t = (this.title || '').trim()
				return t ? t.slice(0, 1) : '书'
			},
			bg() {
				return bookCoverColor(this.title)
			},
			fontRpx() {
				return Math.round(this.height * 0.42)
			}
		}
	}
</script>

<style scoped>
	.bc-gen {
		display: flex;
		align-items: center;
		justify-content: center;
		overflow: hidden;
	}
	.bc-text {
		color: #FFFFFF;
		font-weight: 600;
		line-height: 1;
	}
</style>
