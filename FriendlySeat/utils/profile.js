import { CLOUD_ENV } from './config.js'

const NICK_ADJ = ['友善', '温暖', '开朗', '元气', '好奇', '踏实', '阳光', '安静', '热心', '从容', '快乐', '认真']
const NICK_NOUN = ['鲸鱼', '山茶', '橙子', '布丁', '奶茶', '星星', '小雨', '猫头鹰', '青柠', '云朵', '向日葵', '小鹿']

export function randomNickname() {
	const a = NICK_ADJ[Math.floor(Math.random() * NICK_ADJ.length)]
	const n = NICK_NOUN[Math.floor(Math.random() * NICK_NOUN.length)]
	return `友邻座-${a}${n}`
}

export function uploadAvatar(filePath) {
	return new Promise((resolve, reject) => {
		if (!wx || !wx.cloud) {
			reject(new Error('当前环境不支持云存储'))
			return
		}
		const ext = (filePath.match(/\.(\w+)$/) || [, 'png'])[1]
		const cloudPath = `avatars/${Date.now()}-${Math.floor(Math.random() * 100000)}.${ext}`
		wx.cloud.uploadFile({
			cloudPath,
			filePath,
			config: {
				env: CLOUD_ENV
			},
			success: (res) => resolve(res.fileID),
			fail: (err) => reject(err)
		})
	})
}
