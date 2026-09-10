<template>
	<page-meta :page-style="pageThemeStyle" />
		<view>
		<view class="search-bar">
			<input class="search-input" v-model="keyword" placeholder="搜索场馆名称" confirm-type="search" @confirm="search" />
			<text class="search-btn" @click="search">搜索</text>
		</view>

		<scroll-view scroll-x class="city-scroll" v-if="cities.length">
			<view
				class="city-chip"
				:class="{ active: mode === 'nearby' }"
				@click="selectNearby"
			>📍 附近</view>
			<view
				class="city-chip"
				:class="{ active: mode === 'city' && cityId === null }"
				@click="selectCity(null)"
			>全部</view>
			<view
				class="city-chip"
				:class="{ active: mode === 'city' && cityId === c.id }"
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

		<view class="list-footer">
			<text v-if="loading" class="footer-text">加载中…</text>
			<text v-else-if="venues.length && !hasMore" class="footer-text">已加载全部场馆</text>
		</view>

		<view v-if="!venues.length && !loading" class="empty">没有找到场馆</view>
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
				keyword: '',
				mode: 'nearby',
				page: 1,
				pageSize: 20,
				hasMore: true,
				loading: false,
				location: null,
				autoLocated: false
			}
		},
		onShow() {
			this.loadCities()
			this.initLocation()
		},
		onShareAppMessage() {
			const city = this.cities.find(c => c.id === this.cityId)
			return {
				title: city ? `${city.name}共享座位 - 友邻座` : '友邻座 - 发现身边的共享座位',
				path: '/pages/index/index'
			}
		},
		onShareTimeline() {
			const city = this.cities.find(c => c.id === this.cityId)
			return {
				title: city ? `${city.name}共享座位 - 友邻座` : '友邻座 - 发现身边的共享座位'
			}
		},
		onPullDownRefresh() {
			this.loadVenues(true).then(() => uni.stopPullDownRefresh())
		},
		onReachBottom() {
			this.loadMore()
		},
		methods: {
			async loadCities() {
				try {
					this.cities = await api.getCities()
				} catch (e) {}
			},
			// 首次进入：定位 + 自动识别所在城市（成功后切到该城市，否则保持附近模式）
			async initLocation() {
				const location = await this.getLocation()
				this.location = location
				if (this.autoLocated) {
					this.loadVenues(true)
					return
				}
				this.autoLocated = true
				try {
					const city = await api.getNearestCity(location.latitude, location.longitude)
					if (city && city.id) {
						this.mode = 'city'
						this.cityId = city.id
					}
				} catch (e) {}
				this.loadVenues(true)
			},
			async loadVenues(reset) {
				if (this.loading) return
				this.loading = true
				try {
					const location = this.location || await this.getLocation()
					const targetPage = reset ? 1 : this.page
					const params = {
						lat: location.latitude,
						lng: location.longitude,
						page: targetPage,
						pageSize: this.pageSize
					}
					if (this.mode === 'nearby') {
						params.radiusKm = 20
					} else {
						params.cityId = this.cityId
					}
					if (this.keyword) params.keyword = this.keyword
					const data = await api.getVenues(params)
					if (reset) {
						this.venues = data
						this.page = 1
					} else {
						this.venues = this.venues.concat(data)
					}
					this.hasMore = data.length >= this.pageSize
				} catch (e) {
					uni.showToast({ title: '加载失败', icon: 'none' })
				} finally {
					this.loading = false
				}
			},
			loadMore() {
				if (!this.hasMore || this.loading) return
				this.page += 1
				this.loadVenues()
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
				this.page = 1
				this.loadVenues(true)
			},
			selectNearby() {
				this.mode = 'nearby'
				this.cityId = null
				this.page = 1
				this.loadVenues(true)
			},
			selectCity(id) {
				this.mode = 'city'
				this.cityId = id
				this.page = 1
				this.loadVenues(true)
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
		color: var(--primary);
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
		background: var(--primary);
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
		color: var(--primary);
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
		color: var(--primary);
	}
	.venue-available.none {
		color: #B85450;
	}
	.venue-distance {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.list-footer {
		padding: 24rpx 0 40rpx;
		text-align: center;
	}
	.footer-text {
		font-size: 24rpx;
		color: #B0B0AB;
	}
</style>
