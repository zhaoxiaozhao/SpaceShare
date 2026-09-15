<template>
	<image v-if="url" :src="url" mode="aspectFill" :style="boxStyle" />
	<view v-else class="av-gen" :style="[boxStyle, { backgroundColor: bg }]">
		<text class="av-text" :style="{ fontSize: fontRpx + 'rpx' }">{{ initial }}</text>
	</view>
</template>

<script>
	import { getTheme } from '../../utils/theme.js'

	export default {
		name: 'Avatar',
		props: {
			url: { type: String, default: '' },
			name: { type: String, default: '' },
			size: { type: Number, default: 80 }
		},
		computed: {
			boxStyle() {
				return {
					width: this.size + 'rpx',
					height: this.size + 'rpx',
					borderRadius: '50%',
					flexShrink: 0
				}
			},
			initial() {
				const n = (this.name || '').trim()
				return n ? n.slice(0, 1) : '友'
			},
			bg() {
				return getTheme().primary
			},
			fontRpx() {
				return Math.round(this.size * 0.42)
			}
		}
	}
</script>

<style scoped>
	.av-gen {
		display: flex;
		align-items: center;
		justify-content: center;
		overflow: hidden;
	}
	.av-text {
		color: #FFFFFF;
		font-weight: 600;
		line-height: 1;
	}
</style>
