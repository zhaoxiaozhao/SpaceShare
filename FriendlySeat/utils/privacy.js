// 隐私合规：微信「用户隐私保护指引」授权流程
// 微信在调用隐私接口（位置/相册/相册写入等）且用户尚未同意时，会触发 onNeedPrivacyAuthorization，
// 由开发者弹窗征求同意。此处注册全局监听，通过 uni 事件通知页面上的 PrivacyPopup 组件渲染弹窗。

let pendingResolve = null

export function setupPrivacy() {
	// #ifdef MP-WEIXIN
	if (typeof wx !== 'undefined' && wx.onNeedPrivacyAuthorization) {
		wx.onNeedPrivacyAuthorization((resolve) => {
			pendingResolve = resolve
			uni.$emit('privacy:need')
		})
	}
	// #endif
}

export function resolvePrivacy(agree) {
	if (pendingResolve) {
		const fn = pendingResolve
		pendingResolve = null
		try {
			fn({ event: agree ? 'agree' : 'disagree' })
		} catch (e) {}
	}
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
