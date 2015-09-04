(function () {
    'use strict';

    var serviceId = 'googleServices';

    angular.module('app').factory(serviceId, ['$resource', googleServices]);

    function googleServices($resource) {
        var timeZone = $resource("https://maps.googleapis.com/maps/api/timezone/json?location=:location&timestamp=:timestamp&key=AIzaSyCclZT9g1Xv6V_9E1yWRBBXB_1Ui_zd8Cg",
        { location: '@location', timestamp: '@timestamp' });

        var service = {
            timeZone: timeZone
        };

        return service;
      
    }
})();