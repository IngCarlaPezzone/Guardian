(() => {
  const svg = (name, attrs = {}) => {
    const node = document.createElementNS("http://www.w3.org/2000/svg", name);
    Object.entries(attrs).forEach(([key, value]) => node.setAttribute(key, value));
    return node;
  };
  const text = (value, attrs = {}) => {
    const node = svg("text", attrs);
    node.textContent = value;
    return node;
  };
  const css = getComputedStyle(document.documentElement);
  const colors = {
    total: css.getPropertyValue("--trend-total").trim(),
    math: css.getPropertyValue("--trend-math").trim(),
    comprehension: css.getPropertyValue("--trend-comprehension").trim(),
    other: css.getPropertyValue("--trend-other").trim(),
  };
  const palettes = {
    math: [colors.math, "#5a9b77", "#87b89b", "#1f6748"],
    comprehension: [colors.comprehension, "#d69a64", "#e5b98f", "#9d5d2e"],
    skills: ["#7656a8", "#b15f92", "#2f7f96", "#a56d35", "#5d788f", "#9b5e62"],
  };
  const colorFor = (key, index, category, global) => {
    if (global) return colors[key] || colors.other;
    const palette = palettes[category] || palettes.skills;
    return palette[index % palette.length];
  };
  const addTooltip = (host) => {
    const tip = document.createElement("div");
    tip.className = "trend-tooltip";
    host.append(tip);
    return {
      show(event, lines) {
        tip.replaceChildren(...lines.map((line) => { const item = document.createElement("div"); item.textContent = line; return item; }));
        tip.hidden = false;
        const box = host.getBoundingClientRect();
        tip.style.left = `${Math.min(Math.max(event.clientX - box.left + 12, 8), box.width - tip.offsetWidth - 8)}px`;
        tip.style.top = `${Math.max(event.clientY - box.top - tip.offsetHeight - 12, 8)}px`;
      },
      hide() { tip.hidden = true; },
    };
  };
  const maxOf = (values) => Math.max(1, ...values);
  document.querySelectorAll("[data-metrics-trend]").forEach((host) => {
    const points = JSON.parse(host.querySelector(".trend-data").textContent);
    const category = host.dataset.category;
    const global = host.dataset.global === "true";
    const series = [];
    const known = new Set();
    points.forEach((point) => point.series.forEach((item) => {
      if (!known.has(item.key)) { known.add(item.key); series.push({ key: item.key, label: item.label }); }
    }));
    const legend = document.createElement("div");
    legend.className = "trend-legend";
    if (global) {
      const total = document.createElement("span");
      total.innerHTML = `<i style="--series:${colors.total}"></i>Intentos totales`;
      legend.append(total);
    }
    series.forEach((item, index) => {
      const entry = document.createElement("span");
      entry.innerHTML = `<i style="--series:${colorFor(item.key, index, category, global)}"></i>${item.label} · misiones e intentos`;
      legend.append(entry);
    });
    host.append(legend);
    const width = Math.max(660, points.length * 76);
    const height = 330, left = 54, right = 54, top = 22, bottom = 72;
    const plotWidth = width - left - right, plotHeight = height - top - bottom;
    const missionMax = maxOf(points.map((point) => point.missions));
    const attemptMax = maxOf(points.map((point) => point.attempts));
    const chart = svg("svg", { viewBox: `0 0 ${width} ${height}`, role: "img", "aria-label": "Evolución diaria de misiones e intentos" });
    const tooltip = addTooltip(host);
    const x = (index) => left + plotWidth * ((index + .5) / points.length);
    const yMission = (value) => top + plotHeight - value / missionMax * plotHeight;
    const yAttempt = (value) => top + plotHeight - value / attemptMax * plotHeight;
    for (let step = 0; step <= 4; step += 1) {
      const y = top + plotHeight * step / 4;
      chart.append(svg("line", { x1: left, y1: y, x2: width - right, y2: y, class: "trend-grid-line" }));
      chart.append(text(String(Math.round(missionMax * (4 - step) / 4)), { x: left - 8, y: y + 4, "text-anchor": "end", class: "trend-axis" }));
      chart.append(text(String(Math.round(attemptMax * (4 - step) / 4)), { x: width - right + 8, y: y + 4, class: "trend-axis" }));
    }
    chart.append(text("Misiones", { x: left, y: 12, class: "trend-axis-title" }));
    chart.append(text("Intentos", { x: width - right, y: 12, "text-anchor": "end", class: "trend-axis-title" }));
    const barWidth = Math.min(42, plotWidth / points.length * .58);
    points.forEach((point, index) => {
      let stacked = 0;
      point.series.forEach((item, seriesIndex) => {
        if (!item.missions) return;
        const segmentHeight = item.missions / missionMax * plotHeight;
        const rect = svg("rect", { x: x(index) - barWidth / 2, y: top + plotHeight - stacked - segmentHeight, width: barWidth, height: segmentHeight, fill: colorFor(item.key, seriesIndex, category, global), class: "trend-bar" });
        rect.addEventListener("mousemove", (event) => tooltip.show(event, [point.label, `Misiones: ${point.missions}`, ...point.series.map((entry) => `${entry.label}: ${entry.missions}`)]));
        rect.addEventListener("mouseleave", tooltip.hide);
        chart.append(rect);
        stacked += segmentHeight;
      });
      const label = text(point.label, { x: x(index), y: height - bottom + 16, "text-anchor": "end", transform: `rotate(-42 ${x(index)} ${height - bottom + 16})`, class: "trend-axis trend-date" });
      chart.append(label);
    });
    const drawLine = (label, values, color) => {
      const path = values.map((value, index) => `${index ? "L" : "M"}${x(index)} ${yAttempt(value)}`).join(" ");
      chart.append(svg("path", { d: path, fill: "none", stroke: color, class: "trend-line" }));
      values.forEach((value, index) => {
        const point = svg("circle", { cx: x(index), cy: yAttempt(value), r: 4, fill: color, class: "trend-point" });
        point.addEventListener("mousemove", (event) => tooltip.show(event, [points[index].label, `${label}: ${value}`]));
        point.addEventListener("mouseleave", tooltip.hide);
        chart.append(point);
      });
    };
    if (global) drawLine("Intentos totales", points.map((point) => point.attempts), colors.total);
    series.forEach((item, index) => drawLine(`${item.label} · intentos`, points.map((point) => (point.series.find((entry) => entry.key === item.key) || { attempts: 0 }).attempts), colorFor(item.key, index, category, global)));
    host.append(chart);
  });
})();
