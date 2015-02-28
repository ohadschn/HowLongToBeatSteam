AmCharts.AmSerialChart = AmCharts.Class({
    inherits: AmCharts.AmRectangularChart,
    construct: function (a) {
        this.type = "serial";
        AmCharts.AmSerialChart.base.construct.call(this, a);
        this.cname = "AmSerialChart";
        this.theme = a;
        this.createEvents("changed");
        this.columnSpacing = 5;
        this.columnSpacing3D = 0;
        this.columnWidth = .8;
        this.updateScrollbar = !0;
        var b = new AmCharts.CategoryAxis(a);
        b.chart = this;
        this.categoryAxis = b;
        this.zoomOutOnDataUpdate = !0;
        this.mouseWheelZoomEnabled = this.mouseWheelScrollEnabled = this.rotate = this.skipZoom = !1;
        this.minSelectedTime = 0;
        AmCharts.applyTheme(this, a, this.cname)
    },
    initChart: function () {
        AmCharts.AmSerialChart.base.initChart.call(this);
        this.updateCategoryAxis(this.categoryAxis, this.rotate, "categoryAxis");
        this.dataChanged && (this.updateData(), this.dataChanged = !1, this.dispatchDataUpdated = !0);
        var a = this.chartCursor;
        a && (a.updateData(), a.fullWidth && (a.fullRectSet = this.cursorLineSet));
        var a = this.countColumns(),
            b = this.graphs,
            d;
        for (d = 0; d < b.length; d++) b[d].columnCount = a;
        this.updateScrollbar = !0;
        this.drawChart();
        this.autoMargins && !this.marginsUpdated && (this.marginsUpdated = !0, this.measureMargins())
    },
    handleWheelReal: function (a, b) {
        if (!this.wheelBusy) {
            var d = this.categoryAxis,
                c = d.parseDates,
                e = d.minDuration(),
                g = d = 1;
            this.mouseWheelZoomEnabled ? b || (d = -1) : b && (d = -1);
            var h = this.chartData.length,
                n = this.lastTime,
                f = this.firstTime;
            0 > a ? c ? (h = this.endTime - this.startTime, c = this.startTime + d * e, e = this.endTime + g * e, 0 < g && 0 < d && e >= n && (e = n, c = n - h), this.zoomToDates(new Date(c), new Date(e))) : (0 < g && 0 < d && this.end >= h - 1 && (d = g = 0), c = this.start +
                d, e = this.end + g, this.zoomToIndexes(c, e)) : c ? (h = this.endTime - this.startTime, c = this.startTime - d * e, e = this.endTime - g * e, 0 < g && 0 < d && c <= f && (c = f, e = f + h), this.zoomToDates(new Date(c), new Date(e))) : (0 < g && 0 < d && 1 > this.start && (d = g = 0), c = this.start - d, e = this.end - g, this.zoomToIndexes(c, e))
        }
    },
    validateData: function (a) {
        this.marginsUpdated = !1;
        this.zoomOutOnDataUpdate && !a && (this.endTime = this.end = this.startTime = this.start = NaN);
        AmCharts.AmSerialChart.base.validateData.call(this)
    },
    drawChart: function () {
        AmCharts.AmSerialChart.base.drawChart.call(this);
        var a = this.chartData;
        if (AmCharts.ifArray(a)) {
            var b = this.chartScrollbar;
            b && b.draw();
            if (0 < this.realWidth && 0 < this.realHeight) {
                var a = a.length - 1,
                    d, b = this.categoryAxis;
                if (b.parseDates && !b.equalSpacing) {
                    if (b = this.startTime, d = this.endTime, isNaN(b) || isNaN(d)) b = this.firstTime, d = this.lastTime
                } else if (b = this.start, d = this.end, isNaN(b) || isNaN(d)) b = 0, d = a;
                this.endTime = this.startTime = this.end = this.start = void 0;
                this.zoom(b, d)
            }
        } else this.cleanChart();
        this.dispDUpd();
        this.chartCreated = !0
    },
    cleanChart: function () {
        AmCharts.callMethod("destroy", [this.valueAxes, this.graphs, this.categoryAxis, this.chartScrollbar, this.chartCursor])
    },
    updateCategoryAxis: function (a, b, d) {
        a.chart = this;
        a.id = d;
        a.rotate = b;
        a.axisRenderer = AmCharts.RecAxis;
        a.guideFillRenderer = AmCharts.RecFill;
        a.axisItemRenderer = AmCharts.RecItem;
        a.setOrientation(!this.rotate);
        a.x = this.marginLeftReal;
        a.y = this.marginTopReal;
        a.dx = this.dx;
        a.dy = this.dy;
        a.width = this.plotAreaWidth - 1;
        a.height = this.plotAreaHeight - 1;
        a.viW = this.plotAreaWidth - 1;
        a.viH = this.plotAreaHeight - 1;
        a.viX = this.marginLeftReal;
        a.viY = this.marginTopReal;
        a.marginsChanged = !0
    },
    updateValueAxes: function () {
        AmCharts.AmSerialChart.base.updateValueAxes.call(this);
        var a = this.valueAxes,
            b;
        for (b = 0; b < a.length; b++) {
            var d = a[b],
                c = this.rotate;
            d.rotate = c;
            d.setOrientation(c);
            c = this.categoryAxis;
            if (!c.startOnAxis || c.parseDates) d.expandMinMax = !0
        }
    },
    updateData: function () {
        this.parseData();
        var a = this.graphs,
            b, d = this.chartData;
        for (b = 0; b < a.length; b++) a[b].data = d;
        0 < d.length && (this.firstTime = this.getStartTime(d[0].time), this.lastTime = this.getEndTime(d[d.length -
            1].time))
    },
    getStartTime: function (a) {
        var b = this.categoryAxis;
        return AmCharts.resetDateToMin(new Date(a), b.minPeriod, 1, b.firstDayOfWeek).getTime()
    },
    getEndTime: function (a) {
        var b = AmCharts.extractPeriod(this.categoryAxis.minPeriod);
        return AmCharts.changeDate(new Date(a), b.period, b.count, !0).getTime() - 1
    },
    updateMargins: function () {
        AmCharts.AmSerialChart.base.updateMargins.call(this);
        var a = this.chartScrollbar;
        a && (this.getScrollbarPosition(a, this.rotate, this.categoryAxis.position), this.adjustMargins(a, this.rotate))
    },
    updateScrollbars: function () {
        AmCharts.AmSerialChart.base.updateScrollbars.call(this);
        this.updateChartScrollbar(this.chartScrollbar, this.rotate)
    },
    zoom: function (a, b) {
        var d = this.categoryAxis;
        d.parseDates && !d.equalSpacing ? this.timeZoom(a, b) : this.indexZoom(a, b);
        this.updateLegendValues()
    },
    timeZoom: function (a, b) {
        var d = this.maxSelectedTime;
        isNaN(d) || (b != this.endTime && b - a > d && (a = b - d, this.updateScrollbar = !0), a != this.startTime && b - a > d && (b = a + d, this.updateScrollbar = !0));
        var c = this.minSelectedTime;
        if (0 < c && b - a < c) {
            var e = Math.round(a + (b - a) / 2),
                c = Math.round(c / 2);
            a = e - c;
            b = e + c
        }
        var g = this.chartData,
            e = this.categoryAxis;
        if (AmCharts.ifArray(g) && (a != this.startTime || b != this.endTime)) {
            var h = e.minDuration(),
                c = this.firstTime,
                n = this.lastTime;
            a || (a = c, isNaN(d) || (a = n - d));
            b || (b = n);
            a > n && (a = n);
            b < c && (b = c);
            a < c && (a = c);
            b > n && (b = n);
            b < a && (b = a + h);
            b - a < h / 5 && (b < n ? b = a + h / 5 : a = b - h / 5);
            this.startTime = a;
            this.endTime = b;
            d = g.length - 1;
            h = this.getClosestIndex(g, "time", a, !0, 0, d);
            g = this.getClosestIndex(g, "time", b, !1, h, d);
            e.timeZoom(a, b);
            e.zoom(h, g);
            this.start =
                AmCharts.fitToBounds(h, 0, d);
            this.end = AmCharts.fitToBounds(g, 0, d);
            this.zoomAxesAndGraphs();
            this.zoomScrollbar();
            a != c || b != n ? this.showZB(!0) : this.showZB(!1);
            this.updateColumnsDepth();
            this.dispatchTimeZoomEvent()
        }
    },
    indexZoom: function (a, b) {
        var d = this.maxSelectedSeries;
        isNaN(d) || (b != this.end && b - a > d && (a = b - d, this.updateScrollbar = !0), a != this.start && b - a > d && (b = a + d, this.updateScrollbar = !0));
        if (a != this.start || b != this.end) {
            var c = this.chartData.length - 1;
            isNaN(a) && (a = 0, isNaN(d) || (a = c - d));
            isNaN(b) && (b = c);
            b < a && (b = a);
            b > c && (b = c);
            a > c && (a = c - 1);
            0 > a && (a = 0);
            this.start = a;
            this.end = b;
            this.categoryAxis.zoom(a, b);
            this.zoomAxesAndGraphs();
            this.zoomScrollbar();
            0 !== a || b != this.chartData.length - 1 ? this.showZB(!0) : this.showZB(!1);
            this.updateColumnsDepth();
            this.dispatchIndexZoomEvent()
        }
    },
    updateGraphs: function () {
        AmCharts.AmSerialChart.base.updateGraphs.call(this);
        var a = this.graphs,
            b;
        for (b = 0; b < a.length; b++) {
            var d = a[b];
            d.columnWidthReal = this.columnWidth;
            d.categoryAxis = this.categoryAxis;
            AmCharts.isString(d.fillToGraph) && (d.fillToGraph = this.getGraphById(d.fillToGraph))
        }
    },
    updateColumnsDepth: function () {
        var a, b = this.graphs,
            d;
        AmCharts.remove(this.columnsSet);
        this.columnsArray = [];
        for (a = 0; a < b.length; a++) {
            d = b[a];
            var c = d.columnsArray;
            if (c) {
                var e;
                for (e = 0; e < c.length; e++) this.columnsArray.push(c[e])
            }
        }
        this.columnsArray.sort(this.compareDepth);
        if (0 < this.columnsArray.length) {
            b = this.container.set();
            this.columnSet.push(b);
            for (a = 0; a < this.columnsArray.length; a++) b.push(this.columnsArray[a].column.set);
            d && b.translate(d.x, d.y);
            this.columnsSet = b
        }
    },
    compareDepth: function (a, b) {
        return a.depth > b.depth ? 1 : -1
    },
    zoomScrollbar: function () {
        var a = this.chartScrollbar,
            b = this.categoryAxis;
        a && this.updateScrollbar && a.enabled && (a.dragger.stop(), b.parseDates && !b.equalSpacing ? a.timeZoom(this.startTime, this.endTime) : a.zoom(this.start, this.end), this.updateScrollbar = !0)
    },
    updateTrendLines: function () {
        var a = this.trendLines,
            b;
        for (b = 0; b < a.length; b++) {
            var d = a[b],
                d = AmCharts.processObject(d, AmCharts.TrendLine, this.theme);
            a[b] = d;
            d.chart = this;
            d.id || (d.id = "trendLineAuto" + b + "_" +
                (new Date).getTime());
            AmCharts.isString(d.valueAxis) && (d.valueAxis = this.getValueAxisById(d.valueAxis));
            d.valueAxis || (d.valueAxis = this.valueAxes[0]);
            d.categoryAxis = this.categoryAxis
        }
    },
    zoomAxesAndGraphs: function () {
        if (!this.scrollbarOnly) {
            var a = this.valueAxes,
                b;
            for (b = 0; b < a.length; b++) a[b].zoom(this.start, this.end);
            a = this.graphs;
            for (b = 0; b < a.length; b++) a[b].zoom(this.start, this.end);
            this.zoomTrendLines();
            (b = this.chartCursor) && b.zoom(this.start, this.end, this.startTime, this.endTime)
        }
    },
    countColumns: function () {
        var a = 0,
            b = this.valueAxes.length,
            d = this.graphs.length,
            c, e, g = !1,
            h, n;
        for (n = 0; n < b; n++) {
            e = this.valueAxes[n];
            var f = e.stackType;
            if ("100%" == f || "regular" == f)
                for (g = !1, h = 0; h < d; h++) c = this.graphs[h], c.tcc = 1, c.valueAxis == e && "column" == c.type && (!g && c.stackable && (a++, g = !0), (!c.stackable && c.clustered || c.newStack) && a++, c.columnIndex = a - 1, c.clustered || (c.columnIndex = 0));
            if ("none" == f || "3d" == f) {
                g = !1;
                for (h = 0; h < d; h++) c = this.graphs[h], c.valueAxis == e && "column" == c.type && (c.clustered ? (c.tcc = 1, c.newStack && (a = 0), c.hidden || (c.columnIndex = a, a++)) : c.hidden || (g = !0, c.tcc = 1, c.columnIndex = 0));
                g && 0 == a && (a = 1)
            }
            if ("3d" == f) {
                e = 1;
                for (n = 0; n < d; n++) c = this.graphs[n], c.newStack && e++, c.depthCount = e, c.tcc = a;
                a = e
            }
        }
        return a
    },
    parseData: function () {
        AmCharts.AmSerialChart.base.parseData.call(this);
        this.parseSerialData()
    },
    getCategoryIndexByValue: function (a) {
        var b = this.chartData,
            d, c;
        for (c = 0; c < b.length; c++) b[c].category == a && (d = c);
        return d
    },
    handleCursorChange: function (a) {
        this.updateLegendValues(a.index)
    },
    handleCursorZoom: function (a) {
        this.updateScrollbar = !0;
        this.zoom(a.start, a.end)
    },
    handleScrollbarZoom: function (a) {
        this.updateScrollbar = !1;
        this.zoom(a.start, a.end)
    },
    dispatchTimeZoomEvent: function () {
        if (this.prevStartTime != this.startTime || this.prevEndTime != this.endTime) {
            var a = {
                type: "zoomed"
            };
            a.startDate = new Date(this.startTime);
            a.endDate = new Date(this.endTime);
            a.startIndex = this.start;
            a.endIndex = this.end;
            this.startIndex = this.start;
            this.endIndex = this.end;
            this.startDate = a.startDate;
            this.endDate = a.endDate;
            this.prevStartTime = this.startTime;
            this.prevEndTime = this.endTime;
            var b = this.categoryAxis,
                d = AmCharts.extractPeriod(b.minPeriod).period,
                b = b.dateFormatsObject[d];
            a.startValue = AmCharts.formatDate(a.startDate, b, this);
            a.endValue = AmCharts.formatDate(a.endDate, b, this);
            a.chart = this;
            a.target = this;
            this.fire(a.type, a)
        }
    },
    dispatchIndexZoomEvent: function () {
        if (this.prevStartIndex != this.start || this.prevEndIndex != this.end) {
            this.startIndex = this.start;
            this.endIndex = this.end;
            var a = this.chartData;
            if (AmCharts.ifArray(a) && !isNaN(this.start) && !isNaN(this.end)) {
                var b = {
                    chart: this,
                    target: this,
                    type: "zoomed"
                };
                b.startIndex = this.start;
                b.endIndex = this.end;
                b.startValue = a[this.start].category;
                b.endValue = a[this.end].category;
                this.categoryAxis.parseDates && (this.startTime = a[this.start].time, this.endTime = a[this.end].time, b.startDate = new Date(this.startTime), b.endDate = new Date(this.endTime));
                this.prevStartIndex = this.start;
                this.prevEndIndex = this.end;
                this.fire(b.type, b)
            }
        }
    },
    updateLegendValues: function (a) {
        var b = this.graphs,
            d;
        for (d = 0; d < b.length; d++) {
            var c = b[d];
            isNaN(a) ? c.currentDataItem = void 0 : c.currentDataItem = this.chartData[a].axes[c.valueAxis.id].graphs[c.id]
        }
        this.legend && this.legend.updateValues()
    },
    getClosestIndex: function (a, b, d, c, e, g) {
        0 > e && (e = 0);
        g > a.length - 1 && (g = a.length - 1);
        var h = e + Math.round((g - e) / 2),
            n = a[h][b];
        if (1 >= g - e) {
            if (c) return e;
            c = a[g][b];
            return Math.abs(a[e][b] - d) < Math.abs(c - d) ? e : g
        }
        return d == n ? h : d < n ? this.getClosestIndex(a, b, d, c, e, h) : this.getClosestIndex(a, b, d, c, h, g)
    },
    zoomToIndexes: function (a, b) {
        this.updateScrollbar = !0;
        var d = this.chartData;
        if (d) {
            var c = d.length;
            0 < c && (0 > a && (a = 0), b > c - 1 && (b = c - 1), c = this.categoryAxis, c.parseDates && !c.equalSpacing ? this.zoom(d[a].time,
                this.getEndTime(d[b].time)) : this.zoom(a, b))
        }
    },
    zoomToDates: function (a, b) {
        this.updateScrollbar = !0;
        var d = this.chartData;
        if (this.categoryAxis.equalSpacing) {
            var c = this.getClosestIndex(d, "time", a.getTime(), !0, 0, d.length);
            b = AmCharts.resetDateToMin(b, this.categoryAxis.minPeriod, 1);
            d = this.getClosestIndex(d, "time", b.getTime(), !1, 0, d.length);
            this.zoom(c, d)
        } else this.zoom(a.getTime(), b.getTime())
    },
    zoomToCategoryValues: function (a, b) {
        this.updateScrollbar = !0;
        this.zoom(this.getCategoryIndexByValue(a), this.getCategoryIndexByValue(b))
    },
    formatPeriodString: function (a, b) {
        if (b) {
            var d = ["value", "open", "low", "high", "close"],
                c = "value open low high close average sum count".split(" "),
                e = b.valueAxis,
                g = this.chartData,
                h = b.numberFormatter;
            h || (h = this.nf);
            for (var n = 0; n < d.length; n++) {
                for (var f = d[n], k = 0, l = 0, m, u, w, t, p, x = 0, q = 0, A, r, v, y, C, D = this.start; D <= this.end; D++) {
                    var z = g[D];
                    if (z && (z = z.axes[e.id].graphs[b.id])) {
                        if (z.values) {
                            var B = z.values[f];
                            if (this.rotate) {
                                if (0 > z.x || z.x > z.graph.height) B = NaN
                            } else if (0 > z.x || z.x > z.graph.width) B = NaN;
                            if (!isNaN(B)) {
                                isNaN(m) && (m = B);
                                u = B;
                                if (isNaN(w) || w > B) w = B;
                                if (isNaN(t) || t < B) t = B;
                                p = AmCharts.getDecimals(k);
                                var F = AmCharts.getDecimals(B),
                                    k = k + B,
                                    k = AmCharts.roundTo(k, Math.max(p, F));
                                l++;
                                p = k / l
                            }
                        }
                        if (z.percents && (z = z.percents[f], !isNaN(z))) {
                            isNaN(A) && (A = z);
                            r = z;
                            if (isNaN(v) || v > z) v = z;
                            if (isNaN(y) || y < z) y = z;
                            C = AmCharts.getDecimals(x);
                            B = AmCharts.getDecimals(z);
                            x += z;
                            x = AmCharts.roundTo(x, Math.max(C, B));
                            q++;
                            C = x / q
                        }
                    }
                }
                x = {
                    open: A,
                    close: r,
                    high: y,
                    low: v,
                    average: C,
                    sum: x,
                    count: q
                };
                a = AmCharts.formatValue(a, {
                    open: m,
                    close: u,
                    high: t,
                    low: w,
                    average: p,
                    sum: k,
                    count: l
                }, c, h, f + "\\.", this.usePrefixes, this.prefixesOfSmallNumbers, this.prefixesOfBigNumbers);
                a = AmCharts.formatValue(a, x, c, this.pf, "percents\\." + f + "\\.")
            }
        }
        return a = AmCharts.cleanFromEmpty(a)
    },
    formatString: function (a, b, d) {
        var c = b.graph;
        if (-1 != a.indexOf("[[category]]")) {
            var e = b.serialDataItem.category;
            if (this.categoryAxis.parseDates) {
                var g = this.balloonDateFormat,
                    h = this.chartCursor;
                h && (g = h.categoryBalloonDateFormat); -1 != a.indexOf("[[category]]") && (g = AmCharts.formatDate(e, g, this), -1 != g.indexOf("fff") && (g = AmCharts.formatMilliseconds(g, e)), e = g)
            }
            a = a.replace(/\[\[category\]\]/g, String(e))
        }
        c = c.numberFormatter;
        c || (c = this.nf);
        e = b.graph.valueAxis;
        (g = e.duration) && !isNaN(b.values.value) && (e = AmCharts.formatDuration(b.values.value, g, "", e.durationUnits, e.maxInterval, c), a = a.replace(RegExp("\\[\\[value\\]\\]", "g"), e));
        e = "value open low high close total".split(" ");
        g = this.pf;
        a = AmCharts.formatValue(a, b.percents, e, g, "percents\\.");
        a = AmCharts.formatValue(a, b.values, e, c, "", this.usePrefixes, this.prefixesOfSmallNumbers, this.prefixesOfBigNumbers);
        a = AmCharts.formatValue(a, b.values, ["percents"], g); -1 != a.indexOf("[[") && (a = AmCharts.formatDataContextValue(a, b.dataContext));
        return a = AmCharts.AmSerialChart.base.formatString.call(this, a, b, d)
    },
    addChartScrollbar: function (a) {
        AmCharts.callMethod("destroy", [this.chartScrollbar]);
        a && (a.chart = this, this.listenTo(a, "zoomed", this.handleScrollbarZoom));
        this.rotate ? void 0 === a.width && (a.width = a.scrollbarHeight) : void 0 === a.height && (a.height = a.scrollbarHeight);
        this.chartScrollbar = a
    },
    removeChartScrollbar: function () {
        AmCharts.callMethod("destroy", [this.chartScrollbar]);
        this.chartScrollbar = null
    },
    handleReleaseOutside: function (a) {
        AmCharts.AmSerialChart.base.handleReleaseOutside.call(this, a);
        AmCharts.callMethod("handleReleaseOutside", [this.chartScrollbar])
    }
});
AmCharts.Cuboid = AmCharts.Class({
    construct: function (a, b, d, c, e, g, h, n, f, k, l, m, u, w, t, p, x) {
        this.set = a.set();
        this.container = a;
        this.h = Math.round(d);
        this.w = Math.round(b);
        this.dx = c;
        this.dy = e;
        this.colors = g;
        this.alpha = h;
        this.bwidth = n;
        this.bcolor = f;
        this.balpha = k;
        this.dashLength = w;
        this.topRadius = p;
        this.pattern = t;
        this.rotate = u;
        this.bcn = x;
        u ? 0 > b && 0 === l && (l = 180) : 0 > d && 270 == l && (l = 90);
        this.gradientRotation = l;
        0 === c && 0 === e && (this.cornerRadius = m);
        this.draw()
    },
    draw: function () {
        var a = this.set;
        a.clear();
        var b = this.container,
            d = b.chart,
            c = this.w,
            e = this.h,
            g = this.dx,
            h = this.dy,
            n = this.colors,
            f = this.alpha,
            k = this.bwidth,
            l = this.bcolor,
            m = this.balpha,
            u = this.gradientRotation,
            w = this.cornerRadius,
            t = this.dashLength,
            p = this.pattern,
            x = this.topRadius,
            q = this.bcn,
            A = n,
            r = n;
        "object" == typeof n && (A = n[0], r = n[n.length - 1]);
        var v, y, C, D, z, B, F, K, L, P = f;
        p && (f = 0);
        var E, G, H, I, J = this.rotate;
        if (0 < Math.abs(g) || 0 < Math.abs(h))
            if (isNaN(x)) F = r, r = AmCharts.adjustLuminosity(A, -.2), r = AmCharts.adjustLuminosity(A, -.2), v = AmCharts.polygon(b, [0, g, c + g, c, 0], [0, h, h, 0, 0], r, f, 1, l, 0, u), 0 < m && (L = AmCharts.line(b, [0, g, c + g], [0, h, h], l, m, k, t)), y = AmCharts.polygon(b, [0, 0, c, c, 0], [0, e, e, 0, 0], r, f, 1, l, 0, u), y.translate(g, h), 0 < m && (C = AmCharts.line(b, [g, g], [h, h + e], l, m, k, t)), D = AmCharts.polygon(b, [0, 0, g, g, 0], [0, e, e + h, h, 0], r, f, 1, l, 0, u), z = AmCharts.polygon(b, [c, c, c + g, c + g, c], [0, e, e + h, h, 0], r, f, 1, l, 0, u), 0 < m && (B = AmCharts.line(b, [c, c + g, c + g, c], [0, h, e + h, e], l, m, k, t)), r = AmCharts.adjustLuminosity(F, .2), F = AmCharts.polygon(b, [0, g, c + g, c, 0], [e, e + h, e + h, e, e], r, f, 1, l, 0, u), 0 < m && (K = AmCharts.line(b, [0, g, c +
                g
            ], [e, e + h, e + h], l, m, k, t));
            else {
                var M, N, O;
                J ? (M = e / 2, r = g / 2, O = e / 2, N = c + g / 2, G = Math.abs(e / 2), E = Math.abs(g / 2)) : (r = c / 2, M = h / 2, N = c / 2, O = e + h / 2 + 1, E = Math.abs(c / 2), G = Math.abs(h / 2));
                H = E * x;
                I = G * x; .1 < E && .1 < E && (v = AmCharts.circle(b, E, A, f, k, l, m, !1, G), v.translate(r, M)); .1 < H && .1 < H && (F = AmCharts.circle(b, H, AmCharts.adjustLuminosity(A, .5), f, k, l, m, !1, I), F.translate(N, O))
            }
        f = P;
        1 > Math.abs(e) && (e = 0);
        1 > Math.abs(c) && (c = 0);
        !isNaN(x) && (0 < Math.abs(g) || 0 < Math.abs(h)) ? (n = [A], n = {
            fill: n,
            stroke: l,
            "stroke-width": k,
            "stroke-opacity": m,
            "fill-opacity": f
        }, J ? (f = "M0,0 L" + c + "," + (e / 2 - e / 2 * x), k = " B", 0 < c && (k = " A"), AmCharts.VML ? (f += k + Math.round(c - H) + "," + Math.round(e / 2 - I) + "," + Math.round(c + H) + "," + Math.round(e / 2 + I) + "," + c + ",0," + c + "," + e, f = f + (" L0," + e) + (k + Math.round(-E) + "," + Math.round(e / 2 - G) + "," + Math.round(E) + "," + Math.round(e / 2 + G) + ",0," + e + ",0,0")) : (f += "A" + H + "," + I + ",0,0,0," + c + "," + (e - e / 2 * (1 - x)) + "L0," + e, f += "A" + E + "," + G + ",0,0,1,0,0"), E = 90) : (k = c / 2 - c / 2 * x, f = "M0,0 L" + k + "," + e, AmCharts.VML ? (f = "M0,0 L" + k + "," + e, k = " B", 0 > e && (k = " A"), f += k + Math.round(c / 2 - H) + "," + Math.round(e - I) + "," + Math.round(c / 2 + H) + "," + Math.round(e + I) + ",0," + e + "," + c + "," + e, f += " L" + c + ",0", f += k + Math.round(c / 2 + E) + "," + Math.round(G) + "," + Math.round(c / 2 - E) + "," + Math.round(-G) + "," + c + ",0,0,0") : (f += "A" + H + "," + I + ",0,0,0," + (c - c / 2 * (1 - x)) + "," + e + "L" + c + ",0", f += "A" + E + "," + G + ",0,0,1,0,0"), E = 180), b = b.path(f).attr(n), b.gradient("linearGradient", [A, AmCharts.adjustLuminosity(A, -.3), AmCharts.adjustLuminosity(A, -.3), A], E), J ? b.translate(g / 2, 0) : b.translate(0, h / 2)) : b = 0 === e ? AmCharts.line(b, [0, c], [0, 0], l, m, k, t) : 0 === c ? AmCharts.line(b, [0, 0], [0, e], l, m, k, t) : 0 < w ? AmCharts.rect(b, c, e, n, f, k, l, m, w, u, t) : AmCharts.polygon(b, [0, 0, c, c, 0], [0, e, e, 0, 0], n, f, k, l, m, u, !1, t);
        c = isNaN(x) ? 0 > e ? [v, L, y, C, D, z, B, F, K, b] : [F, K, y, C, D, z, v, L, B, b] : J ? 0 < c ? [v, b, F] : [F, b, v] : 0 > e ? [v, b, F] : [F, b, v];
        AmCharts.setCN(d, b, q + "front");
        AmCharts.setCN(d, y, q + "back");
        AmCharts.setCN(d, F, q + "top");
        AmCharts.setCN(d, v, q + "bottom");
        AmCharts.setCN(d, D, q + "left");
        AmCharts.setCN(d, z, q + "right");
        for (v = 0; v < c.length; v++)
            if (y = c[v]) a.push(y), AmCharts.setCN(d, y, q + "element");
        p && b.pattern(p)
    },
    width: function (a) {
        isNaN(a) && (a = 0);
        this.w = Math.round(a);
        this.draw()
    },
    height: function (a) {
        isNaN(a) && (a = 0);
        this.h = Math.round(a);
        this.draw()
    },
    animateHeight: function (a, b) {
        var d = this;
        d.easing = b;
        d.totalFrames = Math.round(1E3 * a / AmCharts.updateRate);
        d.rh = d.h;
        d.frame = 0;
        d.height(1);
        setTimeout(function () {
            d.updateHeight.call(d)
        }, AmCharts.updateRate)
    },
    updateHeight: function () {
        var a = this;
        a.frame++;
        var b = a.totalFrames;
        a.frame <= b && (b = a.easing(0, a.frame, 1, a.rh - 1, b), a.height(b), setTimeout(function () {
            a.updateHeight.call(a)
        }, AmCharts.updateRate))
    },
    animateWidth: function (a, b) {
        var d = this;
        d.easing = b;
        d.totalFrames = Math.round(1E3 * a / AmCharts.updateRate);
        d.rw = d.w;
        d.frame = 0;
        d.width(1);
        setTimeout(function () {
            d.updateWidth.call(d)
        }, AmCharts.updateRate)
    },
    updateWidth: function () {
        var a = this;
        a.frame++;
        var b = a.totalFrames;
        a.frame <= b && (b = a.easing(0, a.frame, 1, a.rw - 1, b), a.width(b), setTimeout(function () {
            a.updateWidth.call(a)
        }, AmCharts.updateRate))
    }
});
AmCharts.CategoryAxis = AmCharts.Class({
    inherits: AmCharts.AxisBase,
    construct: function (a) {
        this.cname = "CategoryAxis";
        AmCharts.CategoryAxis.base.construct.call(this, a);
        this.minPeriod = "DD";
        this.equalSpacing = this.parseDates = !1;
        this.position = "bottom";
        this.startOnAxis = !1;
        this.firstDayOfWeek = 1;
        this.gridPosition = "middle";
        this.markPeriodChange = this.boldPeriodBeginning = !0;
        this.safeDistance = 30;
        this.centerLabelOnFullPeriod = !0;
        this.periods = [{
            period: "ss",
            count: 1
        }, {
            period: "ss",
            count: 5
        }, {
            period: "ss",
            count: 10
        }, {
            period: "ss",
            count: 30
        }, {
            period: "mm",
            count: 1
        }, {
            period: "mm",
            count: 5
        }, {
            period: "mm",
            count: 10
        }, {
            period: "mm",
            count: 30
        }, {
            period: "hh",
            count: 1
        }, {
            period: "hh",
            count: 3
        }, {
            period: "hh",
            count: 6
        }, {
            period: "hh",
            count: 12
        }, {
            period: "DD",
            count: 1
        }, {
            period: "DD",
            count: 2
        }, {
            period: "DD",
            count: 3
        }, {
            period: "DD",
            count: 4
        }, {
            period: "DD",
            count: 5
        }, {
            period: "WW",
            count: 1
        }, {
            period: "MM",
            count: 1
        }, {
            period: "MM",
            count: 2
        }, {
            period: "MM",
            count: 3
        }, {
            period: "MM",
            count: 6
        }, {
            period: "YYYY",
            count: 1
        }, {
            period: "YYYY",
            count: 2
        }, {
            period: "YYYY",
            count: 5
        }, {
            period: "YYYY",
            count: 10
        }, {
            period: "YYYY",
            count: 50
        }, {
            period: "YYYY",
            count: 100
        }];
        this.dateFormats = [{
            period: "fff",
            format: "JJ:NN:SS"
        }, {
            period: "ss",
            format: "JJ:NN:SS"
        }, {
            period: "mm",
            format: "JJ:NN"
        }, {
            period: "hh",
            format: "JJ:NN"
        }, {
            period: "DD",
            format: "MMM DD"
        }, {
            period: "WW",
            format: "MMM DD"
        }, {
            period: "MM",
            format: "MMM"
        }, {
            period: "YYYY",
            format: "YYYY"
        }];
        this.nextPeriod = {};
        this.nextPeriod.fff = "ss";
        this.nextPeriod.ss = "mm";
        this.nextPeriod.mm = "hh";
        this.nextPeriod.hh = "DD";
        this.nextPeriod.DD = "MM";
        this.nextPeriod.MM = "YYYY";
        AmCharts.applyTheme(this, a, this.cname)
    },
    draw: function () {
        AmCharts.CategoryAxis.base.draw.call(this);
        this.generateDFObject();
        var a = this.chart.chartData;
        this.data = a;
        if (AmCharts.ifArray(a)) {
            var b, d = this.chart;
            "scrollbar" != this.id ? (AmCharts.setCN(d, this.set, "category-axis"), AmCharts.setCN(d, this.labelsSet, "category-axis"), AmCharts.setCN(d, this.axisLine.axisSet, "category-axis")) : this.bcn = this.id + "-";
            var c = this.start,
                e = this.labelFrequency,
                g = 0;
            b = this.end - c + 1;
            var h = this.gridCountR,
                n = this.showFirstLabel,
                f = this.showLastLabel,
                k, l = "",
                m = AmCharts.extractPeriod(this.minPeriod);
            k = AmCharts.getPeriodDuration(m.period, m.count);
            var u, w, t, p, x, q;
            u = this.rotate;
            var A = this.firstDayOfWeek,
                r = this.boldPeriodBeginning,
                a = AmCharts.resetDateToMin(new Date(a[a.length - 1].time + 1.05 * k), this.minPeriod, 1, A).getTime(),
                v;
            this.endTime > a && (this.endTime = a);
            q = this.minorGridEnabled;
            var y, a = this.gridAlpha,
                C;
            if (this.parseDates && !this.equalSpacing) {
                this.timeDifference = this.endTime - this.startTime;
                c = this.choosePeriod(0);
                e = c.period;
                u = c.count;
                w = AmCharts.getPeriodDuration(e, u);
                w < k && (e = m.period, u = m.count, w = k);
                t = e;
                "WW" == t && (t = "DD");
                this.stepWidth = this.getStepWidth(this.timeDifference);
                var h = Math.ceil(this.timeDifference / w) + 5,
                    D = l = AmCharts.resetDateToMin(new Date(this.startTime - w), e, u, A).getTime();
                t == e && 1 == u && this.centerLabelOnFullPeriod && (x = w * this.stepWidth);
                this.cellWidth = k * this.stepWidth;
                b = Math.round(l / w);
                c = -1;
                b / 2 == Math.round(b / 2) && (c = -2, l -= w);
                var z = d.firstTime,
                    B = 0;
                q && 1 < u && (y = this.chooseMinorFrequency(u), C = AmCharts.getPeriodDuration(e, y));
                if (0 < this.gridCountR)
                    for (b = c; b <= h; b++) {
                        m = z + w * (b + Math.floor((D - z) / w)) - B;
                        "DD" == e && (m += 36E5);
                        m = AmCharts.resetDateToMin(new Date(m), e, u, A).getTime();
                        "MM" == e && (q = (m - l) / w, 1.5 <= (m - l) / w && (m = m - (q - 1) * w + AmCharts.getPeriodDuration("DD", 3), m = AmCharts.resetDateToMin(new Date(m), e, 1).getTime(), B += w));
                        k = (m - this.startTime) * this.stepWidth;
                        q = !1;
                        this.nextPeriod[t] && (q = this.checkPeriodChange(this.nextPeriod[t], 1, m, l, t));
                        v = !1;
                        q && this.markPeriodChange ? (q = this.dateFormatsObject[this.nextPeriod[t]], this.twoLineMode && (q = this.dateFormatsObject[t] + "\n" + q,
                            q = AmCharts.fixBrakes(q)), v = !0) : q = this.dateFormatsObject[t];
                        r || (v = !1);
                        l = AmCharts.formatDate(new Date(m), q, d);
                        if (b == c && !n || b == h && !f) l = " ";
                        this.labelFunction && (l = this.labelFunction(l, new Date(m), this, e, u, p).toString());
                        this.boldLabels && (v = !0);
                        p = new this.axisItemRenderer(this, k, l, !1, x, 0, !1, v);
                        this.pushAxisItem(p);
                        p = l = m;
                        if (!isNaN(y))
                            for (k = 1; k < u; k += y) this.gridAlpha = this.minorGridAlpha, q = m + C * k, q = AmCharts.resetDateToMin(new Date(q), e, y, A).getTime(), q = new this.axisItemRenderer(this, (q - this.startTime) * this.stepWidth), this.pushAxisItem(q);
                        this.gridAlpha = a
                    }
            } else if (!this.parseDates) {
                if (this.cellWidth = this.getStepWidth(b), b < h && (h = b), g += this.start, this.stepWidth = this.getStepWidth(b), 0 < h)
                    for (r = Math.floor(b / h), y = this.chooseMinorFrequency(r), k = g, k / 2 == Math.round(k / 2) && k--, 0 > k && (k = 0), h = 0, this.end - k + 1 >= this.autoRotateCount && (this.labelRotation = this.autoRotateAngle), b = k; b <= this.end + 2; b++) {
                        p = !1;
                        0 <= b && b < this.data.length ? (t = this.data[b], l = t.category, p = t.forceShow) : l = "";
                        if (q && !isNaN(y))
                            if (b / y == Math.round(b / y) || p) b / r == Math.round(b /
                                r) || p || (this.gridAlpha = this.minorGridAlpha, l = void 0);
                            else continue;
                        else if (b / r != Math.round(b / r) && !p) continue;
                        k = this.getCoordinate(b - g);
                        p = 0;
                        "start" == this.gridPosition && (k -= this.cellWidth / 2, p = this.cellWidth / 2);
                        A = !0;
                        D = p;
                        "start" == this.tickPosition && (D = 0, A = !1, p = 0);
                        if (b == c && !n || b == this.end && !f) l = void 0;
                        Math.round(h / e) != h / e && (l = void 0);
                        h++;
                        x = this.cellWidth;
                        u && (x = NaN);
                        this.labelFunction && t && (l = this.labelFunction(l, t, this));
                        l = AmCharts.fixBrakes(l);
                        v = !1;
                        this.boldLabels && (v = !0);
                        b > this.end && "start" == this.tickPosition && (l = " ");
                        p = new this.axisItemRenderer(this, k, l, A, x, p, void 0, v, D, !1, t.labelColor, t.className);
                        p.serialDataItem = t;
                        this.pushAxisItem(p);
                        this.gridAlpha = a
                    }
            } else if (this.parseDates && this.equalSpacing) {
                g = this.start;
                this.startTime = this.data[this.start].time;
                this.endTime = this.data[this.end].time;
                this.timeDifference = this.endTime - this.startTime;
                c = this.choosePeriod(0);
                e = c.period;
                u = c.count;
                w = AmCharts.getPeriodDuration(e, u);
                w < k && (e = m.period, u = m.count, w = k);
                t = e;
                "WW" == t && (t = "DD");
                this.stepWidth = this.getStepWidth(b);
                h = Math.ceil(this.timeDifference / w) + 1;
                l = AmCharts.resetDateToMin(new Date(this.startTime - w), e, u, A).getTime();
                this.cellWidth = this.getStepWidth(b);
                b = Math.round(l / w);
                c = -1;
                b / 2 == Math.round(b / 2) && (c = -2, l -= w);
                k = this.start;
                k / 2 == Math.round(k / 2) && k--;
                0 > k && (k = 0);
                x = this.end + 2;
                x >= this.data.length && (x = this.data.length);
                C = !1;
                C = !n;
                this.previousPos = -1E3;
                20 < this.labelRotation && (this.safeDistance = 5);
                w = k;
                if (this.data[k].time != AmCharts.resetDateToMin(new Date(this.data[k].time), e, u, A).getTime())
                    for (A = 0, v = l, b = k; b < x; b++) m = this.data[b].time, this.checkPeriodChange(e, u, m, v) && (A++, 2 <= A && (w = b, b = x), v = m);
                q && 1 < u && (y = this.chooseMinorFrequency(u), AmCharts.getPeriodDuration(e, y));
                if (0 < this.gridCountR)
                    for (b = k; b < x; b++)
                        if (m = this.data[b].time, this.checkPeriodChange(e, u, m, l) && b >= w) {
                            k = this.getCoordinate(b - this.start);
                            q = !1;
                            this.nextPeriod[t] && (q = this.checkPeriodChange(this.nextPeriod[t], 1, m, l, t));
                            v = !1;
                            q && this.markPeriodChange ? (q = this.dateFormatsObject[this.nextPeriod[t]], v = !0) : q = this.dateFormatsObject[t];
                            l = AmCharts.formatDate(new Date(m), q, d);
                            if (b == c && !n || b == h && !f) l = " ";
                            C ? C = !1 : (r || (v = !1), k - this.previousPos > this.safeDistance * Math.cos(this.labelRotation * Math.PI / 180) && (this.labelFunction && (l = this.labelFunction(l, new Date(m), this, e, u, p)), this.boldLabels && (v = !0), p = new this.axisItemRenderer(this, k, l, void 0, void 0, void 0, void 0, v), q = p.graphics(), this.pushAxisItem(p), p = q.getBBox().width, AmCharts.isModern || (p -= k), this.previousPos = k + p));
                            p = l = m
                        } else isNaN(y) || (this.checkPeriodChange(e, y, m, D) && (this.gridAlpha = this.minorGridAlpha, k = this.getCoordinate(b -
                            this.start), q = new this.axisItemRenderer(this, k), this.pushAxisItem(q), D = m), this.gridAlpha = a)
            }
            for (b = 0; b < this.data.length; b++)
                if (n = this.data[b]) f = this.parseDates && !this.equalSpacing ? Math.round((n.time - this.startTime) * this.stepWidth + this.cellWidth / 2) : this.getCoordinate(b - g), n.x[this.id] = f;
            n = this.guides.length;
            for (b = 0; b < n; b++) f = this.guides[b], r = h = r = a = c = NaN, y = f.above, f.toCategory && (h = d.getCategoryIndexByValue(f.toCategory), isNaN(h) || (c = this.getCoordinate(h - g), f.expand && (c += this.cellWidth / 2), p = new this.axisItemRenderer(this, c, "", !0, NaN, NaN, f), this.pushAxisItem(p, y))), f.category && (r = d.getCategoryIndexByValue(f.category), isNaN(r) || (a = this.getCoordinate(r - g), f.expand && (a -= this.cellWidth / 2), r = (c - a) / 2, p = new this.axisItemRenderer(this, a, f.label, !0, NaN, r, f), this.pushAxisItem(p, y))), r = d.dataDateFormat, f.toDate && (f.toDate instanceof Date || (isNaN(f.toDate) ? r && (f.toDate = AmCharts.stringToDate(f.toDate, r)) : f.toDate = new Date(f.toDate)), this.equalSpacing ? (h = d.getClosestIndex(this.data, "time", f.toDate.getTime(), !1, 0, this.data.length -
                1), isNaN(h) || (c = this.getCoordinate(h - g))) : c = (f.toDate.getTime() - this.startTime) * this.stepWidth, p = new this.axisItemRenderer(this, c, "", !0, NaN, NaN, f), this.pushAxisItem(p, y)), f.date && (f.date instanceof Date || (isNaN(f.date) ? r && (f.date = AmCharts.stringToDate(f.date, r)) : f.date = new Date(f.date)), this.equalSpacing ? (r = d.getClosestIndex(this.data, "time", f.date.getTime(), !1, 0, this.data.length - 1), isNaN(r) || (a = this.getCoordinate(r - g))) : a = (f.date.getTime() - this.startTime) * this.stepWidth, r = (c - a) / 2, p = "H" == this.orientation ?
                new this.axisItemRenderer(this, a, f.label, !1, 2 * r, NaN, f) : new this.axisItemRenderer(this, a, f.label, !1, NaN, r, f), this.pushAxisItem(p, y)), (0 < c || 0 < a) && (c < this.width || a < this.width) && (c = new this.guideFillRenderer(this, a, c, f), a = c.graphics(), this.pushAxisItem(c, y), f.graphics = a, a.index = b, f.balloonText && this.addEventListeners(a, f))
        }
        this.axisCreated = !0;
        d = this.x;
        g = this.y;
        this.set.translate(d, g);
        this.labelsSet.translate(d, g);
        this.positionTitle();
        (d = this.axisLine.set) && d.toFront();
        d = this.getBBox().height;
        2 < d - this.previousHeight && this.autoWrap && !this.parseDates && (this.axisCreated = this.chart.marginsUpdated = !1);
        this.previousHeight = d
    },
    chooseMinorFrequency: function (a) {
        for (var b = 10; 0 < b; b--)
            if (a / b == Math.round(a / b)) return a / b
    },
    choosePeriod: function (a) {
        var b = AmCharts.getPeriodDuration(this.periods[a].period, this.periods[a].count),
            d = Math.ceil(this.timeDifference / b),
            c = this.periods;
        return this.timeDifference < b && 0 < a ? c[a - 1] : d <= this.gridCountR ? c[a] : a + 1 < c.length ? this.choosePeriod(a + 1) : c[a]
    },
    getStepWidth: function (a) {
        var b;
        this.startOnAxis ? (b = this.axisWidth / (a - 1), 1 == a && (b = this.axisWidth)) : b = this.axisWidth / a;
        return b
    },
    getCoordinate: function (a) {
        a *= this.stepWidth;
        this.startOnAxis || (a += this.stepWidth / 2);
        return Math.round(a)
    },
    timeZoom: function (a, b) {
        this.startTime = a;
        this.endTime = b
    },
    minDuration: function () {
        var a = AmCharts.extractPeriod(this.minPeriod);
        return AmCharts.getPeriodDuration(a.period, a.count)
    },
    checkPeriodChange: function (a, b, d, c, e) {
        d = new Date(d);
        var g = new Date(c),
            h = this.firstDayOfWeek;
        c = b;
        "DD" == a && (b = 1);
        d = AmCharts.resetDateToMin(d, a, b, h).getTime();
        b = AmCharts.resetDateToMin(g, a, b, h).getTime();
        return "DD" == a && "hh" != e && d - b <= AmCharts.getPeriodDuration(a, c) ? !1 : d != b ? !0 : !1
    },
    generateDFObject: function () {
        this.dateFormatsObject = {};
        var a;
        for (a = 0; a < this.dateFormats.length; a++) {
            var b = this.dateFormats[a];
            this.dateFormatsObject[b.period] = b.format
        }
    },
    xToIndex: function (a) {
        var b = this.data,
            d = this.chart,
            c = d.rotate,
            e = this.stepWidth;
        this.parseDates && !this.equalSpacing ? (a = this.startTime + Math.round(a / e) - this.minDuration() / 2, d = d.getClosestIndex(b, "time",
            a, !1, this.start, this.end + 1)) : (this.startOnAxis || (a -= e / 2), d = this.start + Math.round(a / e));
        var d = AmCharts.fitToBounds(d, 0, b.length - 1),
            g;
        b[d] && (g = b[d].x[this.id]);
        c ? g > this.height + 1 && d-- : g > this.width + 1 && d--;
        0 > g && d++;
        return d = AmCharts.fitToBounds(d, 0, b.length - 1)
    },
    dateToCoordinate: function (a) {
        return this.parseDates && !this.equalSpacing ? (a.getTime() - this.startTime) * this.stepWidth : this.parseDates && this.equalSpacing ? (a = this.chart.getClosestIndex(this.data, "time", a.getTime(), !1, 0, this.data.length - 1), this.getCoordinate(a -
            this.start)) : NaN
    },
    categoryToCoordinate: function (a) {
        return this.chart ? (a = this.chart.getCategoryIndexByValue(a), this.getCoordinate(a - this.start)) : NaN
    },
    coordinateToDate: function (a) {
        return this.equalSpacing ? (a = this.xToIndex(a), new Date(this.data[a].time)) : new Date(this.startTime + a / this.stepWidth)
    }
});