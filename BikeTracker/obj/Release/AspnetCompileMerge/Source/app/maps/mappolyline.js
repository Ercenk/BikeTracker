(function() {
    'use strict';

    // TODO: replace app with your module name
    angular.module('ui.map').directive('polylineCustom', ['$log', '$timeout', mappolyline]);
    
    function mappolyline ($log, $timeout) {
        // Usage:
        // 
        // Creates:
        // 
        var directive = {
            link: link,
            restrict: 'ECA',
            replace: true,
            require: '^googleMap',
            scope: {
                polylines: '='
            },
        };
        return directive;

        function link(scope, element, attrs, mapCtrl) {

                // Wrap polyline initialization inside a $timeout() call to make sure the map is created already
                $timeout(function() {


                    var map = mapCtrl.getMap();
                    var bounds = new google.maps.LatLngBounds();
                    var pathPoints = [];

                    for (var p = 0; p < scope.polylines.length; p++) {



                        var path = scope.polylines[p].path;

                        if (angular.isUndefined(path) ||
                            path === null ||
                            path.length < 2 || !validatePathPoints(path)) {

                            $log.error("polyline: no valid path attribute found");
                            return;
                        }

                        for (var i = 0; i < path.length; i++) {

                            var point = new google.maps.LatLng(path[i].latitude, path[i].longitude);
                            pathPoints.push(point);
                            bounds.extend(point);
                        }
                    }

                    map.fitBounds(bounds);

                    var polyline = new google.maps.Polyline({
                        map: map,
                        path: pathPoints,
                        strokeColor: '#FF0000',
                        strokeOpacity: 1.0,
                        strokeWeight: 5
                    });

                    // Remove polyline on scope $destroy
                    scope.$on("$destroy", function() {
                        polyline.setMap(null);

                    });
                });
        }

        function validatePathPoints(path) {
            for (var i = 0; i < path.length; i++) {
                if (angular.isUndefined(path[i].latitude) ||
                    angular.isUndefined(path[i].longitude)) {
                    return false;
                }
            }

            return true;
        }

        /*
         * Utility functions
         */

        /**
         * Check if a value is true
         */
        function isTrue(val) {
            return angular.isDefined(val) &&
                val !== null &&
                val === true ||
                val === '1' ||
                val === 'y' ||
                val === 'true';
        }
    }

})();