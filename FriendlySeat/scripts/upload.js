// 微信小程序代码上传脚本（miniprogram-ci）
// 用法：node scripts/upload.js [--preview]
// 默认上传体验版；--preview 生成预览二维码
const ci = require('miniprogram-ci')

const APPID = 'wxffc16ae0e1c7e85e'
const PRIVATE_KEY_PATH = 'keys/private.wxffc16ae0e1c7e85e.key'
const PROJECT_PATH = '.'
const VERSION = process.env.CI_VERSION || '1.0.0'
const DESC = process.env.CI_DESC || '友邻座小程序自动上传'

function getProject() {
	const fs = require('fs')
	const path = require('path')
	return new ci.Project({
		appid: APPID,
		type: 'miniProgram',
		projectPath: PROJECT_PATH,
		privateKeyPath: PRIVATE_KEY_PATH,
		ignores: ['node_modules/**/*', 'unpackage/**/*', 'keys/**/*']
	})
}

async function main() {
	const project = getProject()
	const preview = process.argv.includes('--preview')

	if (preview) {
		const res = await ci.preview({
			project,
			desc: DESC,
			setting: { es6: true, minify: true },
			qrcodeFormat: 'image',
			qrcodeOutputDest: 'preview-qrcode.png'
		})
		console.log('预览二维码已生成: preview-qrcode.png')
	} else {
		await ci.upload({
			project,
			version: VERSION,
			desc: DESC,
			setting: { es6: true, minify: true, minifyWXSS: true }
		})
		console.log(`上传成功: ${APPID} v${VERSION}`)
	}
}

main().catch((e) => {
	console.error('上传失败:', e)
	process.exit(1)
})