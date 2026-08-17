<template>
	<view class="doc-page">
		<view class="doc-section">
			<text class="doc-title">友邻座《隐私保护指引》</text>
			<text class="doc-sub">版本日期：2026年8月</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">一、概述</text>
			<text class="doc-p">友邻座非常重视您的个人信息和隐私保护。我们仅收集提供服务所必需的最少信息，并严格按照本指引使用和保护。您使用本服务即表示您同意本指引的描述。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">二、我们收集的信息</text>
			<text class="doc-p">1. 必要信息：</text>
			<text class="doc-p">· 微信用户标识（OpenID）：用于识别您的账号，是登录和服务的必需信息。</text>
			<text class="doc-p">· 预约记录：您预约、取消、到座、使用座位的历史记录。</text>
			<text class="doc-p">· 座位分享记录：您分享座位的相关信息。</text>
			<text class="doc-p">· 账号状态：您的信用分、风险分、封禁状态等。</text>
			<text class="doc-p">2. 按功能需要的信息：</text>
			<text class="doc-p">· 昵称、头像：用于在分享、预约时展示您的身份。</text>
			<text class="doc-p">· 位置信息：仅在您使用"附近场馆"或"确认到座"功能时获取，用于粗粒度的场馆范围校验，不会向其他用户展示您的精确位置。</text>
			<text class="doc-p">· 联系方式：仅当您主动填写并在预约成功后经分享者授权，才向相关用户展示。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">三、我们收集信息的目的</text>
			<text class="doc-p">1. 实现账号登录和身份识别。</text>
			<text class="doc-p">2. 提供座位浏览、分享、预约、到座等服务。</text>
			<text class="doc-p">3. 维护信用体系，保障座位共享秩序。</text>
			<text class="doc-p">4. 处理举报、申诉和违规处罚。</text>
			<text class="doc-p">5. 用于统计分析和改善服务质量（不涉及个人身份识别）。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">四、信息的使用与共享</text>
			<text class="doc-p">1. 我们不会向任何第三方出售您的个人信息。</text>
			<text class="doc-p">2. 仅在与服务直接相关的范围内展示必要信息，例如：分享座位时展示您的昵称和头像。</text>
			<text class="doc-p">3. 联系方式仅在预约成功后、分享者主动授权的情况下对预约者展示，预约结束后自动隐藏。</text>
			<text class="doc-p">4. 不会向其他用户展示您的手机号、精确位置、设备信息等非必要个人资料。</text>
			<text class="doc-p">5. 法律法规要求或经您明确同意时，方可对外提供。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">五、信息的存储与保护</text>
			<text class="doc-p">1. 我们采取技术和管理措施保护您的信息安全，包括加密传输、访问控制、日志审计等。</text>
			<text class="doc-p">2. 您的个人信息将保存于中国大陆境内的服务器。</text>
			<text class="doc-p">3. 我们仅在实现服务目的所需的期限内保存您的信息，超出期限将进行删除或匿名化处理。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">六、您的权利</text>
			<text class="doc-p">您有权：</text>
			<text class="doc-p">1. 查看您的账号信息和信用记录。</text>
			<text class="doc-p">2. 修改您的昵称、头像等资料。</text>
			<text class="doc-p">3. 管理或删除您的联系方式。</text>
			<text class="doc-p">4. 删除账号（通过联系我们申请，我们将依法删除您的个人信息）。</text>
			<text class="doc-p">5. 撤回授权：您可随时关闭相关系统权限（如定位），关闭后不影响使用基本功能。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">七、未成年人保护</text>
			<text class="doc-p">我们非常重视未成年人个人信息保护。若您是未满14周岁的未成年人，请在监护人陪同下使用本服务，并征得监护人同意。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">八、第三方服务</text>
			<text class="doc-p">本服务可能集成微信小程序运行环境等第三方服务，这些服务将按照其自身的隐私政策处理相关信息，建议您一并了解。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">九、政策更新</text>
			<text class="doc-p">我们可能不时更新本隐私政策。更新后将在小程序内提示，重大变更将显著告知。您继续使用即视为接受更新后的政策。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">十、联系我们</text>
			<text class="doc-p">如您对本隐私政策有任何疑问、意见或建议，或需要行使您的个人信息权利，请通过小程序内的"问题反馈"或举报入口联系我们，我们将在合理期限内回复。</text>
		</view>

		<view class="doc-end">友邻座 · 一席相邻，善意相续</view>
	</view>
</template>

<style scoped>
	.doc-page {
		padding: 30rpx;
	}
	.doc-section {
		margin-bottom: 30rpx;
	}
	.doc-title {
		display: block;
		font-size: 36rpx;
		font-weight: 700;
		color: #2B2B27;
		margin-bottom: 8rpx;
	}
	.doc-sub {
		display: block;
		font-size: 22rpx;
		color: #B0B0AB;
	}
	.doc-h2 {
		display: block;
		font-size: 30rpx;
		font-weight: 600;
		color: #3A8A7E;
		margin-bottom: 12rpx;
	}
	.doc-p {
		display: block;
		font-size: 26rpx;
		line-height: 1.7;
		color: #55554F;
		margin-bottom: 8rpx;
	}
	.doc-end {
		text-align: center;
		font-size: 22rpx;
		color: #B0B0AB;
		margin-top: 40rpx;
		padding-bottom: 40rpx;
	}
</style>
