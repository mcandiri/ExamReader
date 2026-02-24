// ExamReader v2 - Chart rendering with vanilla JS and Canvas

window.ExamReaderCharts = {

    renderScoreDistribution: function (elementId, dataJson) {
        const container = document.getElementById(elementId);
        if (!container) return;

        const data = JSON.parse(dataJson);
        container.innerHTML = '';

        const canvas = document.createElement('canvas');
        canvas.width = container.clientWidth || 600;
        canvas.height = 300;
        container.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        const padding = { top: 30, right: 20, bottom: 50, left: 50 };
        const chartWidth = canvas.width - padding.left - padding.right;
        const chartHeight = canvas.height - padding.top - padding.bottom;

        // data is array of { label, count }
        const maxCount = Math.max(...data.map(d => d.count), 1);
        const barWidth = Math.min(chartWidth / data.length - 4, 50);
        const totalBarsWidth = data.length * (barWidth + 4);
        const startX = padding.left + (chartWidth - totalBarsWidth) / 2;

        // Background grid
        ctx.strokeStyle = '#30363d';
        ctx.lineWidth = 1;
        const gridLines = 5;
        for (let i = 0; i <= gridLines; i++) {
            const y = padding.top + (chartHeight / gridLines) * i;
            ctx.beginPath();
            ctx.moveTo(padding.left, y);
            ctx.lineTo(canvas.width - padding.right, y);
            ctx.stroke();

            // Y-axis labels
            const val = Math.round(maxCount - (maxCount / gridLines) * i);
            ctx.fillStyle = '#8b949e';
            ctx.font = '11px -apple-system, sans-serif';
            ctx.textAlign = 'right';
            ctx.fillText(val.toString(), padding.left - 8, y + 4);
        }

        // Bars
        const colors = ['#f85149', '#f85149', '#db6d28', '#db6d28', '#d29922', '#d29922', '#58a6ff', '#58a6ff', '#3fb950', '#3fb950'];
        data.forEach((d, i) => {
            const barHeight = (d.count / maxCount) * chartHeight;
            const x = startX + i * (barWidth + 4);
            const y = padding.top + chartHeight - barHeight;

            // Bar with rounded top
            const color = colors[i % colors.length] || '#58a6ff';
            ctx.fillStyle = color;
            ctx.beginPath();
            const radius = 3;
            ctx.moveTo(x, y + radius);
            ctx.arcTo(x, y, x + barWidth, y, radius);
            ctx.arcTo(x + barWidth, y, x + barWidth, y + barHeight, radius);
            ctx.lineTo(x + barWidth, padding.top + chartHeight);
            ctx.lineTo(x, padding.top + chartHeight);
            ctx.closePath();
            ctx.fill();

            // Count label on top
            if (d.count > 0) {
                ctx.fillStyle = '#e6edf3';
                ctx.font = 'bold 11px -apple-system, sans-serif';
                ctx.textAlign = 'center';
                ctx.fillText(d.count.toString(), x + barWidth / 2, y - 6);
            }

            // X-axis label
            ctx.fillStyle = '#8b949e';
            ctx.font = '11px -apple-system, sans-serif';
            ctx.textAlign = 'center';
            ctx.fillText(d.label, x + barWidth / 2, padding.top + chartHeight + 20);
        });

        // Axes
        ctx.strokeStyle = '#30363d';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(padding.left, padding.top);
        ctx.lineTo(padding.left, padding.top + chartHeight);
        ctx.lineTo(canvas.width - padding.right, padding.top + chartHeight);
        ctx.stroke();
    },

    renderPassFailChart: function (elementId, passCount, failCount) {
        const container = document.getElementById(elementId);
        if (!container) return;

        container.innerHTML = '';

        const canvas = document.createElement('canvas');
        const size = Math.min(container.clientWidth || 250, 250);
        canvas.width = size;
        canvas.height = size;
        container.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        const centerX = size / 2;
        const centerY = size / 2;
        const outerRadius = size / 2 - 20;
        const innerRadius = outerRadius * 0.6;
        const total = passCount + failCount;

        if (total === 0) {
            ctx.fillStyle = '#8b949e';
            ctx.font = '14px -apple-system, sans-serif';
            ctx.textAlign = 'center';
            ctx.fillText('No data', centerX, centerY);
            return;
        }

        const passAngle = (passCount / total) * Math.PI * 2;
        const startAngle = -Math.PI / 2;

        // Pass arc
        ctx.beginPath();
        ctx.arc(centerX, centerY, outerRadius, startAngle, startAngle + passAngle);
        ctx.arc(centerX, centerY, innerRadius, startAngle + passAngle, startAngle, true);
        ctx.closePath();
        ctx.fillStyle = '#3fb950';
        ctx.fill();

        // Fail arc
        ctx.beginPath();
        ctx.arc(centerX, centerY, outerRadius, startAngle + passAngle, startAngle + Math.PI * 2);
        ctx.arc(centerX, centerY, innerRadius, startAngle + Math.PI * 2, startAngle + passAngle, true);
        ctx.closePath();
        ctx.fillStyle = '#f85149';
        ctx.fill();

        // Center text
        const passPercent = Math.round((passCount / total) * 100);
        ctx.fillStyle = '#e6edf3';
        ctx.font = 'bold 24px -apple-system, sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(passPercent + '%', centerX, centerY - 8);
        ctx.fillStyle = '#8b949e';
        ctx.font = '11px -apple-system, sans-serif';
        ctx.fillText('Pass Rate', centerX, centerY + 14);

        // Legend
        const legendY = size - 8;
        ctx.fillStyle = '#3fb950';
        ctx.fillRect(centerX - 70, legendY - 8, 10, 10);
        ctx.fillStyle = '#8b949e';
        ctx.font = '11px -apple-system, sans-serif';
        ctx.textAlign = 'left';
        ctx.fillText('Pass: ' + passCount, centerX - 56, legendY);

        ctx.fillStyle = '#f85149';
        ctx.fillRect(centerX + 10, legendY - 8, 10, 10);
        ctx.fillStyle = '#8b949e';
        ctx.fillText('Fail: ' + failCount, centerX + 24, legendY);
    },

    renderDifficultyChart: function (elementId, dataJson) {
        const container = document.getElementById(elementId);
        if (!container) return;

        const data = JSON.parse(dataJson);
        container.innerHTML = '';

        const barHeight = 28;
        const gap = 6;
        const canvasHeight = Math.max((barHeight + gap) * data.length + 60, 100);

        const canvas = document.createElement('canvas');
        canvas.width = container.clientWidth || 600;
        canvas.height = canvasHeight;
        container.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        const padding = { top: 20, right: 60, bottom: 20, left: 60 };
        const chartWidth = canvas.width - padding.left - padding.right;

        data.forEach((d, i) => {
            const y = padding.top + i * (barHeight + gap);
            const width = (d.value / 100) * chartWidth;

            // Label
            ctx.fillStyle = '#8b949e';
            ctx.font = '12px -apple-system, sans-serif';
            ctx.textAlign = 'right';
            ctx.textBaseline = 'middle';
            ctx.fillText(d.label, padding.left - 10, y + barHeight / 2);

            // Background bar
            ctx.fillStyle = '#21262d';
            ctx.beginPath();
            ctx.roundRect(padding.left, y, chartWidth, barHeight, 4);
            ctx.fill();

            // Value bar
            let color;
            if (d.value >= 80) color = '#3fb950';
            else if (d.value >= 60) color = '#58a6ff';
            else if (d.value >= 40) color = '#d29922';
            else color = '#f85149';

            if (width > 0) {
                ctx.fillStyle = color;
                ctx.beginPath();
                ctx.roundRect(padding.left, y, Math.max(width, 8), barHeight, 4);
                ctx.fill();
            }

            // Value label
            ctx.fillStyle = '#e6edf3';
            ctx.font = 'bold 11px -apple-system, sans-serif';
            ctx.textAlign = 'left';
            ctx.fillText(Math.round(d.value) + '%', padding.left + width + 8, y + barHeight / 2);
        });
    },

    downloadFile: function (fileName, contentType, base64Content) {
        const link = document.createElement('a');
        link.download = fileName;
        link.href = 'data:' + contentType + ';base64,' + base64Content;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
};
