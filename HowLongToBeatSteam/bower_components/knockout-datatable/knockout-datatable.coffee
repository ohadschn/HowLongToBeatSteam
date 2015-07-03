class @DataTable

  pureComputed = ko.pureComputed or ko.computed

  primitiveCompare = (item1, item2) ->
    if not item2?
      not item1?
    else if item1?
      if typeof item1 is 'boolean'
        item1 is item2
      else
        item1.toString().toLowerCase().indexOf(item2.toString().toLowerCase()) >= 0 or item1 is item2
    else
      false

  constructor: (rows, options) ->

    if not options
      unless rows instanceof Array
        options = rows
        rows = []
      else
        options = {}

    # set some default options if none were passed in
    @options =
      recordWord:       options.recordWord          or 'record'
      recordWordPlural: options.recordWordPlural
      sortDir:          options.sortDir             or 'asc'
      sortField:        options.sortField           or undefined
      perPage:          options.perPage             or 15
      paginationLimit:  options.paginationLimit     or 10
      filterFn:         options.filterFn            or undefined
      alwaysMatch:      options.alwaysMatch         or false
      unsortedClass:    options.unsortedClass       or ''
      descSortClass:    options.descSortClass       or ''
      ascSortClass:     options.ascSortClass        or ''

    @initObservables()

    if (serverSideOpts = options.serverSidePagination) and serverSideOpts.enabled
      unless serverSideOpts.path and serverSideOpts.loader
        throw new Error("`path` or `loader` missing from `serverSidePagination` object")
      @options.paginationPath  = serverSideOpts.path
      @options.resultHandlerFn = serverSideOpts.loader

      # if server-side pagination enabled, we don't care about the initial rows
      @initWithServerSidePagination()

    else
      @initWithClientSidePagination(rows)

  initObservables: ->
    @sortDir     = ko.observable @options.sortDir
    @sortField   = ko.observable @options.sortField
    @perPage     = ko.observable @options.perPage
    @currentPageNumber = ko.observable 1
    @filter      = ko.observable('').extend({ rateLimit: { method: "notifyWhenChangesStop", timeout: 400 } });
    @loading     = ko.observable false
    @rows        = ko.observableArray []
    
  getPages: (rowCount) =>
    perPage = @perPage()
    rowIndex = 0
    pageNumber = 1
    pagesArr = new Array(Math.ceil(rowCount / perPage))
    while rowIndex < rowCount
        page =
            number: pageNumber
            start: rowIndex
            end: Math.min(rowCount-1, rowIndex+perPage-1)
        page.blanks = new Array(if pagesArr.length > 1 then perPage-(page.end-page.start+1) else 0)
        pagesArr[pageNumber-1] = page 
        
        pageNumber++
        rowIndex += perPage
    
    return pagesArr
    
  getLimitedPages: () =>
    pages = @pages()
    current = @currentPageNumber()
    limit = @options.paginationLimit
    if (pages.length <= limit)
            return pages
              
    leftMargin = Math.floor(limit/2)
    firstPage = current - Math.floor(leftMargin)
    if (firstPage < 1)
        return pages.slice(0, limit)
            
    rightMargin = if limit%2 == 0 then leftMargin-1 else leftMargin
    lastPage = current + rightMargin
    if (lastPage > pages.length)
        return pages.slice(pages.length - limit, pages.length)
            
    return pages.slice(firstPage-1, lastPage)
    

  initWithClientSidePagination: (rows) ->
    @filtering = ko.observable false

    @filter.subscribe => @currentPageNumber 1
    @perPage.subscribe => @currentPageNumber 1

    @rows rows

    @rowAttributeMap = pureComputed =>
      rows = @rows()
      attrMap = {}

      if rows.length > 0
        row = rows[0]
        (attrMap[key.toLowerCase()] = key) for key of row when row.hasOwnProperty(key)

      attrMap

    filterTrigger = ko.observable().extend({notify:'always'})
    
    @triggerFilterCalculation = =>
        filterTrigger.valueHasMutated()    
        @currentPageNumber 1
        
    @filteredRows = pureComputed =>
      filterTrigger()
      @filtering true
      filter = @filter()

      rows = @rows.slice(0)

      if @options.alwaysMatch or filter isnt ''
        filterFn = @filterFn(filter)
        rows = rows.filter(filterFn)

      if @sortField()? and @sortField() isnt ''
        rows.sort (a,b) =>
          aVal = ko.utils.unwrapObservable a[@sortField()]
          bVal = ko.utils.unwrapObservable b[@sortField()]
          if typeof aVal is 'string' then aVal = aVal.toLowerCase()
          if typeof bVal is 'string' then bVal = bVal.toLowerCase()
          if @sortDir() is 'asc'
            if aVal < bVal or aVal is '' or not aVal? then -1 else (if aVal > bVal or bVal is '' or not bVal? then 1 else 0)
          else
            if aVal < bVal or aVal is '' or not aVal? then 1 else (if aVal > bVal or bVal is '' or not bVal? then -1 else 0)
      else
        rows

      @filtering false

      rows

    .extend {rateLimit: 50, method: 'notifyWhenChangesStop'}
      
    @pages = pureComputed => @getPages @filteredRows().length
    @limitedPages = pureComputed => @getLimitedPages()
    @currentPage = pureComputed => if @pages().length > 0 then @pages()[@currentPageNumber() - 1] else {number: 1, start:0, end: 0, blanks: []}
    @pagedRows = pureComputed => @filteredRows().slice(@currentPage().start, @currentPage().end + 1)

    @leftPagerClass = pureComputed => 'disabled' if @currentPageNumber() is 1
    @rightPagerClass = pureComputed => 'disabled' if @currentPageNumber() is @pages().length

    # info
    @total = pureComputed => @filteredRows().length
    @from = pureComputed => @currentPage().start + 1
    @to = pureComputed => @currentPage().end  + 1

    @recordsText = pureComputed =>
      from = @from()
      to = @to()
      total = @total()
      recordWord = @options.recordWord
      recordWordPlural = @options.recordWordPlural or recordWord + 's'
      if @pages().length > 1
        "#{from} to #{to} of #{total} #{recordWordPlural}"
      else
        "#{total} #{if total > 1 or total is 0 then recordWordPlural else recordWord}"

    # state info
    @showNoData  = pureComputed => @pagedRows().length is 0 and not @loading()
    @showLoading = pureComputed => @loading()

    # sort arrows
    @sortClass = (column) =>
      pureComputed =>
        if @sortField() is column
          'sorted ' +
          if @sortDir() is 'asc'
            @options.ascSortClass
          else
            @options.descSortClass
        else
          @options.unsortedClass

    @addRecord = (record) => @rows.push record

    @removeRecord = (record) =>
      @rows.remove record
      if @pagedRows().length is 0
        @prevPage()

    @replaceRows = (rows) =>
      @rows rows
      @currentPageNumber 1
      @filter undefined

    _defaultMatch = (filter, row, attrMap) ->
      (val for key, val of attrMap).some (val) ->
        primitiveCompare((if ko.isObservable(row[val]) then row[val]() else row[val]), filter)

    @filterFn = @options.filterFn or (filterVar) =>
      # Split up filterVar into :-based conditionals and a filter
      [filter, specials] = [[],{}]
      filterVar.split(' ').forEach (word) ->
        if word.indexOf(':') >= 0
          words = word.split(':')
          specials[words[0]] = switch words[1].toLowerCase()
            when 'yes', 'true' then true
            when 'no', 'false' then false
            when 'blank', 'none', 'null', 'undefined' then undefined
            else words[1].toLowerCase()
        else
          filter.push word
      filter = filter.join(' ')
      return (row) =>
        conditionals = for key, val of specials
          do (key, val) =>
            if rowAttr = @rowAttributeMap()[key.toLowerCase()] # If the current key (lowercased) is in the attr map
              primitiveCompare((if ko.isObservable(row[rowAttr]) then row[rowAttr]() else row[rowAttr]), val)
            else # if the current instance doesn't have the "key" attribute, return false (i.e., it's not a match)
              false
        (false not in conditionals) and (if (@options.alwaysMatch or filter isnt '') then (if row.match? then row.match(filter) else _defaultMatch(filter, row, @rowAttributeMap())) else true)

  initWithServerSidePagination: ->
    _getDataFromServer = (data, cb) =>
      url = "#{@options.paginationPath}?#{("#{encodeURIComponent(key)}=#{encodeURIComponent(val)}" for key, val of data).join('&')}"

      req = new XMLHttpRequest()
      req.open 'GET', url, true
      req.setRequestHeader 'Content-Type', 'application/json'

      req.onload = ->
        if req.status >= 200 and req.status < 400
          cb null, JSON.parse(req.responseText)
        else
          cb new Error("Error communicating with server")

      req.onerror = -> cb new Error "Error communicating with server"

      req.send()

    _gatherData = (perPage, currentPageNumber, filter, sortDir, sortField) ->
      data =
        perPage: perPage
        page:    currentPageNumber

      if filter? and filter isnt ''
        data.filter  = filter

      if sortDir? and sortDir isnt '' and sortField? and sortField isnt ''
        data.sortDir = sortDir
        data.sortBy  = sortField

      return data

    @filtering = ko.observable false
    @pagedRows = ko.observableArray []
    @numFilteredRows = ko.observable 0

    @filter.subscribe => @currentPageNumber 1
    @perPage.subscribe => @currentPageNumber 1

    ko.computed =>
      @loading true
      @filtering true

      data = _gatherData @perPage(), @currentPageNumber(), @filter(), @sortDir(), @sortField()

      _getDataFromServer data, (err, response) =>
        @loading false
        @filtering false
        if err then return console.log err

        {total, results} = response
        @numFilteredRows total
        @pagedRows results.map(@options.resultHandlerFn)

    .extend {rateLimit: 500, method: 'notifyWhenChangesStop'}

    @pages = pureComputed => @getPages @numFilteredRows()
    @limitedPages = pureComputed => @getLimitedPages()
    @currentPage = pureComputed => @pages()[@currentPageNumber() - 1]
    @pagedRows = pureComputed => @filteredRows().slice(@currentPage().start, @currentPage().end + 1)

    @leftPagerClass = pureComputed => 'disabled' if @currentPageNumber() is 1
    @rightPagerClass = pureComputed => 'disabled' if @currentPageNumber() is @pages().length

    # info
    @from = pureComputed => @currentPage().start + 1
    @to = pureComputed => @currentPage().end  + 1

    @recordsText = pureComputed =>
      total = @numFilteredRows()
      from = @from()
      to = @to()
      recordWord = @options.recordWord
      recordWordPlural = @options.recordWordPlural or recordWord + 's'
      if @pages().length > 1
        "#{from} to #{to} of #{total} #{recordWordPlural}"
      else
        "#{total} #{if total > 1 or total is 0 then recordWordPlural else recordWord}"

    # state info
    @showNoData  = pureComputed => @pagedRows().length is 0 and not @loading()
    @showLoading = pureComputed => @loading()

    # sort arrows
    @sortClass = (column) =>
      pureComputed =>
        if @sortField() is column
          'sorted ' +
          if @sortDir() is 'asc'
            @options.ascSortClass
          else
            @options.descSortClass
        else
          @options.unsortedClass

    @addRecord = ->
      throw new Error("#addRecord() not applicable with serverSidePagination enabled")

    @removeRecord = ->
      throw new Error("#removeRecord() not applicable with serverSidePagination enabled")

    @replaceRows = ->
      throw new Error("#replaceRows() not applicable with serverSidePagination enabled")

    @refreshData = =>
      @loading true
      @filtering true

      data = _gatherData @perPage(), @currentPageNumber(), @filter(), @sortDir(), @sortField()

      _getDataFromServer data, (err, response) =>
        @loading false
        @filtering false
        if err then return console.log err

        {total, results} = response
        @numFilteredRows total
        @pagedRows results.map(@options.resultHandlerFn)

  toggleSort: (field) -> =>
    @currentPageNumber 1
    if @sortField() is field
      @sortDir if @sortDir() is 'asc' then 'desc' else 'asc'
    else
      @sortDir 'asc'
      @sortField field

  prevPage: ->
    page = @currentPageNumber()
    if page isnt 1
      @currentPageNumber page - 1

  nextPage: ->
    page = @currentPageNumber()
    if page isnt @pages().length
      @currentPageNumber page + 1

  gotoPage: (page) -> => @currentPageNumber page

  pageClass: (page) -> pureComputed => 'active' if @currentPageNumber() is page
