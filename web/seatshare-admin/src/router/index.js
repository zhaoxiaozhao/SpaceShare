import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  { path: '/login', component: () => import('../views/Login.vue') },
  {
    path: '/',
    component: () => import('../layout/Layout.vue'),
    redirect: '/dashboard',
    children: [
      { path: 'dashboard', component: () => import('../views/Dashboard.vue'), meta: { title: '数据统计' } },
      { path: 'users', component: () => import('../views/Users.vue'), meta: { title: '用户管理' } },
      { path: 'admins', component: () => import('../views/Admins.vue'), meta: { title: '管理员管理' } },
      { path: 'venues', component: () => import('../views/Venues.vue'), meta: { title: '场馆管理' } },
      { path: 'venue-map', component: () => import('../views/VenueMap.vue'), meta: { title: '座位排布' } },
      { path: 'reservations', component: () => import('../views/Reservations.vue'), meta: { title: '预约管理' } },
      { path: 'reports', component: () => import('../views/Reports.vue'), meta: { title: '举报管理' } },
      { path: 'config', component: () => import('../views/Config.vue'), meta: { title: '系统配置' } },
      { path: 'audit', component: () => import('../views/AuditLogs.vue'), meta: { title: '审计日志' } }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('admin_token')
  if (to.path !== '/login' && !token) {
    next('/login')
  } else {
    next()
  }
})

export default router
