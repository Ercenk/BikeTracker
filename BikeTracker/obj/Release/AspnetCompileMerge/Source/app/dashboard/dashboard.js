(function () {

    'use strict';

    var controllerId = 'dashboard';

    // TODO: replace app with your module name
    angular.module('app').controller(controllerId,
        ['$scope', 'common', 'bikeTrackerServer', 'azureTableService', 'googleServices', dashboard]);

    function dashboard($scope, common, bikeTrackerServer, azureTableService, googleServices) {
        var $q = common.$q;
        var getLogFn = common.logger.getLogFn;
        var log = getLogFn(controllerId);

        var vm = this;

        vm.activate = activate;
        vm.title = 'dashboard';
        vm.segments = [];

        vm.gridOptions = {
            data: 'vm.segments',
            columnDefs: [
                { field: 'Id', displayName: "Id", width: "5%" },
                { field: 'Day', displayName: "Day", width: "10%" },
                { field: 'StartTime', displayName: "Start time", width: "8%" },
                { field: 'StartLocation', displayName: "Location", width: "*", cellTemplate: '<a href="{{row.getProperty(\'StartLocationLink\')}}"= target="_blank">{{row.getProperty(\'StartLocation\')}}</a>' },
                { field: 'EndTime', displayName: "End time", width: "8%" },
                { field: 'EndLocation', displayName: "Location", width: "*", cellTemplate: '<a href="{{row.getProperty(\'EndLocationLink\')}}" target="_blank">{{row.getProperty(\'EndLocation\')}}</a>' },
                { field: 'Timezone', displayName: "Timezone", width: "15%" },
                { field: 'Duration', displayName: "Duration (hrs)", width: "8%" },
                { field: 'Details', displayName: "Details", width: "8%", cellTemplate: '<a href="{{row.getProperty(\'SegmentLink\')}}">Details</a>' }
            ],
            selectedItems: vm.selected,
            enableColumnResize: true,
            multiSelect: false,
            enableSorting: false
        };

        vm.averageSpeedChart = {
            type: "ColumnChart",
            data: {
                cols: [
                    {
                        id: "time",
                        label: "Segment time",
                        type: "string"
                    },
                    {
                        id: "avgspeed",
                        label: "Average speed",
                        type: "number"
                    },
                    {
                        id: "maxspeed",
                        label: "Max speed",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Average speed for segments",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Speed - mph", gridLines: 10 },
                hAxis: { title: "Segments" }
            }
        };

        vm.averageAltitudeChart = {
            type: "ColumnChart",
            data: {
                cols: [
                    {
                        id: "location",
                        label: "Location",
                        type: "string"
                    },
                    {
                        id: "avgaltitude",
                        label: "Average altitude (ft)",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Average altitude for segments",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Altitude (ft)", gridLines: 10 },
                hAxis: { title: "Segments" }
            }
        };

        vm.averageLatitudeChart = {
            type: "ColumnChart",
            data: {
                cols: [
                    {
                        id: "location",
                        label: "Location",
                        type: "string"
                    },
                    {
                        id: "avgaltitude",
                        label: "Average Latitude",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Average latitude for segments",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Latitude", gridLines: 10 },
                hAxis: { title: "Segments" }
            }
        };


        function getSegments() {
            var deferred = $q.defer();

            vm.loading = true;
            var id = 0;
            bikeTrackerServer.segmentsUri.get().$promise.then(function (url) {
                azureTableService.getAllData(url.value).get({}, function (data) {

                    if (data !== null && Object.prototype.toString.call(data.value) === '[object Array]') {

                        var sorted = data.value.sort(function (a, b) {
                            var dateA = new Date(a);
                            var dateB = new Date(b);
                            if (dateA > dateB) {
                                return 1;
                            }
                            if (dateA === dateB) {
                                return 0;
                            }
                            return -1;
                        });

                        _(sorted).forEach(function (segment) {
                            var startDate = moment(segment.StartTimeStamp);
                            var endDate = moment(segment.EndTimeStamp);
                            var startMoment = moment(segment.StartTimeStamp);
                            var endMoment = moment(segment.EndTimeStamp);
                            var day = startDate.tz(segment.StartTimeZone).format('MM/DD/YY');
                            var chartTime = day + " " + startDate.tz(segment.StartTimeZone).format('h:mma z');
                            vm.segments.push({
                                Id: id++,
                                StartTimeStamp: segment.StartTimeStamp,
                                Day: day,
                                StartTime: startDate.tz(segment.StartTimeZone).format('h:mma'),
                                EndTime: endDate.tz(segment.StartTimeZone).format('h:mma'),
                                Timezone: startDate.tz(segment.StartTimeZone).format('UTC Z (z)'),
                                StartLatitude: segment.StartLatitude,
                                StartLongitude: segment.StartLongitude,
                                StartLocation: segment.StartLocation,
                                EndLatitude: segment.EndLatitude,
                                EndLongitude: segment.EndLongitude,
                                EndLocation: segment.EndLocation,
                                StartLocationLink: "https://www.google.com/maps/@" + segment.StartLatitude + "," + segment.StartLongitude + ",12z",
                                EndLocationLink: "https://www.google.com/maps/@" + segment.EndLatitude + "," + segment.EndLongitude + ",12z",
                                Duration: Math.round(endMoment.diff(startMoment, 'hours', true) * 100) / 100,
                                //SegmentLink: "#/segment/start/" + segment.StartPartitionAndRow + "/end/" + segment.EndPartitionAndRow +
                                //    "/startLoc/" + encodeURIComponent(segment.StartLocation) + "/endLoc/" + encodeURIComponent(segment.EndLocation) +
                                //    "/segmentTime/" + encodeURIComponent(chartTime)
                                SegmentLink: "#/segment/start/" + segment.StartPartitionAndRow + "/end/" + segment.EndPartitionAndRow 
                        });
                            var segmentLoc = segment.StartLocation + "-" + segment.EndLocation;
                            
                            vm.averageSpeedChart.data.rows.push({ c: [{ v: segmentLoc }, { v: segment.AverageSpeed }, { v: segment.MaxSpeed }] })
                            vm.averageAltitudeChart.data.rows.push({ c: [{ v: segmentLoc }, { v: segment.AverageAltitude }] })
                            vm.averageLatitudeChart.data.rows.push({ c: [{ v: segmentLoc }, { v: (Math.round(((segment.StartLatitude + segment.EndLatitude) / 2) * 100) / 100) }] })
                        });

                        common.$toggleSpinner(false);
                    }

                    deferred.resolve();
                });
            }, function () {
                deferred.reject();
            })

            return deferred.propromise;
        }

        common.$toggleSpinner(true);
        activate();

        function activate() {
            var promises = [getSegments()];
            common.activateController(promises, controllerId)
                .then(function () { log('Activated Dashboard View'); });
        }
    }
})();