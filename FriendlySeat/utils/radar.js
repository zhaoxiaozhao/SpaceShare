// 雷达图（蜘蛛网）绘制：适配小程序 canvas 2d 上下文
// dimensions: [{ label, score }]，score 取值 1..4
// options: cx, cy, radius, dimensions, color, labelColor, labelFont,
//          showScale(网格刻度 1-4), showScore(顶点分值), progress(0..1，用于动画)

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
	const progress = opts.progress == null ? 1 : Math.max(0, Math.min(1, opts.progress))
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

	// 网格刻度 1-4（沿正上方轴线）
	if (opts.showScale) {
		ctx.font = opts.scaleFont || '10px sans-serif'
		ctx.fillStyle = '#C4C2BB'
		ctx.textAlign = 'left'
		ctx.textBaseline = 'middle'
		for (let level = 1; level <= 4; level++) {
			const [x, y] = pt(0, (radius * level) / 4)
			ctx.fillText(String(level), x + 5, y)
		}
	}

	// 数据多边形
	const dataR = (i) => radius * Math.max(0.1, dims[i].score / 4) * progress
	ctx.beginPath()
	dims.forEach((d, i) => {
		const [x, y] = pt(i, dataR(i))
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
		const [x, y] = pt(i, dataR(i))
		ctx.beginPath()
		ctx.arc(x, y, 3, 0, Math.PI * 2)
		ctx.fillStyle = color
		ctx.fill()
	})

	// 顶点分值
	if (opts.showScore && progress > 0.9) {
		ctx.font = opts.scoreFont || '11px sans-serif'
		ctx.fillStyle = color
		ctx.textAlign = 'center'
		ctx.textBaseline = 'middle'
		dims.forEach((d, i) => {
			const r = Math.max(0.1, dims[i].score / 4)
			const vr = radius * r
			const [x, y] = pt(i, Math.max(12, vr - 15))
			ctx.fillText(String(d.score), x, y)
		})
	}

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

// 渐显动画：progress 0 → 1（ease-out），需要 canvas 节点（用于 requestAnimationFrame）
export function animateRadar(canvas, ctx, opts, duration = 700) {
	const W = opts.width || (opts.cx * 2)
	const H = opts.height || (opts.cy * 2)
	const raf = canvas && canvas.requestAnimationFrame ? canvas.requestAnimationFrame.bind(canvas) : null
	const start = Date.now()

	return new Promise((resolve) => {
		const frame = () => {
			const t = Math.min(1, (Date.now() - start) / duration)
			const eased = 1 - Math.pow(1 - t, 3)
			ctx.clearRect(0, 0, W, H)
			drawRadar(ctx, Object.assign({}, opts, { progress: eased }))
			if (t < 1) {
				if (raf) raf(frame)
				else setTimeout(frame, 16)
			} else {
				resolve()
			}
		}
		frame()
	})
}
