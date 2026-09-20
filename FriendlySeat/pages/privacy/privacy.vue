<template>
	<page-meta :page-style="pageThemeStyle" />
		<view class="doc-page">
		<view class="doc-section">
			<text class="doc-title">友邻座《隐私保护指引》</text>
			<text class="doc-sub">版本日期：2026年8月</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">一、概述</text>
			<text class="doc-p">友邻座非常重视您的个人信息和隐私保护。您的学习/阅读记录仅用于生成您的统计与个人报告，平台以聚合形式进行统计分析，我们仅收集提供服务所必需的最少信息，并严格按照本指引使用和保护。您使用本服务即表示您同意本指引的描述。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">二、我们收集的信息</text>
			<text class="doc-p">1. 必要信息：</text>
			<text class="doc-p">· 微信用户标识（OpenID）：用于识别您的账号，是登录和服务的必需信息。</text>
			<text class="doc-p">· 预约记录：您预约、取消、到座、使用座位的历史记录。</text>
			<text class="doc-p">· 座位分享记录：您分享座位的相关信息。</text>
			<text class="doc-p">· 候补记录：您加入候补、候补成功或取消候补的记录。</text>
			<text class="doc-p">· 账号状态：您的信用分、履约记录、账号状态等。</text>
			<text class="doc-p">2. 按功能需要的信息：</text>
			<text class="doc-p">· 昵称、头像：用于在分享、预约时展示您的身份。头像由您主动上传，存储于微信云存储。</text>
			<text class="doc-p">· 位置信息：仅在您使用"附近场馆"或"确认到座"功能时获取，用于粗粒度的场馆范围校验，不会向其他用户展示您的精确位置。您可随时拒绝授权，拒绝后仍可手动选择场馆使用基本功能。</text>
			<text class="doc-p">· 订阅消息授权：经您主动授权后，用于向您推送预约、候补、信用等相关服务通知。</text>
			<text class="doc-p">· 您主动发布的内容：如活动信息（标题、介绍、海报）、换座意向、座位分享备注等，用于向其他用户展示和提供信息共享服务。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">三、我们收集信息的目的</text>
			<text class="doc-p">1. 实现账号登录和身份识别。</text>
			<text class="doc-p">2. 提供座位浏览、分享、预约、候补、到座等服务。</text>
			<text class="doc-p">3. 维护信用体系，保障座位共享秩序。</text>
			<text class="doc-p">4. 处理举报、申诉和违规处罚。</text>
			<text class="doc-p">5. 生成您的学习/阅读统计与个人报告（如学习时长、阅读记录），并以聚合、去标识化的形式用于统计分析和改善服务质量，不涉及个人身份识别。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">四、信息的使用与共享</text>
			<text class="doc-p">1. 我们不会向任何第三方出售您的个人信息。</text>
			<text class="doc-p">2. 仅在与服务直接相关的范围内展示必要信息，例如：分享座位时展示您的昵称和头像。</text>
			<text class="doc-p">3. 不会向其他用户展示您的手机号、精确位置、设备信息等非必要个人资料。</text>
			<text class="doc-p">4. 法律法规要求或经您明确同意时，方可对外提供。</text>
			<text class="doc-p">5. 以下信息会在相应功能页面对其他用户可见：您的昵称、头像，以及您主动发布的内容（座位分享信息、换座意向、活动标题/介绍/海报、活动报名、书单的书名/作者/推荐语、场馆交流板的帖子与评论及其配图、座位便签、活动留言）。请勿在这些内容中填写或诱导他人提供联系方式、身份信息等个人隐私。</text>
			<text class="doc-p">6. 书单分享默认仅通过链接访问。只有您主动开启"公开到热门书单榜"后，该书的书单才会在热门书单榜中向其他用户展示（展示您的昵称、头像及书单内容），您可随时在书单页关闭公开；书单被举报且核实后将从榜单下架。</text>
			<text class="doc-p">7. 您发布的内容不得包含：座位买卖、转让、出租、收费等交易信息，广告导流、联系方式，以及违法或不良信息。平台会对发布内容进行审核（含敏感词过滤与人工审核），并对违规内容予以删除或限制相关功能。</text>
			<text class="doc-p">8. 场馆交流板、座位便签、活动留言等为公开内容，其他用户可见并可举报。被举报内容将先自动隐藏并由人工审核，违规内容会被删除，情节严重的将限制账号相关功能。</text>
			<text class="doc-p">9. 为满足法律法规要求与争议处理、监管核查需要，您发布的内容及其处理记录会留存一定期限（不少于 6 个月）。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">五、信息的存储与保护</text>
			<text class="doc-p">1. 我们采取技术和管理措施保护您的信息安全，包括加密传输、访问控制、日志审计等。</text>
			<text class="doc-p">2. 您的个人信息将保存于中国大陆境内的服务器。</text>
			<text class="doc-p">3. 您上传的头像等文件存储于微信云存储，并按微信云存储的安全规则进行保护。</text>
			<text class="doc-p">4. 我们仅在实现服务目的所需的期限内保存您的信息，超出期限将进行删除或匿名化处理。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">六、您的权利</text>
			<text class="doc-p">您有权：</text>
			<text class="doc-p">1. 查看您的账号信息和信用记录。</text>
			<text class="doc-p">2. 修改您的昵称、头像等资料。</text>
			<text class="doc-p">3. 删除账号（通过联系我们申请，我们将依法删除您的个人信息）。</text>
			<text class="doc-p">4. 撤回授权：您可随时关闭相关系统权限（如定位），或在微信设置中关闭订阅消息授权。关闭后不影响使用基本功能。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">七、未成年人保护</text>
			<text class="doc-p">我们非常重视未成年人个人信息保护。本服务中的社区交流功能（如场馆交流板发帖与评论）不面向未满 14 周岁的未成年人开放；若您是未满 14 周岁的未成年人，请在监护人陪同下使用本服务，并征得监护人同意，不要发布任何内容。</text>
		</view>

		<view class="doc-section">
			<text class="doc-h2">八、第三方服务</text>
			<text class="doc-p">本服务可能集成微信小程序运行环境、微信云存储等第三方服务，这些服务将按照其自身的隐私政策处理相关信息，建议您一并了解。</text>
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
		padding: 24rpx;
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
		font-size: 28rpx;
		font-weight: 600;
		color: var(--primary);
		margin-bottom: 12rpx;
	}
	.doc-p {
		display: block;
		font-size: 24rpx;
		line-height: 1.4;
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