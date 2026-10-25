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
    createTable($("#apdexTable"), {"supportsControllersDiscrimination": true, "overall": {"data": [0.7143027129938124, 500, 1500, "Total"], "isController": false}, "titles": ["Apdex", "T (Toleration threshold)", "F (Frustration threshold)", "Label"], "items": [{"data": [0.0, 500, 1500, "POST /api/need-requests"], "isController": false}, {"data": [1.0, 500, 1500, "GET /api/profile/me"], "isController": false}, {"data": [0.0, 500, 1500, "PT-REQ-03: Donor Offer Submission (Port 5002)"], "isController": true}, {"data": [1.0, 500, 1500, "GET /api/donations"], "isController": false}, {"data": [0.0, 500, 1500, "POST /api/need-requests/1/offers"], "isController": false}, {"data": [1.0, 500, 1500, "POST /api/auth/register"], "isController": false}, {"data": [1.0, 500, 1500, "PT-USR-01: Concurrent User Login (Port 5000)"], "isController": true}, {"data": [0.0, 500, 1500, "PT-DON-03: Surplus Listing Creation (Port 5001)"], "isController": true}, {"data": [0.5, 500, 1500, "Auth - Login as Donor"], "isController": false}, {"data": [1.0, 500, 1500, "PT-DON-04: Donor Dashboard Listings (Port 5001)"], "isController": true}, {"data": [1.0, 500, 1500, "GET /api/donations/donors"], "isController": false}, {"data": [1.0, 500, 1500, "PT-USR-04: Profile Update (Port 5000)"], "isController": true}, {"data": [1.0, 500, 1500, "PT-DON-02: Filtered Marketplace Search (Port 5001)"], "isController": true}, {"data": [0.0, 500, 1500, "PT-REQ-02: Organization Need Creation (Port 5002)"], "isController": true}, {"data": [1.0, 500, 1500, "Auth - Login as Organization"], "isController": false}, {"data": [1.0, 500, 1500, "PT-USR-02: User Registration (Port 5000)"], "isController": true}, {"data": [1.0, 500, 1500, "PT-REQ-05: Claims Inbox Telemetry (Port 5002)"], "isController": true}, {"data": [1.0, 500, 1500, "PT-USR-03: Active Profile Query (Port 5000)"], "isController": true}, {"data": [1.0, 500, 1500, "GET /api/donations?category=Bakery&search=Bread"], "isController": false}, {"data": [1.0, 500, 1500, "GET /api/need-requests/active"], "isController": false}, {"data": [0.0, 500, 1500, "PT-REQ-04: Food Portion Claim (Port 5002)"], "isController": true}, {"data": [1.0, 500, 1500, "POST /api/auth/login"], "isController": false}, {"data": [1.0, 500, 1500, "PT-REQ-01: Active Needs Demands Board (Port 5002)"], "isController": true}, {"data": [1.0, 500, 1500, "GET /api/requests/received"], "isController": false}, {"data": [1.0, 500, 1500, "PT-DON-01: Public Catalog Discovery (Port 5001)"], "isController": true}, {"data": [1.0, 500, 1500, "GET /api/donations/my-donations"], "isController": false}, {"data": [0.0, 500, 1500, "POST /api/requests"], "isController": false}, {"data": [0.0, 500, 1500, "POST /api/donations"], "isController": false}, {"data": [1.0, 500, 1500, "PT-DON-05: Partner Directories Query (Port 5001)"], "isController": true}, {"data": [1.0, 500, 1500, "PUT /api/profile/me"], "isController": false}]}, function(index, item){
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
    createTable($("#statisticsTable"), {"supportsControllersDiscrimination": true, "overall": {"data": ["Total", 2102, 0, 0.0, 4688.037107516654, 2, 18101, 11.0, 17957.0, 18024.85, 18082.0, 5.933595668644532, 98.76702664701739, 4.683833073338621], "isController": false}, "titles": ["Label", "#Samples", "FAIL", "Error %", "Average", "Min", "Max", "Median", "90th pct", "95th pct", "99th pct", "Transactions/s", "Received", "Sent"], "items": [{"data": ["POST /api/need-requests", 150, 0, 0.0, 18028.12666666666, 17595, 18101, 18036.0, 18092.0, 18095.0, 18101.0, 0.498355427090601, 0.42163918963254593, 0.5857720304245988], "isController": false}, {"data": ["GET /api/profile/me", 150, 0, 0.0, 4.993333333333333, 2, 48, 4.0, 7.0, 7.0, 35.76000000000022, 0.5278957722587252, 0.36911462200903056, 0.45933118465090256], "isController": false}, {"data": ["PT-REQ-03: Donor Offer Submission (Port 5002)", 150, 0, 0.0, 17937.18000000001, 17905, 17987, 17938.0, 17959.0, 17970.25, 17982.41, 0.497801377250477, 0.3784586824856882, 0.5114131336596698], "isController": true}, {"data": ["GET /api/donations", 150, 0, 0.0, 6.459999999999997, 2, 246, 4.0, 6.0, 9.449999999999989, 165.93000000000143, 0.5279812461061384, 24.09162614043949, 0.0943560234740462], "isController": false}, {"data": ["POST /api/need-requests/1/offers", 150, 0, 0.0, 17937.18000000001, 17905, 17987, 17938.0, 17959.0, 17970.25, 17982.41, 0.497789813229262, 0.378449890818101, 0.5114012534347497], "isController": false}, {"data": ["POST /api/auth/register", 150, 0, 0.0, 133.73333333333335, 113, 313, 126.0, 159.70000000000002, 184.0, 267.1000000000008, 0.5273297428037068, 0.6282193206147259, 0.2682996054694641], "isController": false}, {"data": ["PT-USR-01: Concurrent User Login (Port 5000)", 150, 0, 0.0, 128.97999999999993, 113, 208, 123.0, 153.9, 182.79999999999995, 200.86000000000013, 0.5273074976095393, 0.6034787134751673, 0.14109595150880253], "isController": true}, {"data": ["PT-DON-03: Surplus Listing Creation (Port 5001)", 150, 0, 0.0, 11442.080000000005, 11070, 12058, 11422.0, 11607.9, 11774.4, 12022.810000000001, 0.5080423097635571, 0.46396567031102354, 0.5983388921629393], "isController": true}, {"data": ["Auth - Login as Donor", 1, 0, 0.0, 577.0, 577, 577, 577.0, 577.0, 577.0, 577.0, 1.7331022530329288, 1.9835896880415946, 0.4637402512998267], "isController": false}, {"data": ["PT-DON-04: Donor Dashboard Listings (Port 5001)", 150, 0, 0.0, 5.880000000000002, 2, 49, 4.5, 7.0, 8.0, 48.49000000000001, 0.5291901274289826, 29.355234059690883, 0.4666588721370814], "isController": true}, {"data": ["GET /api/donations/donors", 150, 0, 0.0, 7.240000000000002, 3, 55, 7.0, 9.0, 10.449999999999989, 49.3900000000001, 0.5291882604876293, 0.355755076679379, 0.4635565133373082], "isController": false}, {"data": ["PT-USR-04: Profile Update (Port 5000)", 150, 0, 0.0, 10.273333333333333, 3, 33, 10.0, 15.0, 17.0, 25.860000000000127, 0.527977529276354, 0.3908271163979261, 0.5980995448833697], "isController": true}, {"data": ["PT-DON-02: Filtered Marketplace Search (Port 5001)", 150, 0, 0.0, 9.826666666666672, 3, 66, 8.0, 12.0, 17.0, 63.450000000000045, 0.5284332306998569, 23.20775883650452, 0.10940219229332976], "isController": true}, {"data": ["PT-REQ-02: Organization Need Creation (Port 5002)", 150, 0, 0.0, 18028.126666666656, 17595, 18101, 18036.0, 18092.0, 18095.0, 18101.0, 0.49837364068589507, 0.4216545994487988, 0.5857934388694893], "isController": true}, {"data": ["Auth - Login as Organization", 1, 0, 0.0, 194.0, 194, 194, 194.0, 194.0, 194.0, 194.0, 5.154639175257732, 6.025491301546392, 1.369201030927835], "isController": false}, {"data": ["PT-USR-02: User Registration (Port 5000)", 150, 0, 0.0, 133.73333333333335, 113, 313, 126.0, 159.70000000000002, 184.0, 267.1000000000008, 0.5273464279310793, 0.6282391979676069, 0.2683080946797777], "isController": true}, {"data": ["PT-REQ-05: Claims Inbox Telemetry (Port 5002)", 150, 0, 0.0, 6.053333333333335, 2, 30, 5.0, 8.0, 17.549999999999784, 30.0, 0.5293059035251773, 21.429652488619926, 0.4641764661773528], "isController": true}, {"data": ["PT-USR-03: Active Profile Query (Port 5000)", 150, 0, 0.0, 4.993333333333333, 2, 48, 4.0, 7.0, 7.0, 35.76000000000022, 0.5279069194519622, 0.3691224163355517, 0.4593408840153304], "isController": true}, {"data": ["GET /api/donations?category=Bakery&search=Bread", 150, 0, 0.0, 9.826666666666672, 3, 66, 8.0, 12.0, 17.0, 63.450000000000045, 0.528412753770225, 23.20685952961577, 0.10939795292899189], "isController": false}, {"data": ["GET /api/need-requests/active", 150, 0, 0.0, 21.993333333333332, 3, 474, 8.0, 16.700000000000017, 64.39999999999986, 473.49, 0.5292181331301137, 21.175361224451994, 0.46564993940452376], "isController": false}, {"data": ["PT-REQ-04: Food Portion Claim (Port 5002)", 150, 0, 0.0, 17947.066666666677, 17918, 17982, 17948.5, 17972.0, 17975.45, 17982.0, 0.4977699904428162, 0.3764774435528831, 0.49679778343023256], "isController": true}, {"data": ["POST /api/auth/login", 150, 0, 0.0, 128.97999999999993, 113, 208, 123.0, 153.9, 182.79999999999995, 200.86000000000013, 0.5272926685227369, 0.6034617422804354, 0.14109198356956046], "isController": false}, {"data": ["PT-REQ-01: Active Needs Demands Board (Port 5002)", 150, 0, 0.0, 21.993333333333332, 3, 474, 8.0, 16.700000000000017, 64.39999999999986, 473.49, 0.5292218674475629, 21.17551064397481, 0.46565322516626384], "isController": true}, {"data": ["GET /api/requests/received", 150, 0, 0.0, 6.053333333333335, 2, 30, 5.0, 8.0, 17.549999999999784, 30.0, 0.5293133746903517, 21.429954968664646, 0.46418301803899975], "isController": false}, {"data": ["PT-DON-01: Public Catalog Discovery (Port 5001)", 150, 0, 0.0, 6.459999999999997, 2, 246, 4.0, 6.0, 9.449999999999989, 165.93000000000143, 0.5280054067753653, 24.092728584540705, 0.0943603412498944], "isController": true}, {"data": ["GET /api/donations/my-donations", 150, 0, 0.0, 5.880000000000003, 2, 49, 4.5, 7.0, 8.0, 48.49000000000001, 0.5291882604876293, 29.355130496722563, 0.46665722580110286], "isController": false}, {"data": ["POST /api/requests", 150, 0, 0.0, 17947.066666666677, 17918, 17982, 17948.5, 17972.0, 17975.45, 17982.0, 0.49778650932188206, 0.37648993724571406, 0.4968142700458627], "isController": false}, {"data": ["POST /api/donations", 150, 0, 0.0, 11442.080000000004, 11070, 12058, 11422.0, 11607.9, 11774.4, 12022.810000000001, 0.5080251032137668, 0.4639499565638536, 0.5983186274177761], "isController": false}, {"data": ["PT-DON-05: Partner Directories Query (Port 5001)", 150, 0, 0.0, 7.240000000000002, 3, 55, 7.0, 9.0, 10.449999999999989, 49.3900000000001, 0.5292069304939617, 0.35576762788285476, 0.46357286782527707], "isController": true}, {"data": ["PUT /api/profile/me", 150, 0, 0.0, 10.273333333333332, 3, 33, 10.0, 15.0, 17.0, 25.860000000000127, 0.5279515129330522, 0.3908078582063023, 0.5980700732444731], "isController": false}]}, function(index, item){
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
    createTable($("#top5ErrorsBySamplerTable"), {"supportsControllersDiscrimination": false, "overall": {"data": ["Total", 2102, 0, "", "", "", "", "", "", "", "", "", ""], "isController": false}, "titles": ["Sample", "#Samples", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors"], "items": [{"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}, {"data": [], "isController": false}]}, function(index, item){
        return item;
    }, [[0, 0]], 0);

});
