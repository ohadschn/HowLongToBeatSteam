(function() {
  var __bind = function(fn, me){ return function(){ return fn.apply(me, arguments); }; },
    __indexOf = [].indexOf || function(item) { for (var i = 0, l = this.length; i < l; i++) { if (i in this && this[i] === item) return i; } return -1; };

  this.DataTable = (function() {
    var primitiveCompare, pureComputed;

    pureComputed = ko.pureComputed || ko.computed;

    primitiveCompare = function(item1, item2) {
      if (item2 == null) {
        return item1 == null;
      } else if (item1 != null) {
        if (typeof item1 === 'boolean') {
          return item1 === item2;
        } else {
          return item1.toString().toLowerCase().indexOf(item2.toString().toLowerCase()) >= 0 || item1 === item2;
        }
      } else {
        return false;
      }
    };

    function DataTable(rows, options) {
      this.getLimitedPages = __bind(this.getLimitedPages, this);
      this.getPages = __bind(this.getPages, this);
      var serverSideOpts;
      if (!options) {
        if (!(rows instanceof Array)) {
          options = rows;
          rows = [];
        } else {
          options = {};
        }
      }
      this.options = {
        recordWord: options.recordWord || 'record',
        recordWordPlural: options.recordWordPlural,
        sortDir: options.sortDir || 'asc',
        sortField: options.sortField || void 0,
        perPage: options.perPage || 15,
        paginationLimit: options.paginationLimit || 10,
        filterFn: options.filterFn || void 0,
        unsortedClass: options.unsortedClass || '',
        descSortClass: options.descSortClass || '',
        ascSortClass: options.ascSortClass || ''
      };
      this.initObservables();
      if ((serverSideOpts = options.serverSidePagination) && serverSideOpts.enabled) {
        if (!(serverSideOpts.path && serverSideOpts.loader)) {
          throw new Error("`path` or `loader` missing from `serverSidePagination` object");
        }
        this.options.paginationPath = serverSideOpts.path;
        this.options.resultHandlerFn = serverSideOpts.loader;
        this.initWithServerSidePagination();
      } else {
        this.initWithClientSidePagination(rows);
      }
    }

    DataTable.prototype.initObservables = function() {
      this.sortDir = ko.observable(this.options.sortDir);
      this.sortField = ko.observable(this.options.sortField);
      this.perPage = ko.observable(this.options.perPage);
      this.currentPageNumber = ko.observable(1);
      this.filter = ko.observable('');
      this.loading = ko.observable(false);
      return this.rows = ko.observableArray([]);
    };

    DataTable.prototype.getPages = function(rowCount) {
      var page, pageNumber, pagesArr, perPage, rowIndex;
      perPage = this.perPage();
      rowIndex = 0;
      pageNumber = 1;
      pagesArr = new Array(Math.ceil(rowCount / perPage));
      while (rowIndex < rowCount) {
        page = {
          number: pageNumber,
          start: rowIndex,
          end: Math.min(rowCount - 1, rowIndex + perPage - 1)
        };
        page.blanks = new Array(pagesArr.length > 1 ? perPage - (page.end - page.start + 1) : 0);
        pagesArr[pageNumber - 1] = page;
        pageNumber++;
        rowIndex += perPage;
      }
      return pagesArr;
    };

    DataTable.prototype.getLimitedPages = function() {
      var current, firstPage, lastPage, leftMargin, limit, pages, rightMargin;
      pages = this.pages();
      current = this.currentPageNumber();
      limit = this.options.paginationLimit;
      if (pages.length <= limit) {
        return pages;
      }
      leftMargin = Math.floor(limit / 2);
      firstPage = current - Math.floor(leftMargin);
      if (firstPage < 1) {
        return pages.slice(0, limit);
      }
      rightMargin = limit % 2 === 0 ? leftMargin - 1 : leftMargin;
      lastPage = current + rightMargin;
      if (lastPage > pages.length) {
        return pages.slice(pages.length - limit, pages.length);
      }
      return pages.slice(firstPage - 1, lastPage);
    };

    DataTable.prototype.initWithClientSidePagination = function(rows) {
      var _defaultMatch;
      this.filtering = ko.observable(false);
      this.filter.subscribe((function(_this) {
        return function() {
          return _this.currentPageNumber(1);
        };
      })(this));
      this.perPage.subscribe((function(_this) {
        return function() {
          return _this.currentPageNumber(1);
        };
      })(this));
      this.rows(rows);
      this.rowAttributeMap = pureComputed((function(_this) {
        return function() {
          var attrMap, key, row;
          rows = _this.rows();
          attrMap = {};
          if (rows.length > 0) {
            row = rows[0];
            for (key in row) {
              if (row.hasOwnProperty(key)) {
                attrMap[key.toLowerCase()] = key;
              }
            }
          }
          return attrMap;
        };
      })(this));
      this.filteredRows = pureComputed((function(_this) {
        return function() {
          var filter, filterFn;
          _this.filtering(true);
          filter = _this.filter();
          rows = _this.rows.slice(0);
          if (filter !== '') {
            filterFn = _this.filterFn(filter);
            rows = rows.filter(filterFn);
          }
          if ((_this.sortField() != null) && _this.sortField() !== '') {
            rows.sort(function(a, b) {
              var aVal, bVal;
              aVal = ko.utils.unwrapObservable(a[_this.sortField()]);
              bVal = ko.utils.unwrapObservable(b[_this.sortField()]);
              if (typeof aVal === 'string') {
                aVal = aVal.toLowerCase();
              }
              if (typeof bVal === 'string') {
                bVal = bVal.toLowerCase();
              }
              if (_this.sortDir() === 'asc') {
                if (aVal < bVal || aVal === '' || (aVal == null)) {
                  return -1;
                } else {
                  if (aVal > bVal || bVal === '' || (bVal == null)) {
                    return 1;
                  } else {
                    return 0;
                  }
                }
              } else {
                if (aVal < bVal || aVal === '' || (aVal == null)) {
                  return 1;
                } else {
                  if (aVal > bVal || bVal === '' || (bVal == null)) {
                    return -1;
                  } else {
                    return 0;
                  }
                }
              }
            });
          } else {
            rows;
          }
          _this.filtering(false);
          return rows;
        };
      })(this)).extend({
        rateLimit: 50,
        method: 'notifyWhenChangesStop'
      });
      this.pages = pureComputed((function(_this) {
        return function() {
          return _this.getPages(_this.filteredRows().length);
        };
      })(this));
      this.limitedPages = pureComputed((function(_this) {
        return function() {
          return _this.getLimitedPages();
        };
      })(this));
      this.currentPage = pureComputed((function(_this) {
        return function() {
          if (_this.pages().length > 0) {
            return _this.pages()[_this.currentPageNumber() - 1];
          } else {
            return {
              number: 1,
              start: 0,
              end: 0,
              blanks: []
            };
          }
        };
      })(this));
      this.pagedRows = pureComputed((function(_this) {
        return function() {
          return _this.filteredRows().slice(_this.currentPage().start, _this.currentPage().end + 1);
        };
      })(this));
      this.leftPagerClass = pureComputed((function(_this) {
        return function() {
          if (_this.currentPageNumber() === 1) {
            return 'disabled';
          }
        };
      })(this));
      this.rightPagerClass = pureComputed((function(_this) {
        return function() {
          if (_this.currentPageNumber() === _this.pages().length) {
            return 'disabled';
          }
        };
      })(this));
      this.total = pureComputed((function(_this) {
        return function() {
          return _this.filteredRows().length;
        };
      })(this));
      this.from = pureComputed((function(_this) {
        return function() {
          return _this.currentPage().start + 1;
        };
      })(this));
      this.to = pureComputed((function(_this) {
        return function() {
          return _this.currentPage().end + 1;
        };
      })(this));
      this.recordsText = pureComputed((function(_this) {
        return function() {
          var from, recordWord, recordWordPlural, to, total;
          from = _this.from();
          to = _this.to();
          total = _this.total();
          recordWord = _this.options.recordWord;
          recordWordPlural = _this.options.recordWordPlural || recordWord + 's';
          if (_this.pages().length > 1) {
            return "" + from + " to " + to + " of " + total + " " + recordWordPlural;
          } else {
            return "" + total + " " + (total > 1 || total === 0 ? recordWordPlural : recordWord);
          }
        };
      })(this));
      this.showNoData = pureComputed((function(_this) {
        return function() {
          return _this.pagedRows().length === 0 && !_this.loading();
        };
      })(this));
      this.showLoading = pureComputed((function(_this) {
        return function() {
          return _this.loading();
        };
      })(this));
      this.sortClass = (function(_this) {
        return function(column) {
          return pureComputed(function() {
            if (_this.sortField() === column) {
              return 'sorted ' + (_this.sortDir() === 'asc' ? _this.options.ascSortClass : _this.options.descSortClass);
            } else {
              return _this.options.unsortedClass;
            }
          });
        };
      })(this);
      this.addRecord = (function(_this) {
        return function(record) {
          return _this.rows.push(record);
        };
      })(this);
      this.removeRecord = (function(_this) {
        return function(record) {
          _this.rows.remove(record);
          if (_this.pagedRows().length === 0) {
            return _this.prevPage();
          }
        };
      })(this);
      this.replaceRows = (function(_this) {
        return function(rows) {
          _this.rows(rows);
          _this.currentPageNumber(1);
          return _this.filter(void 0);
        };
      })(this);
      _defaultMatch = function(filter, row, attrMap) {
        var key, val;
        return ((function() {
          var _results;
          _results = [];
          for (key in attrMap) {
            val = attrMap[key];
            _results.push(val);
          }
          return _results;
        })()).some(function(val) {
          return primitiveCompare((ko.isObservable(row[val]) ? row[val]() : row[val]), filter);
        });
      };
      return this.filterFn = this.options.filterFn || (function(_this) {
        return function(filterVar) {
          var filter, specials, _ref;
          _ref = [[], {}], filter = _ref[0], specials = _ref[1];
          filterVar.split(' ').forEach(function(word) {
            var words;
            if (word.indexOf(':') >= 0) {
              words = word.split(':');
              return specials[words[0]] = (function() {
                switch (words[1].toLowerCase()) {
                  case 'yes':
                  case 'true':
                    return true;
                  case 'no':
                  case 'false':
                    return false;
                  case 'blank':
                  case 'none':
                  case 'null':
                  case 'undefined':
                    return void 0;
                  default:
                    return words[1].toLowerCase();
                }
              })();
            } else {
              return filter.push(word);
            }
          });
          filter = filter.join(' ');
          return function(row) {
            var conditionals, key, val;
            conditionals = (function() {
              var _results;
              _results = [];
              for (key in specials) {
                val = specials[key];
                _results.push((function(_this) {
                  return function(key, val) {
                    var rowAttr;
                    if (rowAttr = _this.rowAttributeMap()[key.toLowerCase()]) {
                      return primitiveCompare((ko.isObservable(row[rowAttr]) ? row[rowAttr]() : row[rowAttr]), val);
                    } else {
                      return false;
                    }
                  };
                })(this)(key, val));
              }
              return _results;
            }).call(_this);
            return (__indexOf.call(conditionals, false) < 0) && (filter !== '' ? (row.match != null ? row.match(filter) : _defaultMatch(filter, row, _this.rowAttributeMap())) : true);
          };
        };
      })(this);
    };

    DataTable.prototype.initWithServerSidePagination = function() {
      var _gatherData, _getDataFromServer;
      _getDataFromServer = (function(_this) {
        return function(data, cb) {
          var key, req, url, val;
          url = "" + _this.options.paginationPath + "?" + (((function() {
            var _results;
            _results = [];
            for (key in data) {
              val = data[key];
              _results.push("" + (encodeURIComponent(key)) + "=" + (encodeURIComponent(val)));
            }
            return _results;
          })()).join('&'));
          req = new XMLHttpRequest();
          req.open('GET', url, true);
          req.setRequestHeader('Content-Type', 'application/json');
          req.onload = function() {
            if (req.status >= 200 && req.status < 400) {
              return cb(null, JSON.parse(req.responseText));
            } else {
              return cb(new Error("Error communicating with server"));
            }
          };
          req.onerror = function() {
            return cb(new Error("Error communicating with server"));
          };
          return req.send();
        };
      })(this);
      _gatherData = function(perPage, currentPageNumber, filter, sortDir, sortField) {
        var data;
        data = {
          perPage: perPage,
          page: currentPageNumber
        };
        if ((filter != null) && filter !== '') {
          data.filter = filter;
        }
        if ((sortDir != null) && sortDir !== '' && (sortField != null) && sortField !== '') {
          data.sortDir = sortDir;
          data.sortBy = sortField;
        }
        return data;
      };
      this.filtering = ko.observable(false);
      this.pagedRows = ko.observableArray([]);
      this.numFilteredRows = ko.observable(0);
      this.filter.subscribe((function(_this) {
        return function() {
          return _this.currentPageNumber(1);
        };
      })(this));
      this.perPage.subscribe((function(_this) {
        return function() {
          return _this.currentPageNumber(1);
        };
      })(this));
      ko.computed((function(_this) {
        return function() {
          var data;
          _this.loading(true);
          _this.filtering(true);
          data = _gatherData(_this.perPage(), _this.currentPageNumber(), _this.filter(), _this.sortDir(), _this.sortField());
          return _getDataFromServer(data, function(err, response) {
            var results, total;
            _this.loading(false);
            _this.filtering(false);
            if (err) {
              return console.log(err);
            }
            total = response.total, results = response.results;
            _this.numFilteredRows(total);
            return _this.pagedRows(results.map(_this.options.resultHandlerFn));
          });
        };
      })(this)).extend({
        rateLimit: 500,
        method: 'notifyWhenChangesStop'
      });
      this.pages = pureComputed((function(_this) {
        return function() {
          return _this.getPages(_this.numFilteredRows());
        };
      })(this));
      this.limitedPages = pureComputed((function(_this) {
        return function() {
          return _this.getLimitedPages();
        };
      })(this));
      this.currentPage = pureComputed((function(_this) {
        return function() {
          return _this.pages()[_this.currentPageNumber() - 1];
        };
      })(this));
      this.pagedRows = pureComputed((function(_this) {
        return function() {
          return _this.filteredRows().slice(_this.currentPage().start, _this.currentPage().end + 1);
        };
      })(this));
      this.leftPagerClass = pureComputed((function(_this) {
        return function() {
          if (_this.currentPageNumber() === 1) {
            return 'disabled';
          }
        };
      })(this));
      this.rightPagerClass = pureComputed((function(_this) {
        return function() {
          if (_this.currentPageNumber() === _this.pages().length) {
            return 'disabled';
          }
        };
      })(this));
      this.from = pureComputed((function(_this) {
        return function() {
          return _this.currentPage().start + 1;
        };
      })(this));
      this.to = pureComputed((function(_this) {
        return function() {
          return _this.currentPage().end + 1;
        };
      })(this));
      this.recordsText = pureComputed((function(_this) {
        return function() {
          var from, recordWord, recordWordPlural, to, total;
          total = _this.numFilteredRows();
          from = _this.from();
          to = _this.to();
          recordWord = _this.options.recordWord;
          recordWordPlural = _this.options.recordWordPlural || recordWord + 's';
          if (_this.pages().length > 1) {
            return "" + from + " to " + to + " of " + total + " " + recordWordPlural;
          } else {
            return "" + total + " " + (total > 1 || total === 0 ? recordWordPlural : recordWord);
          }
        };
      })(this));
      this.showNoData = pureComputed((function(_this) {
        return function() {
          return _this.pagedRows().length === 0 && !_this.loading();
        };
      })(this));
      this.showLoading = pureComputed((function(_this) {
        return function() {
          return _this.loading();
        };
      })(this));
      this.sortClass = (function(_this) {
        return function(column) {
          return pureComputed(function() {
            if (_this.sortField() === column) {
              return 'sorted ' + (_this.sortDir() === 'asc' ? _this.options.ascSortClass : _this.options.descSortClass);
            } else {
              return _this.options.unsortedClass;
            }
          });
        };
      })(this);
      this.addRecord = function() {
        throw new Error("#addRecord() not applicable with serverSidePagination enabled");
      };
      this.removeRecord = function() {
        throw new Error("#removeRecord() not applicable with serverSidePagination enabled");
      };
      this.replaceRows = function() {
        throw new Error("#replaceRows() not applicable with serverSidePagination enabled");
      };
      return this.refreshData = (function(_this) {
        return function() {
          var data;
          _this.loading(true);
          _this.filtering(true);
          data = _gatherData(_this.perPage(), _this.currentPageNumber(), _this.filter(), _this.sortDir(), _this.sortField());
          return _getDataFromServer(data, function(err, response) {
            var results, total;
            _this.loading(false);
            _this.filtering(false);
            if (err) {
              return console.log(err);
            }
            total = response.total, results = response.results;
            _this.numFilteredRows(total);
            return _this.pagedRows(results.map(_this.options.resultHandlerFn));
          });
        };
      })(this);
    };

    DataTable.prototype.toggleSort = function(field) {
      return (function(_this) {
        return function() {
          _this.currentPageNumber(1);
          if (_this.sortField() === field) {
            return _this.sortDir(_this.sortDir() === 'asc' ? 'desc' : 'asc');
          } else {
            _this.sortDir('asc');
            return _this.sortField(field);
          }
        };
      })(this);
    };

    DataTable.prototype.prevPage = function() {
      var page;
      page = this.currentPageNumber();
      if (page !== 1) {
        return this.currentPageNumber(page - 1);
      }
    };

    DataTable.prototype.nextPage = function() {
      var page;
      page = this.currentPageNumber();
      if (page !== this.pages().length) {
        return this.currentPageNumber(page + 1);
      }
    };

    DataTable.prototype.gotoPage = function(page) {
      return (function(_this) {
        return function() {
          return _this.currentPageNumber(page);
        };
      })(this);
    };

    DataTable.prototype.pageClass = function(page) {
      return pureComputed((function(_this) {
        return function() {
          if (_this.currentPageNumber() === page) {
            return 'active';
          }
        };
      })(this));
    };

    return DataTable;

  })();

}).call(this);
