(function () {
    'use strict';

    var serviceId = 'bikeTrackerServer';

    angular.module('app').factory(serviceId, ['$resource', bikeTrackerServer]);

    function bikeTrackerServer($resource) {
        var authorize = $resource(
            '/api/Data/Authorize/:token',
            { token: '@token' });

        var postData = $resource(
            '/api/data/postData',
            { token: '@token' });

        return {
            authorize: authorize,
            postData: postData
        };

    }
})();