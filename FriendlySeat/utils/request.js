import BASE_URL, { CLOUD_ENV, CLOUD_SERVICE, USE_CLOUD } from './config.js'

function request(path, { method = 'GET', data = {}, auth = true, loading = false } = {}) {
	return new Promise((resolve, reject) => {
		if (loading) {
			uni.showLoading({ title: '加载中', mask: true })
		}
		const token = uni.getStorageSync('token')
		const header = { 'Content-Type': 'application/json' }
		if (auth && token) {
			header.Authorization = `Bearer ${token}`
		}
		// 云调用需要指定服务名
		if (USE_CLOUD) {
			header['X-WX-SERVICE'] = CLOUD_SERVICE
		}

		const finish = () => { if (loading) uni.hideLoading() }

		if (USE_CLOUD && wx && wx.cloud) {
			// ===== 云调用方式（微信云托管）=====
			// 云调用 path 需带后端 api/v1 前缀，且不能带 ? 查询串（拆入 data）
			let cloudPath = path
			let cloudData = { ...data }
			const qIdx = path.indexOf('?')
			if (qIdx >= 0) {
				cloudPath = path.slice(0, qIdx)
				const qsStr = path.slice(qIdx + 1)
				qsStr.split('&').forEach(pair => {
					if (!pair) return
					const [k, v] = pair.split('=')
					if (k) cloudData[k] = decodeURIComponent(v || '')
				})
			}
			if (!cloudPath.startsWith('/api/v1/')) {
				cloudPath = '/api/v1' + cloudPath
			}
			wx.cloud.callContainer({
				config: {
					env: CLOUD_ENV
				},
				path: cloudPath,
				method,
				header,
				data: cloudData,
				success: (res) => {
					finish()
					const status = res.statusCode
					if (status >= 200 && status < 300) {
						resolve(res.data)
					} else {
						const message = (res.data && res.data.message) || '请求失败'
						if (status === 401) {
							uni.removeStorageSync('token')
							uni.removeStorageSync('user')
							uni.navigateTo({ url: '/pages/login/login' })
						}
						reject({ code: status, message, data: res.data })
					}
				},
				fail: (err) => {
					finish()
					reject({ code: -1, message: '云调用失败', data: err })
				}
			})
		} else {
			// ===== 普通 HTTPS 方式（兜底/本地开发）=====
			uni.request({
				url: BASE_URL + path,
				method,
				data,
				header,
				success: (res) => {
					finish()
					if (res.statusCode >= 200 && res.statusCode < 300) {
						resolve(res.data)
					} else {
						const message = (res.data && res.data.message) || '请求失败'
						if (res.statusCode === 401) {
							uni.removeStorageSync('token')
							uni.removeStorageSync('user')
							uni.navigateTo({ url: '/pages/login/login' })
						}
						reject({ code: res.statusCode, message, data: res.data })
					}
				},
				fail: (err) => {
					finish()
					reject({ code: -1, message: '网络异常，请检查网络连接' })
				}
			})
		}
	})
}

export const api = {
	get: (path, data, opts) => request(path, { method: 'GET', data, ...opts }),
	post: (path, data, opts) => request(path, { method: 'POST', data, ...opts }),
	patch: (path, data, opts) => request(path, { method: 'PATCH', data, ...opts }),
	del: (path, data, opts) => request(path, { method: 'DELETE', data, ...opts }),

	login: (code, nickname, avatarUrl) => request('/auth/wechat/login', {
		method: 'POST',
		data: { code, nickname, avatarUrl },
		auth: false
	}),
	getCities: () => request('/cities', { auth: false }),
	getVenues: (params) => request('/venues?' + qs(params), { auth: false }),
	getVenue: (id) => request(`/venues/${id}`, { auth: false }),
	getSeat: (id) => request(`/seats/${id}`, { auth: false }),
	getShares: (id) => request(`/seats/${id}/shares`, { auth: false }),
	getVenueShares: (id) => request(`/venues/${id}/shares`, { auth: false }),
	checkIn: (data) => request('/sessions/check-in', { method: 'POST', data }),
	getMySession: () => request('/sessions/my'),
	endSession: () => request('/sessions/end', { method: 'POST' }),
	createShare: (data) => request('/shares', { method: 'POST', data }),
	getMyShares: () => request('/shares/my'),
	cancelShare: (id) => request(`/shares/${id}`, { method: 'DELETE' }),
	getShare: (id) => request(`/shares/${id}`, { auth: false }),
	createReservation: (shareId) => request('/reservations', { method: 'POST', data: { shareId } }),
	getMyReservations: () => request('/reservations/my'),
	cancelReservation: (id) => request(`/reservations/${id}/cancel`, { method: 'POST' }),
	arrive: (id, lat, lng) => request(`/reservations/${id}/arrive?lat=${lat}&lng=${lng}`, { method: 'POST' }),
	complete: (id) => request(`/reservations/${id}/complete`, { method: 'POST' }),
	joinWaitlist: (shareId) => request(`/shares/${shareId}/waitlist`, { method: 'POST' }),
	getMyWaitlist: () => request('/waitlist/my'),
	cancelWaitlist: (id) => request(`/waitlist/${id}`, { method: 'DELETE' }),
	getCredit: () => request('/credit'),
	getContribution: () => request('/credit/contribution'),
	getMy: () => request('/me'),
	updateProfile: (data) => request('/me/update-profile', { method: 'POST', data }),
	getContacts: () => request('/me/contacts'),
	upsertContact: (data) => request('/me/contacts', { method: 'POST', data }),
	getShareContact: (id) => request(`/shares/${id}/contact`),
	getNotifications: (unread) => request(`/me/notifications?unread=${unread || ''}`),
	markNotificationsRead: () => request('/me/notifications/read', { method: 'POST' }),
	getUnreadCount: () => request('/me/notifications/unread-count'),
	getAds: (placement) => request(`/ads?placement=${placement || 'home_feed'}`, { auth: false }),
	createReport: (data) => request('/reports', { method: 'POST', data }),
	getMyReports: () => request('/reports/my')
}

function qs(params) {
	if (!params) return ''
	return Object.entries(params)
		.filter(([, v]) => v !== undefined && v !== null && v !== '')
		.map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
		.join('&')
}
