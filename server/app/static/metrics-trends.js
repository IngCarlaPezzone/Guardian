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
    total: css.getPropertyValue("--trend-total").trim(), math: css.getPropertyValue("--trend-math").trim(),
    comprehension: css.getPropertyValue("--trend-comprehension").trim(), other: css.getPropertyValue("--trend-other").trim(),
  };
  const palettes = {
    math: [colors.math, "#5a9b77", "#87b89b", "#1f6748"],
    comprehension: [colors.comprehension, "#d69a64", "#e5b98f", "#9d5d2e"],
    skills: ["#7656a8", "#b15f92", "#2f7f96", "#a56d35", "#5d788f", "#9b5e62"],
  };
  const colorFor = (key, index, category, global) => global ? (colors[key] || colors.other) : (palettes[category] || palettes.skills)[index % (palettes[category] || palettes.skills).length];
  const addLegendEntry = (group, kind, color, label) => {
    const entry = document.createElement("span"), marker = document.createElement("i");
    marker.className = `trend-legend-${kind}`;
    marker.style.setProperty("--series", color);
    entry.append(marker, document.createTextNode(label));
    group.append(entry);
  };
  const addLegendGroup = (legend, color, label, includeMissions) => {
    const group = document.createElement("div");
    group.className = "trend-legend-group";
    if (includeMissions) addLegendEntry(group, "bar", color, `Misiones · ${label}`);
    addLegendEntry(group, "line", color, `Intentos · ${label}`);
    legend.append(group);
  };
  const addTooltip = (host) => {
    const tip = document.createElement("div");
    tip.className = "trend-tooltip";
    tip.hidden = true;
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
  const maxWithHeadroom = (values) => Math.max(1, ...values) + 1;

  document.querySelectorAll("[data-metrics-trend]").forEach((host) => {
    const points = JSON.parse(host.querySelector(".trend-data").textContent);
    const category = host.dataset.category, global = host.dataset.global === "true", series = [], known = new Set();
    points.forEach((point) => point.series.forEach((item) => {
      if (!known.has(item.key)) { known.add(item.key); series.push({ key: item.key, label: item.label }); }
    }));
    const legend = document.createElement("div");
    legend.className = "trend-legend";
    series.forEach((item, index) => {
      const color = colorFor(item.key, index, category, global);
      addLegendGroup(legend, color, item.label, true);
    });
    if (global) addLegendGroup(legend, colors.total, "Total", false);
    host.append(legend);

    const width = Math.max(600, points.length * 68), height = 245, left = 48, right = 48, top = 18, bottom = 62;
    host.classList.toggle("trend-chart-compact", width <= 600);
    legend.style.width = `${width}px`;
    const plotWidth = width - left - right, plotHeight = height - top - bottom;
    const missionMax = maxWithHeadroom(points.map((point) => point.missions)), attemptMax = maxWithHeadroom(points.map((point) => point.attempts));
    const chart = svg("svg", { width, height, viewBox: `0 0 ${width} ${height}`, role: "img", "aria-label": "Evolución diaria de misiones e intentos" });
    const tooltip = addTooltip(host);
    const x = (index) => left + plotWidth * ((index + .5) / points.length);
    const yMission = (value) => top + plotHeight - value / missionMax * plotHeight;
    const yAttempt = (value) => top + plotHeight - value / attemptMax * plotHeight;
    const axisCenter = top + plotHeight / 2;
    for (let step = 0; step <= 4; step += 1) {
      const y = top + plotHeight * step / 4;
      chart.append(svg("line", { x1: left, y1: y, x2: width - right, y2: y, class: "trend-grid-line" }));
      chart.append(text(String(Math.round(missionMax * (4 - step) / 4)), { x: left - 7, y: y + 3, "text-anchor": "end", class: "trend-axis" }));
      chart.append(text(String(Math.round(attemptMax * (4 - step) / 4)), { x: width - right + 7, y: y + 3, class: "trend-axis" }));
    }
    chart.append(text("Misiones", { x: 13, y: axisCenter, "text-anchor": "middle", transform: `rotate(-90 13 ${axisCenter})`, class: "trend-axis-title" }));
    chart.append(text("Intentos", { x: width - 13, y: axisCenter, "text-anchor": "middle", transform: `rotate(90 ${width - 13} ${axisCenter})`, class: "trend-axis-title" }));
    const barWidth = Math.min(36, plotWidth / points.length * .55);
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
      chart.append(text(point.label, { x: x(index), y: height - bottom + 14, "text-anchor": "end", transform: `rotate(-42 ${x(index)} ${height - bottom + 14})`, class: "trend-axis trend-date" }));
    });
    const lineValues = [];
    const drawLine = (label, values, color) => {
      lineValues.push({ label, values });
      const path = values.map((value, index) => `${index ? "L" : "M"}${x(index)} ${yAttempt(value)}`).join(" ");
      chart.append(svg("path", { d: path, fill: "none", stroke: "#667085", class: "trend-line trend-line-outline" }));
      chart.append(svg("path", { d: path, fill: "none", stroke: color, class: "trend-line" }));
      values.forEach((value, index) => chart.append(svg("circle", { cx: x(index), cy: yAttempt(value), r: 3.7, fill: color, class: "trend-point" })));
    };
    if (global) drawLine("Intentos · Total", points.map((point) => point.attempts), colors.total);
    series.forEach((item, index) => drawLine(`Intentos · ${item.label}`, points.map((point) => (point.series.find((entry) => entry.key === item.key) || { attempts: 0 }).attempts), colorFor(item.key, index, category, global)));
    points.forEach((point, index) => {
      const coordinates = new Set(lineValues.map((line) => yAttempt(line.values[index]).toFixed(2)));
      coordinates.forEach((y) => {
        const hit = svg("circle", { cx: x(index), cy: y, r: 9, fill: "transparent", class: "trend-hit" });
        hit.addEventListener("mousemove", (event) => tooltip.show(event, [point.label, ...lineValues.map((line) => `${line.label}: ${line.values[index]}`)]));
        hit.addEventListener("mouseleave", tooltip.hide);
        chart.append(hit);
      });
    });
    host.append(chart);
  });
})();
