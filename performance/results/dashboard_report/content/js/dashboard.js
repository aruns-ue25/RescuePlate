/*
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
var showControllersOnly = false;
var seriesFilter = "";
var filtersOnlySampleSeries = true;

/*
 * Add header in statistics table to group metrics by category
 * format
 *
 */
function summaryTableHeader(header) {
    var newRow = header.insertRow(-1);
    newRow.className = "tablesorter-no-sort";
    var cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 1;
    cell.innerHTML = "Requests";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 3;
    cell.innerHTML = "Executions";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 7;
    cell.innerHTML = "Response Times (ms)";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 1;
    cell.innerHTML = "Throughput";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 2;
    cell.innerHTML = "Network (KB/sec)";
    newRow.appendChild(cell);
}

/*
 * Populates the table identified by id parameter with the specified data and
 * format
 *
 */
function createTable(table, info, formatter, defaultSorts, seriesIndex, headerCreator) {
    var tableRef = table[0];

    // Create header and populate it with data.titles array
    var header = tableRef.createTHead();

    // Call callback is available
    if(headerCreator) {
        headerCreator(header);
    }

    var newRow = header.insertRow(-1);
    for (var index = 0; index < info.titles.length; index++) {
        var cell = document.createElement('th');
        cell.innerHTML = info.titles[index];
        newRow.appendChild(cell);
    }

    var tBody;

    // Create overall body if defined
    if(info.overall){
        tBody = document.createElement('tbody');
        tBody.className = "tablesorter-no-sort";
        tableRef.appendChild(tBody);
        var newRow = tBody.insertRow(-1);
        var data = info.overall.data;
        for(var index=0;index < data.length; index++){
            var cell = newRow.insertCell(-1);
            cell.innerHTML = formatter ? formatter(index, data[index]): data[index];
        }
    }

    // Create regular body
    tBody = document.createElement('tbody');
    tableRef.appendChild(tBody);

    var regexp;
    if(seriesFilter) {
        regexp = new RegExp(seriesFilter, 'i');
    }
    // Populate body with data.items array
    for(var index=0; index < info.items.length; index++){
        var item = info.items[index];
        if((!regexp || filtersOnlySampleSeries && !info.supportsControllersDiscrimination || regexp.test(item.data[seriesIndex]))
                &&
                (!showControllersOnly || !info.supportsControllersDiscrimination || item.isController)){
            if(item.data.length > 0) {
                var newRow = tBody.insertRow(-1);
                for(var col=0; col < item.data.length; col++){
                    var cell = newRow.insertCell(-1);
                    cell.innerHTML = formatter ? formatter(col, item.data[col]) : item.data[col];
                }
            }
        }
    }

    // Add support of columns sort
    table.tablesorter({sortList : defaultSorts});
}

