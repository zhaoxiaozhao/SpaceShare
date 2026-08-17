// 友邻座 API 调用配置
// 生产环境使用微信云托管「云调用」（wx.cloud.callContainer），无需配置 request 合法域名

// ===== 云调用配置（微信云托管）=====
export const CLOUD_ENV = 'prod-d3gm8r5478549fe3b' // 云开发/云托管环境 ID
export const CLOUD_SERVICE = 'friendlyseat-api' // 云托管服务名
export const USE_CLOUD = true // true=云调用；false=普通 HTTPS

// ===== 普通 HTTPS 兜底（USE_CLOUD=false 时使用）=====
export const BASE_URL = 'https://friendlyseat-api-298258-11-1470097997.sh.run.tcloudbase.com/api/v1'
// 本地开发
// export const BASE_URL = 'http://localhost:5000/api/v1'

export default BASE_URL
