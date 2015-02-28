﻿AmCharts.addInitHandler(function (a) {
    function e() {
        var c = a.divRealWidth,
            d = a.divRealHeight,
            e = !1;
        for (var f in b.rules) {
            var h = b.rules[f];
            (void 0 == h.minWidth || h.minWidth <= c) && (void 0 == h.maxWidth || h.maxWidth >= c) && (void 0 == h.minHeight || h.minHeight <= d) && (void 0 == h.maxHeight || h.maxHeight >= d) && (void 0 == h.rotate || !0 === h.rotate && !0 === a.rotate || !1 === h.rotate && (void 0 === a.rotate || !1 === a.rotate)) && (void 0 == h.legendPosition || void 0 !== a.legend && void 0 !== a.legend.position && a.legend.position === h.legendPosition) ? void 0 == b.currentRules[f] && (b.currentRules[f] = !0, e = !0) : void 0 != b.currentRules[f] && (b.currentRules[f] = void 0, e = !0)
        }
        if (e) {
            l();
            for (var f in b.currentRules) void 0 != b.currentRules[f] && j(a, b.rules[f].overrides);
            g()
        }
    }

    function f(a, b) {
        if (a instanceof Array)
            for (var c in a)
                if ("object" == typeof a[c] && a[c].id == b) return a[c];
        return !1
    }

    function g() {
        a.dataChanged = !0, "xy" !== a.type && (a.marginsUpdated = !1), a.zoomOutOnDataUpdate = !1, a.validateNow(!0), m(a, "zoomOutOnDataUpdate")
    }

    function h(a) {
        return a instanceof Array
    }

    function i(a) {
        return "object" == typeof a
    }

    function j(a, b) {
        for (var c in b)
            if (void 0 === a[c]) a[c] = b[c], k(a, c, "_r_none");
            else if (h(a[c])) {
                if (a[c].length && !i(a[c][0])) k(a, c, a[c]), a[c] = b[c];
                else if (h(b[c]))
                    for (var d in b[c]) {
                        var e = !1;
                        void 0 === b[c][d].id && void 0 != a[c][d] ? e = a[c][d] : void 0 !== b[c][d].id && (e = f(a[c], b[c][d].id)), e && j(e, b[c][d])
                    } else if (i(b[c]))
                        for (var d in a[c]) j(a[c][d], b[c])
            } else i(a[c]) ? j(a[c], b[c]) : (k(a, c, a[c]), a[c] = b[c])
    }

    function k(a, c, d) {
        void 0 === a["_r_" + c] && (a["_r_" + c] = d), b.overridden.push({
            o: a,
            p: c
        })
    }

    function l() {
        for (var a; a = b.overridden.pop() ;) "_r_none" === a.o["_r_" + a.p] ? delete a.o[a.p] : a.o[a.p] = a.o["_r_" + a.p]
    }

    function m(a, b) {
        a[b] = a["_r_" + b]
    }
    if (void 0 !== a.responsive && !a.responsive.ready) {
        var b = a.responsive;
        if (b.ready = !0, b.currentRules = {}, b.overridden = [], b.original = {}, !0 === b.enabled) {
            var c = a.version.split(".");
            if (!(Number(c[0]) < 3 || 3 == Number(c[0]) && Number(c[1]) < 13)) {
                var d = {
                    pie: [{
                        maxWidth: 550,
                        legendPosition: "left",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 550,
                        legendPosition: "right",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 150,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        legendPosition: "top",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        legendPosition: "bottom",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 150,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 400,
                        overrides: {
                            labelsEnabled: !1
                        }
                    }, {
                        maxWidth: 100,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        overrides: {
                            pullOutRadius: 0
                        }
                    }, {
                        maxHeight: 200,
                        overrides: {
                            titles: {
                                enabled: !1
                            },
                            labelsEnabled: !1
                        }
                    }, {
                        maxWidth: 60,
                        overrides: {
                            autoMargins: !1,
                            marginTop: 0,
                            marginBottom: 0,
                            marginLeft: 0,
                            marginRight: 0,
                            radius: "50%",
                            innerRadius: 0,
                            balloon: {
                                enabled: !1
                            },
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 60,
                        overrides: {
                            marginTop: 0,
                            marginBottom: 0,
                            marginLeft: 0,
                            marginRight: 0,
                            radius: "50%",
                            innerRadius: 0,
                            balloon: {
                                enabled: !1
                            },
                            legend: {
                                enabled: !1
                            }
                        }
                    }],
                    funnel: [{
                        maxWidth: 550,
                        legendPosition: "left",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 550,
                        legendPosition: "right",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 150,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 500,
                        legendPosition: "top",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 500,
                        legendPosition: "bottom",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 150,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 400,
                        overrides: {
                            labelsEnabled: !1,
                            marginLeft: 10,
                            marginRight: 10,
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        overrides: {
                            pullOutRadius: 0,
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 300,
                        overrides: {
                            titles: {
                                enabled: !1
                            }
                        }
                    }],
                    radar: [{
                        maxWidth: 550,
                        legendPosition: "left",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 550,
                        legendPosition: "right",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 150,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        legendPosition: "top",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        legendPosition: "bottom",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 150,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 300,
                        overrides: {
                            labelsEnabled: !1
                        }
                    }, {
                        maxWidth: 200,
                        overrides: {
                            autoMargins: !1,
                            marginTop: 0,
                            marginBottom: 0,
                            marginLeft: 0,
                            marginRight: 0,
                            radius: "50%",
                            titles: {
                                enabled: !1
                            },
                            valueAxes: {
                                labelsEnabled: !1,
                                radarCategoriesEnabled: !1
                            }
                        }
                    }, {
                        maxHeight: 300,
                        overrides: {
                            labelsEnabled: !1
                        }
                    }, {
                        maxHeight: 200,
                        overrides: {
                            autoMargins: !1,
                            marginTop: 0,
                            marginBottom: 0,
                            marginLeft: 0,
                            marginRight: 0,
                            radius: "50%",
                            titles: {
                                enabled: !1
                            },
                            valueAxes: {
                                radarCategoriesEnabled: !1
                            }
                        }
                    }, {
                        maxHeight: 100,
                        overrides: {
                            valueAxes: {
                                labelsEnabled: !1
                            }
                        }
                    }],
                    gauge: [{
                        maxWidth: 550,
                        legendPosition: "left",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 550,
                        legendPosition: "right",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 150,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 500,
                        legendPosition: "top",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 500,
                        legendPosition: "bottom",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 150,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 200,
                        overrides: {
                            titles: {
                                enabled: !1
                            },
                            allLabels: {
                                enabled: !1
                            },
                            axes: {
                                labelsEnabled: !1
                            }
                        }
                    }, {
                        maxHeight: 200,
                        overrides: {
                            titles: {
                                enabled: !1
                            },
                            allLabels: {
                                enabled: !1
                            },
                            axes: {
                                labelsEnabled: !1
                            }
                        }
                    }],
                    serial: [{
                        maxWidth: 550,
                        legendPosition: "left",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 550,
                        legendPosition: "right",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 100,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        legendPosition: "top",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        legendPosition: "bottom",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 100,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 350,
                        overrides: {
                            autoMarginOffset: 0,
                            graphs: {
                                hideBulletsCount: 10
                            }
                        }
                    }, {
                        maxWidth: 350,
                        rotate: !1,
                        overrides: {
                            marginLeft: 10,
                            marginRight: 10,
                            valueAxes: {
                                ignoreAxisWidth: !0,
                                inside: !0,
                                title: "",
                                showFirstLabel: !1,
                                showLastLabel: !1
                            },
                            graphs: {
                                bullet: "none"
                            }
                        }
                    }, {
                        maxWidth: 350,
                        rotate: !0,
                        overrides: {
                            marginLeft: 10,
                            marginRight: 10,
                            categoryAxis: {
                                ignoreAxisWidth: !0,
                                inside: !0,
                                title: ""
                            }
                        }
                    }, {
                        maxWidth: 200,
                        rotate: !1,
                        overrides: {
                            marginLeft: 10,
                            marginRight: 10,
                            marginTop: 10,
                            marginBottom: 10,
                            categoryAxis: {
                                ignoreAxisWidth: !0,
                                labelsEnabled: !1,
                                inside: !0,
                                title: "",
                                guides: {
                                    inside: !0
                                }
                            },
                            valueAxes: {
                                ignoreAxisWidth: !0,
                                labelsEnabled: !1,
                                axisAlpha: 0,
                                guides: {
                                    label: ""
                                }
                            },
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 200,
                        rotate: !0,
                        overrides: {
                            chartScrollbar: {
                                scrollbarHeight: 4,
                                graph: "",
                                resizeEnabled: !1
                            },
                            categoryAxis: {
                                labelsEnabled: !1,
                                axisAlpha: 0,
                                guides: {
                                    label: ""
                                }
                            },
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 100,
                        rotate: !1,
                        overrides: {
                            valueAxes: {
                                gridAlpha: 0
                            }
                        }
                    }, {
                        maxWidth: 100,
                        rotate: !0,
                        overrides: {
                            categoryAxis: {
                                gridAlpha: 0
                            }
                        }
                    }, {
                        maxHeight: 300,
                        overrides: {
                            autoMarginOffset: 0,
                            graphs: {
                                hideBulletsCount: 10
                            }
                        }
                    }, {
                        maxHeight: 200,
                        rotate: !1,
                        overrides: {
                            marginTop: 10,
                            marginBottom: 10,
                            categoryAxis: {
                                ignoreAxisWidth: !0,
                                inside: !0,
                                title: "",
                                showFirstLabel: !1,
                                showLastLabel: !1
                            }
                        }
                    }, {
                        maxHeight: 200,
                        rotate: !0,
                        overrides: {
                            marginTop: 10,
                            marginBottom: 10,
                            valueAxes: {
                                ignoreAxisWidth: !0,
                                inside: !0,
                                title: "",
                                showFirstLabel: !1,
                                showLastLabel: !1
                            },
                            graphs: {
                                bullet: "none"
                            }
                        }
                    }, {
                        maxHeight: 150,
                        rotate: !1,
                        overrides: {
                            titles: {
                                enabled: !1
                            },
                            chartScrollbar: {
                                scrollbarHeight: 4,
                                graph: "",
                                resizeEnabled: !1
                            },
                            categoryAxis: {
                                labelsEnabled: !1,
                                ignoreAxisWidth: !0,
                                axisAlpha: 0,
                                guides: {
                                    label: ""
                                }
                            }
                        }
                    }, {
                        maxHeight: 150,
                        rotate: !0,
                        overrides: {
                            titles: {
                                enabled: !1
                            },
                            valueAxes: {
                                labelsEnabled: !1,
                                ignoreAxisWidth: !0,
                                axisAlpha: 0,
                                guides: {
                                    label: ""
                                }
                            }
                        }
                    }, {
                        maxHeight: 100,
                        rotate: !1,
                        overrides: {
                            valueAxes: {
                                labelsEnabled: !1,
                                ignoreAxisWidth: !0,
                                axisAlpha: 0,
                                gridAlpha: 0,
                                guides: {
                                    label: ""
                                }
                            }
                        }
                    }, {
                        maxHeight: 100,
                        rotate: !0,
                        overrides: {
                            categoryAxis: {
                                labelsEnabled: !1,
                                ignoreAxisWidth: !0,
                                axisAlpha: 0,
                                gridAlpha: 0,
                                guides: {
                                    label: ""
                                }
                            }
                        }
                    }, {
                        maxWidth: 100,
                        overrides: {
                            autoMargins: !1,
                            marginTop: 0,
                            marginBottom: 0,
                            marginLeft: 0,
                            marginRight: 0,
                            categoryAxis: {
                                labelsEnabled: !1
                            },
                            valueAxes: {
                                labelsEnabled: !1
                            }
                        }
                    }, {
                        maxHeight: 100,
                        overrides: {
                            autoMargins: !1,
                            marginTop: 0,
                            marginBottom: 0,
                            marginLeft: 0,
                            marginRight: 0,
                            categoryAxis: {
                                labelsEnabled: !1
                            },
                            valueAxes: {
                                labelsEnabled: !1
                            }
                        }
                    }],
                    xy: [{
                        maxWidth: 550,
                        legendPosition: "left",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 550,
                        legendPosition: "right",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 100,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        legendPosition: "top",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 350,
                        legendPosition: "bottom",
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxHeight: 100,
                        overrides: {
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 250,
                        overrides: {
                            autoMarginOffset: 0,
                            autoMargins: !1,
                            marginTop: 0,
                            marginBottom: 0,
                            marginLeft: 0,
                            marginRight: 0,
                            valueAxes: {
                                inside: !0,
                                title: "",
                                showFirstLabel: !1,
                                showLastLabel: !1
                            },
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 150,
                        overrides: {
                            valueyAxes: {
                                labelsEnabled: !1,
                                axisAlpha: 0,
                                gridAlpha: 0,
                                guides: {
                                    label: ""
                                }
                            }
                        }
                    }, {
                        maxHeight: 250,
                        overrides: {
                            autoMarginOffset: 0,
                            autoMargins: !1,
                            marginTop: 0,
                            marginBottom: 0,
                            marginLeft: 0,
                            marginRight: 0,
                            valueAxes: {
                                inside: !0,
                                title: "",
                                showFirstLabel: !1,
                                showLastLabel: !1
                            },
                            legend: {
                                enabled: !1
                            }
                        }
                    }, {
                        maxWidth: 150,
                        overrides: {
                            valueyAxes: {
                                labelsEnabled: !1,
                                axisAlpha: 0,
                                gridAlpha: 0,
                                guides: {
                                    label: ""
                                }
                            }
                        }
                    }],
                    stock: [{
                        maxWidth: 500,
                        overrides: {
                            dataSetSelector: {
                                position: "top"
                            },
                            periodSelector: {
                                position: "bottom"
                            }
                        }
                    }, {
                        maxWidth: 400,
                        overrides: {
                            dataSetSelector: {
                                selectText: "",
                                compareText: ""
                            },
                            periodSelector: {
                                periodsText: "",
                                inputFieldsEnabled: !1
                            }
                        }
                    }],
                    map: [{
                        maxWidth: 200,
                        overrides: {
                            zoomControl: {
                                zoomControlEnabled: !1
                            },
                            smallMap: {
                                enabled: !1
                            },
                            valueLegend: {
                                enabled: !1
                            },
                            dataProvider: {
                                areas: {
                                    descriptionWindowWidth: 160,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                },
                                images: {
                                    descriptionWindowWidth: 160,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                },
                                lines: {
                                    descriptionWindowWidth: 160,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                }
                            }
                        }
                    }, {
                        maxWidth: 150,
                        overrides: {
                            dataProvider: {
                                areas: {
                                    descriptionWindowWidth: 110,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                },
                                images: {
                                    descriptionWindowWidth: 110,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                },
                                lines: {
                                    descriptionWindowWidth: 110,
                                    descriptionWindowLeft: 10,
                                    descriptionWindowRight: 10
                                }
                            }
                        }
                    }, {
                        maxHeight: 200,
                        overrides: {
                            zoomControl: {
                                zoomControlEnabled: !1
                            },
                            smallMap: {
                                enabled: !1
                            },
                            valueLegend: {
                                enabled: !1
                            },
                            dataProvider: {
                                areas: {
                                    descriptionWindowHeight: 160,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                },
                                images: {
                                    descriptionWindowHeight: 160,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                },
                                lines: {
                                    descriptionWindowHeight: 160,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                }
                            }
                        }
                    }, {
                        maxHeight: 150,
                        overrides: {
                            dataProvider: {
                                areas: {
                                    descriptionWindowHeight: 110,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                },
                                images: {
                                    descriptionWindowHeight: 110,
                                    descriptionWindowRight: 10,
                                    descriptionWindowTop: 10
                                },
                                lines: {
                                    descriptionWindowHeight: 110,
                                    descriptionWindowLeft: 10,
                                    descriptionWindowRight: 10
                                }
                            }
                        }
                    }]
                };
                void 0 != b.rules && 0 != b.rules.length && h(b.rules) ? !1 !== b.addDefaultRules && (b.rules = b.rules.concat(d[a.type])) : b.rules = d[a.type], k(a, "zoomOutOnDataUpdate", a.zoomOutOnDataUpdate), a.addListener("resized", e), a.addListener("init", e)
            }
        }
    }
}, ["pie", "serial", "xy", "funnel", "radar", "gauge", "stock", "map"]);