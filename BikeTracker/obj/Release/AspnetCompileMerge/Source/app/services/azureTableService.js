(function () {
    'use strict';

    var serviceId = 'azureTableService';

    // TODO: replace app with your module name
    angular.module('app').factory(serviceId, ['$resource', azureTableService]);

    function azureTableService($resource) {
        // Define the functions and properties to reveal.
        var service = {
            getAllData: getAllData,
            getSegmentData: getSegmentData
        };

        return service;

        function getAllData(url) {
            return $resource(
                url + "/:startAndEnd",
                { startAndEnd: '@startAndEnd' },
            {
                get: {
                    method: "GET",
                    isArray: false,
                    headers: {
                        'x-ms-version': '2013-08-15',
                        'MaxDataServiceVersion': '3.0',
                        'Accept': 'application/json;odata=nometadata'
                    }
                }
            });
        }

        function getSegmentData(url) {
            return $resource(
               // url + "&$filter=(PartitionKey%20eq%20'':startPartitionKey''%20and%20RowKey%20eq%20'':startRowKey'')%20or%20(PartitionKey%20gt%20'':startPartitionKey''%20and%20%PartitionKey%20lt%20'':endPartitionKey'')%20or%20(PartitionKey%20eq%20'':endPartitionKey''%20and%20RowKey%20lt%20'':endRowKey'')",
 url + "&$filter=(PartitionKey%20eq%20':startPartitionKey'%20and%20RowKey%20ge%20':startRowKey')%20or%20(PartitionKey%20gt%20':startPartitionKey'%20and%20PartitionKey%20lt%20':endPartitionKey')%20or%20(PartitionKey%20eq%20':endPartitionKey'%20and%20RowKey%20lt%20':endRowKey')",

            {
                startPartitionKey: '@startPartitionKey',
                startRowKey: '@startRowkey',
                //endPartitionKey: '@endPartitionKey',
                //endRowKey: '@endRowKey'
            },
            {
                get: {
                    method: "GET",
                    isArray: false,
                    headers: {
                        //'x-ms-version': '2014-02-14',
                        //'MaxDataServiceVersion': '3.0',
                        'Accept': 'application/json;odata=nometadata'
                    }
                }
            });
        }

    }
})();