$(document).ready(function() {

    // Customize table sorter default options
    $.extend( $.tablesorter.defaults, {
        theme: 'blue',
        cssInfoBlock: "tablesorter-no-sort",
        widthFixed: true,
        widgets: ['zebra']
    });

    var data = {"OkPercent": 100.0, "KoPercent": 0.0};
    var dataset = [
        {
            "label" : "FAIL",
            "data" : data.KoPercent,
            "color" : "#FF6347"
        },
        {
            "label" : "PASS",
            "data" : data.OkPercent,
            "color" : "#9ACD32"
        }];
    $.plot($("#flot-requests-summary"), dataset, {
        series : {
            pie : {
                show : true,
                radius : 1,
                label : {
                    show : true,
                    radius : 3 / 4,
                    formatter : function(label, series) {
                        return '<div style="font-size:8pt;text-align:center;padding:2px;color:white;">'
                            + label
                            + '<br/>'
                            + Math.round10(series.percent, -2)
                            + '%</div>';
                    },
                    background : {
                        opacity : 0.5,
                        color : '#000'
                    }
                }
            }
        },
        legend : {
            show : true
        }
    });

    // Creates APDEX table
    createTable($("#apdexTable"), {"supportsControllersDiscrimination": true, "overall": {"data": [0.9996250937265684, 500, 1500, "Total"], "isController": false}, "titles": ["Apdex", "T (Toleration threshold)", "F (Frustration threshold)", "Label"], "items": [{"data": [0.998, 500, 1500, "TC-03: Poll Delivery Tracking By Request"], "isController": true}, {"data": [1.0, 500, 1500, "TC-08: Complete Donation Lifecycle"], "isController": true}, {"data": [1.0, 500, 1500, "TC-01: Delivery Service Health Check"], "isController": true}, {"data": [1.0, 500, 1500, "GET /api/health (Delivery Service)"], "isController": false}, {"data": [1.0, 500, 1500, "POST /api/deliveries/arrange"], "isController": false}, {"data": [1.0, 500, 1500, "TC-06: Record Delivery Collected Transition"], "isController": true}, {"data": [1.0, 500, 1500, "TC-02: Admin Delivery Logistics Summary"], "isController": true}, {"data": [1.0, 500, 1500, "GET /api/deliveries/admin/summary"], "isController": false}, {"data": [1.0, 500, 1500, "POST /api/deliveries/1/complete"], "isController": false}, {"data": [0.998, 500, 1500, "GET /api/deliveries/request/1"], "isController": false}, {"data": [1.0, 500, 1500, "TC-04: Get Delivery Tracking By ID"], "isController": true}, {"data": [0.5, 500, 1500, "Auth - Login as Admin"], "isController": false}, {"data": [1.0, 500, 1500, "POST /api/deliveries/1/receive"], "isController": false}, {"data": [1.0, 500, 1500, "GET /api/deliveries/1"], "isController": false}, {"data": [1.0, 500, 1500, "TC-05: Arrange Delivery Collection Concurrency"], "isController": true}, {"data": [1.0, 500, 1500, "POST /api/deliveries/1/collect"], "isController": false}, {"data": [1.0, 500, 1500, "TC-07: Confirm Receipt & Condition Review"], "isController": true}]}, function(index, item){
        switch(index){
            case 0:
                item = item.toFixed(3);
                break;
            case 1:
            case 2:
                item = formatDuration(item);
                break;
        }
        return item;
    }, [[0, 0]], 3);

    // Create statistics table
    createTable($("#statisticsTable"), {"supportsControllersDiscrimination": true, "overall": {"data": ["Total", 2001, 0, 0.0, 5.819590204897542, 0, 873, 3.0, 7.0, 8.0, 35.940000000000055, 62.90474693492612, 36.801915916771456, 58.03117509234518], "isController": false}, "titles": ["Label", "#Samples", "FAIL", "Error %", "Average", "Min", "Max", "Median", "90th pct", "95th pct", "99th pct", "Transactions/s", "Received", "Sent"], "items": [{"data": ["TC-03: Poll Delivery Tracking By Request", 250, 0, 0.0, 9.091999999999992, 2, 603, 4.5, 8.0, 10.899999999999977, 179.57000000000266, 8.991835413444592, 13.31792685366687, 7.736139647699888], "isController": true}, {"data": ["TC-08: Complete Donation Lifecycle", 250, 0, 0.0, 4.492, 1, 53, 4.0, 7.0, 8.0, 24.0, 9.362594562205079, 2.953240276945547, 8.905436624597408], "isController": true}, {"data": ["TC-01: Delivery Service Health Check", 250, 0, 0.0, 2.6880000000000006, 0, 115, 2.0, 4.0, 4.449999999999989, 16.820000000000164, 8.952551477170994, 3.1727003133393015, 7.5799434870188005], "isController": true}, {"data": ["GET /api/health (Delivery Service)", 250, 0, 0.0, 2.6880000000000006, 0, 115, 2.0, 4.0, 4.449999999999989, 16.820000000000164, 8.951910337666058, 3.1724730995094355, 7.5794006472231175], "isController": false}, {"data": ["POST /api/deliveries/arrange", 250, 0, 0.0, 5.795999999999998, 2, 81, 4.0, 8.0, 10.449999999999989, 62.13000000000034, 9.214551619918176, 2.8435530389591244, 10.267386131178357], "isController": false}, {"data": ["TC-06: Record Delivery Collected Transition", 250, 0, 0.0, 7.571999999999997, 1, 290, 4.0, 7.0, 8.0, 290.0, 9.240777703851556, 2.737002846159533, 8.663229097360835], "isController": true}, {"data": ["TC-02: Admin Delivery Logistics Summary", 250, 0, 0.0, 2.2000000000000006, 1, 22, 2.0, 3.0, 4.0, 12.430000000000064, 8.988602452090749, 1.0357959856901449, 7.768469892676087], "isController": true}, {"data": ["GET /api/deliveries/admin/summary", 250, 0, 0.0, 2.2000000000000006, 1, 22, 2.0, 3.0, 4.0, 12.430000000000064, 8.988279283813906, 1.035758745595743, 7.768190591968073], "isController": false}, {"data": ["POST /api/deliveries/1/complete", 250, 0, 0.0, 4.492, 1, 53, 4.0, 7.0, 8.0, 24.0, 9.361893349310964, 2.953019093581486, 8.90476965061414], "isController": false}, {"data": ["GET /api/deliveries/request/1", 250, 0, 0.0, 9.091999999999992, 2, 603, 4.5, 8.0, 10.899999999999977, 179.57000000000266, 8.988602452090749, 13.313138471667926, 7.7333581643476075], "isController": false}, {"data": ["TC-04: Get Delivery Tracking By ID", 250, 0, 0.0, 6.3199999999999985, 2, 152, 4.0, 8.0, 9.449999999999989, 95.0, 9.186110600771633, 13.605670413834282, 7.83151811960316], "isController": true}, {"data": ["Auth - Login as Admin", 1, 0, 0.0, 873.0, 873, 873, 873.0, 873.0, 873.0, 873.0, 1.1454753722794961, 1.2718803694158076, 0.3076227806414662], "isController": false}, {"data": ["POST /api/deliveries/1/receive", 250, 0, 0.0, 4.932, 1, 68, 4.0, 7.0, 8.0, 68.0, 9.34020772621983, 3.046513066950609, 8.929749378876187], "isController": false}, {"data": ["GET /api/deliveries/1", 250, 0, 0.0, 6.3160000000000025, 2, 152, 4.0, 8.0, 9.0, 95.0, 9.184760645137587, 13.603670976615598, 7.830367229692494], "isController": false}, {"data": ["TC-05: Arrange Delivery Collection Concurrency", 250, 0, 0.0, 5.795999999999998, 2, 81, 4.0, 8.0, 10.449999999999989, 62.13000000000034, 9.214551619918176, 2.8435530389591244, 10.267386131178357], "isController": true}, {"data": ["POST /api/deliveries/1/collect", 250, 0, 0.0, 7.571999999999997, 1, 290, 4.0, 7.0, 8.0, 290.0, 9.241119284367723, 2.7371040180386648, 8.66354932909474], "isController": false}, {"data": ["TC-07: Confirm Receipt & Condition Review", 250, 0, 0.0, 4.932, 1, 68, 4.0, 7.0, 8.0, 68.0, 9.339509862522416, 3.046285443439928, 8.929082183017035], "isController": true}]}, function(index, item){
        switch(index){
            // Errors pct
            case 3:
                item = item.toFixed(2) + '%';
                break;
            // Mean
            case 4:
            // Mean
            case 7:
            // Median
            case 8:
            // Percentile 1
            case 9:
            // Percentile 2
            case 10:
            // Percentile 3
            case 11:
            // Throughput
            case 12:
            // Kbytes/s
            case 13:
            // Sent Kbytes/s
                item = item.toFixed(2);
                break;
        }
        return item;
    }, [[0, 0]], 0, summaryTableHeader);

    // Create error table
    createTable($("#errorsTable"), {"supportsControllersDiscrimination": false, "titles": ["Type of error", "Number of errors", "% in errors", "% in all samples"], "items": []}, function(index, item){
        switch(index){
            case 2:
            case 3:
                item = item.toFixed(2) + '%';
                break;
        }
        return item;
    }, [[1, 1]]);

        // Create top5 errors by sampler
    createTable($("#top5ErrorsBySamplerTable"), {"supportsControllersDiscrimination": false, "overall": {"data": ["Total", 2001, 0, "", "", "", "", "", "", "", "", "", ""], "isController": false}, "titles": ["Sample", "#Samples", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors"], "items": [{"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}]}, function(index, item){
        return item;
    }, [[0, 0]], 0);

});
