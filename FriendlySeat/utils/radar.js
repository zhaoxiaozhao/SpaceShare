// 雷达图（蜘蛛网）绘制：适配小程序 canvas 2d 上下文
// dimensions: [{ label, score }]，score 取值 1..4

function hexA(hex, a) {
	const h = String(hex || '#6BAF8B').replace('#', '')
	const r = parseInt(h.slice(0, 2), 16)
	const g = parseInt(h.slice(2, 4), 16)
	const b = parseInt(h.slice(4, 6), 16)
	return `rgba(${r},${g},${b},${a})`
}

export function drawRadar(ctx, opts) {
	const cx = opts.cx
	const cy = opts.cy
	const radius = opts.radius
	const dims = (opts.dimensions || []).filter((d) => d && d.label)
	const n = dims.length
	if (!n) return

	const color = opts.color || '#6BAF8B'
	const step = (Math.PI * 2) / n
	const angle = (i) => -Math.PI / 2 + i * step
	const pt = (i, r) => [cx + Math.cos(angle(i)) * r, cy + Math.sin(angle(i)) * r]

	// 网格（4 圈）
	for (let level = 1; level <= 4; level++) {
		const r = (radius * level) / 4
		ctx.beginPath()
		for (let i = 0; i < n; i++) {
			const [x, y] = pt(i, r)
			if (i === 0) ctx.moveTo(x, y)
			else ctx.lineTo(x, y)
		}
		ctx.closePath()
		ctx.strokeStyle = level === 4 ? '#D9D6CC' : '#ECEAE3'
		ctx.lineWidth = 1
		ctx.stroke()
	}

	// 轴线
	for (let i = 0; i < n; i++) {
		const [x, y] = pt(i, radius)
		ctx.beginPath()
		ctx.moveTo(cx, cy)
		ctx.lineTo(x, y)
		ctx.strokeStyle = '#ECEAE3'
		ctx.lineWidth = 1
		ctx.stroke()
	}

	// 数据多边形
	ctx.beginPath()
	dims.forEach((d, i) => {
		const r = radius * Math.max(0.1, d.score / 4)
		const [x, y] = pt(i, r)
		if (i === 0) ctx.moveTo(x, y)
		else ctx.lineTo(x, y)
	})
	ctx.closePath()
	ctx.fillStyle = hexA(color, 0.22)
	ctx.fill()
	ctx.strokeStyle = color
	ctx.lineWidth = 2
	ctx.stroke()

	// 顶点
	dims.forEach((d, i) => {
		const r = radius * Math.max(0.1, d.score / 4)
		const [x, y] = pt(i, r)
		ctx.beginPath()
		ctx.arc(x, y, 3, 0, Math.PI * 2)
		ctx.fillStyle = color
		ctx.fill()
	})

	// 维度名
	ctx.font = opts.labelFont || '14px sans-serif'
	ctx.fillStyle = opts.labelColor || '#8A8A86'
	ctx.textAlign = 'center'
	ctx.textBaseline = 'middle'
	const lr = radius + 20
	dims.forEach((d, i) => {
		const [x, y] = pt(i, lr)
		ctx.fillText(d.label, x, y)
	})
	ctx.textBaseline = 'alphabetic'
}
