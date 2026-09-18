import request from './request'

export const authApi = {
  login: (data) => request.post('/auth/login', data),
  me: () => request.get('/auth/me')
}

export const statsApi = {
  overview: () => request.get('/stats/overview'),
  trend: (days = 14) => request.get(`/stats/trend?days=${days}`),
  learning: (days = 7) => request.get(`/stats/learning?days=${days}`),
  learningUsers: (take = 100) => request.get(`/stats/learning/users?take=${take}`)
}

export const userApi = {
  admins: () => request.get(''),
  createAdmin: (data) => request.post('', data),
  setAdminStatus: (id, status) => request.post(`/${id}/status?status=${status}`),
  resetPassword: (id, newPassword) => request.post(`/${id}/password`, { newPassword }),
  all: (keyword) => request.get(`/all?keyword=${keyword || ''}`),
  detail: (id) => request.get(`/detail/${id}`),
  setUserStatus: (id, status) => request.post(`/detail/${id}/status?status=${status}`),
  adjustCredit: (id, change, reason) => request.post(`/detail/${id}/credit?change=${change}&reason=${encodeURIComponent(reason || '')}`),
  adjustRisk: (id, change, reason) => request.post(`/detail/${id}/risk?change=${change}&reason=${encodeURIComponent(reason || '')}`),
  sendDemoNotifications: (id) => request.post(`/detail/${id}/demo-notifications`)
}

export const venueApi = {
  cities: () => request.get('/cities'),
  createCity: (data) => request.post('/cities', data),
  updateCity: (id, data) => request.put(`/cities/${id}`, data),
  venues: () => request.get('/venues'),
  createVenue: (data) => request.post('/venues', data),
  updateVenue: (id, data) => request.put(`/venues/${id}`, data),
  setVenueStatus: (id, visible) => request.post(`/venues/${id}/status?visible=${visible}`),
  deleteVenue: (id) => request.delete(`/venues/${id}`),
  venueDetail: (id) => request.get(`/venues/${id}/detail`),
  addFloor: (data) => request.post('/floors', data),
  updateFloor: (id, data) => request.put(`/floors/${id}`, data),
  deleteFloor: (id) => request.delete(`/floors/${id}`),
  addArea: (data) => request.post('/areas', data),
  updateArea: (id, data) => request.put(`/areas/${id}`, data),
  deleteArea: (id) => request.delete(`/areas/${id}`),
  addZone: (data) => request.post('/zones', data),
  updateZone: (id, data) => request.put(`/zones/${id}`, data),
  deleteZone: (id) => request.delete(`/zones/${id}`),
  addSeat: (data) => request.post('/seats', data),
  updateSeat: (id, data) => request.put(`/seats/${id}`, data),
  deleteSeat: (id) => request.delete(`/seats/${id}`),
  setSeatStatus: (id, status) => request.post(`/seats/${id}/status?status=${status}`),
  addPoi: (data) => request.post('/pois', data),
  updatePoi: (id, data) => request.put(`/pois/${id}`, data),
  deletePoi: (id) => request.delete(`/pois/${id}`)
}

export const reportApi = {
  reports: (status) => request.get(`/reports?status=${status || ''}`),
  handle: (id, status, note) => request.post(`/reports/${id}/handle?status=${status}&note=${encodeURIComponent(note || '')}`),
  reservations: (status) => request.get(`/reservations?status=${status || ''}`),
  forceCancel: (id, reason) => request.post(`/reservations/${id}/force-cancel?reason=${encodeURIComponent(reason || '')}`),
  auditLogs: () => request.get('/audit-logs')
}

export const configApi = {
  all: () => request.get('/config'),
  update: (id, value) => request.put(`/config/${id}`, { value })
}

export const swapApi = {
  list: (status) => request.get(`/swaps?status=${status || ''}`),
  takeDown: (id) => request.post(`/swaps/${id}/take-down`)
}

export const activityApi = {
  list: (status) => request.get(`/activities?status=${status || ''}`),
  review: (id, approve, remark) => request.post(`/activities/${id}/review`, { approve, remark }),
  takeDown: (id) => request.post(`/activities/${id}/take-down`)
}

export const seatNoteApi = {
  list: (status) => request.get(`/seat-notes?status=${status || ''}`),
  review: (id, approve) => request.post(`/seat-notes/${id}/review`, { approve })
}

export const activityCommentApi = {
  list: (status) => request.get(`/activity-comments?status=${status || ''}`),
  review: (id, approve) => request.post(`/activity-comments/${id}/review`, { approve })
}
