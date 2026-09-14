<template>
	<view class="op">
		<view class="op-trigger" :class="{ disabled }" @click="open">
			<text class="op-text" :class="{ ph: !selectedText }">{{ selectedText || placeholder }}</text>
			<text class="op-arrow">▾</text>
		</view>
		<view v-if="visible" class="op-mask" @click="close">
			<view class="op-sheet" @click.stop>
				<view class="op-head">
					<text class="op-title">{{ title }}</text>
				</view>
				<scroll-view scroll-y class="op-list">
					<view
						class="op-item"
						:class="{ active: i === value }"
						v-for="(item, i) in range"
						:key="i"
						@click="pick(i)"
					>
						<text class="op-item-text">{{ textOf(item) }}</text>
						<text class="op-check" v-if="i === value">✓</text>
					</view>
				</scroll-view>
			</view>
		</view>
	</view>
</template>

<script>
	export default {
		name: 'OptionPicker',
		props: {
			range: { type: Array, default: () => [] },
			rangeKey: { type: String, default: '' },
			value: { type: Number, default: -1 },
			title: { type: String, default: '请选择' },
			placeholder: { type: String, default: '请选择' },
			disabled: { type: Boolean, default: false }
		},
		data() {
			return { visible: false }
		},
		computed: {
			selectedText() {
				const item = this.range[this.value]
				return item === undefined || item === null ? '' : this.textOf(item)
			}
		},
		methods: {
			textOf(item) {
				if (this.rangeKey && item && typeof item === 'object') return item[this.rangeKey]
				return item
			},
			open() {
				if (this.disabled) return
				this.visible = true
			},
			close() {
				this.visible = false
			},
			pick(i) {
				this.visible = false
				this.$emit('change', i)
			}
		}
	}
</script>

<style scoped>
	.op {
		width: 100%;
		min-width: 0;
	}
	.op-trigger {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 12rpx;
		background: #F8F7F3;
		border: 1rpx solid #ECEAE3;
		border-radius: 12rpx;
		padding: 18rpx 20rpx;
	}
	.op-trigger.disabled {
		opacity: 0.5;
	}
	.op-text {
		flex: 1;
		min-width: 0;
		font-size: 28rpx;
		color: #33332E;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.op-text.ph {
		color: #A5A39D;
	}
	.op-arrow {
		flex-shrink: 0;
		font-size: 24rpx;
		color: #A5A39D;
	}
	.op-mask {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.4);
		display: flex;
		align-items: flex-end;
		z-index: 1000;
	}
	.op-sheet {
		width: 100%;
		max-height: 70vh;
		background: #fff;
		border-radius: 24rpx 24rpx 0 0;
		padding: 28rpx 0 12rpx;
		display: flex;
		flex-direction: column;
	}
	.op-head {
		padding: 0 32rpx 20rpx;
		text-align: center;
	}
	.op-title {
		font-size: 30rpx;
		font-weight: 700;
	}
	.op-list {
		max-height: 55vh;
	}
	.op-item {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 28rpx 32rpx;
		font-size: 30rpx;
		color: #33332E;
	}
	.op-item.active {
		color: var(--primary);
		font-weight: 600;
	}
	.op-item-text {
		flex: 1;
	}
	.op-check {
		color: var(--primary);
		font-size: 30rpx;
	}
</style>
