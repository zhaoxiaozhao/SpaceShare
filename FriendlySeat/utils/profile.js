import { CLOUD_ENV } from './config.js'

const NICK_ADJ = [
	'友善', '温暖', '开朗', '元气', '好奇', '踏实', '阳光', '安静', '热心', '从容',
	'快乐', '认真', '温柔', '明亮', '清爽', '松弛', '专注', '低调', '有趣', '干净',
	'悠哉', '清醒', '自在', '安然', '温润', '诗意', '悠闲', '温和', '热爱', '勇敢',
	'真诚', '淡定', '淡然', '朝气', '知足', '惜时', '微光', '慢热', '佛系', '碎念'
]
const NICK_NOUN = [
	'鲸鱼', '山茶', '橙子', '布丁', '奶茶', '星星', '小雨', '猫头鹰', '青柠', '云朵',
	'向日葵', '小鹿', '咖啡', '抹茶', '绿豆', '小满', '月亮', '银河', '纸鸢', '风铃',
	'竹影', '松果', '海豚', '柠檬', '麦田', '萤火', '木棉', '露水', '桃子', '栗子',
	'山雀', '海盐', '苔藓', '蘑菇', '柚子', '蜜桃', '白鹭', '蒲公英', '小舟', '暖阳'
]

export function randomNickname() {
	const a = NICK_ADJ[Math.floor(Math.random() * NICK_ADJ.length)]
	const n = NICK_NOUN[Math.floor(Math.random() * NICK_NOUN.length)]
	return `${a}${n}`
}

export function uploadAvatar(filePath) {
	return uploadImage(filePath, 'avatars')
}

// 通用：上传图片到云存储，返回 fileID
export function uploadImage(filePath, folder = 'uploads') {
	return new Promise((resolve, reject) => {
		if (!wx || !wx.cloud) {
			reject(new Error('当前环境不支持云存储'))
			return
		}
		const ext = (filePath.match(/\.(\w+)$/) || [, 'png'])[1]
		const cloudPath = `${folder}/${Date.now()}-${Math.floor(Math.random() * 100000)}.${ext}`
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

// 云存储 fileID 转临时 https 链接（用于分享卡片图片等）
export function getTempFileUrl(fileID) {
	return new Promise((resolve) => {
		if (!fileID || !wx || !wx.cloud) {
			resolve('')
			return
		}
		if (!String(fileID).startsWith('cloud://')) {
			resolve(fileID)
			return
		}
		wx.cloud.getTempFileURL({
			fileList: [fileID],
			config: {
				env: CLOUD_ENV
			},
			success: (res) => {
				const f = res.fileList && res.fileList[0]
				resolve(f && f.tempFileURL ? f.tempFileURL : '')
			},
			fail: () => resolve('')
		})
	})
}
