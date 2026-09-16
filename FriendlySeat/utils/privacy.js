// 隐私合规：微信「用户隐私保护指引」授权流程
// 微信在调用隐私接口（位置/相册/相册写入等）且用户尚未同意时，会触发 onNeedPrivacyAuthorization，
// 由开发者弹窗征求同意（同意按钮需为 open-type="agreePrivacyAuthorization" 的 button，并在 resolve 时带 buttonId）。

let pendings = []
const AGREE_BUTTON_ID = 'privacy-agree-btn'

export function setupPrivacy() {
	// #ifdef MP-WEIXIN
	if (typeof wx !== 'undefined' && wx.onNeedPrivacyAuthorization) {
		wx.onNeedPrivacyAuthorization((resolve) => {
			// 已同意过则直接放行（避免反复弹窗）
			if (uni.getStorageSync('privacy_agreed')) {
				try { resolve({ event: 'agree', buttonId: AGREE_BUTTON_ID }) } catch (e) {}
				return
			}
			pendings.push(resolve)
			uni.$emit('privacy:need')
		})
	}
	// #endif
}

export function resolvePrivacy(agree) {
	if (agree) uni.setStorageSync('privacy_agreed', 1)
	const list = pendings
	pendings = []
	list.forEach((fn) => {
		try {
			fn(agree ? { event: 'agree', buttonId: AGREE_BUTTON_ID } : { event: 'disagree' })
		} catch (e) {}
	})
}

export function openPrivacyContract() {
	// #ifdef MP-WEIXIN
	if (typeof wx !== 'undefined' && wx.openPrivacyContract) {
		wx.openPrivacyContract({ fail: () => {} })
		return true
	}
	// #endif
	uni.navigateTo({ url: '/pages/privacy/privacy' })
	return false
}

export const PRIVACY_AGREE_BUTTON_ID = AGREE_BUTTON_ID
