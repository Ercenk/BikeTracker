(function () {
    'use strict';
    var controllerId = 'segmentCtrl';
    angular.module('app').controller(controllerId, ['$scope', '$routeParams', 'common', 'bikeTrackerServer', 'azureTableService', segmentCtrl]);

    function segmentCtrl($scope, $routeParams, common, bikeTrackerServer, azureTableService) {
        var $q = common.$q;
        var getLogFn = common.logger.getLogFn;
        var log = getLogFn(controllerId);

        var vm = this;

        var segmentStartTime = $routeParams.startTime.split(',');
        var segmentEndTime = $routeParams.endTime.split(',');
        vm.segmentStartLoc = decodeURIComponent($routeParams.startLoc);
        vm.segmentEndLoc = decodeURIComponent($routeParams.endLoc);
        vm.segmentLocalTime = decodeURIComponent($routeParams.segmentTime);
                
        vm.opened = {
            dtStart: false,
            dtEnd: false
        };

        vm.title = 'Dashboard';


        vm.openDatePopup = function($event, field) {
            $event.preventDefault();
            $event.stopPropagation();

            vm.opened[field] = true;
        };


        vm.datePickerOptions = {
            formatYear: 'yy',
            startingDay: 1
        };

        vm.loading = false;
        //vm.mapOptions = {
        //    center: new google.maps.LatLng(47.601539, -122.335156),
        //    zoom: 6
        //};
        var localMapInstance = null;
        var dataReceived = false;

        vm.map = {
            travelledPoints: [],
            center: {
                latitude: 47.601539,
                longitude: -122.335156
            },
            options: {
                streetViewControl: true
            },
            zoom: 8,
            points: [{id: 1, path: []}], //[{ latitude: 47.601539, longitude: -122.335156 }, { latitude: 47.602539, longitude: -122.332156 }],
            lineStyle: {
                color: '#333',
                weight: 5,
                opacity: 0.7
            },

            events: {
                tilesloaded: function (map, eventName, originalEventArgs) {
                    if (dataReceived) {
                        $scope.$apply(function() {
                            vm.loading = false;
                        });
                        dataReceived = false;
                    }
                }
            }
        };

        vm.averageSpeedChart = {
            type: "LineChart",
            data: {
                cols: [
                    {
                        id: "time",
                        label: "Time",
                        type: "string"
                    },
                    {
                        id: "avgspeed",
                        label: "Average speed",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Average speed",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Speed - mph", gridLines: 10 },
                hAxis: { title: "Time" }
            }
        };

        vm.altitudeChart = {
            type: "LineChart",
            data: {
                cols: [
                    {
                        id: "time",
                        label: "Time",
                        type: "string"
                    },
                    {
                        id: "altitude",
                        label: "Altitude (ft)",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Altitude",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Altitude (ft)", gridLines: 10 },
                hAxis: { title: "Time" }
            }
        };

        vm.rollChart = {
            type: "LineChart",
            data: {
                cols: [
                    {
                        id: "time",
                        label: "Time",
                        type: "string"
                    },
                    {
                        id: "roll",
                        label: "Roll (deg)",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Roll",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Roll (deg)", gridLines: 10 },
                hAxis: { title: "Time" }
            }
        };

        vm.temperatureChart = {
            type: "LineChart",
            data: {
                cols: [
                    {
                        id: "time",
                        label: "Time",
                        type: "string"
                    },
                    {
                        id: "temperature",
                        label: "Temperature (Celcius)",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Temperature",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Temperature (Celcius)", gridLines: 10 },
                hAxis: { title: "Time" }
            }
        };

        vm.pressureChart = {
            type: "LineChart",
            data: {
                cols: [
                    {
                        id: "time",
                        label: "Time",
                        type: "string"
                    },
                    {
                        id: "pressure",
                        label: "Barometric Pressure",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Barometric Pressure",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Barometric Pressure", gridLines: 10 },
                hAxis: { title: "Time" }
            }
        };

        vm.xAccelerationChart = {
            type: "LineChart",
            data: {
                cols: [
                    {
                        id: "time",
                        label: "Time",
                        type: "string"
                    },
                    {
                        id: "xAccel",
                        label: "Forward Acceleration",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Forward Acceleration",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Forward Acceleration", gridLines: 10 },
                hAxis: { title: "Time" }
            }
        };

        vm.yAccelerationChart = {
            type: "LineChart",
            data: {
                cols: [
                    {
                        id: "time",
                        label: "Time",
                        type: "string"
                    },
                    {
                        id: "yAccel",
                        label: "Sideways Acceleration",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Sideways Acceleration",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Sideways Acceleration", gridLines: 10 },
                hAxis: { title: "Time" }
            }
        };

        vm.zAccelerationChart = {
            type: "LineChart",
            data: {
                cols: [
                    {
                        id: "time",
                        label: "Time",
                        type: "string"
                    },
                    {
                        id: "zAccel",
                        label: "Vertical Acceleration",
                        type: "number"
                    }
                ],
                rows: []
            },
            options: {
                title: "Vertical Acceleration",
                isStacked: false,
                fill: 20,
                displayExactValues: true,
                vAxis: { title: "Vertical Acceleration", gridLines: 10 },
                hAxis: { title: "Time" }
            }
        };

        var chartTabInitialized = false;
        var lastMarkerId = 0;

        var colors = [
            "#999900",
            "#99CC00",
            "#99FF00",
            "#999933",
            "#99CC33",
            "#99FF33",
            "#999966",
            "#99CC66",
            "#99FF66",
            "#999999",
            "#99CC99",
            "#99FF99",
            "#9999CC",
            "#99CCCC",
            "#99FFCC",
            "#9999FF",
            "#99CCFF",
            "#99FFFF",
            "#3300CC", 
            "#3300FF"
        ];

        vm.speedRange = [];
        for (var i = 0; i < 20; i++) {
            vm.speedRange.push({
                range: (5 * i) + " - " + ((5 * (i + 1)) - 1),
                icon: "/content/images/point" + (i + 1) + ".png"
            });
        }

        vm.chartTabSelect = chartTabSelect;

        function chartTabSelect() {
            if (chartTabInitialized) {
                return;
            }

            vm.loading = true;
            vm.numberOfPoints = 0;

            getData(function(point) {

                var pointDate = moment(point.PointTimeStamp);

                var time = pointDate.tz(point.TimeZone).format('h:mma');
                vm.averageSpeedChart.data.rows.push({ c: [{ v: time }, { v: point.AverageSpeed }] });
                vm.altitudeChart.data.rows.push({ c: [{ v: time }, { v: point.GpsAltitude }] });
                vm.rollChart.data.rows.push({ c: [{ v: time }, { v: point.AverageRoll  }] });
                vm.temperatureChart.data.rows.push({ c: [{ v: time }, { v: point.AverageTemperature}] });
                vm.pressureChart.data.rows.push({ c: [{ v: time }, { v: point.AverageBmp }] });
                vm.xAccelerationChart.data.rows.push({ c: [{ v: time }, { v: point.AverageAccelerationX + 2.32}] });
                vm.yAccelerationChart.data.rows.push({ c: [{ v: time }, { v: point.AverageAccelerationY - 0.34 }] });
                vm.zAccelerationChart.data.rows.push({ c: [{ v: time }, { v: point.AverageAccelerationZ - 10 }] });

            }, function(data) {
                vm.loading = false;
            });

        }        

        function initializeData() {
            var deferred = $q.defer();

            vm.numberOfPoints = 0;
            vm.loading = true;

            getData(function(point) {

                //vm.map.points[0].path.push({
                //    "latitude": point.Latitude,
                //    "longitude": point.Longitude,
                //    "stroke": { color: colors[Math.floor(point["AverageSpeed"] / 5)], weight: 2 }
                //});

                vm.map.travelledPoints.push({
                    "id": lastMarkerId++,
                    "icon": "/content/images/point" + (Math.floor(point.AverageSpeed / 5) + 1) + ".png",
                    "latitude": point.Latitude,
                    "longitude": point.Longitude,
                });
            }, function(data) {
                var lastPoint = _.last(data.value);
                vm.map.center.latitude = lastPoint.Latitude;
                vm.map.center.longitude = lastPoint.Longitude;
                vm.map.zoom = 8;
                
            }, deferred);

            return deferred.propromise;
        }

        function getData(dataUpdater, finished, deferred) {
            bikeTrackerServer.dataPointsUri.get().$promise.then(function (url) {
                var getParameters = {
                    startPartitionKey: segmentStartTime[0],
                    startRowKey: segmentStartTime[1],
                    endPartitionKey: segmentEndTime[0],
                    endRowKey: segmentEndTime[1]
                };

                var result = updateData(url, getParameters, dataUpdater, finished);

                if (!deferred) {
                    return;
                }

                if (result) {
                    deferred.resolve();
                } else {
                    deferred.reject();
                }
            }, function () {
                if (!deferred) {
                    return;
                }

                deferred.reject();
            });
        }

        vm.numberOfPoints = 0;

        function updateData(url, parameters, dataUpdater, finished) {
            azureTableService.getSegmentData(url.value).get(parameters, function (data, responseHeaders) {
                vm.numberOfPoints += data.value.length;
                if (data !== null && Object.prototype.toString.call(data.value) === '[object Array]' && data.value.length > 2) {
                    //vm.map.points[0] = data.value[0];
                    //vm.map.points[1] = data.value[1];
                    //var restOfData = _.last(data.value, data.value.length - 2);
                    //_(restOfData).forEach(function (point) {
                    _(data.value).forEach(dataUpdater);
              
                    dataReceived = true;

                    var headers = responseHeaders();

                    if (typeof headers["x-ms-continuation-nextrowkey"] !== "undefined" && 
                        (typeof headers["x-ms-continuation-nextpartitionkey"] !== "undefined" ||
                        headers["x-ms-continuation-nextpartitionkey"] !== "")) {
                        parameters.NextRowKey = headers["x-ms-continuation-nextrowkey"];
                        parameters.NextPartitionKey = headers["x-ms-continuation-nextpartitionkey"];
                        updateData(url, parameters, dataUpdater, finished);
                    } else {
                        if (finished) {
                            finished(data);
                        }
                    }
                }

                return true;
            });
        }


        activate();

        function activate() {
            //var promises = [getMessageCount(), getPeople()];
            var promises = [initializeData()];
            common.activateController(promises, controllerId)
                .then(function () { log('Activated Dashboard View'); });
        }


    }
})();