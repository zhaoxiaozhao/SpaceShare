import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '../router'

// 优先使用环境变量指定的后台地址（如 .env.prod 中 VITE_ADMIN_API_BASE 指向生产域名），
// 否则回退到相对路径 /admin-api（docker 内由 nginx 反代；dev 由 vite proxy 转发）
const apiBase = import.meta.env.VITE_ADMIN_API_BASE
  ? `${import.meta.env.VITE_ADMIN_API_BASE}/api/v1/admin`
  : '/admin-api/api/v1/admin'

const request = axios.create({
  baseURL: apiBase,
  timeout: 15000
})

request.interceptors.request.use((config) => {
  const token = localStorage.getItem('admin_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

request.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const status = error.response?.status
    const message = error.response?.data?.message || '请求失败'
    if (status === 401) {
      localStorage.removeItem('admin_token')
      router.push('/login')
    }
    ElMessage.error(message)
    return Promise.reject(error)
  }
)

export default request
