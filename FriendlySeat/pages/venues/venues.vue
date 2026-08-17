<template>
	<view>
		<view class="search-bar">
			<input class="search-input" v-model="keyword" placeholder="搜索场馆名称" confirm-type="search" @confirm="search" />
			<text class="search-btn" @click="search">搜索</text>
		</view>

		<scroll-view scroll-x class="city-scroll" v-if="cities.length">
			<view
				class="city-chip"
				:class="{ active: cityId === null }"
				@click="selectCity(null)"
			>全部</view>
			<view
				class="city-chip"
				:class="{ active: cityId === c.id }"
				v-for="c in cities"
				:key="c.id"
				@click="selectCity(c.id)"
			>{{c.name}}</view>
		</scroll-view>

		<view class="card venue-card" v-for="v in venues" :key="v.id" @click="goVenue(v.id)">
			<view class="venue-main">
				<text class="venue-name">{{v.name}}</text>
				<text class="venue-type">{{v.type}}</text>
				<text class="venue-addr">{{v.address}}</text>
				<text class="venue-hours">{{v.openingTime}} - {{v.closingTime}}</text>
			</view>
			<view class="venue-meta">
				<text class="venue-count">{{v.seatCount}} 座位</text>
				<text class="venue-available" v-if="v.availableCount > 0">可预约 {{v.availableCount}}</text>
				<text class="venue-available none" v-else>暂无分享</text>
				<text class="venue-distance" v-if="v.distanceKm">{{v.distanceKm}}km</text>
			</view>
		</view>

		<view v-if="!venues.length" class="empty">没有找到场馆</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				cities: [],
				venues: [],
				cityId: null,
				keyword: ''
			}
		},
		onShow() {
			this.loadCities()
			this.loadVenues()
		},
		onPullDownRefresh() {
			this.loadVenues().then(() => uni.stopPullDownRefresh())
		},
		methods: {
			async loadCities() {
				try {
					this.cities = await api.getCities()
				} catch (e) {}
			},
			async loadVenues() {
				try {
					const location = await this.getLocation()
					this.venues = await api.getVenues({
						cityId: this.cityId,
						keyword: this.keyword,
						lat: location.latitude,
						lng: location.longitude
					})
				} catch (e) {
					uni.showToast({ title: '加载失败', icon: 'none' })
				}
			},
			getLocation() {
				return new Promise((resolve) => {
					uni.getLocation({
						type: 'gcj02',
						success: (res) => resolve({ latitude: res.latitude, longitude: res.longitude }),
						fail: () => resolve({ latitude: 30.5728, longitude: 104.0668 })
					})
				})
			},
			search() {
				this.loadVenues()
			},
			selectCity(id) {
				this.cityId = id
				this.loadVenues()
			},
			goVenue(id) {
				uni.navigateTo({ url: `/pages/venue/venue?id=${id}` })
			}
		}
	}
</script>

<style scoped>
	.search-bar {
		display: flex;
		align-items: center;
		padding: 20rpx;
		gap: 16rpx;
	}
	.search-input {
		flex: 1;
		background: #FFFFFF;
		border-radius: 40rpx;
		padding: 16rpx 30rpx;
		font-size: 28rpx;
	}
	.search-btn {
		color: #3A8A7E;
		font-size: 28rpx;
	}
	.city-scroll {
		white-space: nowrap;
		padding: 0 20rpx 10rpx;
	}
	.city-chip {
		display: inline-block;
		padding: 10rpx 26rpx;
		margin-right: 16rpx;
		background: #FFFFFF;
		border-radius: 30rpx;
		font-size: 26rpx;
		color: #55554F;
	}
	.city-chip.active {
		background: #3A8A7E;
		color: #FFFFFF;
	}
	.venue-card {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.venue-main {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 6rpx;
	}
	.venue-name {
		font-size: 30rpx;
		font-weight: 600;
	}
	.venue-type {
		font-size: 22rpx;
		color: #3A8A7E;
	}
	.venue-addr {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.venue-hours {
		font-size: 22rpx;
		color: #B0B0AB;
	}
	.venue-meta {
		display: flex;
		flex-direction: column;
		align-items: flex-end;
		gap: 6rpx;
	}
	.venue-count {
		font-size: 26rpx;
		color: #55554F;
	}
	.venue-available {
		font-size: 24rpx;
		color: #3A8A7E;
	}
	.venue-available.none {
		color: #B85450;
	}
	.venue-distance {
		font-size: 22rpx;
		color: #8A8A86;
	}
</style>
